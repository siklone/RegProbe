[CmdletBinding()]
param()

$script:RegProbeVmCredentialEnvUser = 'REGPROBE_VM_GUEST_USER'
$script:RegProbeVmCredentialEnvPassword = 'REGPROBE_VM_GUEST_PASSWORD'
$script:RegProbeVmCredentialEnvFile = 'REGPROBE_VM_CREDENTIAL_FILE'

function New-RegProbePlaintextCredential {
    param(
        [Parameter(Mandatory = $true)]
        [string]$UserName,
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $secure = ConvertTo-SecureString -String $Password -AsPlainText -Force
    return [pscredential]::new($UserName, $secure)
}

function Resolve-RegProbeVmCredential {
    param(
        [string]$GuestUser = 'Administrator',
        [string]$GuestPassword = '',
        [pscredential]$GuestCredential,
        [string]$CredentialFilePath = ''
    )

    if ($GuestCredential) {
        return $GuestCredential
    }

    if (-not [string]::IsNullOrWhiteSpace($GuestPassword)) {
        return New-RegProbePlaintextCredential -UserName $GuestUser -Password $GuestPassword
    }

    $envUserValue = [Environment]::GetEnvironmentVariable($script:RegProbeVmCredentialEnvUser)
    $envPasswordValue = [Environment]::GetEnvironmentVariable($script:RegProbeVmCredentialEnvPassword)
    $envCredentialFileValue = [Environment]::GetEnvironmentVariable($script:RegProbeVmCredentialEnvFile)
    $envUser = if ([string]::IsNullOrWhiteSpace($envUserValue)) { $GuestUser } else { $envUserValue }
    if (-not [string]::IsNullOrWhiteSpace($envPasswordValue)) {
        return New-RegProbePlaintextCredential -UserName $envUser -Password $envPasswordValue
    }

    $credentialPathCandidates = @(
        $CredentialFilePath,
        $envCredentialFileValue
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

    foreach ($candidate in $credentialPathCandidates) {
        $expanded = [Environment]::ExpandEnvironmentVariables($candidate)
        if (-not (Test-Path -LiteralPath $expanded)) {
            continue
        }

        $imported = Import-Clixml -LiteralPath $expanded
        if ($imported -is [pscredential]) {
            return $imported
        }

        throw "VM credential file did not contain a PSCredential: $expanded"
    }

    throw "Missing VM guest credential. Provide -GuestCredential, -GuestPassword, env:$script:RegProbeVmCredentialEnvPassword, or env:$script:RegProbeVmCredentialEnvFile."
}

function Get-RegProbeVmrunAuthArguments {
    param(
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential
    )

    return @('-gu', $Credential.UserName, '-gp', $Credential.GetNetworkCredential().Password)
}

function Format-RegProbeVmrunArgumentsForLog {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $formatted = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $Arguments.Count; $i++) {
        $current = [string]$Arguments[$i]
        if ($current -eq '-gp' -and ($i + 1) -lt $Arguments.Count) {
            $formatted.Add($current)
            $formatted.Add('<redacted>')
            $i++
            continue
        }
        $formatted.Add($current)
    }

    return [string]::Join(' ', $formatted)
}

function Invoke-RegProbeVmrun {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VmrunPath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    $output = & $VmrunPath @Arguments 2>&1 | Out-String
    if (-not $IgnoreExitCode -and $LASTEXITCODE -ne 0) {
        $displayArgs = Format-RegProbeVmrunArgumentsForLog -Arguments $Arguments
        throw "vmrun failed ($LASTEXITCODE): $displayArgs :: $($output.Trim())"
    }

    return $output.Trim()
}
