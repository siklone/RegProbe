[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$helperPath = Join-Path $repoRoot 'scripts\vm\test-vm-storage-health.ps1'
$wrapperPath = Join-Path $repoRoot 'registry-research-framework\tools\run-power-control-batch-mega-trigger-runtime-safe.ps1'
$tempRoot = Join-Path $env:TEMP 'RegProbeVmStorageHealthSmoke'

Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $healthyRoot = Join-Path $tempRoot 'healthy'
    New-Item -ItemType Directory -Path $healthyRoot -Force | Out-Null
    @'
ide0:0.present = "TRUE"
ide0:0.fileName = "disk.vmdk"
'@ | Set-Content -Path (Join-Path $healthyRoot 'Healthy.vmx') -Encoding ASCII
    @'
# Disk DescriptorFile
version=1
CID=fffffffe
parentCID=ffffffff
createType="monolithicFlat"
RW 8 FLAT "disk-flat.vmdk" 0
'@ | Set-Content -Path (Join-Path $healthyRoot 'disk.vmdk') -Encoding ASCII
    [IO.File]::WriteAllBytes((Join-Path $healthyRoot 'disk-flat.vmdk'), (1..128 | ForEach-Object { [byte]($_ % 256) }))

    $healthy = & $helperPath -VmPath (Join-Path $healthyRoot 'Healthy.vmx') -SkipEventLog
    if ($healthy.status -eq 'unsafe') {
        throw "Expected healthy or warning result for readable fake VM, got unsafe."
    }
    if (-not (@($healthy.referenced_files | Where-Object { $_.readable }).Count -ge 2)) {
        throw "Expected readable descriptor and extent references for healthy fake VM."
    }

    $brokenRoot = Join-Path $tempRoot 'broken'
    New-Item -ItemType Directory -Path $brokenRoot -Force | Out-Null
    @'
ide0:0.present = "TRUE"
ide0:0.fileName = "missing.vmdk"
'@ | Set-Content -Path (Join-Path $brokenRoot 'Broken.vmx') -Encoding ASCII

    $broken = & $helperPath -VmPath (Join-Path $brokenRoot 'Broken.vmx') -SkipEventLog
    if ($broken.status -ne 'unsafe') {
        throw "Expected unsafe result for broken fake VM, got $($broken.status)."
    }
    if (-not (@($broken.findings | Where-Object { $_.code -eq 'referenced-file-missing' }).Count -ge 1)) {
        throw 'Expected referenced-file-missing finding for broken fake VM.'
    }

    $wrapperOutputRoot = Join-Path $tempRoot 'wrapper-output'
    $wrapped = & $wrapperPath -VmPath (Join-Path $brokenRoot 'Broken.vmx') -CollectionMode operational -SkipEventLog -SkipRunner -OutputRoot $wrapperOutputRoot
    if ($wrapped.status -ne 'storage-unsafe') {
        throw "Expected wrapper to stop on storage-unsafe, got $($wrapped.status)."
    }

    Write-Host 'Invoke-VmStorageHealthSmoke.ps1 passed.'
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
