[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BeforeFile,
    [Parameter(Mandatory = $true)]
    [string]$AfterFile,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile,
    [int]$MaxEntriesPerSection = 200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-AllText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $reader = [System.IO.StreamReader]::new($Path, [System.Text.Encoding]::UTF8, $true)
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Get-NormalizedLines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return @()
    }

    return ($Text -replace "`r`n", "`n" -replace "`r", "`n") -split "`n"
}

function Join-RegistryContinuationLines {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    $result = New-Object System.Collections.Generic.List[string]
    $pending = $null

    foreach ($rawLine in $Lines) {
        $line = $rawLine

        if ($null -ne $pending) {
            $line = $pending + ($line -replace '^\s+', '')
            $pending = $null
        }

        if ($line -match '\\\s*$') {
            $pending = $line -replace '\\\s*$', ''
            continue
        }

        $result.Add($line)
    }

    if ($null -ne $pending) {
        $result.Add($pending)
    }

    return $result
}

function Test-IsRegistryExportText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    return $Text -match 'Windows\s+Registry\s+Editor\s+Version' -or
        $Text -match '(?m)^\s*\[(?:-)?HKEY_'
}

function Test-IsRegistryDumpText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    return $Text -match '(?m)^\s*HKEY_[A-Z_]+\\'
}

function Convert-RegistryValueName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    if ($Token -eq '@') {
        return '(Default)'
    }

    if ($Token.Length -ge 2 -and $Token[0] -eq '"' -and $Token[$Token.Length - 1] -eq '"') {
        $Token = $Token.Substring(1, $Token.Length - 2)
    }

    return $Token -replace '\\"', '"' -replace '\\\\', '\'
}

function Convert-RegistryData {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RawData
    )

    $trimmed = $RawData.Trim()

    if ($trimmed -eq '-') {
        return [pscustomobject]@{
            ValueType = 'deleted'
            DataText  = '(deleted)'
        }
    }

    if ($trimmed -match '^"(.*)"$') {
        $value = $Matches[1] -replace '\\"', '"' -replace '\\\\', '\'
        return [pscustomobject]@{
            ValueType = 'string'
            DataText  = $value
        }
    }

    if ($trimmed -match '^(?<type>hex(?:\([0-9a-fA-F]+\))?|dword|qword):(?<data>.*)$') {
        return [pscustomobject]@{
            ValueType = $Matches['type'].ToLowerInvariant()
            DataText  = $Matches['data'].Trim().ToLowerInvariant()
        }
    }

    return [pscustomobject]@{
        ValueType = 'raw'
        DataText  = $trimmed
    }
}

function Get-ValueEntryId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$KeyPath,
        [Parameter(Mandatory = $true)]
        [string]$ValueName
    )

    return "$KeyPath`n$ValueName"
}

function Parse-RegistryExport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $lines = Join-RegistryContinuationLines -Lines (Get-NormalizedLines -Text $Text)
    $keys = @{}
    $values = @{}
    $currentKey = $null

    foreach ($rawLine in $lines) {
        $line = $rawLine.Trim()

        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line -match '^(?:Windows Registry Editor Version|REGEDIT4)$') {
            continue
        }

        if ($line.StartsWith(';') -or $line.StartsWith('#')) {
            continue
        }

        if ($line -match '^\[(?<key>.+)\]$') {
            $currentKey = $Matches['key']
            $isDeletedKey = $false

            if ($currentKey.StartsWith('-')) {
                $currentKey = $currentKey.Substring(1)
                $isDeletedKey = $true
            }

            $keys[$currentKey] = [pscustomobject]@{
                KeyPath   = $currentKey
                IsDeleted = $isDeletedKey
            }
            continue
        }

        if ($null -eq $currentKey) {
            continue
        }

        if ($line -notmatch '^(?<name>@|"(?:[^"\\]|\\.)*")=(?<data>.*)$') {
            continue
        }

        $valueName = Convert-RegistryValueName -Token $Matches['name']
        $data = Convert-RegistryData -RawData $Matches['data']
        $entryId = Get-ValueEntryId -KeyPath $currentKey -ValueName $valueName

        $values[$entryId] = [pscustomobject]@{
            KeyPath   = $currentKey
            ValueName = $valueName
            ValueType = $data.ValueType
            DataText  = $data.DataText
        }
    }

    return [pscustomobject]@{
        Keys   = $keys
        Values = $values
    }
}

