param(
    [switch]$Force,
    [switch]$SkipDownload
)

$ErrorActionPreference = "Stop"

function Resolve-DomainsForKey {
    param(
        [Parameter(Mandatory = $true)][string]$Key
    )

    $k = $Key.ToLowerInvariant()

    if ($k.StartsWith("os/") -or $k -eq "windows10" -or $k -eq "windows11") {
        return @("microsoft.com")
    }

    if ($k -match "nvidia|^gpu_(rtx|gtx|quadro)|geforce") {
        return @("nvidia.com")
    }

    if ($k -match "ryzen|radeon|^gpu_rx|^gpu_rdna|^amd_|^cpu_amd|threadripper|epyc|athlon|phenom|fx") {
        return @("amd.com")
    }

    if ($k -match "intel|^cpu_i[3579]|^cpu_xeon|pentium|celeron|arc") {
        return @("intel.com")
    }

    if ($k -match "asrock") { return @("asrock.com") }
    if ($k -match "asus|rog|tuf|proart") { return @("asus.com") }
    if ($k -match "msi|meg|mpg|mag") { return @("msi.com") }
    if ($k -match "gigabyte|aorus") { return @("www.aorus.com", "www.gigabyte.com", "gigabyte.us") }
    if ($k -match "biostar") { return @("www.biostar.com.tw", "biostar.com") }
    if ($k -match "supermicro") { return @("supermicro.com") }
    if ($k -match "evga") { return @("nvidia.com") }

    if ($k -match "corsair") { return @("corsair.com") }
    if ($k -match "kingston|fury") { return @("kingston.com") }
    if ($k -match "gskill|trident|ripjaws") { return @("gskill.com") }
    if ($k -match "crucial|ballistix") { return @("crucial.com") }
    if ($k -match "samsung") { return @("samsung.com") }
    if ($k -match "hynix") { return @("skhynix.com") }
    if ($k -match "micron") { return @("micron.com") }

    if ($k -match "wd_|western|westerndigital") { return @("westerndigital.com") }
    if ($k -match "seagate|barracuda|firecuda") { return @("seagate.com") }
    if ($k -match "kioxia|exceria") { return @("kioxia.com") }

    if ($k -match "realtek|rtl") { return @("www.realtek.com") }
    if ($k -match "broadcom|bcm") { return @("broadcom.com") }
    if ($k -match "mediatek|mt79") { return @("mediatek.com") }
    if ($k -match "killer") { return @("intel.com", "killernetworking.com") }

    if ($k -match "asmedia|asm") { return @("www.asmedia.com.tw") }
    if ($k -match "renesas") { return @("renesas.com") }
    if ($k -match "^usb_via") { return @("via.com.tw") }

    if ($k -match "^display_acer|predator") { return @("acer.com") }
    if ($k -match "^display_lg|ultragear") { return @("lg.com") }
    if ($k -match "^display_samsung|odyssey") { return @("samsung.com") }
    if ($k -match "^display_dell|alienware|ultrasharp") { return @("dell.com", "alienware.com") }
    if ($k -match "^display_benq") { return @("benq.com") }
    if ($k -match "^display_viewsonic") { return @("viewsonic.com") }
    if ($k -match "^display_aoc") { return @("aoc.com") }

    if ($k.StartsWith("cpu_")) { return @("intel.com", "amd.com") }
    if ($k.StartsWith("gpu_")) { return @("nvidia.com", "amd.com", "intel.com") }
    if ($k.StartsWith("mb_") -or $k.StartsWith("motherboard")) { return @("asus.com", "msi.com", "asrock.com", "gigabyte.com.tw") }
    if ($k.StartsWith("memory_")) { return @("kingston.com", "corsair.com", "crucial.com") }
    if ($k.StartsWith("storage_")) { return @("samsung.com", "westerndigital.com", "seagate.com", "crucial.com") }
    if ($k.StartsWith("network_")) { return @("intel.com", "broadcom.com", "realtek.com.tw") }
    if ($k.StartsWith("usb_")) { return @("intel.com", "asmedia.com.tw") }
    if ($k.StartsWith("display_")) { return @("microsoft.com") }

    return @("microsoft.com")
}

function Get-UniqueSortedStrings {
    param(
        [Parameter(Mandatory = $true)][object[]]$Values
    )

    return @(
        $Values |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_.ToString().Trim().ToLowerInvariant() } |
            Sort-Object -Unique
    )
}

function Get-CategoryFallbacks {
    param(
        [Parameter(Mandatory = $true)][object[]]$Entries
    )

    $fallbacks = [ordered]@{}
    foreach ($entry in $Entries) {
        $category = [string]$entry.category
        $key = [string]$entry.key
        if ([string]::IsNullOrWhiteSpace($category) -or [string]::IsNullOrWhiteSpace($key)) {
            continue
        }

        if ($key -eq ($category + "_default") -or ($category -eq "os" -and $key -eq "os/windows10")) {
            $fallbacks[$category] = $key
        }
    }

    return $fallbacks
}

