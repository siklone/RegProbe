[CmdletBinding()]
param(
    [ValidateSet('arm', 'collect')]
    [string]$Stage = 'arm',

    [string]$RegistryPath = '',
    [string]$ValueName = '',
    [string]$OutputName = 'wpr-boot-registry',
    [string]$OutputRoot = '',
    [string]$UploadBaseUrl = '',
    [string]$StateFile = '',
    [string[]]$MatchFragments = @(),
    [int]$WprTimeoutSeconds = 180,
    [int]$TracerptTimeoutSeconds = 180,
    [int]$UploadRetryCount = 20,
    [int]$UploadRetryDelaySeconds = 5
)

$ErrorActionPreference = 'Stop'

$wpr = 'C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe'
$tracerpt = 'C:\Windows\System32\tracerpt.exe'

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $Payload | ConvertTo-Json -Depth 10 | Set-Content -Path $Path -Encoding UTF8
}

function Get-BootTimeUtc {
    try {
        $boot = (Get-CimInstance Win32_OperatingSystem -ErrorAction Stop).LastBootUpTime
        return $boot.ToUniversalTime().ToString('o')
    }
    catch {
        return $null
    }
}

function Invoke-ArtifactUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RemoteName
    )

    if ([string]::IsNullOrWhiteSpace($UploadBaseUrl) -or -not (Test-Path -Path $Path -PathType Leaf)) {
        return $null
    }

    $targetUri = '{0}/{1}' -f $UploadBaseUrl.TrimEnd('/'), $RemoteName
    for ($attempt = 1; $attempt -le [Math]::Max($UploadRetryCount, 1); $attempt++) {
        try {
            Invoke-WebRequest -Method Put -Uri $targetUri -InFile $Path -UseBasicParsing | Out-Null
            return [ordered]@{
                path = $Path
                uri = $targetUri
                attempts = $attempt
            }
        }
        catch {
            if ($attempt -ge [Math]::Max($UploadRetryCount, 1)) {
                throw
            }
            Start-Sleep -Seconds ([Math]::Max($UploadRetryDelaySeconds, 1))
        }
    }

    return $null
}

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [int]$TimeoutSeconds = 0,
        [switch]$IgnoreExitCode
    )

    $stdoutPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-wpr-' + [Guid]::NewGuid().ToString('N') + '.stdout.txt')
    $stderrPath = Join-Path ([System.IO.Path]::GetTempPath()) ('regprobe-wpr-' + [Guid]::NewGuid().ToString('N') + '.stderr.txt')

    try {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $timedOut = $false
        if ($TimeoutSeconds -gt 0) {
            if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
                $timedOut = $true
                try {
                    $proc.Kill()
                }
                catch {
                }
                $proc.WaitForExit()
            }
        }
        else {
            $proc.WaitForExit()
        }
        $stdout = if (Test-Path $stdoutPath) { (Get-Content -Path $stdoutPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        $stderr = if (Test-Path $stderrPath) { (Get-Content -Path $stderrPath -Raw -ErrorAction SilentlyContinue) } else { '' }
        if ($timedOut) {
            if (-not $IgnoreExitCode) {
                throw "$([System.IO.Path]::GetFileName($FilePath)) timed out after $TimeoutSeconds second(s)"
            }
        }
        elseif (-not $IgnoreExitCode -and $proc.ExitCode -ne 0) {
            throw "$([System.IO.Path]::GetFileName($FilePath)) failed with exit code $($proc.ExitCode)"
        }

        return [ordered]@{
            exit_code = if ($timedOut) { -1 } else { $proc.ExitCode }
            timed_out = $timedOut
            stdout = ('{0}' -f $stdout).Trim()
            stderr = ('{0}' -f $stderr).Trim()
        }
    }
    finally {
        Remove-Item -Path $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path 'C:\RegProbe-Diag\wpr-boot-registry' $OutputName
}

Ensure-Directory -Path $OutputRoot

if ([string]::IsNullOrWhiteSpace($StateFile)) {
    $StateFile = Join-Path $OutputRoot 'state.json'
}

$summaryArmPath = Join-Path $OutputRoot 'summary-arm.json'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stagePath = Join-Path $OutputRoot 'stage.json'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.txt')

function Publish-Stage {
    param(
        [Parameter(Mandatory = $true)][string]$StageName,
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$Message = '',
        [hashtable]$Extra = @{}
    )

    $payload = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        output_name = $OutputName
        stage = $StageName
        status = $Status
        message = $Message
    }
    foreach ($entry in $Extra.GetEnumerator()) {
        $payload[$entry.Key] = $entry.Value
    }

    Write-JsonFile -Path $stagePath -Payload $payload
    try {
        Invoke-ArtifactUpload -Path $stagePath -RemoteName ('{0}-stage.json' -f $OutputName) | Out-Null
    }
    catch {
    }
}

