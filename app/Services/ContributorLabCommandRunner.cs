using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.Application.Services;

public sealed record ContributorCommandRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    public bool IsSuccess => !TimedOut && ExitCode == 0;
}

public interface IContributorLabCommandRunner
{
    Task<ContributorCommandRunResult> RunAsync(string repoRoot, string command, CancellationToken cancellationToken = default);
}

public sealed class ContributorLabCommandRunner : IContributorLabCommandRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45);

    public async Task<ContributorCommandRunResult> RunAsync(
        string repoRoot,
        string command,
        CancellationToken cancellationToken = default)
    {
        if (!ContributorLabCatalog.IsAllowlistedCommand(command))
        {
            return new ContributorCommandRunResult(
                ExitCode: -1,
                StandardOutput: string.Empty,
                StandardError: "Blocked: command is not allowlisted for Contributor Lab.",
                TimedOut: false);
        }

        var tokens = SplitCommandLine(command);
        if (tokens.Count == 0)
        {
            return new ContributorCommandRunResult(-1, string.Empty, "No command was provided.", TimedOut: false);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = tokens[0],
            WorkingDirectory = string.IsNullOrWhiteSpace(repoRoot)
                ? Environment.CurrentDirectory
                : repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        for (var index = 1; index < tokens.Count; index++)
        {
            startInfo.ArgumentList.Add(tokens[index]);
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            return new ContributorCommandRunResult(
                process.ExitCode,
                await stdoutTask.ConfigureAwait(false),
                await stderrTask.ConfigureAwait(false),
                TimedOut: false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ContributorCommandRunResult(
                ExitCode: -1,
                StandardOutput: string.Empty,
                StandardError: $"Timed out after {DefaultTimeout.TotalSeconds:0} seconds.",
                TimedOut: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ContributorCommandRunResult(
                ExitCode: -1,
                StandardOutput: string.Empty,
                StandardError: ex.Message,
                TimedOut: false);
        }
    }

    internal static IReadOnlyList<string> SplitCommandLine(string command)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var escaping = false;

        var text = command ?? string.Empty;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (escaping)
            {
                current.Append(character);
                escaping = false;
                continue;
            }

            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (character == '\\' && inQuotes && index + 1 < text.Length && text[index + 1] == '"')
            {
                escaping = true;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                FlushToken();
                continue;
            }

            current.Append(character);
        }

        if (escaping)
        {
            current.Append('\\');
        }

        FlushToken();
        return tokens;

        void FlushToken()
        {
            if (current.Length == 0)
            {
                return;
            }

            tokens.Add(current.ToString());
            current.Clear();
        }
    }
}