function Parse-RegistryDumpText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $lines = Get-NormalizedLines -Text $Text
    $keys = @{}
    $values = @{}
    $currentKey = $null

    foreach ($rawLine in $lines) {
        $line = $rawLine.TrimEnd()
        $trimmed = $line.Trim()

        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        if ($trimmed -like 'Filtered host-side registry review*' -or
            $trimmed -like 'Probe folder:*' -or
            $trimmed -like 'Source ETL placeholder:*' -or
            $trimmed -like 'Parser:*' -or
            $trimmed -like 'Summary counts*' -or
            $trimmed -like 'Conclusion*' -or
            $trimmed -like 'Representative events*' -or
            $trimmed -like '[*]') {
            continue
        }

        if ($trimmed -match '^HKEY_[A-Z_]+\\') {
            $currentKey = $trimmed
            $keys[$currentKey] = [pscustomobject]@{
                KeyPath   = $currentKey
                IsDeleted = $false
            }
            continue
        }

        if ($null -eq $currentKey) {
            continue
        }

        if ($rawLine -notmatch '^\s{2,}') {
            continue
        }

        if ($trimmed -notmatch '^(?<name>\S.*?)\s{2,}(?<type>REG_[A-Z0-9_]+)\s{2,}(?<data>.*)$') {
            continue
        }

        $valueName = $Matches['name'].Trim()
        $valueType = $Matches['type'].Trim().ToLowerInvariant()
        $dataText = $Matches['data'].Trim()
        $entryId = Get-ValueEntryId -KeyPath $currentKey -ValueName $valueName

        $values[$entryId] = [pscustomobject]@{
            KeyPath   = $currentKey
            ValueName = $valueName
            ValueType = $valueType
            DataText  = $dataText
        }
    }

    return [pscustomobject]@{
        Keys   = $keys
        Values = $values
    }
}

function Get-LineCountMap {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    $map = @{}

    foreach ($line in $Lines) {
        if ($map.ContainsKey($line)) {
            $map[$line] += 1
        }
        else {
            $map[$line] = 1
        }
    }

    return $map
}

function Test-IsNoiseLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Line
    )

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $true
    }

    return $Line -match '(?i)\b(NO MORE ENTRIES|NAME NOT FOUND|BUFFER OVERFLOW|KEY DELETED|PATH NOT FOUND|END OF FILE)\b'
}

function Get-LineSummaryDiff {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BeforeText,
        [Parameter(Mandatory = $true)]
        [string]$AfterText
    )

    $beforeLinesAll = Get-NormalizedLines -Text $BeforeText
    $afterLinesAll = Get-NormalizedLines -Text $AfterText
    $beforeNoiseCount = @($beforeLinesAll | Where-Object { Test-IsNoiseLine -Line $_ }).Count
    $afterNoiseCount = @($afterLinesAll | Where-Object { Test-IsNoiseLine -Line $_ }).Count
    $beforeLines = @($beforeLinesAll | Where-Object { -not (Test-IsNoiseLine -Line $_) })
    $afterLines = @($afterLinesAll | Where-Object { -not (Test-IsNoiseLine -Line $_) })
    $beforeMap = Get-LineCountMap -Lines $beforeLines
    $afterMap = Get-LineCountMap -Lines $afterLines

    $added = New-Object System.Collections.Generic.List[object]
    $removed = New-Object System.Collections.Generic.List[object]
    $allLines = @($beforeMap.Keys + $afterMap.Keys | Sort-Object -Unique)

    foreach ($line in $allLines) {
        $beforeCount = if ($beforeMap.ContainsKey($line)) { [int]$beforeMap[$line] } else { 0 }
        $afterCount = if ($afterMap.ContainsKey($line)) { [int]$afterMap[$line] } else { 0 }

        if ($afterCount -gt $beforeCount) {
            $added.Add([pscustomobject]@{
                Line  = $line
                Count = $afterCount - $beforeCount
            })
        }

        if ($beforeCount -gt $afterCount) {
            $removed.Add([pscustomobject]@{
                Line  = $line
                Count = $beforeCount - $afterCount
            })
        }
    }

    return [pscustomobject]@{
        BeforeLineCount    = $beforeLines.Count
        AfterLineCount     = $afterLines.Count
        IgnoredBeforeNoise = $beforeNoiseCount
        IgnoredAfterNoise  = $afterNoiseCount
        Added              = $added
        Removed            = $removed
    }
}

function Shorten-Text {
    param(
        [AllowNull()]
        [string]$Text,
        [int]$MaxLength = 120
    )

    if ($null -eq $Text) {
        return ''
    }

    if ($Text.Length -le $MaxLength) {
        return $Text
    }

    return $Text.Substring(0, $MaxLength - 3) + '...'
}

