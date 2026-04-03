[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$KeyNames,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$LogPath = 'C:\RegProbe-Diag\windbg-registry-trace.log'
)

$ErrorActionPreference = 'Stop'

$parent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('$$ RegProbe WinDbg boot registry watch script')
$lines.Add('$$ This script logs CmQueryValueKey value-name probes and relies on post-filtering for the target names below.')
$lines.Add('.symfix')
$lines.Add('.reload /f')
$lines.Add(('.logopen /t "{0}"' -f $LogPath.Replace('\', '\\')))
$lines.Add('.echo REGPROBE_WATCH_KEYS_BEGIN')
foreach ($keyName in @($KeyNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
    $lines.Add(('.echo {0}' -f $keyName))
}
$lines.Add('.echo REGPROBE_WATCH_KEYS_END')
$lines.Add('bc *')
$lines.Add('bu nt!CmQueryValueKey')
$lines.Add('bs 0 ".echo REGPROBE_VALUE_BEGIN; du poi(@rdx+8); .echo REGPROBE_VALUE_END; gc"')
$lines.Add('g')

Set-Content -Path $OutputPath -Value $lines -Encoding ASCII

[pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    output_path = $OutputPath
    log_path = $LogPath
    key_count = @($KeyNames).Count
    mode = 'cmquery-valuename-postfilter'
} | ConvertTo-Json -Depth 5
