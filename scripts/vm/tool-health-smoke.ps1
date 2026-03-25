[CmdletBinding()]
param(
    [string]$OutputRoot = 'C:\Tools\ValidationController\smoke'
)

$ErrorActionPreference = 'Continue'

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\Tools\GhidraProjects' -Force | Out-Null

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    try {
        $payload = & $Action
        if ($payload -is [System.Collections.IDictionary]) {
            return $payload
        }

        if ($payload -is [bool]) {
            return [ordered]@{ success = $payload }
        }

        return [ordered]@{ success = $true; detail = [string]$payload }
    }
    catch {
        return [ordered]@{
            success = $false
            error = $_.Exception.Message
        }
    }
}

function Invoke-Capture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,

        [Parameter(Mandatory = $true)]
        [string]$StdoutPath,

        [switch]$IgnoreExitCode
    )

    $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru -Wait -NoNewWindow -RedirectStandardOutput $StdoutPath -RedirectStandardError ($StdoutPath + '.stderr.txt')
    if (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
        throw "$([IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode)"
    }

    return $proc.ExitCode
}

$toolMap = [ordered]@{
    procmon = 'C:\Tools\Sysinternals\Procmon64.exe'
    procmon_wrapper = 'C:\Tools\Scripts\procmon-safe.ps1'
    wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
    wpa = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpa.exe'
    xperf = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\xperf.exe'
    dotnet = 'C:\Tools\DotNetSDK\8.0.416\dotnet.exe'
    winsat = 'C:\Windows\System32\winsat.exe'
    diskspd = 'C:\Tools\Perf\diskspd.exe'
    ghidra_launcher = 'C:\Tools\Scripts\ghidra-headless.cmd'
}

$optionalToolMap = [ordered]@{
    ttd = 'C:\Tools\Debuggers\ttd.exe'
    windbg = 'C:\Tools\Debuggers\windbg.exe'
}

$result = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    machine = $env:COMPUTERNAME
    user = $env:USERNAME
    tools = [ordered]@{}
    optional_tools = [ordered]@{}
    smokes = [ordered]@{}
}

foreach ($entry in $toolMap.GetEnumerator()) {
    $result.tools[$entry.Key] = [ordered]@{
        path = $entry.Value
        exists = [bool](Test-Path $entry.Value)
    }
}

foreach ($entry in $optionalToolMap.GetEnumerator()) {
    $result.optional_tools[$entry.Key] = [ordered]@{
        path = $entry.Value
        exists = [bool](Test-Path $entry.Value)
    }
}

$aida = Get-ChildItem -Path 'C:\Tools' -Recurse -Filter 'aida64.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
$result.tools['aida64'] = [ordered]@{
    path = if ($aida) { $aida.FullName } else { '' }
    exists = [bool]$aida
}

$result.smokes['dotnet_info'] = Invoke-Step {
    if (-not (Test-Path $toolMap.dotnet)) {
        throw 'dotnet.exe not found'
    }

    $out = Join-Path $OutputRoot 'dotnet-info.txt'
    Invoke-Capture -FilePath $toolMap.dotnet -ArgumentList @('--info') -StdoutPath $out | Out-Null
    [ordered]@{ success = [bool](Test-Path $out); output = $out }
}

$result.smokes['procmon'] = Invoke-Step {
    if (-not (Test-Path $toolMap.procmon_wrapper)) {
        throw 'procmon-safe.ps1 not found'
    }

    $out = 'C:\Tools\Perf\Procmon\tool-health-procmon.pml'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $toolMap.procmon_wrapper -DurationSeconds 5 -MaxMegabytes 32 -OutputDirectory 'C:\Tools\Perf\Procmon' -OutputName 'tool-health-procmon.pml' | Out-Null
    [ordered]@{ success = [bool](Test-Path $out); output = $out }
}

