# Self-contained PowerShell script you can paste into the VM (not required to be in the repo).
# Behavior:
# - Try MinGit (no admin) and fall back to Git for Windows silent installer (admin may be required)
# - Clone the configured RegProbe repo/branch into the configured work directory
# - Run the configured dotnet build and dotnet test targets
# - Write logs under C:\work: git_*.log, build.log, test.log, dotnet-info.log
# - Print exit codes and first critical error lines when failures occur
#
# Optional environment variables:
# - REGPROBE_REPO_URL
# - REGPROBE_REPO_BRANCH
# - REGPROBE_VM_WORKDIR
# - REGPROBE_REPO_DIR
# - REGPROBE_DOTNET_COMMAND
# - REGPROBE_BUILD_CONFIGURATION
# - REGPROBE_TEST_PROJECT

$ErrorActionPreference = 'Continue'

function Write-Log {
    param([string]$Message)
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Host "$ts  $Message"
}

function Try-Invoke-MinGit {
    param(
        [string]$TempZip = "$env:TEMP\MinGit.zip",
        [string]$Dest = "C:\work\mingit"
    )
    try {
        Write-Log "Attempting to download MinGit..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $url = 'https://github.com/git-for-windows/git/releases/latest/download/MinGit-64-bit.zip'
        Invoke-WebRequest -Uri $url -OutFile $TempZip -UseBasicParsing -TimeoutSec 240
        if (Test-Path $Dest) { Remove-Item -Recurse -Force -Path $Dest -ErrorAction SilentlyContinue }
        Expand-Archive -Path $TempZip -DestinationPath $Dest -Force
        $gitExe = Get-ChildItem -Path $Dest -Filter git.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $gitExe) {
            Write-Log "MinGit downloaded but git.exe not found under $Dest"
            return $false
        }
        $gitDir = Split-Path $gitExe.FullName -Parent
        $env:PATH = "$gitDir;$env:PATH"
        Write-Log "MinGit ready (git.exe at $($gitExe.FullName)). git --version -> $(git --version 2>&1)"
        return $true
    } catch {
        Write-Log "MinGit download/extract failed: $_"
        return $false
    }
}

function Try-Install-GitForWindows {
    param(
        [string]$Installer = "$env:TEMP\Git-Installer.exe",
        [string]$Url = 'https://github.com/git-for-windows/git/releases/latest/download/Git-64-bit.exe'
    )
    try {
        Write-Log "Attempting full Git for Windows silent install (requires admin)..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $Url -OutFile $Installer -UseBasicParsing -TimeoutSec 600
        Start-Process -FilePath $Installer -ArgumentList '/VERYSILENT','/NORESTART' -Wait -PassThru | Out-Null

        # The installer may place git into Program Files; check common locations and add to PATH if found
        $candidates = @(
            'C:\Program Files\Git\cmd\git.exe',
            'C:\Program Files\Git\bin\git.exe',
            'C:\Program Files (x86)\Git\cmd\git.exe',
            'C:\Program Files (x86)\Git\bin\git.exe'
        )
        foreach ($p in $candidates) {
            if (Test-Path $p) {
                $gitDir = Split-Path $p -Parent
                $env:PATH = "$gitDir;$env:PATH"
                Write-Log "Found git at $p; added $gitDir to PATH"
                try { git --version 2>&1 | Out-Null; if ($LASTEXITCODE -eq 0) { return $true } } catch {}
            }
        }

        # As a final check, see if git is now callable
        try { git --version 2>&1 | Out-Null; if ($LASTEXITCODE -eq 0) { Write-Log "git available after installer"; return $true } } catch {}

        Write-Log "Full Git installer finished but git not found in common locations or PATH"
        return $false
    } catch {
        Write-Log "Full Git installer failed: $_"
        return $false
    }
}

function Ensure-Git {
    # Return $true if git is usable in this process
    try {
        $ver = git --version 2>&1
        if ($LASTEXITCODE -eq 0) { Write-Log "git already available: $ver"; return $true }
    } catch {}

    if (Try-Invoke-MinGit) {
        try { git --version 2>&1 | Out-Null; if ($LASTEXITCODE -eq 0) { return $true } } catch {}
    }

    if (Try-Install-GitForWindows) {
        try { git --version 2>&1 | Out-Null; if ($LASTEXITCODE -eq 0) { return $true } } catch {}
    }

    Write-Log "git was not installed or not detected in PATH. Please install git manually and re-run."
    return $false
}

