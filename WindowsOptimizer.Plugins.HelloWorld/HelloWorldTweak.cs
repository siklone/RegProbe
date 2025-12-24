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
        _filePath = Path.Combine(desktop, "HelloWindowsOptimizer.txt");
    }

    public string Id => "plugin.hello-world";
    public string Name => "Hello World Plugin Tweak";
    public string Description => "A demonstration tweak that creates a text file on your desktop. Safe and reversible.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => false;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        var exists = File.Exists(_filePath);
        return Task.FromResult(new TweakResult(
            exists ? TweakStatus.Applied : TweakStatus.Detected,
            exists ? "Demo file found on desktop." : "Demo file not found.",
            DateTimeOffset.Now));
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        try
        {
            await File.WriteAllTextAsync(_filePath, "Hello from the Windows Optimizer Plugin SDK!\nCreated at: " + DateTime.Now, ct);
            return new TweakResult(TweakStatus.Applied, "Demo file created on desktop.", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, "Failed to create demo file.", DateTimeOffset.Now, ex);
        }
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        var exists = File.Exists(_filePath);
        return Task.FromResult(new TweakResult(
            exists ? TweakStatus.Verified : TweakStatus.Failed,
            exists ? "Demo file verification successful." : "Demo file is missing.",
            DateTimeOffset.Now));
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
            return Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Demo file removed from desktop.", DateTimeOffset.Now));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(TweakStatus.Failed, "Failed to remove demo file.", DateTimeOffset.Now, ex));
        }
    }
}
