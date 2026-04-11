param(
    [string]$OutputPath = 'C:\RegProbe-Diag\bootstrap\qga-vioserial-path-results.txt'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class RegProbeVioSerialNative
{
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public static string TryOpen(string path)
    {
        IntPtr handle = CreateFileW(path, 0xC0000000, 0, IntPtr.Zero, 3, 0x80, IntPtr.Zero);
        if (handle == INVALID_HANDLE_VALUE)
        {
            int error = Marshal.GetLastWin32Error();
            return "FAIL " + error + " " + new Win32Exception(error).Message;
        }

        CloseHandle(handle);
        return "OK";
    }
}
"@

function Get-VioSerialEntities {
    $entities = Get-CimInstance Win32_PnPEntity | Where-Object {
        ($_.PNPDeviceID -match 'VioSerialPort') -or
        ($_.Name -match '^vport0p') -or
        ($_.Caption -match '^vport0p')
    }

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entity in $entities) {
        if (-not $entity.PNPDeviceID) {
            continue
        }

        if ($seen.Add($entity.PNPDeviceID)) {
            [pscustomobject]@{
                Name       = $entity.Name
                Caption    = $entity.Caption
                InstanceId = $entity.PNPDeviceID
            }
        }
    }
}

function Get-PnpUtilInterfacePaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstanceId
    )

    $lines = & pnputil.exe /enum-devices /instanceid "$InstanceId" /interfaces 2>&1
    $paths = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $lines) {
        $trimmed = [string]$line
        if ($trimmed -match '\\\\\?\\') {
            $paths.Add($trimmed.Trim())
        }
    }

    [pscustomobject]@{
        RawLines = @($lines)
        Paths    = @($paths | Select-Object -Unique)
    }
}

function Get-RegistryInterfacePaths {
    $lines = & reg.exe query 'HKLM\SYSTEM\CurrentControlSet\Control\DeviceClasses' /f VioSerialPort /s 2>&1
    $paths = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $lines) {
        $trimmed = ([string]$line).Trim()
        if (-not $trimmed.StartsWith('HKEY_LOCAL_MACHINE\', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $leaf = Split-Path -Path $trimmed -Leaf
        if (-not $leaf.StartsWith('##?#', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $paths.Add(('\\?\' + $leaf.Substring(4)))
    }

    [pscustomobject]@{
        RawLines = @($lines)
        Paths    = @($paths | Select-Object -Unique)
    }
}

function Add-Section {
    param(
        [System.Collections.Generic.List[string]]$Sink,
        [string]$Title,
        [string[]]$Lines
    )

    $Sink.Add("=== $Title ===")
    foreach ($line in $Lines) {
        $Sink.Add($line)
    }
    $Sink.Add('')
}

$report = [System.Collections.Generic.List[string]]::new()
$report.Add("Timestamp={0:o}" -f [datetime]::UtcNow)
$report.Add("ComputerName=$env:COMPUTERNAME")
$report.Add('')

$entities = @(Get-VioSerialEntities)
if (-not $entities) {
    Add-Section -Sink $report -Title 'VIOSERIAL ENTITIES' -Lines @('NONE')
} else {
    $entityLines = foreach ($entity in $entities) {
        $name = if ($entity.Name) { $entity.Name } else { $entity.Caption }
        "InstanceId={0} Name={1}" -f $entity.InstanceId, $name
    }
    Add-Section -Sink $report -Title 'VIOSERIAL ENTITIES' -Lines $entityLines
}

$allPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

foreach ($entity in $entities) {
    $pnputil = Get-PnpUtilInterfacePaths -InstanceId $entity.InstanceId
    $lines = [System.Collections.Generic.List[string]]::new()
    if (-not $pnputil.Paths) {
        $lines.Add('PnpUtilPaths=NONE')
    } else {
        foreach ($path in $pnputil.Paths) {
            [void]$allPaths.Add($path)
            $lines.Add("PnpUtilPath=$path")
        }
    }
    Add-Section -Sink $report -Title ("PNPUTIL {0}" -f $entity.InstanceId) -Lines $lines.ToArray()
}

$registry = Get-RegistryInterfacePaths
$registryLines = [System.Collections.Generic.List[string]]::new()
if (-not $registry.Paths) {
    $registryLines.Add('RegistryPaths=NONE')
} else {
    foreach ($path in $registry.Paths) {
        [void]$allPaths.Add($path)
        $registryLines.Add("RegistryPath=$path")
    }
}
Add-Section -Sink $report -Title 'REGISTRY DEVICECLASSES PATHS' -Lines $registryLines.ToArray()

$openLines = [System.Collections.Generic.List[string]]::new()
if ($allPaths.Count -eq 0) {
    $openLines.Add('Candidates=NONE')
} else {
    foreach ($path in $allPaths) {
        $openLines.Add(("OpenTest={0} => {1}" -f $path, [RegProbeVioSerialNative]::TryOpen($path)))
    }
}

$knownAliases = @(
    '\\.\Global\org.qemu.guest_agent.0',
    '\\.\Global\vport0p2',
    '\\.\vport0p2'
)
foreach ($path in $knownAliases) {
    $openLines.Add(("AliasTest={0} => {1}" -f $path, [RegProbeVioSerialNative]::TryOpen($path)))
}

Add-Section -Sink $report -Title 'OPEN TESTS' -Lines $openLines.ToArray()

$directory = Split-Path -Parent $OutputPath
if ($directory) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$report | Set-Content -Path $OutputPath -Encoding UTF8
$report | ForEach-Object { Write-Host $_ }
