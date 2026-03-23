#requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SharedRoot,

    [string]$TaskName = 'WindowsOptimizerValidationAgent'
)

$ErrorActionPreference = 'Stop'

function Write-AgentLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = (Get-Date).ToString('s')
    Add-Content -Path $script:AgentLogPath -Value "[$timestamp] $Message"
}

function Save-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$InputObject
    )

    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 8 | Set-Content -Path $Path -Encoding UTF8
}

function Load-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    return Get-Content -Path $Path -Raw | ConvertFrom-Json
}

function ConvertTo-MutableState {
    param(
        [AllowNull()]
        [object]$InputObject
    )

    if ($null -eq $InputObject) {
        return $null
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $map = [ordered]@{}
        foreach ($key in $InputObject.Keys) {
            $map[$key] = ConvertTo-MutableState -InputObject $InputObject[$key]
        }
        return $map
    }

    if ($InputObject -is [pscustomobject]) {
        $map = [ordered]@{}
        foreach ($prop in $InputObject.PSObject.Properties) {
            $map[$prop.Name] = ConvertTo-MutableState -InputObject $prop.Value
        }
        return $map
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
        $items = New-Object System.Collections.ArrayList
        foreach ($item in $InputObject) {
            [void]$items.Add((ConvertTo-MutableState -InputObject $item))
        }
        return $items
    }

    return $InputObject
}

function New-State {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestId
    )

    return [ordered]@{
        test_id = $TestId
        phase = 'BOOT_START'
        run_count = 0
        baseline = $null
        benchmark_runs = [System.Collections.ArrayList]::new()
        restore_complete = $false
        errors = [System.Collections.ArrayList]::new()
        timings = [ordered]@{
            boot_started_at = (Get-Date).ToString('o')
            last_updated_at = (Get-Date).ToString('o')
        }
    }
}

function Ensure-StateList {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not $State.Contains($Name) -or $null -eq $State[$Name]) {
        $State[$Name] = [System.Collections.ArrayList]::new()
        return
    }

    if ($State[$Name] -is [System.Collections.ArrayList]) {
        return
    }

    $list = [System.Collections.ArrayList]::new()
    if ($State[$Name] -is [System.Collections.IEnumerable] -and $State[$Name] -isnot [string]) {
        foreach ($item in $State[$Name]) {
            [void]$list.Add($item)
        }
    } else {
        [void]$list.Add($State[$Name])
    }

    $State[$Name] = $list
}

function Update-StatePhase {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State,

        [Parameter(Mandatory = $true)]
        [string]$Phase,

        [string]$Detail
    )

    if (-not $State.Contains('timings') -or $null -eq $State.timings) {
        $State.timings = [ordered]@{}
    } elseif ($State.timings -isnot [System.Collections.IDictionary]) {
        $State.timings = ConvertTo-MutableState -InputObject $State.timings
    }

    $State.phase = $Phase
    $State.timings['last_updated_at'] = (Get-Date).ToString('o')
    if ($Detail) {
        $State.last_detail = $Detail
    }
    Save-JsonFile -Path $script:StatePath -InputObject $State
    Write-AgentLog "$Phase :: $Detail"
}

function Wait-ForStableIdle {
    param(
        [int]$TimeoutSeconds = 180,
        [int]$CpuThreshold = 20,
        [int]$DiskThreshold = 20,
        [int]$StableWindowSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $stableSeconds = 0

    while ((Get-Date) -lt $deadline) {
        $cpu = (Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples[0].CookedValue
        $disk = 0
        try {
            $disk = (Get-Counter '\PhysicalDisk(_Total)\% Disk Time').CounterSamples[0].CookedValue
        } catch {
            $disk = 0
        }

        if ($cpu -le $CpuThreshold -and $disk -le $DiskThreshold) {
            $stableSeconds += 5
        } else {
            $stableSeconds = 0
        }

        if ($stableSeconds -ge $StableWindowSeconds) {
            return [ordered]@{
                cpu = [math]::Round($cpu, 2)
                disk = [math]::Round($disk, 2)
                stable_seconds = $stableSeconds
            }
        }

        Start-Sleep -Seconds 5
    }

    throw "Idle stabilization timed out after $TimeoutSeconds seconds."
}

function Get-RegistryBaseline {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Config
    )

    $existing = Get-ItemProperty -Path $Config.registry_path -Name $Config.value_name -ErrorAction SilentlyContinue
    if ($null -eq $existing) {
        return [ordered]@{
            exists = $false
            value = $null
        }
    }

    return [ordered]@{
        exists = $true
        value = $existing.$($Config.value_name)
    }
}

function Set-RegistryValue {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Config,

        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    if (-not (Test-Path $Config.registry_path)) {
        New-Item -Path $Config.registry_path -Force | Out-Null
    }

    New-ItemProperty -Path $Config.registry_path -Name $Config.value_name -PropertyType $Config.value_type -Value $Value -Force | Out-Null
}

function Restore-RegistryBaseline {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Config,

        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline
    )

    if ($Baseline.exists) {
        Set-RegistryValue -Config $Config -Value $Baseline.value
        return
    }

    Remove-ItemProperty -Path $Config.registry_path -Name $Config.value_name -ErrorAction SilentlyContinue
}

