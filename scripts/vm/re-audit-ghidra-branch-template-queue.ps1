[CmdletBinding()]
param(
    [string]$QueuePath = '',
    [string]$VmPath = 'H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx',
    [string]$VmrunPath = 'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    [string]$RunnerPath = '',
    [int]$MaxArtifacts = 0,
    [string]$OutputFile = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($RunnerPath)) {
    $RunnerPath = Join-Path $repoRoot 'scripts\vm\run-ghidra-symbolized-branch-probe.ps1'
}
if ([string]::IsNullOrWhiteSpace($QueuePath)) {
    $QueuePath = Join-Path $repoRoot 'registry-research-framework\audit\static-evidence-v32-branch-template-missing-20260401.json'
}
if ([string]::IsNullOrWhiteSpace($OutputFile)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputFile = Join-Path $repoRoot ("registry-research-framework\audit\ghidra-branch-template-reaudit-$stamp.json")
}

function Invoke-Vmrun {
    param(
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    if (-not $IgnoreExitCode -and $exitCode -ne 0) {
        throw "vmrun failed ($exitCode): $($output.Trim())"
    }
    return [pscustomobject]@{
        exit_code = $exitCode
        output = $output.Trim()
    }
}

function Wait-GuestReady {
    param([int]$TimeoutSeconds = 240)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $running = Invoke-Vmrun -Arguments @('-T', 'ws', 'list') -IgnoreExitCode
        if ($running.output -notmatch [regex]::Escape($VmPath)) {
            Invoke-Vmrun -Arguments @('-T', 'ws', 'start', $VmPath, 'nogui') -IgnoreExitCode | Out-Null
            Start-Sleep -Seconds 5
        }

        $tools = Invoke-Vmrun -Arguments @('-T', 'ws', 'checkToolsState', $VmPath) -IgnoreExitCode
        if ($tools.output -match 'running|installed') {
            return [pscustomobject]@{
                vm_running = $true
                tools_state = $tools.output
            }
        }
        Start-Sleep -Seconds 5
    } while ((Get-Date) -lt $deadline)

    throw "VM guest did not reach a usable VMware Tools state within $TimeoutSeconds seconds."
}

function Parse-ArtifactMetadata {
    param([string]$MarkdownPath)

    $text = Get-Content -Path $MarkdownPath -Raw
    $programMatch = [regex]::Match($text, '(?m)^- Program: `([^`]+)`')
    $probeMatch = [regex]::Match($text, '(?m)^- Probe: `([^`]+)`')
    $patternsLineMatch = [regex]::Match($text, '(?m)^- Patterns: (.+)$')
    if (-not $programMatch.Success -or -not $probeMatch.Success -or -not $patternsLineMatch.Success) {
        throw "Failed to parse Program/Probe/Patterns from $MarkdownPath"
    }

    $program = $programMatch.Groups[1].Value.Trim()
    if ($program.StartsWith('/')) {
        $program = $program.TrimStart('/')
    }
    $program = $program -replace '/', '\'
    if ($program -notmatch '^[A-Za-z]:\\' -and $program -notmatch '\\') {
        $program = Join-Path 'C:\Windows\System32' $program
    }

    $patterns = [regex]::Matches($patternsLineMatch.Groups[1].Value, '`([^`]+)`') | ForEach-Object {
        $_.Groups[1].Value.Trim()
    } | Where-Object { $_ }

    [pscustomobject]@{
        target_binary = $program
        probe = $probeMatch.Groups[1].Value.Trim()
        patterns = @($patterns)
    }
}

function Resolve-LatestProbeRoot {
    param(
        [string]$OutputName,
        [string]$HostRoot = 'H:\Temp\vm-tooling-staging\ghidra-v32-probes'
    )

    $match = Get-ChildItem -Path $HostRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "$OutputName-*" } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if (-not $match) {
        throw "Could not locate host probe output for $OutputName under $HostRoot"
    }
    return $match.FullName
}

$queue = Get-Content -Path $QueuePath -Raw | ConvertFrom-Json
$entries = @($queue.entries)
if ($MaxArtifacts -gt 0) {
    $entries = @($entries | Select-Object -First $MaxArtifacts)
}

$results = New-Object System.Collections.Generic.List[object]
$index = 0
foreach ($entry in $entries) {
    $index += 1
    try {
        $artifactPath = Join-Path $repoRoot $entry.artifact
        $artifactDir = Split-Path -Parent $artifactPath
        $metadata = Parse-ArtifactMetadata -MarkdownPath $artifactPath
        $runName = "{0}-branch-template-refresh" -f $metadata.probe

        $shellBefore = Wait-GuestReady
        $runnerOutput = & $RunnerPath `
            -VmPath $VmPath `
            -VmrunPath $VmrunPath `
            -TargetBinary $metadata.target_binary `
            -OutputName $runName `
            -Patterns $metadata.patterns
        $shellAfter = Wait-GuestReady

        $hostProbeRoot = Resolve-LatestProbeRoot -OutputName $runName
        foreach ($fileName in @('evidence.json', 'ghidra-matches.md', 'ghidra-run.log')) {
            $source = Join-Path $hostProbeRoot $fileName
            $destination = Join-Path $artifactDir $fileName
            if (Test-Path $source) {
                Copy-Item -Path $source -Destination $destination -Force
            }
        }

        $legacyMarkdown = Join-Path $artifactDir ("{0}.md" -f $metadata.probe)
        if ((Test-Path $legacyMarkdown) -and (Test-Path (Join-Path $artifactDir 'ghidra-matches.md'))) {
            Copy-Item -Path (Join-Path $artifactDir 'ghidra-matches.md') -Destination $legacyMarkdown -Force
        }

        $evidencePayload = Get-Content -Path (Join-Path $artifactDir 'evidence.json') -Raw | ConvertFrom-Json
        $results.Add([pscustomobject]@{
            index = $index
            artifact = $entry.artifact
            target_binary = $metadata.target_binary
            probe = $metadata.probe
            patterns = $metadata.patterns
            host_probe_root = $hostProbeRoot
            status = $evidencePayload.status
            pdb_loaded = $evidencePayload.pdb_loaded
            match_count = @($evidencePayload.matches).Count
            shell_before = $shellBefore
            shell_after = $shellAfter
            runner_output = $runnerOutput
        })
    }
    catch {
        $results.Add([pscustomobject]@{
            index = $index
            artifact = $entry.artifact
            status = 'failed'
            error = $_.Exception.Message
        })
    }
}

$payload = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    queue_path = $QueuePath
    processed_count = $results.Count
    entries = $results
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputFile) -Force | Out-Null
$payload | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputFile -Encoding UTF8
$payload | ConvertTo-Json -Depth 8