function Resolve-CategoryForKey {
    param(
        [Parameter(Mandatory = $true)][string]$Key
    )

    $normalized = $Key.Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($normalized)) { return "cpu" }
    if ($normalized.StartsWith("os/") -or $normalized -eq "windows10" -or $normalized -eq "windows11") { return "os" }
    if ($normalized.StartsWith("mb_") -or $normalized.StartsWith("motherboard_")) { return "motherboard" }
    if ($normalized.StartsWith("chipset_")) { return "chipset" }
    if ($normalized.StartsWith("cpu_") -or $normalized -eq "amd_ryzen_cpu") { return "cpu" }
    if ($normalized.StartsWith("gpu_") -or $normalized -eq "amd_gpu") { return "gpu" }
    if ($normalized.StartsWith("memory_")) { return "memory" }
    if ($normalized.StartsWith("storage_")) { return "storage" }
    if ($normalized.StartsWith("network_")) { return "network" }
    if ($normalized.StartsWith("usb_")) { return "usb" }
    if ($normalized.StartsWith("display_")) { return "display" }
    if ($normalized -in @("asus", "msi", "gigabyte", "asrock", "intel_core", "nvidia", "amd_ryzen")) { return "brand" }

    $underscoreIndex = $normalized.IndexOf("_")
    if ($underscoreIndex -gt 0) {
        return $normalized.Substring(0, $underscoreIndex)
    }

    return "cpu"
}

function Get-HardwareDbIconEntries {
    param(
        [Parameter(Mandatory = $true)][string]$HardwareDbRoot
    )

    $entries = @()
    $dbFiles = Get-ChildItem -Path $HardwareDbRoot -Filter "hardware_db_*.json" -File -ErrorAction SilentlyContinue
    foreach ($file in $dbFiles) {
        $doc = Get-Content $file.FullName -Raw | ConvertFrom-Json
        foreach ($item in @($doc.items)) {
            $iconKey = [string]$item.iconKey
            if ([string]::IsNullOrWhiteSpace($iconKey)) {
                continue
            }

            $parts = @(
                [string]$item.brand,
                [string]$item.series,
                [string]$item.modelName
            ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

            $entries += [pscustomobject]@{
                key = $iconKey
                category = Resolve-CategoryForKey -Key $iconKey
                description = if ($parts.Count -gt 0) {
                    "Auto-discovered from hardware DB: " + ($parts -join " ").Trim()
                } else {
                    "Auto-discovered from hardware DB"
                }
            }
        }
    }

    return $entries
}

function Build-IconSourceDb {
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$OutputPath
    )

    if (-not (Test-Path $ManifestPath)) {
        throw "Icon pack manifest not found: $ManifestPath"
    }

    $manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
    $requiredIcons = @($manifest.requiredIcons)
    if ($requiredIcons.Count -eq 0) {
        throw "No required icons found in manifest: $ManifestPath"
    }

    $manifestDir = Split-Path -Parent $ManifestPath
    $discoveredIcons = Get-HardwareDbIconEntries -HardwareDbRoot $manifestDir

    $dedupedEntries = [ordered]@{}
    foreach ($entry in @($requiredIcons + $discoveredIcons)) {
        $key = [string]$entry.key
        if ([string]::IsNullOrWhiteSpace($key) -or $dedupedEntries.Contains($key)) {
            continue
        }

        $dedupedEntries[$key] = $entry
    }

    $sortedEntries = @(
        $dedupedEntries.Values |
            Sort-Object @{ Expression = { [string]$_.category } }, @{ Expression = { [string]$_.key } }
    )

    $items = @()
    foreach ($entry in $sortedEntries) {
        $domains = Get-UniqueSortedStrings -Values @(Resolve-DomainsForKey -Key $entry.key)
        $items += [ordered]@{
            key = $entry.key
            category = $entry.category
            description = $entry.description
            provider = "google_favicon"
            preferredDomain = if ($domains.Count -gt 0) { $domains[0] } else { $null }
            domainCount = $domains.Count
            domains = @($domains)
            urlTemplate = "https://www.google.com/s2/favicons?sz=128&domain={domain}"
        }
    }

    $categoryFallbacks = Get-CategoryFallbacks -Entries $sortedEntries
    $db = [ordered]@{
        version = ("{0}-web-icon-db" -f $manifest.version)
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        source = "Generated from IconPackManifest + domain mapping rules"
        manifestVersion = $manifest.version
        provider = "google_favicon"
        categoryFallbacks = $categoryFallbacks
        itemCount = $items.Count
        items = $items
    }

    $json = $db | ConvertTo-Json -Depth 12
    $dir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
    }

    Set-Content -Path $OutputPath -Value $json -Encoding UTF8
}

