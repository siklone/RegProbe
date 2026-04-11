[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$VmPath,

    [int]$RecentEventWindowHours = 24,

    [switch]$SkipEventLog,

    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

$script:Findings = New-Object System.Collections.Generic.List[object]

function Add-Finding {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('warning', 'unsafe')]
        [string]$Severity,

        [Parameter(Mandatory = $true)]
        [string]$Code,

        [Parameter(Mandatory = $true)]
        [string]$Message,

        [string]$Path = ''
    )

    $script:Findings.Add([ordered]@{
            severity = $Severity
            code = $Code
            message = $Message
            path = $Path
        }) | Out-Null
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$InputObject,

        [int]$Depth = 10
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory,

        [Parameter(Mandatory = $true)]
        [string]$Candidate
    )

    if ([System.IO.Path]::IsPathRooted($Candidate)) {
        return [System.IO.Path]::GetFullPath($Candidate)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BaseDirectory $Candidate))
}

function Test-ReadableFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [int]$SampleBytes = 64
    )

    $result = [ordered]@{
        path = $Path
        exists = $false
        readable = $false
        size = $null
        sample_bytes = 0
        error = $null
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        $result.error = 'missing'
        return $result
    }

    $result.exists = $true
    try {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        $result.size = [int64]$item.Length
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            $buffer = New-Object byte[] $SampleBytes
            $read = $stream.Read($buffer, 0, $buffer.Length)
            $result.sample_bytes = [int]$read
            $result.readable = $true
        }
        finally {
            $stream.Dispose()
        }
    }
    catch {
        $result.error = $_.Exception.Message
    }

    return $result
}

function Get-VmxReferencedDisks {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VmxPath
    )

    $vmDirectory = Split-Path -Parent $VmxPath
    $references = New-Object System.Collections.Generic.List[object]
    $lines = Get-Content -LiteralPath $VmxPath -ErrorAction Stop
    foreach ($line in $lines) {
        if ($line -match 'fileName\s*=\s*"([^"]+\.vmdk)"') {
            $candidate = $Matches[1]
            $resolved = Resolve-AbsolutePath -BaseDirectory $vmDirectory -Candidate $candidate
            $references.Add([ordered]@{
                    kind = 'vmx-disk'
                    source = $VmxPath
                    path = $resolved
                }) | Out-Null
        }
    }

    return @($references | Sort-Object path -Unique)
}

function Get-VmdkDescriptorReferences {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DescriptorPath
    )

    if (-not (Test-Path -LiteralPath $DescriptorPath)) {
        return @()
    }

    $descriptorDirectory = Split-Path -Parent $DescriptorPath
    $references = New-Object System.Collections.Generic.List[object]
    try {
        $lines = Get-Content -LiteralPath $DescriptorPath -ErrorAction Stop
    }
    catch {
        return @()
    }

    foreach ($line in $lines) {
        if ($line -match 'parentFileNameHint\s*=\s*"([^"]+)"') {
            $resolvedParent = Resolve-AbsolutePath -BaseDirectory $descriptorDirectory -Candidate $Matches[1]
            $references.Add([ordered]@{
                    kind = 'vmdk-parent'
                    source = $DescriptorPath
                    path = $resolvedParent
                }) | Out-Null
        }

        if ($line -match '"([^"]+\.(?:vmdk|VMDK))"') {
            $resolvedExtent = Resolve-AbsolutePath -BaseDirectory $descriptorDirectory -Candidate $Matches[1]
            if ($resolvedExtent -ne $DescriptorPath) {
                $references.Add([ordered]@{
                        kind = 'vmdk-extent'
                        source = $DescriptorPath
                        path = $resolvedExtent
                    }) | Out-Null
            }
        }
    }

    return @($references | Sort-Object kind, path -Unique)
}