function Clone-Or-Update-Repo {
    param(
        [string]$RepoUrl,
        [string]$Branch,
        [string]$TargetDir
    )

    # Logs go into C:\work to avoid depending on $TargetDir existing
    $gitCloneLog = Join-Path $work 'git_clone.log'
    $gitFetchLog = Join-Path $work 'git_fetch.log'
    $gitCheckoutLog = Join-Path $work 'git_checkout.log'
    $gitCheckoutBLog = Join-Path $work 'git_checkout_b.log'

    if (-not (Test-Path (Split-Path $TargetDir -Parent))) {
        New-Item -ItemType Directory -Force -Path (Split-Path $TargetDir -Parent) | Out-Null
    }

    if (Test-Path (Join-Path $TargetDir '.git')) {
        Write-Log "Repository exists at $TargetDir; fetching and checking out $Branch"
        Push-Location $TargetDir
        git fetch origin $Branch 2>&1 | Tee-Object -FilePath $gitFetchLog
        if ($LASTEXITCODE -ne 0) { Write-Log "git fetch returned $LASTEXITCODE (see $gitFetchLog)" }
        git checkout $Branch 2>&1 | Tee-Object -FilePath $gitCheckoutLog
        if ($LASTEXITCODE -ne 0) {
            Write-Log "git checkout failed, trying forced branch creation from origin"
            git checkout -B $Branch origin/$Branch 2>&1 | Tee-Object -FilePath $gitCheckoutBLog
        }
        git rev-parse --abbrev-ref HEAD 2>&1 | Write-Host
        git rev-parse HEAD 2>&1 | Write-Host
        Pop-Location
        return
    }

    Write-Log "Cloning $RepoUrl (branch $Branch) into $TargetDir"
    git clone --branch $Branch --single-branch $RepoUrl $TargetDir 2>&1 | Tee-Object -FilePath $gitCloneLog
    if ($LASTEXITCODE -ne 0) { Write-Log "git clone failed with exit code $LASTEXITCODE (see $gitCloneLog)" }
}

function Run-External-Logged {
    param(
        [Parameter(Mandatory=$true)] [string]$Exe,
        [Parameter(Mandatory=$true)] [string[]]$Args,
        [Parameter(Mandatory=$true)] [string]$LogPath
    )
    Write-Log "Executing: $Exe $($Args -join ' ') (logging -> $LogPath)"
    # Ensure log directory exists
    New-Item -ItemType Directory -Force -Path (Split-Path $LogPath) | Out-Null
    & $Exe @Args 2>&1 | Tee-Object -FilePath $LogPath
    $exit = $LASTEXITCODE
    Write-Log "Process exit code: $exit"
    return $exit
}

function Show-First-Errors {
    param(
        [string]$LogPath,
        [int]$Lines = 10
    )
    if (-not (Test-Path $LogPath)) { Write-Log "Log not found: $LogPath"; return }
    Write-Log "Searching for critical lines in $LogPath"
    $patterns = '(?i:error|exception|failed|fail|unhandled)'
    $matches = Select-String -Path $LogPath -Pattern $patterns -SimpleMatch:$false -ErrorAction SilentlyContinue | Select-Object -First $Lines
    if ($null -eq $matches -or $matches.Count -eq 0) {
        Write-Host "(No obvious 'error/exception' lines found in $LogPath. Showing tail $Lines lines)"
        Get-Content $LogPath -Tail $Lines | ForEach-Object { Write-Host "  $_" }
    } else {
        # Use explicit variable bracing to avoid PowerShell parsing issues when paths contain ':' or '\\'
        Write-Host ("First critical matching lines from {0}:" -f ${LogPath})
        $matches | ForEach-Object { Write-Host "  $($_.Line.Trim())" }
    }
}

# --------------------
# Main
# --------------------

Write-Log "Starting VM validation script"