function Add-Section {
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.StringBuilder]$Builder,
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [Parameter(Mandatory = $true)]
        [object[]]$Items,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Formatter,
        [int]$Limit = 200
    )

    [void]$Builder.AppendLine()
    [void]$Builder.AppendLine($Title)

    if ($Items.Count -eq 0) {
        [void]$Builder.AppendLine('- none')
        return
    }

    $displayCount = [Math]::Min($Items.Count, $Limit)
    for ($i = 0; $i -lt $displayCount; $i++) {
        [void]$Builder.AppendLine((& $Formatter $Items[$i]))
    }

    if ($Items.Count -gt $Limit) {
        [void]$Builder.AppendLine(("... {0} more omitted" -f ($Items.Count - $Limit)))
    }
}

function Format-ValueEntry {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Entry
    )

    return "- [{0}] {1} | {2} | {3}" -f
        $Entry.KeyPath,
        $Entry.ValueName,
        $Entry.ValueType,
        (Shorten-Text -Text $Entry.DataText)
}

function Format-ModifiedValueEntry {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Entry
    )

    return "- [{0}] {1} | {2}:{3} -> {4}:{5}" -f
        $Entry.KeyPath,
        $Entry.ValueName,
        $Entry.BeforeType,
        (Shorten-Text -Text $Entry.BeforeData),
        $Entry.AfterType,
        (Shorten-Text -Text $Entry.AfterData)
}

$beforeText = Read-AllText -Path $BeforeFile
$afterText = Read-AllText -Path $AfterFile

$beforeIsRegistryExport = Test-IsRegistryExportText -Text $beforeText
$afterIsRegistryExport = Test-IsRegistryExportText -Text $afterText
$beforeIsRegistryDump = Test-IsRegistryDumpText -Text $beforeText
$afterIsRegistryDump = Test-IsRegistryDumpText -Text $afterText
$beforeIsSemanticRegistry = $beforeIsRegistryExport -or $beforeIsRegistryDump
$afterIsSemanticRegistry = $afterIsRegistryExport -or $afterIsRegistryDump
$builder = New-Object System.Text.StringBuilder

[void]$builder.AppendLine('Registry sideeffect diff')
[void]$builder.AppendLine(("Before: {0}" -f $BeforeFile))
[void]$builder.AppendLine(("After:  {0}" -f $AfterFile))
[void]$builder.AppendLine(("Max entries per section: {0}" -f $MaxEntriesPerSection))

