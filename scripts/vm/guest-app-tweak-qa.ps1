[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TweakId,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$UserName = 'rai',
    [string]$AppExe = 'C:\Tools\AppSmoke\RegProbe.App.exe',
    [int]$TimeoutSeconds = 300,
    [switch]$SkipRollback,
    [switch]$AllowGatedMutation
)

$ErrorActionPreference = 'Stop'

function Wait-ForPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            return $true
        }

        Start-Sleep -Milliseconds 1000
    }

    return $false
}

$taskName = 'RegProbe-QA-' + ($TweakId -replace '[^A-Za-z0-9_.-]', '-')
$outputDir = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null

$argList = @(
    '--tweaks',
    '--qa-run-tweak', $TweakId,
    '--qa-output', $OutputPath,
    '--qa-shutdown'
)

if ($SkipRollback) {
    $argList += '--qa-skip-rollback'
}

if ($AllowGatedMutation) {
    $argList += '--qa-allow-gated-mutation'
}

$argumentLine = [string]::Join(' ', $argList)
$action = New-ScheduledTaskAction -Execute $AppExe -Argument $argumentLine
$principal = New-ScheduledTaskPrincipal -UserId $UserName -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 10) -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName $taskName

if (-not (Wait-ForPath -Path $OutputPath -TimeoutSeconds $TimeoutSeconds)) {
    $taskInfo = Get-ScheduledTaskInfo -TaskName $taskName -ErrorAction SilentlyContinue
    throw ("Timed out waiting for QA report '{0}'. LastTaskResult={1}; State={2}" -f $OutputPath, $taskInfo.LastTaskResult, $taskInfo.State)
}

$reportText = Get-Content -LiteralPath $OutputPath -Raw
Write-Output $reportText

$report = $reportText | ConvertFrom-Json
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null

if (-not $report.Success) {
    exit 2
}

exit 0