function Get-DriveContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $driveLetter = ([System.IO.Path]::GetPathRoot($Path)).TrimEnd('\').TrimEnd(':')
    $volume = $null
    $disk = $null

    try {
        $volume = Get-Volume -DriveLetter $driveLetter -ErrorAction Stop
    }
    catch {
    }

    if ($volume) {
        try {
            $partition = Get-Partition -DriveLetter $driveLetter -ErrorAction Stop
            $disk = Get-Disk -Number $partition.DiskNumber -ErrorAction Stop
        }
        catch {
        }
    }

    $volumeSummary = $null
    if ($volume) {
        $volumeSummary = [ordered]@{
            drive_letter = $volume.DriveLetter
            file_system = $volume.FileSystem
            health_status = $volume.HealthStatus
            size = [int64]$volume.Size
            size_remaining = [int64]$volume.SizeRemaining
        }
    }

    $diskSummary = $null
    if ($disk) {
        $diskSummary = [ordered]@{
            number = [int]$disk.Number
            friendly_name = $disk.FriendlyName
            serial_number = $disk.SerialNumber
            bus_type = [string]$disk.BusType
            health_status = [string]$disk.HealthStatus
        }
    }

    return [ordered]@{
        drive_letter = $driveLetter
        volume = $volumeSummary
        disk = $diskSummary
    }
}

function Get-RecentIoEvents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DriveLetter,

        [Parameter(Mandatory = $true)]
        [int]$WindowHours
    )

    if ($WindowHours -le 0) {
        return @()
    }

    $startTime = (Get-Date).AddHours(-1 * [Math]::Abs($WindowHours))
    $events = New-Object System.Collections.Generic.List[object]

    try {
        $ntfsEvents = Get-WinEvent -FilterHashtable @{
            LogName = 'System'
            StartTime = $startTime
        } -ErrorAction Stop |
            Where-Object {
                ($_.ProviderName -in @('Ntfs', 'Microsoft-Windows-Ntfs')) -and
                ($_.Message -match [regex]::Escape(('{0}:' -f $DriveLetter)))
            } |
            Select-Object -First 10 TimeCreated, Id, ProviderName, LevelDisplayName, Message

        foreach ($event in $ntfsEvents) {
            $events.Add($event) | Out-Null
        }
    }
    catch {
        Add-Finding -Severity 'warning' -Code 'event-log-query-failed' -Message $_.Exception.Message
    }

    try {
        $diskEvents = Get-WinEvent -FilterHashtable @{
            LogName = 'System'
            ProviderName = 'disk'
            StartTime = $startTime
        } -ErrorAction Stop |
            Where-Object { $_.Id -in @(7, 51, 129, 153, 157) } |
            Select-Object -First 10 TimeCreated, Id, ProviderName, LevelDisplayName, Message

        foreach ($event in $diskEvents) {
            $events.Add($event) | Out-Null
        }
    }
    catch {
        Add-Finding -Severity 'warning' -Code 'disk-event-query-failed' -Message $_.Exception.Message
    }

    return $events.ToArray()
}

$resolvedVmPath = [System.IO.Path]::GetFullPath($VmPath)
$vmxReadable = Test-ReadableFile -Path $resolvedVmPath
if (-not $vmxReadable.exists) {
    Add-Finding -Severity 'unsafe' -Code 'vmx-missing' -Message 'VMX file is missing.' -Path $resolvedVmPath
}
elseif (-not $vmxReadable.readable) {
    Add-Finding -Severity 'unsafe' -Code 'vmx-unreadable' -Message 'VMX file could not be read.' -Path $resolvedVmPath
}

$referencedFiles = New-Object System.Collections.Generic.List[object]
$seenPaths = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