$repoUrl = if ($env:REGPROBE_REPO_URL) { $env:REGPROBE_REPO_URL } else { 'https://github.com/siklone/RegProbe.git' }
$branch = if ($env:REGPROBE_REPO_BRANCH) { $env:REGPROBE_REPO_BRANCH } else { 'main' }
$work = if ($env:REGPROBE_VM_WORKDIR) { $env:REGPROBE_VM_WORKDIR } else { 'C:\work' }
$repoDir = if ($env:REGPROBE_REPO_DIR) { $env:REGPROBE_REPO_DIR } else { Join-Path $work 'RegProbe' }
$dotnetCommand = if ($env:REGPROBE_DOTNET_COMMAND) { $env:REGPROBE_DOTNET_COMMAND } else { 'dotnet' }
$buildConfiguration = if ($env:REGPROBE_BUILD_CONFIGURATION) { $env:REGPROBE_BUILD_CONFIGURATION } else { 'Release' }
$testProject = if ($env:REGPROBE_TEST_PROJECT) { $env:REGPROBE_TEST_PROJECT } else { 'tests/tests.csproj' }

Write-Log "Work dir: $work"
New-Item -ItemType Directory -Force -Path $work | Out-Null

Write-Log "Checking or installing git..."
$gitOk = Ensure-Git
if (-not $gitOk) {
    Write-Log "git is required to continue. Exiting with code 20."
    exit 20
}

# Clone
Write-Log "Cloning or updating repository..."
Clone-Or-Update-Repo -RepoUrl $repoUrl -Branch $branch -TargetDir $repoDir

# Verify clone produced expected repo structure
if (-not (Test-Path (Join-Path $repoDir '.git')) -or -not (Test-Path (Join-Path $repoDir 'RegProbe.sln'))) {
    Write-Log "Clone/update did not produce expected repository files:"
    Write-Log "  .git present: $(Test-Path (Join-Path $repoDir '.git'))"
    Write-Log "  RegProbe.sln present: $(Test-Path (Join-Path $repoDir 'RegProbe.sln'))"
    Write-Log "Please check the git logs in C:\work (git_clone.log, git_fetch.log, git_checkout.log). Exiting with code 22."
    exit 22
}

# Save dotnet info
$dotnetInfoLog = Join-Path $repoDir 'dotnet-info.log'
& $dotnetCommand --info 2>&1 | Tee-Object -FilePath $dotnetInfoLog
if ($LASTEXITCODE -ne 0) { Write-Log "$dotnetCommand --info returned non-zero: $LASTEXITCODE" }

# Build
$buildLog = Join-Path $repoDir 'build.log'
Set-Location $repoDir
Write-Log "Running $dotnetCommand build ($buildConfiguration)"
$buildExit = Run-External-Logged -Exe $dotnetCommand -Args @('build','RegProbe.sln','-c',$buildConfiguration) -LogPath $buildLog

if ($buildExit -ne 0) {
    Write-Host "\nBUILD FAILED (exit code $buildExit). Showing first critical build log lines:\n"
    Show-First-Errors -LogPath $buildLog -Lines 20
} else {
    Write-Host "\nBUILD SUCCEEDED (exit code 0)"
}

# Test (only attempt if build succeeded)
$testLog = Join-Path $repoDir 'test.log'
$testExit = -1
if ($buildExit -eq 0) {
    Write-Log "Running $dotnetCommand test (no-build, $buildConfiguration)"
    $testExit = Run-External-Logged -Exe $dotnetCommand -Args @('test',$testProject,'-c',$buildConfiguration,'--no-build','-v','minimal') -LogPath $testLog
    if ($testExit -ne 0) {
        Write-Host "\nTESTS FAILED (exit code $testExit). Showing first critical test log lines:\n"
        Show-First-Errors -LogPath $testLog -Lines 40
    } else {
        Write-Host "\nTESTS SUCCEEDED (exit code 0)"
    }
} else {
    Write-Log "Skipping tests because build failed"
}

# Summary
Write-Host "\n===== SUMMARY ====="
Write-Host "Repository: $repoDir"
Write-Host "Repository URL: $repoUrl"
Write-Host "Branch: $branch"
Write-Host "dotnet command: $dotnetCommand"
Write-Host "git available: $gitOk"
Write-Host "dotnet-info: $dotnetInfoLog"
Write-Host "build log: $buildLog"
Write-Host "test log: $testLog"
Write-Host "BUILD exit code: $buildExit"
Write-Host "TEST exit code: $testExit"

if ($buildExit -ne 0 -or $testExit -ne 0) {
    Write-Host "\nIf something failed, please share the following files from the VM:\n  - $buildLog\n  - $testLog (if present)\n  - $repoDir\git_clone.log (or git_fetch.log / git_checkout.log)\n  - $dotnetInfoLog"
    exit 30
}

Write-Host "\nAll checks passed. You can proceed to open a PR if desired."
exit 0
