using System;
using System.IO;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class RemoveProductKeyTweak : CommandTweak
{
    private const string CscriptExe = "cscript.exe";

    public RemoveProductKeyTweak(ICommandRunner commandRunner)
        : base(
            id: "cleanup.product-key",
            name: "Remove Product Key from Registry",
            description: "Removes the Windows product key from the registry to prevent theft by malicious code. The key can be reactivated if needed.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, CscriptExe);
        var slmgrPath = Path.Combine(Environment.SystemDirectory, "slmgr.vbs");

        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "//NoLogo", slmgrPath, "/dli" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, CscriptExe);
        var slmgrPath = Path.Combine(Environment.SystemDirectory, "slmgr.vbs");

        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "//NoLogo", slmgrPath, "/cpky" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        // Product key cannot be automatically restored
        // User must manually re-enter key if needed
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;

        if (output.Contains("License Status:", StringComparison.OrdinalIgnoreCase))
        {
            if (output.Contains("Licensed", StringComparison.OrdinalIgnoreCase))
            {
                state = "Product key present in registry";
            }
            else
            {
                state = "Product key status unknown";
            }
            return true;
        }

        state = "License information retrieved";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        // slmgr /cpky always returns 0 on success
        return result.ExitCode == 0;
    }
}