function Download-IconsFromDb {
    param(
        [Parameter(Mandatory = $true)][string]$DbPath,
        [Parameter(Mandatory = $true)][string]$IconsRoot,
        [switch]$ForceDownload
    )

    if (-not (Test-Path $DbPath)) {
        throw "Icon source DB not found: $DbPath"
    }

    $db = Get-Content $DbPath -Raw | ConvertFrom-Json
    $items = @($db.items)
    if ($items.Count -eq 0) {
        throw "Icon source DB has no items: $DbPath"
    }

    New-Item -Path $IconsRoot -ItemType Directory -Force | Out-Null

    $domainCache = @{}
    $results = @()
    $ok = 0
    $failed = 0
    $skipped = 0

    foreach ($item in $items) {
        $relative = ($item.key -replace "/", "\") + ".png"
        $targetPath = Join-Path $IconsRoot $relative
        $targetDir = Split-Path -Parent $targetPath
        if (-not (Test-Path $targetDir)) {
            New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
        }

        if ((-not $ForceDownload) -and (Test-Path $targetPath)) {
            $skipped++
            $results += [ordered]@{
                key = $item.key
                status = "skipped_existing"
                target = $targetPath
                domain = $null
                url = $null
                message = $null
            }
            continue
        }

        $success = $false
        $lastError = $null

        foreach ($domain in @($item.domains)) {
            try {
                if ($domainCache.ContainsKey($domain) -and (Test-Path $domainCache[$domain])) {
                    Copy-Item -Path $domainCache[$domain] -Destination $targetPath -Force
                    $success = $true
                    $ok++
                    $results += [ordered]@{
                        key = $item.key
                        status = "downloaded_from_cache"
                        target = $targetPath
                        domain = $domain
                        url = $null
                        message = $null
                    }
                    break
                }

                $url = $item.urlTemplate.Replace("{domain}", $domain)
                $tmpDomainPath = Join-Path ([System.IO.Path]::GetTempPath()) ("wo_icon_" + ($domain -replace "[^a-zA-Z0-9]", "_") + ".png")

                Invoke-WebRequest -Uri $url -OutFile $tmpDomainPath -TimeoutSec 20
                $downloaded = Get-Item $tmpDomainPath
                if ($downloaded.Length -le 64) {
                    throw "Downloaded file too small ($($downloaded.Length) bytes)."
                }

                $domainCache[$domain] = $tmpDomainPath
                Copy-Item -Path $tmpDomainPath -Destination $targetPath -Force

                $success = $true
                $ok++
                $results += [ordered]@{
                    key = $item.key
                    status = "downloaded"
                    target = $targetPath
                    domain = $domain
                    url = $url
                    message = $null
                }
                break
            }
            catch {
                $lastError = $_.Exception.Message
            }
            finally {
                Start-Sleep -Milliseconds 80
            }
        }

        if (-not $success) {
            $failed++
            $results += [ordered]@{
                key = $item.key
                status = "failed"
                target = $targetPath
                domain = $null
                url = $null
                message = $lastError
            }
        }
    }

    $report = [ordered]@{
        version = $db.version
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        total = $items.Count
        downloaded = $ok
        skipped = $skipped
        failed = $failed
        results = $results
    }

    $reportPath = Join-Path (Split-Path -Parent $DbPath) "HardwareIconDownloadReport.json"
    ($report | ConvertTo-Json -Depth 12) | Set-Content -Path $reportPath -Encoding UTF8

    return @{
        ReportPath = $reportPath
        Downloaded = $ok
        Skipped = $skipped
        Failed = $failed
        Total = $items.Count
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$manifestPath = Join-Path $repoRoot "WindowsOptimizer.App\Assets\HardwareDb\IconPackManifest.json"
$dbPath = Join-Path $repoRoot "WindowsOptimizer.App\Assets\HardwareDb\HardwareIconSourceDb.json"
$iconsRoot = Join-Path $repoRoot "WindowsOptimizer.App\Assets\Icons"

Build-IconSourceDb -ManifestPath $manifestPath -OutputPath $dbPath
Write-Output "Icon source DB generated: $dbPath"

if (-not $SkipDownload) {
    $summary = Download-IconsFromDb -DbPath $dbPath -IconsRoot $iconsRoot -ForceDownload:$Force
    Write-Output ("Download summary: total={0}, downloaded={1}, skipped={2}, failed={3}" -f $summary.Total, $summary.Downloaded, $summary.Skipped, $summary.Failed)
    Write-Output ("Report: {0}" -f $summary.ReportPath)
}