$result.smokes['wpr'] = Invoke-Step {
    if (-not (Test-Path $toolMap.wpr)) {
        throw 'wpr.exe not found'
    }

    $etl = Join-Path $OutputRoot 'tool-health-wpr.etl'
    & $toolMap.wpr -cancel | Out-Null
    & $toolMap.wpr -start GeneralProfile -filemode | Out-Null
    Start-Sleep -Seconds 3
    & $toolMap.wpr -stop $etl | Out-Null
    [ordered]@{ success = [bool](Test-Path $etl); output = $etl }
}

$result.smokes['ghidra'] = Invoke-Step {
    if (-not (Test-Path $toolMap.ghidra_launcher)) {
        throw 'ghidra-headless.cmd not found'
    }

    $analyzeHeadless = Get-ChildItem -Path 'C:\Tools\Ghidra' -Recurse -Filter 'analyzeHeadless.bat' -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $analyzeHeadless) {
        throw 'analyzeHeadless.bat not found'
    }

    $stdout = Join-Path $OutputRoot 'ghidra-import.txt'
    $stderr = $stdout + '.stderr.txt'
    $projectRoot = 'C:\Tools\GhidraProjects'
    $smokeBinary = 'C:\Windows\System32\choice.exe'
    New-Item -ItemType Directory -Path $projectRoot -Force | Out-Null

    $proc = Start-Process -FilePath $analyzeHeadless.FullName `
        -ArgumentList @($projectRoot, 'ToolHealthProject', '-import', $smokeBinary, '-noanalysis', '-deleteProject') `
        -PassThru -Wait -WindowStyle Hidden `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr

    $stdoutText = if (Test-Path $stdout) { Get-Content -Path $stdout -Raw } else { '' }
    [ordered]@{
        success = ($proc.ExitCode -eq 0) -and [bool](Test-Path $stdout) -and ($stdoutText -match 'Import succeeded')
        output = $stdout
        exit_code = $proc.ExitCode
        target = $smokeBinary
    }
}

$result.smokes['winsat_cpu'] = Invoke-Step {
    if (-not (Test-Path $toolMap.winsat)) {
        throw 'winsat.exe not found'
    }

    $out = Join-Path $OutputRoot 'winsat-cpu.txt'
    Invoke-Capture -FilePath $toolMap.winsat -ArgumentList @('cpuformal') -StdoutPath $out -IgnoreExitCode | Out-Null
    [ordered]@{
        success = [bool](Test-Path $out)
        output = $out
    }
}

$result.smokes['winsat_mem'] = Invoke-Step {
    if (-not (Test-Path $toolMap.winsat)) {
        throw 'winsat.exe not found'
    }

    $out = Join-Path $OutputRoot 'winsat-mem.txt'
    Invoke-Capture -FilePath $toolMap.winsat -ArgumentList @('mem') -StdoutPath $out -IgnoreExitCode | Out-Null
    [ordered]@{
        success = [bool](Test-Path $out)
        output = $out
    }
}

$result.smokes['diskspd'] = Invoke-Step {
    if (-not (Test-Path $toolMap.diskspd)) {
        throw 'diskspd.exe not found'
    }

    $workDir = 'C:\Tools\DiskSpd\work'
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
    $out = Join-Path $OutputRoot 'diskspd.txt'
    Invoke-Capture -FilePath $toolMap.diskspd -ArgumentList @('-c32M', '-b64K', '-d5', '-o1', '-t1', '-Sh', '-L', (Join-Path $workDir 'tool-health.dat')) -StdoutPath $out | Out-Null
    [ordered]@{
        success = [bool](Test-Path $out)
        output = $out
    }
}

$result.smokes['aida64_visibility'] = [ordered]@{
    success = [bool]$aida
    output = if ($aida) { $aida.FullName } else { '' }
}

$resultPath = Join-Path $OutputRoot 'tool-health.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -Path $resultPath -Encoding UTF8
Write-Output $resultPath