if ($Stage -eq 'arm') {
    foreach ($path in @($summaryArmPath, $summaryPath, $stagePath, $etlPath, $csvPath, $hitsPath)) {
        Remove-Item -Path $path -Force -ErrorAction SilentlyContinue
    }

    $summary = [ordered]@{
        generated_utc = [DateTime]::UtcNow.ToString('o')
        stage = 'arm'
        registry_path = $RegistryPath
        value_name = $ValueName
        output_name = $OutputName
        status = 'ok'
        error_kind = $null
        error = $null
        wpr_exists = [bool](Test-Path $wpr)
        tracerpt_exists = [bool](Test-Path $tracerpt)
        before_boot_time_utc = $null
        etl_path = $etlPath
        state_file = $StateFile
    }

    try {
        Publish-Stage -StageName 'arm-start' -Status 'starting'
        $cancelBoot = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-cancelboot' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancelboot') -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.cancelboot = $cancelBoot

        $cancelLive = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-cancel' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-cancel') -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.cancel = $cancelLive

        $arm = if (Test-Path $wpr) {
            Publish-Stage -StageName 'arm-addboot' -Status 'starting'
            Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-addboot', 'Power', '-addboot', 'Registry', '-filemode', '-recordtempto', $OutputRoot) -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        } else {
            $null
        }
        $summary.arm = $arm

        $state = [ordered]@{
            generated_utc = [DateTime]::UtcNow.ToString('o')
            registry_path = $RegistryPath
            value_name = $ValueName
            output_name = $OutputName
            output_root = $OutputRoot
            upload_base_url = $UploadBaseUrl
            etl_path = $etlPath
            csv_path = $csvPath
            hits_path = $hitsPath
            before_boot_time_utc = Get-BootTimeUtc
            match_fragments = @($MatchFragments)
        }
        $summary.before_boot_time_utc = $state.before_boot_time_utc
        Write-JsonFile -Path $StateFile -Payload $state

        if ($arm) {
            if ($arm.timed_out) {
                $summary.status = 'error'
                $summary.error_kind = 'wpr-addboot-timeout'
                $summary.error = "wpr -addboot timed out after $WprTimeoutSeconds second(s)."
            }
            elseif ($arm.exit_code -ne 0) {
                $summary.status = 'error'
                $summary.error_kind = 'wpr-addboot-nonzero-exit'
                $armExitCode = if ($null -eq $arm.exit_code) { '<null>' } else { [string]$arm.exit_code }
                $summary.error = "wpr -addboot exited with code $armExitCode."
            }
        }
    }
    catch {
        $summary.status = 'error'
        $summary.error_kind = 'arm-exception'
        $summary.error = $_.Exception.Message
    }

    Write-JsonFile -Path $summaryArmPath -Payload $summary
    Invoke-ArtifactUpload -Path $summaryArmPath -RemoteName ('{0}-summary-arm.json' -f $OutputName) | Out-Null
    Publish-Stage -StageName 'arm-complete' -Status $summary.status -Message $summary.error
    Write-Output $summaryArmPath
    return
}

$state = Get-Content -Path $StateFile -Raw | ConvertFrom-Json
$RegistryPath = [string]$state.registry_path
$ValueName = [string]$state.value_name
$OutputName = [string]$state.output_name
$OutputRoot = [string]$state.output_root
$UploadBaseUrl = [string]$state.upload_base_url
$summaryArmPath = Join-Path $OutputRoot 'summary-arm.json'
$summaryPath = Join-Path $OutputRoot 'summary.json'
$stagePath = Join-Path $OutputRoot 'stage.json'
$etlPath = Join-Path $OutputRoot ($OutputName + '.etl')
$csvPath = Join-Path $OutputRoot ($OutputName + '.csv')
$hitsPath = Join-Path $OutputRoot ($OutputName + '.hits.txt')

$summary = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    stage = 'collect'
    registry_path = [string]$state.registry_path
    value_name = [string]$state.value_name
    output_name = [string]$state.output_name
    status = 'ok'
    error_kind = $null
    error = $null
    before_boot_time_utc = [string]$state.before_boot_time_utc
    after_boot_time_utc = Get-BootTimeUtc
    reboot_observed = $false
    wpr_exists = [bool](Test-Path $wpr)
    tracerpt_exists = [bool](Test-Path $tracerpt)
    stopboot = $null
    etl_path = $etlPath
    etl_exists = $false
    csv_path = $csvPath
    csv_exists = $false
    hit_line_count = 0
    fragment_hit_counts = [ordered]@{}
}

