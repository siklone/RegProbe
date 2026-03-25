[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RecordId,

    [Parameter(Mandatory = $true)]
    [string]$TestId,

    [Parameter(Mandatory = $true)]
    [string]$Symptom,

    [string]$TweakId,
    [string]$Family = '',
    [string]$SnapshotName = '',
    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [string]$ValueState = '',
    [bool]$ShellRecovered = $false,
    [bool]$NeededSnapshotRevert = $false,
    [string]$Notes = '',
    [string]$IncidentPath = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($IncidentPath)) {
    $IncidentPath = Join-Path $PSScriptRoot '..\..\research\vm-incidents.json'
}

$incidentDirectory = Split-Path -Parent $IncidentPath
if (-not (Test-Path $incidentDirectory)) {
    New-Item -ItemType Directory -Path $incidentDirectory -Force | Out-Null
}

if (Test-Path $IncidentPath) {
    $payload = Get-Content $IncidentPath -Raw | ConvertFrom-Json -Depth 10
}
else {
    $payload = [ordered]@{
        schema_version = '1.0'
        generated_utc = [DateTime]::UtcNow.ToString('o')
        summary = [ordered]@{
            total_incidents = 0
            shell_recovered = 0
            needed_snapshot_revert = 0
        }
        incidents = @()
    }
}

$incidentId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMddHHmmss'), ($RecordId -replace '[^a-zA-Z0-9\-]+', '-')
$entry = [ordered]@{
    incident_id = $incidentId
    record_id = $RecordId
    tweak_id = if ($TweakId) { $TweakId } else { $RecordId }
    test_id = $TestId
    family = $Family
    snapshot_name = $SnapshotName
    registry_path = $RegistryPath
    value_name = $ValueName
    value_state = $ValueState
    symptom = $Symptom
    shell_recovered = $ShellRecovered
    needed_snapshot_revert = $NeededSnapshotRevert
    recorded_utc = [DateTime]::UtcNow.ToString('o')
    notes = $Notes
}

$incidents = @($payload.incidents | Where-Object {
    -not (($_.record_id -eq $RecordId) -and ($_.test_id -eq $TestId) -and ($_.symptom -eq $Symptom))
})
$incidents += [pscustomobject]$entry
$payload.incidents = $incidents
$payload.generated_utc = [DateTime]::UtcNow.ToString('o')
$payload.summary = [ordered]@{
    total_incidents = @($incidents).Count
    shell_recovered = @($incidents | Where-Object { $_.shell_recovered }).Count
    needed_snapshot_revert = @($incidents | Where-Object { $_.needed_snapshot_revert }).Count
}

$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $IncidentPath -Encoding UTF8
Write-Output $IncidentPath
