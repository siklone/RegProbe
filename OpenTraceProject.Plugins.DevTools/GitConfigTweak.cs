using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;

namespace OpenTraceProject.Plugins.DevTools;

/// <summary>
/// Optimizes Git configuration for better performance and developer experience.
/// Sources:
/// - Git Official Documentation: https://git-scm.com/docs/git-config
/// - Git Performance Tips: https://git-scm.com/docs/git-config#_performance
/// - Atlassian Git Tutorial: https://www.atlassian.com/git/tutorials/setting-up-a-repository/git-config
/// </summary>
public sealed class GitConfigTweak : ITweak
{
    public string Id => "plugin.devtools.git-config";
    public string Name => "Optimize Git Configuration";
    public string Description => "Configures Git for optimal performance: parallel indexing, compression, and large file handling. Improves commit and clone speeds. Based on Git official documentation and best practices.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => false;
    public bool SupportsRollback => true;

    private readonly Dictionary<string, string> _optimizedSettings = new()
    {
        ["core.preloadindex"] = "true",
        ["core.fscache"] = "true",
        ["gc.auto"] = "256",
        ["http.postbuffer"] = "524288000",
        ["pack.threads"] = "0",
        ["pack.deltaCacheSize"] = "2047m",
        ["pack.packSizeLimit"] = "2g",
        ["pack.windowMemory"] = "2g",
        ["core.compression"] = "0",
        ["core.loosecompression"] = "0",
        ["protocol.version"] = "2"
    };

    private Dictionary<string, string>? _originalSettings;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        try
        {
            var allConfigured = true;
            var message = new System.Text.StringBuilder();

            foreach (var setting in _optimizedSettings)
            {
                var currentValue = GetGitConfig(setting.Key);
                if (currentValue != setting.Value)
                {
                    allConfigured = false;
                    message.AppendLine($"{setting.Key}: current='{currentValue}', target='{setting.Value}'");
                }
            }

            var status = allConfigured ? TweakStatus.Applied : TweakStatus.Detected;
            var resultMessage = allConfigured 
                ? "Git is already optimized for development"
                : $"Git needs optimization:\n{message}";

            return Task.FromResult(new TweakResult(status, resultMessage, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to detect Git configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        try
        {
            _originalSettings = new Dictionary<string, string>();
            var results = new System.Text.StringBuilder();

            foreach (var setting in _optimizedSettings)
            {
                var originalValue = GetGitConfig(setting.Key);
                _originalSettings[setting.Key] = originalValue ?? "";
                
                SetGitConfig(setting.Key, setting.Value);
                results.AppendLine($"Set {setting.Key}={setting.Value}");
            }

            return Task.FromResult(new TweakResult(
                TweakStatus.Applied,
                $"Git configuration optimized successfully:\n{results}",
                DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to optimize Git configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        try
        {
            var allCorrect = true;
            var issues = new System.Text.StringBuilder();

            foreach (var setting in _optimizedSettings)
            {
                var currentValue = GetGitConfig(setting.Key);
                if (currentValue != setting.Value)
                {
                    allCorrect = false;
                    issues.AppendLine($"{setting.Key}: expected '{setting.Value}', got '{currentValue}'");
                }
            }

            var status = allCorrect ? TweakStatus.Verified : TweakStatus.Failed;
            var message = allCorrect 
                ? "Git configuration verified successfully"
                : $"Git configuration issues found:\n{issues}";

            return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to verify Git configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        try
        {
            if (_originalSettings == null)
            {
                return Task.FromResult(new TweakResult(
                    TweakStatus.RolledBack,
                    "No original settings stored, nothing to rollback",
                    DateTimeOffset.UtcNow));
            }

            var results = new System.Text.StringBuilder();

            foreach (var setting in _originalSettings)
            {
                if (string.IsNullOrEmpty(setting.Value))
                {
                    UnsetGitConfig(setting.Key);
                    results.AppendLine($"Unset {setting.Key}");
                }
                else
                {
                    SetGitConfig(setting.Key, setting.Value);
                    results.AppendLine($"Restored {setting.Key}={setting.Value}");
                }
            }

            return Task.FromResult(new TweakResult(
                TweakStatus.RolledBack,
                $"Git configuration rolled back:\n{results}",
                DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to rollback Git configuration: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    private string? GetGitConfig(string key)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global {key}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return process.ExitCode == 0 ? output : null;
    }

    private void SetGitConfig(string key, string value)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global {key} \"{value}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to set {key}: {process.StandardError.ReadToEnd()}");
        }
    }

    private void UnsetGitConfig(string key)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"config --global --unset {key}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
    }
}
