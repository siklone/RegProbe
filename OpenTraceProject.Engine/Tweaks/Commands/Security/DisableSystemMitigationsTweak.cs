using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Engine.Tweaks.Commands.Security;

public sealed class DisableSystemMitigationsTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";
    private const string ResourceSuffix = "Tweaks.Commands.Security.DisableSystemMitigations.xml";

    private static readonly Lazy<string> DesiredPolicyXml = new(LoadDesiredPolicyXml, isThreadSafe: true);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _workspaceDirectory;
    private readonly string _desiredPolicyPath;
    private string? _backupExportPath;
    private bool _hasBackupSnapshot;

    public DisableSystemMitigationsTweak(ICommandRunner commandRunner)
        : base(
            id: "security.disable-system-mitigations",
            name: "Disable System Mitigations",
            description: "Imports the documented Exploit Protection XML baseline that disables the researched system-wide mitigation bundle.",
            risk: TweakRiskLevel.Risky,
            commandRunner: commandRunner)
    {
        _workspaceDirectory = Path.Combine(Path.GetTempPath(), "OpenTraceProject", "ExploitProtection");
        _desiredPolicyPath = Path.Combine(_workspaceDirectory, "security-disable-system-mitigations.xml");
        EnsureDesiredPolicyFile();
    }

    protected override CommandRequest GetDetectCommand()
    {
        EnsureDesiredPolicyFile();

        var exportPath = CreateExportPath();
        var shouldDeleteExportAfterCompare = _hasBackupSnapshot;
        if (!_hasBackupSnapshot)
        {
            _backupExportPath = exportPath;
        }

        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildDetectScript(exportPath, _desiredPolicyPath, shouldDeleteExportAfterCompare)
            }),
            TimeoutSeconds: 120);
    }

    protected override CommandRequest GetApplyCommand()
    {
        EnsureDesiredPolicyFile();

        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildApplyScript(_desiredPolicyPath)
            }),
            TimeoutSeconds: 120);
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        EnsureDesiredPolicyFile();

        var backupPath = !string.IsNullOrWhiteSpace(detectedState)
            ? detectedState
            : _backupExportPath;

        if (string.IsNullOrWhiteSpace(backupPath))
        {
            return null;
        }

        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildRollbackScript(backupPath, _desiredPolicyPath)
            }),
            TimeoutSeconds: 120);
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var snapshot = TryParseSnapshot(result.StandardOutput);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.BackupPath))
        {
            state = string.Empty;
            return false;
        }

        _backupExportPath ??= snapshot.BackupPath;
        _hasBackupSnapshot = true;
        state = _backupExportPath;
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        var snapshot = TryParseSnapshot(result.StandardOutput);
        return snapshot?.MatchesDesired == true;
    }

    private static string GetPowerShellPath()
    {
        return Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            PowerShellExe);
    }

    private string CreateExportPath()
    {
        Directory.CreateDirectory(_workspaceDirectory);
        return Path.Combine(_workspaceDirectory, $"security-disable-system-mitigations-export-{Guid.NewGuid():N}.xml");
    }

    private static string BuildDetectScript(string exportPath, string desiredPath, bool deleteExportAfterCompare)
    {
        var deleteLiteral = deleteExportAfterCompare ? "$true" : "$false";
        return
            "$currentExportPath = " + Quote(exportPath) + "; " +
            "$desiredPath = " + Quote(desiredPath) + "; " +
            "Get-ProcessMitigation -RegistryConfigFilePath $currentExportPath | Out-Null; " +
            "function Normalize-Xml([string]$text) { if ($null -eq $text) { return '' } return (($text -replace ' Audit=\"false\"', '') -replace \"`r`n\", \"`n\").Trim() }; " +
            "$currentText = Get-Content -LiteralPath $currentExportPath -Raw; " +
            "$desiredText = Get-Content -LiteralPath $desiredPath -Raw; " +
            "$matches = (Normalize-Xml $currentText) -eq (Normalize-Xml $desiredText); " +
            "if (" + deleteLiteral + ") { Remove-Item -LiteralPath $currentExportPath -Force -ErrorAction SilentlyContinue }; " +
            "[pscustomobject]@{ BackupPath = $currentExportPath; MatchesDesired = $matches } | ConvertTo-Json -Compress";
    }

    private static string BuildApplyScript(string desiredPath)
    {
        return
            "$policyPath = " + Quote(desiredPath) + "; " +
            "Set-ProcessMitigation -PolicyFilePath $policyPath | Out-Null; " +
            "Remove-Item -LiteralPath $policyPath -Force -ErrorAction SilentlyContinue; " +
            "Write-Output 'Imported exploit protection XML.'";
    }

    private static string BuildRollbackScript(string backupPath, string desiredPath)
    {
        return
            "$backupPath = " + Quote(backupPath) + "; " +
            "$policyPath = " + Quote(desiredPath) + "; " +
            "Set-ProcessMitigation -PolicyFilePath $backupPath | Out-Null; " +
            "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue; " +
            "Remove-Item -LiteralPath $policyPath -Force -ErrorAction SilentlyContinue; " +
            "Write-Output 'Restored exploit protection XML.'";
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static Snapshot? TryParseSnapshot(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Snapshot>(output, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string LoadDesiredPolicyXml()
    {
        var assembly = typeof(DisableSystemMitigationsTweak).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new InvalidOperationException("Exploit protection XML resource was not found.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Exploit protection XML resource stream was not available.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private void EnsureDesiredPolicyFile()
    {
        Directory.CreateDirectory(_workspaceDirectory);
        File.WriteAllText(_desiredPolicyPath, DesiredPolicyXml.Value);
    }

    private sealed record Snapshot(string BackupPath, bool MatchesDesired);
}
