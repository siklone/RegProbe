using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Plugins.HelloWorld;

public sealed class HelloWorldTweak : ITweak
{
    private readonly string _filePath;

    public HelloWorldTweak()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _filePath = Path.Combine(desktop, "HelloFromWindowsOptimizer.txt");
    }

    public string Id => "plugin.helloworld";
    public string Name => "Hello World Plugin Example";
    public string Description => "Creates a simple text file on the Desktop to demonstrate plugin functionality. Safe and reversible.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => false;
    public bool SupportsRollback => true;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        var fileExists = File.Exists(_filePath);
        var status = fileExists ? TweakStatus.Applied : TweakStatus.NotApplied;
        var message = fileExists
            ? $"Hello World file exists at {_filePath}"
            : "Hello World file not found";

        return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        try
        {
            var content = $@"Hello from Windows Optimizer!

This file was created by the HelloWorld plugin to demonstrate the plugin system.

Created at: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}
Plugin Version: 1.0.0
Author: Windows Optimizer Team

This is a safe, reversible tweak example.
";

            File.WriteAllText(_filePath, content);

            return Task.FromResult(new TweakResult(
                TweakStatus.Applied,
                $"Successfully created Hello World file at {_filePath}",
                DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to create Hello World file: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        var fileExists = File.Exists(_filePath);
        var status = fileExists ? TweakStatus.Verified : TweakStatus.Failed;
        var message = fileExists
            ? "Hello World file verified successfully"
            : "Hello World file not found after apply";

        return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
                return Task.FromResult(new TweakResult(
                    TweakStatus.RolledBack,
                    $"Successfully deleted Hello World file from {_filePath}",
                    DateTimeOffset.UtcNow));
            }
            else
            {
                return Task.FromResult(new TweakResult(
                    TweakStatus.RolledBack,
                    "Hello World file already doesn't exist",
                    DateTimeOffset.UtcNow));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Failed to delete Hello World file: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }
}
