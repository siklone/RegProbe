[CmdletBinding()]
param(
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$GuestUser = 'Administrator',
    [string]$GuestPassword = 'CodexVm2026!',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

function Invoke-Vmrun {
    param([string[]]$Arguments)

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "vmrun failed ($LASTEXITCODE): $($output.Trim())"
    }

    return $output.Trim()
}

$runningList = Invoke-Vmrun -Arguments @('-T', 'ws', 'list')
$vmRunning = $runningList -match [regex]::Escape($VmPath)
$toolsState = if ($vmRunning) {
    try {
        Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath)
    }
    catch {
        'query-failed'
    }
}
else {
    'not-running'
}
$processText = ''
$processQueryError = $null

if ($vmRunning -and $toolsState -match 'running|installed') {
    try {
        $processText = Invoke-Vmrun -Arguments @('-T', 'ws', '-gu', $GuestUser, '-gp', $GuestPassword, 'listProcessesInGuest', $VmPath)
    }
    catch {
        $processQueryError = $_.Exception.Message
    }
}

$checks = [ordered]@{
    explorer = [bool]($processText -match '\bexplorer\.exe\b')
    sihost = [bool]($processText -match '\bsihost\.exe\b')
    shellhost = [bool]($processText -match '\bShellHost\.exe\b')
    ctfmon = [bool]($processText -match '\bctfmon\.exe\b')
    app = [bool]($processText -match '\bOpenTraceProject\.App\.exe\b')
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    vm_path = $VmPath
    vm_running = $vmRunning
    tools_state = $toolsState
    process_query_error = $processQueryError
    shell_healthy = ($checks.explorer -and $checks.sihost -and $checks.shellhost)
    checks = $checks
}

if ($OutputPath) {
    $result | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputPath -Encoding UTF8
}

$result | ConvertTo-Json -Depth 5
