using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Infrastructure.Commands;

public sealed class LocalCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Executable))
        {
            throw new ArgumentException("Executable path is required.", nameof(request));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory)
                ? Path.GetDirectoryName(request.Executable) ?? string.Empty
                : request.WorkingDirectory
        };

        foreach (var arg in request.Arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        var startTime = DateTimeOffset.UtcNow;

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start command process.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var timedOut = false;
        var timeoutSeconds = Math.Max(1, request.TimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(true);
            }
            catch
            {
            }
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var exitCode = process.HasExited ? process.ExitCode : -1;
        var duration = DateTimeOffset.UtcNow - startTime;

        return new CommandResult(exitCode, stdout, stderr, timedOut, duration);
    }
}
