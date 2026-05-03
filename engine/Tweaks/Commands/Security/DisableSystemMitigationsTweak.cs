using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Security;

public sealed class DisableSystemMitigationsTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";
    private const string ResourceSuffix = "Tweaks.Commands.Security.DisableSystemMitigations.xml";
    private const string SourceRelativePath = "engine/Tweaks/Commands/Security/DisableSystemMitigations.xml";

    private static readonly Lazy<string> DesiredPolicyXml = new(LoadDesiredPolicyXml, isThreadSafe: true);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _workspaceDirectory;
    private readonly string _desiredPolicyPath;
    private string? _backupExportPath;
    private bool _hasBackupSnapshot;

    public DisableSystemMitigationsTweak(
        ICommandRunner commandRunner,
        string? name = null,
        string? description = null)
        : base(
            id: "security.disable-system-mitigations",
            name: string.IsNullOrWhiteSpace(name) ? "Disable System Mitigations" : name,
            description: string.IsNullOrWhiteSpace(description)
                ? "Imports the documented Exploit Protection XML baseline that disables the researched system-wide mitigation bundle."
                : description,
            risk: TweakRiskLevel.Risky,
            commandRunner: commandRunner)
    {
        _workspaceDirectory = Path.Combine(Path.GetTempPath(), "RegProbe", "ExploitProtection");
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
            "function Test-DesiredNode([System.Xml.XmlNode]$desiredNode, [System.Xml.XmlNode]$currentNode) { " +
            "  if ($null -eq $desiredNode -or $null -eq $currentNode) { return $false } " +
            "  if ($desiredNode.Name -ne $currentNode.Name) { return $false } " +
            "  foreach ($attribute in @($desiredNode.Attributes)) { " +
            "    if ($null -eq $attribute) { continue } " +
            "    $currentAttribute = $currentNode.Attributes[$attribute.Name]; " +
            "    if ($null -eq $currentAttribute) { " +
            "      if ($attribute.Name -eq 'Audit' -and $attribute.Value -eq 'false') { continue } " +
            "      return $false " +
            "    } " +
            "    if ($currentAttribute.Value -ne $attribute.Value) { return $false } " +
            "  } " +
            "  $desiredChildren = @($desiredNode.ChildNodes | Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element }); " +
            "  foreach ($desiredChild in $desiredChildren) { " +
            "    $candidates = @($currentNode.ChildNodes | Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element -and $_.Name -eq $desiredChild.Name }); " +
            "    if ($desiredChild.Attributes['Executable']) { " +
            "      $executable = $desiredChild.Attributes['Executable'].Value; " +
            "      $candidates = @($candidates | Where-Object { $_.Attributes['Executable'] -and $_.Attributes['Executable'].Value -eq $executable }); " +
            "    } " +
            "    $matched = $false; " +
            "    foreach ($candidate in $candidates) { " +
            "      if (Test-DesiredNode $desiredChild $candidate) { $matched = $true; break } " +
            "    } " +
            "    if (-not $matched) { return $false } " +
            "  } " +
            "  return $true " +
            "} " +
            "[xml]$currentXml = Get-Content -LiteralPath $currentExportPath -Raw; " +
            "[xml]$desiredXml = Get-Content -LiteralPath $desiredPath -Raw; " +
            "$matches = Test-DesiredNode $desiredXml.DocumentElement $currentXml.DocumentElement; " +
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

        if (!string.IsNullOrWhiteSpace(resourceName))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Exploit protection XML resource stream was not available.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        var sourcePath = TryFindSourcePolicyPath();
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            return File.ReadAllText(sourcePath, Encoding.UTF8);
        }

        throw new InvalidOperationException("Exploit protection XML resource was not found.");
    }

    private static string? TryFindSourcePolicyPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 8 && current is not null; depth++)
        {
            var candidate = Path.Combine(
                current.FullName,
                SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private void EnsureDesiredPolicyFile()
    {
        Directory.CreateDirectory(_workspaceDirectory);
        File.WriteAllText(_desiredPolicyPath, DesiredPolicyXml.Value);
    }

    private sealed record Snapshot(string BackupPath, bool MatchesDesired);
}