try {
    Publish-Stage -StageName 'collect-start' -Status 'starting'
    if ($summary.before_boot_time_utc -and $summary.after_boot_time_utc) {
        $summary.reboot_observed = ([datetimeoffset]::Parse($summary.after_boot_time_utc) -gt [datetimeoffset]::Parse($summary.before_boot_time_utc))
    }

    if (Test-Path $wpr) {
        Publish-Stage -StageName 'collect-stopboot' -Status 'starting'
        $stopResult = Invoke-NativeProcess -FilePath $wpr -ArgumentList @('-stopboot', $etlPath) -TimeoutSeconds $WprTimeoutSeconds -IgnoreExitCode
        $summary.stopboot = $stopResult
        if ($stopResult.timed_out) {
            $summary.status = 'error'
            $summary.error_kind = 'wpr-stopboot-timeout'
            $summary.error = "wpr -stopboot timed out after $WprTimeoutSeconds second(s)."
        }
        elseif ($stopResult.exit_code -ne 0) {
            $summary.status = 'error'
            $summary.error_kind = 'wpr-stopboot-nonzero-exit'
            $summary.error = "wpr -stopboot exited with code $($stopResult.exit_code)."
        }
    }

    $summary.etl_exists = [bool](Test-Path $etlPath)

    if ($summary.status -eq 'ok' -and $summary.etl_exists -and (Test-Path $tracerpt)) {
        Publish-Stage -StageName 'collect-tracerpt' -Status 'starting'
        $convertResult = Invoke-NativeProcess -FilePath $tracerpt -ArgumentList @($etlPath, '-o', $csvPath, '-of', 'CSV') -TimeoutSeconds $TracerptTimeoutSeconds -IgnoreExitCode
        $summary['tracerpt'] = $convertResult
        $summary.csv_exists = [bool](Test-Path $csvPath)
        if ($convertResult.timed_out) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-timeout'
            $summary.error = "tracerpt timed out after $TracerptTimeoutSeconds second(s)."
        }
        elseif ($convertResult.exit_code -ne 0) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-nonzero-exit'
            $summary.error = "tracerpt exited with code $($convertResult.exit_code)."
        }
        elseif (-not $summary.csv_exists) {
            $summary.status = 'error'
            $summary.error_kind = 'tracerpt-missing-csv'
            $summary.error = "tracerpt did not create $csvPath"
        }

        if ($summary.csv_exists) {
            $lines = Get-Content -Path $csvPath -ErrorAction SilentlyContinue
            $summary['csv_line_count'] = @($lines).Count
            $fragments = New-Object System.Collections.Generic.List[string]
            foreach ($fragment in @([string]$state.registry_path, [string]$state.value_name) + @($state.match_fragments)) {
                if (-not [string]::IsNullOrWhiteSpace($fragment) -and -not $fragments.Contains($fragment)) {
                    $fragments.Add($fragment)
                }
            }

            $hitLines = New-Object System.Collections.Generic.List[string]
            foreach ($fragment in $fragments) {
                $summary.fragment_hit_counts[$fragment] = 0
            }

            foreach ($line in $lines) {
                foreach ($fragment in $fragments) {
                    if ($line -like "*$fragment*") {
                        $summary.fragment_hit_counts[$fragment]++
                        $hitLines.Add($line)
                        break
                    }
                }
            }

            if ($hitLines.Count -gt 0) {
                $hitLines | Set-Content -Path $hitsPath -Encoding UTF8
            }
            $summary.hit_line_count = $hitLines.Count
            $summary['hits_path'] = $hitsPath
            $summary['hits_exists'] = [bool](Test-Path $hitsPath)
        }
    }
}
catch {
    $summary.status = 'error'
    $summary.error_kind = 'collect-exception'
    $summary.error = $_.Exception.Message
}

Write-JsonFile -Path $summaryPath -Payload $summary
Invoke-ArtifactUpload -Path $summaryPath -RemoteName ('{0}-summary.json' -f $OutputName) | Out-Null
if (Test-Path $hitsPath) {
    Invoke-ArtifactUpload -Path $hitsPath -RemoteName ('{0}.hits.txt' -f $OutputName) | Out-Null
}
Publish-Stage -StageName 'collect-complete' -Status $summary.status -Message $summary.error

Write-Output $summaryPath