function Start-RebootAndExit {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State,

        [Parameter(Mandatory = $true)]
        [string]$Phase
    )

    Update-StatePhase -State $State -Phase $Phase -Detail 'Guest restart requested.'
    exit 0
}

function Invoke-BenchmarkRun {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Config,

        [int]$RunIndex
    )

    Write-AgentLog "BENCH_TRACE :: Invoke-BenchmarkRun start run=$RunIndex"
    $stdout = Join-Path $script:ArtifactsPath ("benchmark-run-{0:D2}.stdout.txt" -f $RunIndex)
    $stderr = Join-Path $script:ArtifactsPath ("benchmark-run-{0:D2}.stderr.txt" -f $RunIndex)
    $perfCsv = Join-Path $script:ArtifactsPath ("benchmark-run-{0:D2}.perf.csv" -f $RunIndex)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'C:\Windows\System32\cmd.exe'
    $psi.Arguments = "/c $($Config.benchmark_command)"
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    $counterSamples = New-Object System.Collections.Generic.List[object]
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    [void]$proc.Start()
    Write-AgentLog "BENCH_TRACE :: Process started run=$RunIndex pid=$($proc.Id)"

    while (-not $proc.HasExited) {
        $cpu = (Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples[0].CookedValue
        $commit = (Get-Counter '\Memory\Committed Bytes').CounterSamples[0].CookedValue
        $diskLatency = 0
        $diskTransfers = 0
        try {
            $diskLatency = (Get-Counter '\PhysicalDisk(_Total)\Avg. Disk sec/Transfer').CounterSamples[0].CookedValue
            $diskTransfers = (Get-Counter '\PhysicalDisk(_Total)\Disk Transfers/sec').CounterSamples[0].CookedValue
        } catch {
            $diskLatency = 0
            $diskTransfers = 0
        }

        $counterSamples.Add([ordered]@{
            timestamp = (Get-Date).ToString('o')
            cpu_percent = [math]::Round($cpu, 2)
            committed_bytes = [int64]$commit
            avg_disk_sec_per_transfer = [math]::Round($diskLatency, 6)
            disk_transfers_per_sec = [math]::Round($diskTransfers, 2)
        }) | Out-Null

        Start-Sleep -Seconds 1
    }

    $sw.Stop()
    Write-AgentLog "BENCH_TRACE :: Process exited run=$RunIndex exit=$($proc.ExitCode)"
    $stdoutText = $proc.StandardOutput.ReadToEnd()
    $stderrText = $proc.StandardError.ReadToEnd()
    $stdoutText | Set-Content -Path $stdout -Encoding UTF8
    $stderrText | Set-Content -Path $stderr -Encoding UTF8
    Write-AgentLog "BENCH_TRACE :: Streams captured run=$RunIndex stdout=$($stdoutText.Length) stderr=$($stderrText.Length)"
    $counterRows = foreach ($sample in $counterSamples) {
        [pscustomobject]$sample
    }
    $counterRows | Export-Csv -Path $perfCsv -NoTypeInformation -Encoding UTF8
    Write-AgentLog "BENCH_TRACE :: Perf CSV exported run=$RunIndex path=$perfCsv"

    return [ordered]@{
        run_index = $RunIndex
        exit_code = $proc.ExitCode
        duration_seconds = [math]::Round($sw.Elapsed.TotalSeconds, 2)
        stdout_path = $stdout
        stderr_path = $stderr
        perf_csv = $perfCsv
    }
}

$TestDir = Join-Path $SharedRoot 'controller\current'
$ConfigPath = Join-Path $TestDir 'config.json'
$script:StatePath = Join-Path $TestDir 'status.json'
$script:ResultPath = Join-Path $TestDir 'result.json'
$script:ArtifactsPath = Join-Path $TestDir 'artifacts'
$script:AgentLogPath = Join-Path $TestDir 'agent.log'

if (-not (Test-Path $ConfigPath)) {
    throw "Config file not found at $ConfigPath"
}

if (-not (Test-Path $TestDir)) {
    New-Item -ItemType Directory -Force -Path $TestDir | Out-Null
}
if (-not (Test-Path $script:ArtifactsPath)) {
    New-Item -ItemType Directory -Force -Path $script:ArtifactsPath | Out-Null
}

$config = Load-JsonFile -Path $ConfigPath
$stateObject = Load-JsonFile -Path $script:StatePath
if ($null -eq $stateObject) {
    $stateObject = New-State -TestId $config.test_id
}

$state = ConvertTo-MutableState -InputObject $stateObject
if ($null -eq $state) {
    $state = New-State -TestId $config.test_id
}
Ensure-StateList -State $state -Name 'benchmark_runs'
Ensure-StateList -State $state -Name 'errors'

