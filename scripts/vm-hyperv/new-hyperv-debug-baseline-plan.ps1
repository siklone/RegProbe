[CmdletBinding()]
param(
    [string]$FeasibilityPath = '',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $InputObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-RepoRelativeOrOriginalPath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$CandidatePath
    )

    $baseResolved = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\')
    $candidateResolved = [System.IO.Path]::GetFullPath($CandidatePath)

    if ($candidateResolved.StartsWith($baseResolved, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $candidateResolved.Substring($baseResolved.Length).TrimStart('\')
        return $relative -replace '\\', '/'
    }

    return $CandidatePath
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$configPath = Join-Path $repoRoot 'registry-research-framework\config\debug-environments.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$hyperVConfig = @($config.environments | Where-Object { $_.id -eq 'hyperv-debug' }) | Select-Object -First 1

if ($null -eq $hyperVConfig) {
    throw "Hyper-V debug environment config missing from $configPath"
}

$feasibility = $null
if (-not [string]::IsNullOrWhiteSpace($FeasibilityPath) -and (Test-Path -LiteralPath $FeasibilityPath)) {
    $feasibility = Get-Content -LiteralPath $FeasibilityPath -Raw | ConvertFrom-Json
}

$status = if ($null -ne $feasibility -and $feasibility.hyperv.supported_now) {
    'ready-to-provision'
}
else {
    'blocked-prereqs'
}

$plan = [ordered]@{
    generated_utc = [DateTime]::UtcNow.ToString('o')
    status = $status
    debug_environment = 'hyperv'
    debug_vm_name = [string]$hyperVConfig.vm_name
    debug_transport = [string]$hyperVConfig.transport_candidates[0]
    debug_transport_candidates = @($hyperVConfig.transport_candidates)
    debug_baseline = [string]$hyperVConfig.baseline_name
    transport_reliability = 'low'
    reproducible_runs = 0
    vm_role = [string]$hyperVConfig.role
    generation = [int]$hyperVConfig.generation
    storage_root = [string]$hyperVConfig.default_storage_root
    storage_root_resolution = 'expand-at-provision-time'
    required_features = @(
        'Microsoft-Hyper-V',
        'Microsoft-Hyper-V-Management-PowerShell'
    )
    checkpoint_contract = @($hyperVConfig.checkpoint_contract)
    provisioning_steps = @(
        'Enable Microsoft-Hyper-V and Microsoft-Hyper-V-Management-PowerShell on the host.',
        'Reboot the host so Hyper-V cmdlets and services are fully available.',
        'Create a Generation 2 VM named RegProbe-Debug-HyperV under the configured storage root.',
        'Keep the VM debugger-first and minimal; do not reuse the runtime VMware scratch surface.',
        'Enable kernel debugging and record a clean checkpoint plus debug-enabled checkpoint.',
        'Bring up serial transport first; if it is still unreliable, evaluate KDNET as the fallback transport.',
        'Do not run single-key arbiter semantics until transport smoke is reproducible in two consecutive runs.'
    )
    vm_role_split = [ordered]@{
        vmware = 'runtime_research'
        hyperv = 'debug_arbiter_only'
    }
    feasibility_ref = if ($null -ne $feasibility) { Get-RepoRelativeOrOriginalPath -BasePath $repoRoot -CandidatePath $FeasibilityPath } else { $null }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-JsonFile -Path $OutputPath -InputObject $plan
}

$plan | ConvertTo-Json -Depth 10
