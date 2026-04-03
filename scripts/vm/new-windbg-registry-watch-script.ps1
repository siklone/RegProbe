[CmdletBinding()]
param(
    [string[]]$KeyNames = @(),
    [string]$TargetKey = '',
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$LogPath = 'C:\RegProbe-Diag\windbg-registry-trace.log',
    [ValidateSet('multi-postfilter', 'minimal', 'symbols', 'attach-only', 'breakin-once', 'breakin-twice', 'breakin-delayed-10', 'breakin-delayed-30', 'singlekey-smoke', 'singlekey-firsthit', 'singlekey-rawbounded')]
    [string]$TraceProfile = 'multi-postfilter',
    [int]$RawHitLimit = 100
)

$ErrorActionPreference = 'Stop'

$parent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

$resolvedKeyNames = @($KeyNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
if (-not [string]::IsNullOrWhiteSpace($TargetKey)) {
    $resolvedKeyNames = @($TargetKey)
}

if ($TraceProfile -like 'singlekey-*' -and $resolvedKeyNames.Count -ne 1) {
    throw "TraceProfile '$TraceProfile' requires exactly one target key."
}

$resolvedTargetKey = if ($resolvedKeyNames.Count -eq 1) { [string]$resolvedKeyNames[0] } else { $null }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('$$ RegProbe WinDbg boot registry watch script')
$lines.Add('$$ This script targets nt!CmQueryValueKey using public symbols and parser-safe bu + bs 0 commands.')
$lines.Add('.symfix')
$lines.Add('.reload /f')
$lines.Add(('.logopen /t "{0}"' -f $LogPath.Replace('\', '\\')))

switch ($TraceProfile) {
    'minimal' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'symbols' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'attach-only' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'breakin-once' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'breakin-twice' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'breakin-delayed-10' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'breakin-delayed-30' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add('.echo REGPROBE_ATTACH_BEGIN')
        $lines.Add('.echo REGPROBE_ATTACH_READY')
        $lines.Add('g')
    }
    'singlekey-smoke' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add(('.echo REGPROBE_TARGET|{0}' -f $resolvedTargetKey))
        $lines.Add('bc *')
        $lines.Add('bu nt!CmQueryValueKey')
        $lines.Add('bs 0 "gc"')
        $lines.Add('g')
    }
    'singlekey-firsthit' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add(('.echo REGPROBE_TARGET|{0}' -f $resolvedTargetKey))
        $lines.Add('bc *')
        $lines.Add('bu nt!CmQueryValueKey')
        $lines.Add('bs 0 ".echo REGPROBE_FIRSTHIT_BEGIN; r rdx; dq @rdx L4; du poi(@rdx+8) L1; .echo REGPROBE_FIRSTHIT_END; qd"')
        $lines.Add('g')
    }
    'singlekey-rawbounded' {
        $lines.Add(('.echo REGPROBE_PROFILE|{0}' -f $TraceProfile))
        $lines.Add(('.echo REGPROBE_TARGET|{0}' -f $resolvedTargetKey))
        $lines.Add(('.echo REGPROBE_RAW_HIT_LIMIT|{0}' -f $RawHitLimit))
        $lines.Add('bc *')
        $lines.Add('bu nt!CmQueryValueKey')
        $lines.Add('bs 0 ".echo REGPROBE_VALUE_BEGIN; du poi(@rdx+8) L1; .echo REGPROBE_VALUE_END; gc"')
        $lines.Add('g')
    }
    default {
        $lines.Add('.echo REGPROBE_WATCH_KEYS_BEGIN')
        foreach ($keyName in $resolvedKeyNames) {
            $lines.Add(('.echo {0}' -f $keyName))
        }
        $lines.Add('.echo REGPROBE_WATCH_KEYS_END')
        $lines.Add('bc *')
        $lines.Add('bu nt!CmQueryValueKey')
        $lines.Add('bs 0 ".echo REGPROBE_VALUE_BEGIN; du poi(@rdx+8) L1; .echo REGPROBE_VALUE_END; gc"')
        $lines.Add('g')
    }
}

Set-Content -Path $OutputPath -Value $lines -Encoding ASCII

[pscustomobject]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    output_path = $OutputPath
    log_path = $LogPath
    key_count = @($resolvedKeyNames).Count
    target_key = $resolvedTargetKey
    trace_profile = $TraceProfile
    raw_hit_limit = $RawHitLimit
    mode = if ($TraceProfile -eq 'multi-postfilter') { 'cmquery-valuename-postfilter' } else { $TraceProfile }
} | ConvertTo-Json -Depth 5