try {
    if (-not $state.baseline) {
        $baseline = Get-RegistryBaseline -Config $config
        $state.baseline = $baseline
        Update-StatePhase -State $state -Phase 'BASELINE_CAPTURED' -Detail "Baseline exists=$($baseline.exists)"
    }

    if (-not $state.candidate_applied) {
        Set-RegistryValue -Config $config -Value $config.candidate_value
        $state.candidate_applied = $true
        Update-StatePhase -State $state -Phase 'VALUE_APPLIED' -Detail "Applied candidate value $($config.candidate_value)"

        if ($config.restart_mode -eq 'reboot' -and -not $state.after_apply_reboot_complete) {
            Start-RebootAndExit -State $state -Phase 'RESTART_AFTER_APPLY'
        }
    }

    if ($config.restart_mode -eq 'reboot' -and -not $state.after_apply_reboot_complete) {
        $state.after_apply_reboot_complete = $true
        Update-StatePhase -State $state -Phase 'POST_REBOOT_AFTER_APPLY' -Detail 'Resumed after apply reboot.'
    }

    if (-not $state.idle_ready) {
        $idle = Wait-ForStableIdle -TimeoutSeconds $config.idle_timeout_seconds -CpuThreshold $config.idle_cpu_threshold -DiskThreshold $config.idle_disk_threshold -StableWindowSeconds $config.idle_window_seconds
        $state.idle_ready = $true
        $state.idle_snapshot = $idle
        Update-StatePhase -State $state -Phase 'IDLE_REACHED' -Detail "CPU=$($idle.cpu) Disk=$($idle.disk)"
    }

    if (-not $state.benchmark_complete) {
        $warmupRuns = [int]$config.warmup_runs
        $measuredRuns = [int]$config.measured_runs
        $totalRuns = $warmupRuns + $measuredRuns

        for ($i = $state.run_count + 1; $i -le $totalRuns; $i++) {
            $kind = if ($i -le $warmupRuns) { 'warmup' } else { 'measured' }
            Update-StatePhase -State $state -Phase 'BENCH_START' -Detail "Run $i ($kind)"
            $runResult = Invoke-BenchmarkRun -Config $config -RunIndex $i
            Write-AgentLog "BENCH_TRACE :: Invoke-BenchmarkRun returned run=$i null=$($null -eq $runResult)"
            if ($null -eq $runResult) {
                throw "Invoke-BenchmarkRun returned null for run $i."
            }
            if ($runResult -is [System.Collections.IDictionary]) {
                $runResult['kind'] = $kind
            } else {
                Add-Member -InputObject $runResult -NotePropertyName 'kind' -NotePropertyValue $kind -Force
            }
            Write-AgentLog "BENCH_TRACE :: Kind attached run=$i type=$($runResult.GetType().FullName)"
            $state.run_count = $i
            Ensure-StateList -State $state -Name 'benchmark_runs'
            $benchmarkRuns = $state['benchmark_runs']
            if ($null -eq $benchmarkRuns) {
                throw 'benchmark_runs list is null after Ensure-StateList.'
            }
            Write-AgentLog "BENCH_TRACE :: benchmark_runs type=$($benchmarkRuns.GetType().FullName)"
            [void]$benchmarkRuns.Add((ConvertTo-MutableState -InputObject $runResult))
            Write-AgentLog "BENCH_TRACE :: Run appended run=$i count=$($benchmarkRuns.Count)"
            Update-StatePhase -State $state -Phase 'BENCH_DONE' -Detail "Run $i exit=$($runResult.exit_code)"
        }

        $state.benchmark_complete = $true
    }

    if (-not $state.restore_complete) {
        Restore-RegistryBaseline -Config $config -Baseline $state.baseline
        $state.restore_complete = $true
        Update-StatePhase -State $state -Phase 'RESTORE_DONE' -Detail 'Baseline restored.'

        if ($config.restart_mode -eq 'reboot' -and -not $state.after_restore_reboot_complete) {
            Start-RebootAndExit -State $state -Phase 'RESTART_AFTER_RESTORE'
        }
    }

    if ($config.restart_mode -eq 'reboot' -and $state.restore_complete -and -not $state.after_restore_reboot_complete) {
        $state.after_restore_reboot_complete = $true
        Update-StatePhase -State $state -Phase 'POST_REBOOT_AFTER_RESTORE' -Detail 'Resumed after restore reboot.'
    }

    $result = [ordered]@{
        test_id = $config.test_id
        registry_path = $config.registry_path
        value_name = $config.value_name
        candidate_value = $config.candidate_value
        baseline = $state.baseline
        restart_mode = $config.restart_mode
        idle_snapshot = $state.idle_snapshot
        benchmark_runs = $state['benchmark_runs']
        completed_at = (Get-Date).ToString('o')
    }
    Save-JsonFile -Path $script:ResultPath -InputObject $result
    Update-StatePhase -State $state -Phase 'COMPLETE' -Detail 'Validation run finished.'
} catch {
    $message = $_.Exception.Message
    if ($_.InvocationInfo) {
        $message = "$message @ $($_.InvocationInfo.PositionMessage)"
    }
    Ensure-StateList -State $state -Name 'errors'
    $errorList = $state['errors']
    if ($null -eq $errorList) {
        throw 'errors list is null after Ensure-StateList.'
    }
    [void]$errorList.Add($message)
    Update-StatePhase -State $state -Phase 'ERROR' -Detail $message
    throw
}