if ($beforeIsSemanticRegistry -and $afterIsSemanticRegistry) {
    $beforeFormat = if ($beforeIsRegistryExport) { 'registry-export' } else { 'registry-dump-text' }
    $afterFormat = if ($afterIsRegistryExport) { 'registry-export' } else { 'registry-dump-text' }
    $before = if ($beforeIsRegistryExport) { Parse-RegistryExport -Text $beforeText } else { Parse-RegistryDumpText -Text $beforeText }
    $after = if ($afterIsRegistryExport) { Parse-RegistryExport -Text $afterText } else { Parse-RegistryDumpText -Text $afterText }

    $addedKeys = New-Object System.Collections.Generic.List[object]
    $removedKeys = New-Object System.Collections.Generic.List[object]
    $addedValues = New-Object System.Collections.Generic.List[object]
    $removedValues = New-Object System.Collections.Generic.List[object]
    $modifiedValues = New-Object System.Collections.Generic.List[object]
    $allKeys = @($before.Keys.Keys + $after.Keys.Keys | Sort-Object -Unique)
    $allValueIds = @($before.Values.Keys + $after.Values.Keys | Sort-Object -Unique)

    foreach ($keyPath in $allKeys) {
        $inBefore = $before.Keys.ContainsKey($keyPath)
        $inAfter = $after.Keys.ContainsKey($keyPath)

        if ($inAfter -and -not $inBefore) {
            $addedKeys.Add($after.Keys[$keyPath])
        }
        elseif ($inBefore -and -not $inAfter) {
            $removedKeys.Add($before.Keys[$keyPath])
        }
    }

    foreach ($valueId in $allValueIds) {
        $inBefore = $before.Values.ContainsKey($valueId)
        $inAfter = $after.Values.ContainsKey($valueId)

        if ($inAfter -and -not $inBefore) {
            $addedValues.Add($after.Values[$valueId])
            continue
        }

        if ($inBefore -and -not $inAfter) {
            $removedValues.Add($before.Values[$valueId])
            continue
        }

        $beforeValue = $before.Values[$valueId]
        $afterValue = $after.Values[$valueId]

        if ($beforeValue.ValueType -ne $afterValue.ValueType -or $beforeValue.DataText -ne $afterValue.DataText) {
            $modifiedValues.Add([pscustomobject]@{
                KeyPath    = $afterValue.KeyPath
                ValueName  = $afterValue.ValueName
                BeforeType = $beforeValue.ValueType
                BeforeData = $beforeValue.DataText
                AfterType  = $afterValue.ValueType
                AfterData  = $afterValue.DataText
            })
        }
    }

    $unchangedValues = $allValueIds.Count - $addedValues.Count - $removedValues.Count - $modifiedValues.Count

    [void]$builder.AppendLine(("Detected format: semantic-registry ({0} -> {1})" -f $beforeFormat, $afterFormat))
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('Summary counts')
    [void]$builder.AppendLine(("- before_keys: {0}" -f $before.Keys.Count))
    [void]$builder.AppendLine(("- after_keys: {0}" -f $after.Keys.Count))
    [void]$builder.AppendLine(("- added_keys: {0}" -f $addedKeys.Count))
    [void]$builder.AppendLine(("- removed_keys: {0}" -f $removedKeys.Count))
    [void]$builder.AppendLine(("- before_values: {0}" -f $before.Values.Count))
    [void]$builder.AppendLine(("- after_values: {0}" -f $after.Values.Count))
    [void]$builder.AppendLine(("- added_values: {0}" -f $addedValues.Count))
    [void]$builder.AppendLine(("- removed_values: {0}" -f $removedValues.Count))
    [void]$builder.AppendLine(("- modified_values: {0}" -f $modifiedValues.Count))
    [void]$builder.AppendLine(("- unchanged_values: {0}" -f $unchangedValues))

    Add-Section -Builder $builder -Title 'Added keys' -Items @($addedKeys | Sort-Object KeyPath) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        "- [{0}]" -f $item.KeyPath
    }

    Add-Section -Builder $builder -Title 'Removed keys' -Items @($removedKeys | Sort-Object KeyPath) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        "- [{0}]" -f $item.KeyPath
    }

    Add-Section -Builder $builder -Title 'Added values' -Items @($addedValues | Sort-Object KeyPath, ValueName) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        Format-ValueEntry -Entry $item
    }

    Add-Section -Builder $builder -Title 'Removed values' -Items @($removedValues | Sort-Object KeyPath, ValueName) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        Format-ValueEntry -Entry $item
    }

    Add-Section -Builder $builder -Title 'Modified values' -Items @($modifiedValues | Sort-Object KeyPath, ValueName) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        Format-ModifiedValueEntry -Entry $item
    }
}
else {
    $lineDiff = Get-LineSummaryDiff -BeforeText $beforeText -AfterText $afterText
    $addedLineCount = ($lineDiff.Added | Measure-Object -Property Count -Sum).Sum
    $removedLineCount = ($lineDiff.Removed | Measure-Object -Property Count -Sum).Sum

    if ($null -eq $addedLineCount) {
        $addedLineCount = 0
    }

    if ($null -eq $removedLineCount) {
        $removedLineCount = 0
    }

    [void]$builder.AppendLine('Detected format: generic-text')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('Summary counts')
    [void]$builder.AppendLine(("- before_lines: {0}" -f $lineDiff.BeforeLineCount))
    [void]$builder.AppendLine(("- after_lines: {0}" -f $lineDiff.AfterLineCount))
    [void]$builder.AppendLine(("- ignored_before_noise_lines: {0}" -f $lineDiff.IgnoredBeforeNoise))
    [void]$builder.AppendLine(("- ignored_after_noise_lines: {0}" -f $lineDiff.IgnoredAfterNoise))
    [void]$builder.AppendLine(("- added_lines: {0}" -f $addedLineCount))
    [void]$builder.AppendLine(("- removed_lines: {0}" -f $removedLineCount))
    [void]$builder.AppendLine('- note: semantic registry diff was skipped because one or both inputs do not look like supported registry exports or registry dump text.')
    [void]$builder.AppendLine('- note: common registry noise lines are excluded from the generic text summary.')

    Add-Section -Builder $builder -Title 'Added line samples' -Items @($lineDiff.Added | Sort-Object Count -Descending, Line) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        "- ({0}x) {1}" -f $item.Count, (Shorten-Text -Text $item.Line)
    }

    Add-Section -Builder $builder -Title 'Removed line samples' -Items @($lineDiff.Removed | Sort-Object Count -Descending, Line) -Limit $MaxEntriesPerSection -Formatter {
        param($item)
        "- ({0}x) {1}" -f $item.Count, (Shorten-Text -Text $item.Line)
    }
}

[System.IO.File]::WriteAllText($OutputFile, $builder.ToString(), [System.Text.Encoding]::UTF8)