if ($vmxReadable.readable) {
    foreach ($vmxReference in (Get-VmxReferencedDisks -VmxPath $resolvedVmPath)) {
        if ($seenPaths.Add($vmxReference.path)) {
            $fileInfo = Test-ReadableFile -Path $vmxReference.path
            $referencedFiles.Add([ordered]@{
                    kind = $vmxReference.kind
                    source = $vmxReference.source
                    path = $vmxReference.path
                    exists = $fileInfo.exists
                    readable = $fileInfo.readable
                    size = $fileInfo.size
                    sample_bytes = $fileInfo.sample_bytes
                    error = $fileInfo.error
                }) | Out-Null

            if (-not $fileInfo.exists) {
                Add-Finding -Severity 'unsafe' -Code 'referenced-file-missing' -Message 'VMX references a missing disk descriptor.' -Path $vmxReference.path
            }
            elseif (-not $fileInfo.readable) {
                Add-Finding -Severity 'unsafe' -Code 'referenced-file-unreadable' -Message 'VMX references a disk descriptor that could not be read.' -Path $vmxReference.path
            }
        }

        foreach ($descriptorReference in (Get-VmdkDescriptorReferences -DescriptorPath $vmxReference.path)) {
            if (-not $seenPaths.Add($descriptorReference.path)) {
                continue
            }

            $fileInfo = Test-ReadableFile -Path $descriptorReference.path
            $referencedFiles.Add([ordered]@{
                    kind = $descriptorReference.kind
                    source = $descriptorReference.source
                    path = $descriptorReference.path
                    exists = $fileInfo.exists
                    readable = $fileInfo.readable
                    size = $fileInfo.size
                    sample_bytes = $fileInfo.sample_bytes
                    error = $fileInfo.error
                }) | Out-Null

            if (-not $fileInfo.exists) {
                Add-Finding -Severity 'unsafe' -Code 'descriptor-reference-missing' -Message 'VMDK descriptor references a missing parent or extent file.' -Path $descriptorReference.path
            }
            elseif (-not $fileInfo.readable) {
                Add-Finding -Severity 'unsafe' -Code 'descriptor-reference-unreadable' -Message 'VMDK descriptor references a parent or extent file that could not be read.' -Path $descriptorReference.path
            }
        }
    }
}

$driveContext = Get-DriveContext -Path $resolvedVmPath
if ($driveContext.volume -and $driveContext.volume.health_status -and $driveContext.volume.health_status -ne 'Healthy') {
    Add-Finding -Severity 'unsafe' -Code 'volume-health-not-healthy' -Message ("Volume {0}: reports health status {1}." -f $driveContext.drive_letter, $driveContext.volume.health_status)
}

if ($driveContext.disk -and $driveContext.disk.bus_type -eq 'USB') {
    Add-Finding -Severity 'warning' -Code 'usb-backed-vm-volume' -Message ("VM is running from USB-backed storage on drive {0}:." -f $driveContext.drive_letter)
}

$recentEvents = @()
if (-not $SkipEventLog) {
    $recentEvents = Get-RecentIoEvents -DriveLetter $driveContext.drive_letter -WindowHours $RecentEventWindowHours
    $matchingNtfsEvents = @($recentEvents | Where-Object { $_.ProviderName -in @('Ntfs', 'Microsoft-Windows-Ntfs') })
    $matchingDiskEvents = @($recentEvents | Where-Object { $_.ProviderName -eq 'disk' })

    if (@($matchingNtfsEvents).Count -gt 0) {
        Add-Finding -Severity 'unsafe' -Code 'recent-ntfs-io-events' -Message ("Recent NTFS I/O errors were detected for drive {0}:." -f $driveContext.drive_letter)
    }

    if (@($matchingDiskEvents).Count -gt 0 -and $driveContext.disk -and $driveContext.disk.bus_type -eq 'USB') {
        Add-Finding -Severity 'unsafe' -Code 'recent-usb-disk-io-events' -Message ("Recent disk retry/I/O events were detected on USB-backed storage for drive {0}:." -f $driveContext.drive_letter)
    }
    elseif (@($matchingDiskEvents).Count -gt 0) {
        Add-Finding -Severity 'warning' -Code 'recent-disk-io-events' -Message 'Recent generic disk retry/I/O events were detected on the host.'
    }
}

$status = 'healthy'
$unsafeFindingCount = ($script:Findings | Where-Object { $_.severity -eq 'unsafe' } | Measure-Object).Count
$totalFindingCount = $script:Findings.Count
if ($unsafeFindingCount -gt 0) {
    $status = 'unsafe'
}
elseif ($totalFindingCount -gt 0) {
    $status = 'warning'
}

$vmDirectory = Split-Path -Parent $resolvedVmPath
$recentEventSample = @($recentEvents | Select-Object -First 10)
$referencedFileArray = $referencedFiles.ToArray()
$findingArray = $script:Findings.ToArray()

$result = [ordered]@{}
$result['generated_utc'] = [DateTime]::UtcNow.ToString('o')
$result['vm_path'] = $resolvedVmPath
$result['vm_directory'] = $vmDirectory
$result['status'] = $status
$result['drive_context'] = $driveContext
$result['referenced_files'] = $referencedFileArray
$result['findings'] = $findingArray
$result['recent_events'] = $recentEventSample

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-JsonFile -Path $OutputPath -InputObject $result
}

$result
