[CmdletBinding()]
param(
    [string]$GuestRoot = ".",
    [string]$NtoskrnlPath = "C:\Windows\System32\ntoskrnl.exe",
    [string]$OutputPath = ".\evidence\files\vm-tooling-staging\executionrequired-xref-20260411"
)

$ErrorActionPreference = "Stop"

function Resolve-GuestPath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    if ([System.IO.Path]::IsPathRooted($TargetPath)) {
        return $TargetPath
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $TargetPath))
}

$resolvedGuestRoot = [System.IO.Path]::GetFullPath($GuestRoot)
$resolvedOutputPath = Resolve-GuestPath -BasePath $resolvedGuestRoot -TargetPath $OutputPath
New-Item -ItemType Directory -Force -Path $resolvedOutputPath | Out-Null

$targets = @(
    "AllowAudioToEnableExecutionRequiredPowerRequests",
    "AllowSystemRequiredPowerRequests"
)

$result = [ordered]@{
    ntoskrnl_path = $NtoskrnlPath
    collected_utc = (Get-Date).ToUniversalTime().ToString("o")
    guest_root = $resolvedGuestRoot
    output_path = $resolvedOutputPath
    string_hits = @()
}

$bytes = [System.IO.File]::ReadAllBytes($NtoskrnlPath)
$encoding = [System.Text.Encoding]::Unicode

foreach ($target in $targets) {
    $pattern = $encoding.GetBytes($target)

    for ($i = 0; $i -le ($bytes.Length - $pattern.Length); $i++) {
        $match = $true
        for ($j = 0; $j -lt $pattern.Length; $j++) {
            if ($bytes[$i + $j] -ne $pattern[$j]) {
                $match = $false
                break
            }
        }

        if ($match) {
            $result.string_hits += [ordered]@{
                string = $target
                offset = ("0x{0:X}" -f $i)
                offset_decimal = $i
            }
        }
    }
}

$outputFile = Join-Path $resolvedOutputPath "string-offsets.json"
$json = $result | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText(
    $outputFile,
    $json,
    (New-Object System.Text.UTF8Encoding($false))
)

Write-Host ("String hits found: {0}" -f $result.string_hits.Count)
Write-Host ("Output: {0}" -f $outputFile)
