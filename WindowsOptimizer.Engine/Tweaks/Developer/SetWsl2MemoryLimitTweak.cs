using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Developer;

public sealed class SetWsl2MemoryLimitTweak : ITweak
{
    private const string TargetMemory = "4GB";

    private readonly string? _settingsPathOverride;
    private string? _settingsPath;
    private bool _hasDetected;
    private bool _settingsFileExisted;
    private string? _originalText;

    public SetWsl2MemoryLimitTweak(string? settingsPath = null)
    {
        _settingsPathOverride = settingsPath;
        Id = "developer.wsl2-memory";
        Name = "Set WSL 2 Memory Limit";
        Description = "Limits WSL 2 memory to 4GB by writing the documented [wsl2] memory setting in .wslconfig.";
        Risk = TweakRiskLevel.Advanced;
        RequiresElevation = false;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var state = await LoadStateAsync(captureSnapshot: true, ct);
            var currentMemory = TryGetWsl2Memory(state.Text);

            var message = currentMemory is null
                ? "WSL 2 memory is not explicitly configured."
                : $"WSL 2 memory is configured as {currentMemory}.";

            return new TweakResult(
                string.Equals(currentMemory, TargetMemory, StringComparison.OrdinalIgnoreCase)
                    ? TweakStatus.Applied
                    : TweakStatus.Detected,
                message,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Detect error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected || string.IsNullOrWhiteSpace(_settingsPath))
        {
            return new TweakResult(
                TweakStatus.Failed,
                "Must call DetectAsync first.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var state = await LoadStateAsync(captureSnapshot: false, ct);
            var updatedText = SetOrUpdateWsl2Memory(state.Text, TargetMemory);
            await PersistSettingsAsync(_settingsPath, updatedText, ct);

            return new TweakResult(
                TweakStatus.Applied,
                $"Set WSL 2 memory limit to {TargetMemory}.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Apply error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var state = await LoadStateAsync(captureSnapshot: false, ct);
            var currentMemory = TryGetWsl2Memory(state.Text);
            if (string.Equals(currentMemory, TargetMemory, StringComparison.OrdinalIgnoreCase))
            {
                return new TweakResult(
                    TweakStatus.Verified,
                    $"Verified WSL 2 memory limit {TargetMemory}.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Failed,
                "Verification failed. WSL 2 memory limit does not match expected state.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Verify error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected || string.IsNullOrWhiteSpace(_settingsPath))
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            if (!_settingsFileExisted)
            {
                if (File.Exists(_settingsPath))
                {
                    File.Delete(_settingsPath);
                }

                return new TweakResult(
                    TweakStatus.RolledBack,
                    "Removed .wslconfig created by the tweak.",
                    DateTimeOffset.UtcNow);
            }

            await File.WriteAllTextAsync(_settingsPath, _originalText ?? string.Empty, ct);
            return new TweakResult(
                TweakStatus.RolledBack,
                "Restored original .wslconfig.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Rollback error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    private async Task<WslConfigState> LoadStateAsync(bool captureSnapshot, CancellationToken ct)
    {
        var path = ResolveSettingsPath();
        _settingsPath = path;

        var fileExists = File.Exists(path);
        var text = fileExists ? await File.ReadAllTextAsync(path, ct) : null;

        if (captureSnapshot)
        {
            _settingsFileExisted = fileExists;
            _originalText = text;
            _hasDetected = true;
        }

        return new WslConfigState(fileExists, text);
    }

    private string ResolveSettingsPath()
    {
        if (!string.IsNullOrWhiteSpace(_settingsPathOverride))
        {
            return _settingsPathOverride;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig");
    }

    private static string SetOrUpdateWsl2Memory(string? text, string memoryValue)
    {
        var lines = SplitLines(text ?? string.Empty).ToList();
        if (lines.Count == 0)
        {
            return $"[wsl2]{Environment.NewLine}memory={memoryValue}{Environment.NewLine}";
        }

        var output = new List<string>(lines.Count + 4);
        var inWsl2Section = false;
        var sectionFound = false;
        var memoryWritten = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (IsSectionHeader(trimmed))
            {
                if (inWsl2Section && !memoryWritten)
                {
                    output.Add($"memory={memoryValue}");
                    memoryWritten = true;
                }

                inWsl2Section = trimmed.Equals("[wsl2]", StringComparison.OrdinalIgnoreCase);
                sectionFound |= inWsl2Section;
                output.Add(line);
                continue;
            }

            if (inWsl2Section && StartsWithSetting(trimmed, "memory"))
            {
                output.Add($"memory={memoryValue}");
                memoryWritten = true;
                continue;
            }

            output.Add(line);
        }

        if (inWsl2Section && !memoryWritten)
        {
            output.Add($"memory={memoryValue}");
            memoryWritten = true;
        }

        if (!sectionFound)
        {
            if (output.Count > 0 && !string.IsNullOrWhiteSpace(output[^1]))
            {
                output.Add(string.Empty);
            }

            output.Add("[wsl2]");
            output.Add($"memory={memoryValue}");
        }

        return string.Join(Environment.NewLine, output) + Environment.NewLine;
    }

    private static string? TryGetWsl2Memory(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var inWsl2Section = false;
        foreach (var line in SplitLines(text))
        {
            var trimmed = line.Trim();
            if (IsSectionHeader(trimmed))
            {
                inWsl2Section = trimmed.Equals("[wsl2]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inWsl2Section)
            {
                continue;
            }

            if (StartsWithSetting(trimmed, "memory"))
            {
                var separatorIndex = trimmed.IndexOf('=');
                return separatorIndex >= 0 ? trimmed[(separatorIndex + 1)..].Trim() : null;
            }
        }

        return null;
    }

    private static bool IsSectionHeader(string line)
    {
        return line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal);
    }

    private static bool StartsWithSetting(string line, string settingName)
    {
        return line.StartsWith(settingName, StringComparison.OrdinalIgnoreCase)
               && line.Length > settingName.Length
               && line[settingName.Length] == '=';
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }

    private static async Task PersistSettingsAsync(string settingsPath, string text, CancellationToken ct)
    {
        var settingsDirectory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        await File.WriteAllTextAsync(settingsPath, text, ct);
    }

    private sealed record WslConfigState(bool FileExisted, string? Text);
}
