$ErrorActionPreference = "Stop"

$base = Split-Path -Parent $MyInvocation.MyCommand.Path

function Normalize-AliasKey {
    param(
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $normalized = $Value.Trim().ToLowerInvariant()
    $normalized = $normalized.Replace("geforce", "nvidia geforce")
    $normalized = $normalized.Replace("radeon tm", "radeon")
    $normalized = $normalized.Replace("-", " ")
    $normalized = $normalized.Replace("wi fi", "wifi")
    $normalized = [regex]::Replace($normalized, "\b\(r\)|\(tm\)|cpu|processor|graphics|adapter|series|to\s+be\s+filled\s+by\s+o\.?e\.?m\.?|default\s+string|none|unknown|standard\b", " ")
    $normalized = [regex]::Replace($normalized, "[^a-z0-9+\-\s]", " ")
    $normalized = [regex]::Replace($normalized, "\s+", " ")
    return $normalized.Trim()
}

function Get-UniqueAliases {
    param(
        [object[]]$Aliases
    )

    $seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    $result = New-Object System.Collections.Generic.List[string]

    foreach ($alias in ($Aliases | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $clean = $alias.Trim().ToLowerInvariant()
        $key = Normalize-AliasKey -Value $clean
        if ([string]::IsNullOrWhiteSpace($key) -or -not $seen.Add($key)) {
            continue
        }

        $result.Add($clean)
    }

    return @($result)
}

function Merge-DbItems {
    param(
        [object[]]$Items
    )

    $merged = New-Object System.Collections.Generic.List[object]
    $index = @{}

    foreach ($item in $Items) {
        if ($item.Contains("aliases") -and $null -ne $item.aliases) {
            $item.aliases = @(Get-UniqueAliases -Aliases $item.aliases)
        }
        else {
            $item.aliases = @()
        }

        $identity = Normalize-AliasKey -Value ([string]$item.normalizedName)
        if ([string]::IsNullOrWhiteSpace($identity)) {
            $identity = Normalize-AliasKey -Value ([string]$item.modelName)
        }

        if ([string]::IsNullOrWhiteSpace($identity)) {
            $identity = [string]$item.id
        }

        if (-not $index.ContainsKey($identity)) {
            $index[$identity] = $item
            $merged.Add($item)
            continue
        }

        $existing = $index[$identity]
        $aliasPool = New-Object System.Collections.Generic.List[string]

        foreach ($value in @($existing.aliases) + @($item.aliases) + @($existing.modelName) + @($item.modelName)) {
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                $aliasPool.Add($value)
            }
        }

        $existing.aliases = @(Get-UniqueAliases -Aliases $aliasPool)

        foreach ($key in $item.Keys) {
            if ($key -eq "aliases") {
                continue
            }

            $existingValue = [string]$existing[$key]
            $incomingValue = [string]$item[$key]
            if ([string]::IsNullOrWhiteSpace($existingValue) -and -not [string]::IsNullOrWhiteSpace($incomingValue)) {
                $existing[$key] = $item[$key]
            }
        }
    }

    return $merged.ToArray()
}

function Save-Db {
    param(
        [string]$File,
        [object[]]$Items
    )

    $Items = @(Merge-DbItems -Items $Items)

    $doc = [ordered]@{
        version = "2026.03.08-phase3-icon-enriched-deduped"
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        items = $Items
    }

    $json = $doc | ConvertTo-Json -Depth 10
    Set-Content -Path (Join-Path $base $File) -Value $json -Encoding UTF8
}

function Expand-Deterministic {
    param(
        [object[]]$Seeds,
        [int]$TargetCount,
        [scriptblock]$Builder
    )

    $items = @()
    for ($i = 0; $i -lt $TargetCount; $i++) {
        $seed = $Seeds[$i % $Seeds.Count]
        $items += & $Builder $seed $i
    }

    return $items
}

function Resolve-ChipsetIconKey {
    param(
        [string]$Brand,
        [string]$Model
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $modelKey = $Model.Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($modelKey)) {
        if ($brandKey -eq "intel") { return "chipset_intel" }
        if ($brandKey -eq "amd") { return "chipset_amd" }
        return "chipset_default"
    }

    if ($brandKey -eq "intel") {
        switch -Regex ($modelKey) {
            '^z890$' { return "chipset_intel_z890" }
            '^z790$' { return "chipset_intel_z790" }
            '^z690$' { return "chipset_intel_z690" }
            '^z590$' { return "chipset_intel_z590" }
            '^z490$' { return "chipset_intel_z490" }
            '^b860$' { return "chipset_intel_b860" }
            '^b760$' { return "chipset_intel_b760" }
            '^b660$' { return "chipset_intel_b660" }
            '^h810$' { return "chipset_intel_h810" }
            '^h770$' { return "chipset_intel_h770" }
            '^h670$' { return "chipset_intel_h670" }
            default { return "chipset_intel" }
        }
    }

    if ($brandKey -eq "amd") {
        switch -Regex ($modelKey) {
            '^x870e$' { return "chipset_amd_x870e" }
            '^x870$' { return "chipset_amd_x870" }
            '^x670e$' { return "chipset_amd_x670e" }
            '^x670$' { return "chipset_amd_x670" }
            '^x570$' { return "chipset_amd_x570" }
            '^x470$' { return "chipset_amd_x470" }
            '^b850$' { return "chipset_amd_b850" }
            '^b650e$' { return "chipset_amd_b650e" }
            '^b650$' { return "chipset_amd_b650" }
            '^b550$' { return "chipset_amd_b550" }
            '^b450$' { return "chipset_amd_b450" }
            '^a620$' { return "chipset_amd_a620" }
            '^a520$' { return "chipset_amd_a520" }
            default { return "chipset_amd" }
        }
    }

    return "chipset_default"
}

function Resolve-MemoryModuleIconKey {
    param(
        [string]$Brand,
        [string]$Series,
        [string]$Type
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $seriesKey = $Series.Trim().ToLowerInvariant()
    $typeKey = $Type.Trim().ToLowerInvariant()

    if ($seriesKey -match "dominator") { return "memory_corsair_dominator" }
    if ($seriesKey -match "vengeance") { return "memory_corsair_vengeance" }
    if ($seriesKey -match "fury") { return "memory_kingston_fury" }
    if ($seriesKey -match "trident") { return "memory_gskill_trident" }
    if ($seriesKey -match "ripjaws") { return "memory_gskill_ripjaws" }
    if ($seriesKey -match "ballistix") { return "memory_crucial_ballistix" }

    if ($brandKey -eq "corsair") { return "memory_corsair" }
    if ($brandKey -eq "kingston") { return "memory_kingston" }
    if ($brandKey -eq "g.skill") { return "memory_gskill" }
    if ($brandKey -eq "crucial") { return "memory_crucial" }
    if ($brandKey -eq "samsung") { return "memory_samsung" }
    if ($brandKey -eq "sk hynix") { return "memory_hynix" }
    if ($brandKey -eq "micron") { return "memory_micron" }

    if ($typeKey -eq "ddr5") { return "memory_ddr5" }
    if ($typeKey -eq "ddr4") { return "memory_ddr4" }
    return "memory_default"
}

function Resolve-MemoryChipIconKey {
    param(
        [string]$Brand,
        [string]$Family
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $familyKey = $Family.Trim().ToLowerInvariant()

    switch ($brandKey) {
        "samsung" { return "memory_samsung" }
        "sk hynix" { return "memory_hynix" }
        "micron" { return "memory_micron" }
    }

    if ($familyKey -eq "ddr5") { return "memory_ddr5" }
    if ($familyKey -eq "ddr4") { return "memory_ddr4" }
    return "memory_default"
}

function Resolve-StorageControllerIconKey {
    param(
        [string]$Brand,
        [string]$Model,
        [string]$Interface
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $modelKey = $Model.Trim().ToLowerInvariant()
    $ifKey = $Interface.Trim().ToLowerInvariant()

    if ($modelKey -match "990\s*pro") { return "storage_samsung_990pro" }
    if ($modelKey -match "980\s*pro") { return "storage_samsung_980pro" }
    if ($modelKey -match "970\s*evo") { return "storage_samsung_970evo" }
    if ($modelKey -match "(^| )970($| )") { return "storage_samsung_970" }
    if ($modelKey -match "sn850x|black\s*sn850") { return "storage_wd_black" }
    if ($modelKey -match "sn770|wd\s*black") { return "storage_wd_black" }
    if ($modelKey -match "sn580|wd\s*blue") { return "storage_wd_blue" }
    if ($modelKey -match "firecuda") { return "storage_seagate_firecuda" }
    if ($modelKey -match "barracuda") { return "storage_seagate_barracuda" }
    if ($modelKey -match "mx500") { return "storage_crucial" }
    if ($modelKey -match "p5\s*plus") { return "storage_crucial_p5" }
    if ($modelKey -match "kc3000") { return "storage_kingston_kc3000" }
    if ($modelKey -match "mp600") { return "storage_corsair_mp600" }
    if ($modelKey -match "exceria|kioxia") { return "storage_kioxia" }

    if ($brandKey -eq "samsung") { return "storage_samsung" }
    if ($brandKey -eq "western digital" -or $brandKey -eq "wd") { return "storage_wd" }
    if ($brandKey -eq "seagate") { return "storage_seagate" }
    if ($brandKey -eq "crucial") { return "storage_crucial" }
    if ($brandKey -eq "kingston") { return "storage_kingston" }
    if ($brandKey -eq "corsair") { return "storage_corsair" }
    if ($brandKey -eq "kioxia") { return "storage_kioxia" }
    if ($brandKey -eq "marvell") { return "storage_ssd" }
    if ($brandKey -eq "jmicron") { return "storage_default" }
    if ($ifKey -like "usb*") { return "storage_default" }
    if ($modelKey -match "pcie|nvme" -or $ifKey -like "pcie*") { return "storage_nvme" }
    if ($modelKey -match "ssd") { return "storage_ssd" }
    if ($modelKey -match "hdd") { return "storage_hdd" }
    if ($ifKey -like "sata*") { return "storage_ssd" }
    return "storage_default"
}

function Format-StorageCapacityLabel {
    param(
        [int]$CapacityGb
    )

    if ($CapacityGb -ge 1000 -and $CapacityGb % 1000 -eq 0) {
        return ("{0}TB" -f [int]($CapacityGb / 1000))
    }

    return ("{0}GB" -f $CapacityGb)
}

function Get-StorageCapacityAliasLabels {
    param(
        [int]$CapacityGb
    )

    $labels = New-Object System.Collections.Generic.List[string]
    $labels.Add((Format-StorageCapacityLabel -CapacityGb $CapacityGb))
    $labels.Add(("{0}GB" -f $CapacityGb))

    if ($CapacityGb -ge 1000 -and $CapacityGb % 1000 -eq 0) {
        $labels.Add(("{0}TB" -f [int]($CapacityGb / 1000)))
    }

    return @($labels | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
}

function Add-StorageAliasVariants {
    param(
        [System.Collections.Generic.List[string]]$Aliases,
        [string]$Brand,
        [string]$Type,
        [string]$Alias
    )

    if ([string]::IsNullOrWhiteSpace($Alias)) {
        return
    }

    $variants = New-Object System.Collections.Generic.List[string]
    $variants.Add($Alias)

    if ($Alias.Contains("/")) {
        $baseAlias = $Alias.Split("/")[0]
        if (-not [string]::IsNullOrWhiteSpace($baseAlias)) {
            $variants.Add($baseAlias)
        }
    }

    $compactAlias = $Alias -replace "[^A-Za-z0-9]", ""
    if (-not [string]::IsNullOrWhiteSpace($compactAlias)) {
        $variants.Add($compactAlias)
    }

    foreach ($variant in ($variants | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        $lower = $variant.ToLowerInvariant()
        $Aliases.Add($lower)
        $Aliases.Add(("{0} {1}" -f $Brand, $variant).Trim().ToLowerInvariant())
        if ($Type -like "*SSD*") {
            $Aliases.Add(("{0} SSD {1}" -f $Brand, $variant).Trim().ToLowerInvariant())
        }
    }
}

function Get-MotherboardVendorAliases {
    param(
        [string]$Brand
    )

    switch ($Brand.Trim().ToLowerInvariant()) {
        "asus" { return @("ASUSTeK COMPUTER INC.", "ASUS") }
        "msi" { return @("Micro-Star International Co., Ltd.", "MSI") }
        "gigabyte" { return @("Gigabyte Technology Co., Ltd.", "Gigabyte") }
        "asrock" { return @("ASRock Inc.", "ASRock") }
        "biostar" { return @("Biostar Group", "Biostar") }
        "supermicro" { return @("Super Micro Computer, Inc.", "Supermicro") }
        "evga" { return @("EVGA Corp.", "EVGA") }
        default { return @($Brand) }
    }
}

function Add-MotherboardAliasVariants {
    param(
        [System.Collections.Generic.List[string]]$Aliases,
        [string]$Brand,
        [string]$Series,
        [string]$Chipset,
        [string]$ModelName
    )

    if ([string]::IsNullOrWhiteSpace($ModelName)) {
        return
    }

    $vendorAliases = Get-MotherboardVendorAliases -Brand $Brand
    $variants = New-Object System.Collections.Generic.List[string]

    foreach ($candidate in @(
        $ModelName,
        ("{0} {1}" -f $Brand, $ModelName).Trim(),
        ("{0} {1}" -f $Chipset, $Series).Trim(),
        ("{0} {1} {2}" -f $Brand, $Chipset, $Series).Trim(),
        ("{0} {1}" -f $Brand, $Series).Trim()
    )) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $variants.Add($candidate)
        }
    }

    foreach ($existing in @($variants)) {
        if ($existing -match "\bwifi(7|6e|6)?\b") {
            $variants.Add(($existing -replace "\s*wifi(7|6e|6)?\b", "").Trim())
        }

        if ($existing -match "phantom gaming") {
            $variants.Add(($existing -replace "phantom gaming", "PG").Trim())
        }

        if ($existing -match "\bpg\b") {
            $variants.Add(($existing -replace "\bpg\b", "Phantom Gaming").Trim())
        }
    }

    foreach ($variant in ($variants | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        $Aliases.Add($variant.ToLowerInvariant())

        $compact = $variant -replace "[^A-Za-z0-9]", ""
        if (-not [string]::IsNullOrWhiteSpace($compact)) {
            $Aliases.Add($compact.ToLowerInvariant())
        }

        foreach ($vendor in $vendorAliases) {
            $normalizedVariant = ($variant -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            $normalizedVendor = ($vendor -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            if (-not [string]::IsNullOrWhiteSpace($normalizedVendor) -and $normalizedVariant.StartsWith($normalizedVendor)) {
                continue
            }

            $prefixed = ("{0} {1}" -f $vendor, $variant).Trim()
            if ([string]::IsNullOrWhiteSpace($prefixed)) {
                continue
            }

            $Aliases.Add($prefixed.ToLowerInvariant())

            $prefixedCompact = $prefixed -replace "[^A-Za-z0-9]", ""
            if (-not [string]::IsNullOrWhiteSpace($prefixedCompact)) {
                $Aliases.Add($prefixedCompact.ToLowerInvariant())
            }
        }
    }
}

function Resolve-UsbIconKey {
    param(
        [string]$Brand,
        [string]$Model,
        [string]$Standard
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $modelKey = $Model.Trim().ToLowerInvariant()
    $standardKey = $Standard.Trim().ToLowerInvariant()

    if ($modelKey -eq "asm3142") { return "usb_asmedia_3142" }
    if ($standardKey -match "usb4") { return "usb_usb4" }
    if ($modelKey -match "thunderbolt") { return "usb_usb4" }
    if ($brandKey -eq "asmedia") { return "usb_asmedia" }
    if ($brandKey -eq "amd") { return "usb_amd" }
    if ($brandKey -eq "renesas") { return "usb_renesas" }
    if ($brandKey -eq "via") { return "usb_via" }
    if ($brandKey -eq "intel") { return "usb_intel" }
    if ($standardKey -match "3\.2") { return "usb_32" }
    if ($standardKey -match "3\.1") { return "usb_31" }
    if ($standardKey -match "3\.0") { return "usb_30" }
    if ($standardKey -match "2\.0") { return "usb_20" }
    return "usb_default"
}

function Resolve-MotherboardIconKey {
    param(
        [string]$Brand,
        [string]$Series
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $seriesKey = $Series.Trim().ToLowerInvariant()

    if ($brandKey -eq "asus") {
        if ($seriesKey -match "rog") { return "mb_asus_rog" }
        if ($seriesKey -match "tuf") { return "mb_asus_tuf" }
        if ($seriesKey -match "prime") { return "mb_asus_prime" }
        return "mb_asus"
    }

    if ($brandKey -eq "msi") {
        if ($seriesKey -match "^meg") { return "mb_msi_meg" }
        if ($seriesKey -match "^mpg") { return "mb_msi_mpg" }
        if ($seriesKey -match "^mag") { return "mb_msi_mag" }
        return "mb_msi"
    }

    if ($brandKey -eq "gigabyte") {
        if ($seriesKey -match "aorus") { return "mb_gigabyte_aorus" }
        if ($seriesKey -match "gaming") { return "mb_gigabyte_gaming" }
        return "mb_gigabyte"
    }

    if ($brandKey -eq "asrock") {
        if ($seriesKey -match "taichi") { return "mb_asrock_taichi" }
        if ($seriesKey -match "phantom") { return "mb_asrock_phantom" }
        return "mb_asrock"
    }

    if ($brandKey -eq "biostar") { return "mb_biostar" }
    if ($brandKey -eq "supermicro") { return "mb_supermicro" }
    if ($brandKey -eq "evga") { return "mb_evga" }
    return "motherboard_default"
}

function Resolve-NetworkIconKey {
    param(
        [string]$Brand,
        [string]$Model,
        [string]$Generation
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $modelKey = $Model.Trim().ToLowerInvariant()
    $genKey = $Generation.Trim().ToLowerInvariant()

    if ($modelKey -match "i226") { return "network_intel_i226" }
    if ($modelKey -match "i225") { return "network_intel_i225" }
    if ($modelKey -match "i219|i211|i210") { return "network_1gbe" }
    if ($modelKey -match "ax211") { return "network_intel_ax211" }
    if ($modelKey -match "ax210") { return "network_intel_ax210" }
    if ($modelKey -match "ax201") { return "network_intel_ax201" }
    if ($modelKey -match "ax200") { return "network_intel_ax200" }
    if ($modelKey -match "rtl8125") { return "network_realtek_8125" }
    if ($modelKey -match "rtl8111") { return "network_realtek_8111" }
    if ($modelKey -match "rtl8852") { return "network_wifi6" }
    if ($modelKey -match "^e3") { return "network_killer_e3000" }
    if ($modelKey -match "^e2") { return "network_killer_e2600" }
    if ($brandKey -eq "qualcomm" -and $modelKey -match "865") { return "network_wifi7" }

    if ($genKey -match "wifi 7|wi-fi 7") { return "network_wifi7" }
    if ($genKey -match "wifi 6e|wi-fi 6e") { return "network_wifi6e" }
    if ($genKey -match "wifi 6|wi-fi 6") { return "network_wifi6" }
    if ($genKey -match "2\.5gbe|2\.5g") { return "network_2_5gbe" }
    if ($genKey -match "1gbe|gigabit") { return "network_1gbe" }

    if ($brandKey -eq "intel") { return "network_intel" }
    if ($brandKey -eq "realtek") { return "network_realtek" }
    if ($brandKey -eq "killer") { return "network_killer" }
    if ($brandKey -eq "broadcom") { return "network_broadcom" }
    if ($brandKey -eq "mediatek") { return "network_mediatek" }
    if ($genKey -match "ethernet") { return "network_1gbe" }
    return "network_default"
}

function Get-GpuVendorAliases {
    param(
        [string]$Vendor
    )

    switch ($Vendor.Trim().ToLowerInvariant()) {
        "nvidia" { return @("NVIDIA") }
        "amd" { return @("AMD") }
        "intel" { return @("Intel") }
        default { return @($Vendor) }
    }
}

function Resolve-GpuDbIconKey {
    param(
        [string]$Vendor,
        [string]$ModelName
    )

    $vendorKey = $Vendor.Trim().ToLowerInvariant()
    $modelKey = $ModelName.Trim().ToLowerInvariant()

    if ($vendorKey -eq "nvidia") {
        if ($modelKey -match "rtx 5090|rtx 5080|rtx 5070|rtx 5060") { return "gpu_rtx50" }
        if ($modelKey -match "rtx 4090|rtx 4080|rtx 4070|rtx 4060") { return "gpu_rtx40" }
        if ($modelKey -match "rtx 3090|rtx 3080|rtx 3070|rtx 3060") { return "gpu_rtx30" }
        if ($modelKey -match "rtx 2080|rtx 2070|rtx 2060") { return "gpu_rtx20" }
        if ($modelKey -match "gtx 16") { return "gpu_gtx16" }
        if ($modelKey -match "gtx 10") { return "gpu_gtx10" }
        return "gpu_nvidia"
    }

    if ($vendorKey -eq "amd") {
        if ($modelKey -match "rx 7900|rx 7800|rx 7700|rx 7600") { return "gpu_rx7000" }
        if ($modelKey -match "rx 6900|rx 6800|rx 6700|rx 6600") { return "gpu_rx6000" }
        return "gpu_radeon"
    }

    if ($vendorKey -eq "intel") {
        return "gpu_intel_arc"
    }

    return "gpu_default"
}

function Add-GpuAliasVariants {
    param(
        [System.Collections.Generic.List[string]]$Aliases,
        [string]$Vendor,
        [string]$ModelName,
        [string[]]$ExtraAliases
    )

    if ([string]::IsNullOrWhiteSpace($ModelName)) {
        return
    }

    $vendorAliases = Get-GpuVendorAliases -Vendor $Vendor
    $variants = New-Object System.Collections.Generic.List[string]

    foreach ($candidate in @($ModelName) + @($ExtraAliases)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $variants.Add($candidate.Trim())
        }
    }

    foreach ($existing in @($variants)) {
        $variants.Add(($existing -replace "^GeForce\s+", "").Trim())
        $variants.Add(($existing -replace "^Radeon\s+", "").Trim())
        $variants.Add(($existing -replace "^Intel Arc\s+", "Arc ").Trim())
    }

    foreach ($variant in ($variants | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        $Aliases.Add($variant.ToLowerInvariant())

        $compact = $variant -replace "[^A-Za-z0-9+]", ""
        if (-not [string]::IsNullOrWhiteSpace($compact)) {
            $Aliases.Add($compact.ToLowerInvariant())
        }

        foreach ($vendorAlias in $vendorAliases) {
            $normalizedVariant = ($variant -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            $normalizedVendor = ($vendorAlias -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            if (-not [string]::IsNullOrWhiteSpace($normalizedVendor) -and $normalizedVariant.StartsWith($normalizedVendor)) {
                continue
            }

            $prefixed = ("{0} {1}" -f $vendorAlias, $variant).Trim()
            if ([string]::IsNullOrWhiteSpace($prefixed)) {
                continue
            }

            $Aliases.Add($prefixed.ToLowerInvariant())

            $prefixedCompact = $prefixed -replace "[^A-Za-z0-9+]", ""
            if (-not [string]::IsNullOrWhiteSpace($prefixedCompact)) {
                $Aliases.Add($prefixedCompact.ToLowerInvariant())
            }
        }
    }
}

$gpuSeeds = @(
    @{ Vendor = "NVIDIA"; Series = "RTX 20"; ModelName = "GeForce RTX 2060"; Codename = "Turing"; Architecture = "Turing"; ProcessNode = "12nm"; Units = 1920; VramGB = 6; BoostMHz = 1680; ReleaseYear = 2019; Aliases = @("RTX 2060", "NVIDIA GeForce RTX 2060") },
    @{ Vendor = "NVIDIA"; Series = "RTX 30"; ModelName = "GeForce RTX 3060"; Codename = "Ampere"; Architecture = "Ampere"; ProcessNode = "8nm"; Units = 3584; VramGB = 12; BoostMHz = 1777; ReleaseYear = 2021; Aliases = @("RTX 3060", "NVIDIA GeForce RTX 3060", "GeForce RTX 3060 VENTUS 2X 12G OC") },
    @{ Vendor = "NVIDIA"; Series = "RTX 30"; ModelName = "GeForce RTX 3070"; Codename = "Ampere"; Architecture = "Ampere"; ProcessNode = "8nm"; Units = 5888; VramGB = 8; BoostMHz = 1725; ReleaseYear = 2020; Aliases = @("RTX 3070", "NVIDIA GeForce RTX 3070", "TUF-RTX3070-O8G-GAMING") },
    @{ Vendor = "NVIDIA"; Series = "RTX 30"; ModelName = "GeForce RTX 3080"; Codename = "Ampere"; Architecture = "Ampere"; ProcessNode = "8nm"; Units = 8704; VramGB = 10; BoostMHz = 1710; ReleaseYear = 2020; Aliases = @("RTX 3080", "NVIDIA GeForce RTX 3080", "RTX 3080 SUPRIM X 10G") },
    @{ Vendor = "NVIDIA"; Series = "RTX 30"; ModelName = "GeForce RTX 3090"; Codename = "Ampere"; Architecture = "Ampere"; ProcessNode = "8nm"; Units = 10496; VramGB = 24; BoostMHz = 1695; ReleaseYear = 2020; Aliases = @("RTX 3090", "NVIDIA GeForce RTX 3090", "AORUS GeForce RTX 3090 XTREME 24G") },
    @{ Vendor = "NVIDIA"; Series = "RTX 40"; ModelName = "GeForce RTX 4060 Ti"; Codename = "Ada Lovelace"; Architecture = "Ada Lovelace"; ProcessNode = "4N"; Units = 4352; VramGB = 16; BoostMHz = 2535; ReleaseYear = 2023; Aliases = @("RTX 4060 Ti", "NVIDIA GeForce RTX 4060 Ti", "GeForce RTX 4060 Ti VENTUS 2X BLACK 16G OC") },
    @{ Vendor = "NVIDIA"; Series = "RTX 40"; ModelName = "GeForce RTX 4070 SUPER"; Codename = "Ada Lovelace"; Architecture = "Ada Lovelace"; ProcessNode = "4N"; Units = 7168; VramGB = 12; BoostMHz = 2475; ReleaseYear = 2024; Aliases = @("RTX 4070 SUPER", "NVIDIA GeForce RTX 4070 SUPER", "TUF-RTX4070S-O12G-GAMING", "GV-N407SGAMING OC-12GD") },
    @{ Vendor = "NVIDIA"; Series = "RTX 40"; ModelName = "GeForce RTX 4080 SUPER"; Codename = "Ada Lovelace"; Architecture = "Ada Lovelace"; ProcessNode = "4N"; Units = 10240; VramGB = 16; BoostMHz = 2550; ReleaseYear = 2024; Aliases = @("RTX 4080 SUPER", "NVIDIA GeForce RTX 4080 SUPER", "RTX 4080 SUPER SUPRIM X 16G", "TUF-RTX4080S-O16G-GAMING") },
    @{ Vendor = "NVIDIA"; Series = "RTX 40"; ModelName = "GeForce RTX 4090"; Codename = "Ada Lovelace"; Architecture = "Ada Lovelace"; ProcessNode = "4N"; Units = 16384; VramGB = 24; BoostMHz = 2520; ReleaseYear = 2022; Aliases = @("RTX 4090", "NVIDIA GeForce RTX 4090", "GV-N4090GAMING OC-24GD", "AORUS GeForce RTX 4090 MASTER 24G", "RTX 4090 SUPRIM X 24G", "TUF-RTX4090-O24G-GAMING") },
    @{ Vendor = "NVIDIA"; Series = "RTX 50"; ModelName = "GeForce RTX 5070"; Codename = "Blackwell"; Architecture = "Blackwell"; ProcessNode = "4N"; Units = 6144; VramGB = 12; BoostMHz = 2512; ReleaseYear = 2025; Aliases = @("RTX 5070", "NVIDIA GeForce RTX 5070", "VCG507012TFXXPB1-O", "PNY GeForce RTX 5070 Triple Fan OC", "GeForce RTX 5070 VENTUS 3X OC", "GV-N5070GAMING OC-12GD") },
    @{ Vendor = "NVIDIA"; Series = "RTX 50"; ModelName = "GeForce RTX 5080"; Codename = "Blackwell"; Architecture = "Blackwell"; ProcessNode = "4N"; Units = 10752; VramGB = 16; BoostMHz = 2617; ReleaseYear = 2025; Aliases = @("RTX 5080", "NVIDIA GeForce RTX 5080", "GeForce RTX 5080 SUPRIM SOC", "GV-N5080AORUS M-16GD") },
    @{ Vendor = "AMD"; Series = "RX 6000"; ModelName = "Radeon RX 6800"; Codename = "Navi 21"; Architecture = "RDNA2"; ProcessNode = "7nm"; Units = 3840; VramGB = 16; BoostMHz = 2105; ReleaseYear = 2020; Aliases = @("RX 6800", "AMD Radeon RX 6800", "Speedster MERC 319 RX 6800") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7900 XT"; Codename = "Navi 31"; Architecture = "RDNA3"; ProcessNode = "5nm"; Units = 5376; VramGB = 20; BoostMHz = 2400; ReleaseYear = 2022; Aliases = @("RX 7900 XT", "AMD Radeon RX 7900 XT", "RX7900XT MERC 310", "PowerColor Hellhound Radeon RX 7900 XT") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7900 XTX"; Codename = "Navi 31"; Architecture = "RDNA3"; ProcessNode = "5nm"; Units = 6144; VramGB = 24; BoostMHz = 2500; ReleaseYear = 2022; Aliases = @("RX 7900 XTX", "AMD Radeon RX 7900 XTX", "RX7900XTX NITRO+", "NITRO+ AMD Radeon RX 7900 XTX Vapor-X", "PowerColor Red Devil Radeon RX 7900 XTX", "ASRock Radeon RX 7900 XTX Taichi White") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7900 GRE"; Codename = "Navi 31"; Architecture = "RDNA3"; ProcessNode = "5nm"; Units = 5120; VramGB = 16; BoostMHz = 2245; ReleaseYear = 2024; Aliases = @("RX 7900 GRE", "AMD Radeon RX 7900 GRE", "SAPPHIRE PURE Radeon RX 7900 GRE", "Hellhound RX 7900 GRE") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7800 XT"; Codename = "Navi 32"; Architecture = "RDNA3"; ProcessNode = "5nm"; Units = 3840; VramGB = 16; BoostMHz = 2430; ReleaseYear = 2023; Aliases = @("RX 7800 XT", "AMD Radeon RX 7800 XT", "NITRO+ Radeon RX 7800 XT", "Hellhound RX 7800 XT") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7700 XT"; Codename = "Navi 32"; Architecture = "RDNA3"; ProcessNode = "5nm"; Units = 3456; VramGB = 12; BoostMHz = 2544; ReleaseYear = 2023; Aliases = @("RX 7700 XT", "AMD Radeon RX 7700 XT", "ASRock Challenger RX 7700 XT", "PowerColor Hellhound RX 7700 XT") },
    @{ Vendor = "AMD"; Series = "RX 7000"; ModelName = "Radeon RX 7600 XT"; Codename = "Navi 33"; Architecture = "RDNA3"; ProcessNode = "6nm"; Units = 2048; VramGB = 16; BoostMHz = 2755; ReleaseYear = 2024; Aliases = @("RX 7600 XT", "AMD Radeon RX 7600 XT", "SAPPHIRE PULSE Radeon RX 7600 XT") },
    @{ Vendor = "Intel"; Series = "Arc"; ModelName = "Intel Arc A770"; Codename = "Alchemist"; Architecture = "Xe HPG"; ProcessNode = "6nm"; Units = 4096; VramGB = 16; BoostMHz = 2100; ReleaseYear = 2022; Aliases = @("Arc A770", "Intel Arc A770", "Intel Arc A770 Limited Edition") },
    @{ Vendor = "Intel"; Series = "Arc"; ModelName = "Intel Arc A750"; Codename = "Alchemist"; Architecture = "Xe HPG"; ProcessNode = "6nm"; Units = 3584; VramGB = 8; BoostMHz = 2050; ReleaseYear = 2022; Aliases = @("Arc A750", "Intel Arc A750", "Intel Arc A750 Limited Edition") }
)

$gpus = @()
for ($i = 0; $i -lt $gpuSeeds.Count; $i++) {
    $seed = $gpuSeeds[$i]
    $modelSlug = ($seed.ModelName -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
    $aliases = New-Object System.Collections.Generic.List[string]
    Add-GpuAliasVariants -Aliases $aliases -Vendor $seed.Vendor -ModelName $seed.ModelName -ExtraAliases $seed.Aliases
    $aliases = $aliases | Sort-Object -Unique

    $gpus += [ordered]@{
        id = ("gpu_{0}_{1}" -f $seed.Vendor.ToLowerInvariant(), $modelSlug)
        brand = $seed.Vendor
        series = $seed.Series
        modelName = $seed.ModelName
        generation = $seed.Series
        codename = $seed.Codename
        releaseYear = $seed.ReleaseYear
        architecture = $seed.Architecture
        processNode = $seed.ProcessNode
        units = $seed.Units
        vramGB = $seed.VramGB
        boostMHz = $seed.BoostMHz
        tags = @("gpu", $seed.Vendor.ToLowerInvariant())
        iconKey = Resolve-GpuDbIconKey -Vendor $seed.Vendor -ModelName $seed.ModelName
        aliases = $aliases
        normalizedName = ("{0} {1}" -f $seed.Vendor, $seed.ModelName).ToLowerInvariant()
    }
}

Save-Db "hardware_db_gpu.json" $gpus

function Get-DisplayVendorAliases {
    param(
        [string]$Brand
    )

    switch ($Brand.Trim().ToLowerInvariant()) {
        "dell" { return @("DEL", "DELL", "Dell") }
        "asus" { return @("ACI", "AUS", "ASUS") }
        "acer" { return @("ACR", "Acer") }
        "lg" { return @("GSM", "LGD", "LG Electronics", "LG") }
        "samsung" { return @("SAM", "SEC", "Samsung") }
        "benq" { return @("BNQ", "BenQ") }
        "viewsonic" { return @("VSC", "ViewSonic") }
        "aoc" { return @("AOC") }
        default { return @($Brand) }
    }
}

function Resolve-DisplayDbIconKey {
    param(
        [string]$Brand,
        [string]$Series,
        [string]$ModelName
    )

    $brandKey = $Brand.Trim().ToLowerInvariant()
    $seriesKey = $Series.Trim().ToLowerInvariant()
    $modelKey = $ModelName.Trim().ToLowerInvariant()

    if ($brandKey -eq "dell") {
        if ($seriesKey -match "alienware" -or $modelKey -match "^aw") { return "display_dell_alienware" }
        if ($seriesKey -match "ultrasharp" -or $modelKey -match "^u[0-9]") { return "display_dell_ultrasharp" }
        return "display_dell"
    }

    if ($brandKey -eq "asus") {
        if ($seriesKey -match "rog" -or $modelKey -match "^pg|^xg") { return "display_asus_rog" }
        if ($seriesKey -match "proart" -or $modelKey -match "^pa") { return "display_asus_proart" }
        return "display_asus"
    }

    if ($brandKey -eq "acer") {
        if ($seriesKey -match "predator" -or $modelKey -match "^xb|^x3|^x2") { return "display_acer_predator" }
        return "display_acer"
    }

    if ($brandKey -eq "lg") {
        if ($seriesKey -match "ultragear" -or $modelKey -match "gp|gq|gr|gs") { return "display_lg_ultragear" }
        return "display_lg"
    }

    if ($brandKey -eq "samsung") {
        if ($seriesKey -match "odyssey" -or $modelKey -match "^g[0-9]") { return "display_samsung_odyssey" }
        return "display_samsung"
    }

    if ($brandKey -eq "benq") { return "display_benq" }
    if ($brandKey -eq "viewsonic") { return "display_viewsonic" }
    if ($brandKey -eq "aoc") { return "display_aoc" }
    return "display_default"
}

function Add-DisplayAliasVariants {
    param(
        [System.Collections.Generic.List[string]]$Aliases,
        [string]$Brand,
        [string]$Series,
        [string]$ModelCode,
        [string]$ProductName,
        [string[]]$ExtraAliases
    )

    if ([string]::IsNullOrWhiteSpace($ModelCode) -and [string]::IsNullOrWhiteSpace($ProductName)) {
        return
    }

    $vendorAliases = Get-DisplayVendorAliases -Brand $Brand
    $variants = New-Object System.Collections.Generic.List[string]
    $normalizedBrand = ($Brand -replace "[^A-Za-z0-9]", "").ToLowerInvariant()

    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in @(
        $ModelCode,
        $ProductName,
        ("{0} {1}" -f $Series, $ModelCode).Trim()
    ) + @($ExtraAliases)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $candidates.Add($candidate.Trim())
        }
    }

    foreach ($candidate in @($ModelCode, $ProductName)) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        $normalizedCandidate = ($candidate -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
        if (-not [string]::IsNullOrWhiteSpace($normalizedBrand) -and $normalizedCandidate.StartsWith($normalizedBrand)) {
            continue
        }

        $candidates.Add(("{0} {1}" -f $Brand, $candidate).Trim())
    }

    foreach ($candidate in ($candidates | Sort-Object -Unique)) {
        $variants.Add($candidate)
    }

    foreach ($existing in @($variants)) {
        if ($existing -match "\b([A-Z]{2,4}\d{2,4}[A-Z]{0,4})\b") {
            $variants.Add($Matches[1])
        }

        if ($existing -match "\s*monitor$") {
            $variants.Add(($existing -replace "\s*monitor$", "").Trim())
        }
    }

    foreach ($variant in ($variants | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
        $Aliases.Add($variant.ToLowerInvariant())

        $compact = $variant -replace "[^A-Za-z0-9]", ""
        if (-not [string]::IsNullOrWhiteSpace($compact)) {
            $Aliases.Add($compact.ToLowerInvariant())
        }

        foreach ($vendorAlias in $vendorAliases) {
            $normalizedVariant = ($variant -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            $normalizedVendor = ($vendorAlias -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
            if (-not [string]::IsNullOrWhiteSpace($normalizedVendor) -and $normalizedVariant.StartsWith($normalizedVendor)) {
                continue
            }

            $prefixed = ("{0} {1}" -f $vendorAlias, $variant).Trim()
            if ([string]::IsNullOrWhiteSpace($prefixed)) {
                continue
            }

            $Aliases.Add($prefixed.ToLowerInvariant())

            $prefixedCompact = $prefixed -replace "[^A-Za-z0-9]", ""
            if (-not [string]::IsNullOrWhiteSpace($prefixedCompact)) {
                $Aliases.Add($prefixedCompact.ToLowerInvariant())
            }
        }
    }
}

$displaySeeds = @(
    @{ Brand = "Dell"; Series = "Alienware"; ModelCode = "AW3423DWF"; ProductName = "Alienware AW3423DWF"; PanelType = "QD-OLED"; ScreenSizeInches = 34.2; ReleaseYear = 2022; Aliases = @("AW3423DW", "Alienware 34 Curved QD-OLED") },
    @{ Brand = "Dell"; Series = "Alienware"; ModelCode = "AW3225QF"; ProductName = "Alienware AW3225QF"; PanelType = "QD-OLED"; ScreenSizeInches = 31.6; ReleaseYear = 2024; Aliases = @("Alienware 32 4K QD-OLED") },
    @{ Brand = "Dell"; Series = "UltraSharp"; ModelCode = "U2723QE"; ProductName = "Dell UltraSharp U2723QE"; PanelType = "IPS Black"; ScreenSizeInches = 27.0; ReleaseYear = 2022; Aliases = @("UltraSharp U2723QE") },
    @{ Brand = "Dell"; Series = "UltraSharp"; ModelCode = "U4025QW"; ProductName = "Dell UltraSharp U4025QW"; PanelType = "IPS Black"; ScreenSizeInches = 39.7; ReleaseYear = 2024; Aliases = @("UltraSharp U4025QW") },
    @{ Brand = "ASUS"; Series = "ROG Swift"; ModelCode = "PG32UCDM"; ProductName = "ROG Swift PG32UCDM"; PanelType = "QD-OLED"; ScreenSizeInches = 31.5; ReleaseYear = 2024; Aliases = @("ASUS PG32UCDM", "ROG PG32UCDM") },
    @{ Brand = "ASUS"; Series = "ROG Swift"; ModelCode = "XG27AQM"; ProductName = "ROG Strix XG27AQM"; PanelType = "IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2021; Aliases = @("ASUS XG27AQM", "ROG Strix XG27AQM") },
    @{ Brand = "ASUS"; Series = "ProArt"; ModelCode = "PA279CV"; ProductName = "ProArt Display PA279CV"; PanelType = "IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2021; Aliases = @("ASUS PA279CV", "ProArt PA279CV") },
    @{ Brand = "Acer"; Series = "Predator"; ModelCode = "XB273K"; ProductName = "Predator XB273K"; PanelType = "IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2019; Aliases = @("Acer XB273K", "Predator XB273K GP") },
    @{ Brand = "Acer"; Series = "Predator"; ModelCode = "X34GS"; ProductName = "Predator X34GS"; PanelType = "Nano IPS"; ScreenSizeInches = 34.0; ReleaseYear = 2021; Aliases = @("Acer X34GS") },
    @{ Brand = "LG"; Series = "UltraGear"; ModelCode = "27GP850"; ProductName = "LG UltraGear 27GP850"; PanelType = "Nano IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2021; Aliases = @("27GP850-B", "LG 27GP850", "UltraGear 27GP850") },
    @{ Brand = "LG"; Series = "UltraGear"; ModelCode = "32GQ950"; ProductName = "LG UltraGear 32GQ950"; PanelType = "Nano IPS"; ScreenSizeInches = 32.0; ReleaseYear = 2022; Aliases = @("32GQ950-B", "LG 32GQ950") },
    @{ Brand = "LG"; Series = "UltraGear"; ModelCode = "45GR95QE"; ProductName = "LG UltraGear 45GR95QE"; PanelType = "OLED"; ScreenSizeInches = 44.5; ReleaseYear = 2023; Aliases = @("45GR95QE-B", "LG 45GR95QE") },
    @{ Brand = "Samsung"; Series = "Odyssey"; ModelCode = "G85SB"; ProductName = "Samsung Odyssey OLED G8"; PanelType = "QD-OLED"; ScreenSizeInches = 34.0; ReleaseYear = 2022; Aliases = @("Odyssey G85SB", "LS34BG850SNXZA") },
    @{ Brand = "Samsung"; Series = "Odyssey"; ModelCode = "G95SC"; ProductName = "Samsung Odyssey OLED G9"; PanelType = "QD-OLED"; ScreenSizeInches = 49.0; ReleaseYear = 2023; Aliases = @("Odyssey G95SC", "LS49CG954SNXZA") },
    @{ Brand = "Samsung"; Series = "Odyssey"; ModelCode = "G80SD"; ProductName = "Samsung Odyssey OLED G8 G80SD"; PanelType = "QD-OLED"; ScreenSizeInches = 32.0; ReleaseYear = 2024; Aliases = @("Odyssey G80SD", "LS32DG802SNXZA") },
    @{ Brand = "BenQ"; Series = "MOBIUZ"; ModelCode = "EX2710Q"; ProductName = "BenQ MOBIUZ EX2710Q"; PanelType = "IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2021; Aliases = @("BenQ EX2710Q") },
    @{ Brand = "ViewSonic"; Series = "Elite"; ModelCode = "XG2431"; ProductName = "ViewSonic XG2431"; PanelType = "IPS"; ScreenSizeInches = 24.0; ReleaseYear = 2021; Aliases = @("ViewSonic XG2431") },
    @{ Brand = "AOC"; Series = "AGON"; ModelCode = "AG274QG"; ProductName = "AOC AGON AG274QG"; PanelType = "IPS"; ScreenSizeInches = 27.0; ReleaseYear = 2022; Aliases = @("AOC AG274QG") }
)

$displays = @()
for ($i = 0; $i -lt $displaySeeds.Count; $i++) {
    $seed = $displaySeeds[$i]
    $aliases = New-Object System.Collections.Generic.List[string]
    Add-DisplayAliasVariants -Aliases $aliases -Brand $seed.Brand -Series $seed.Series -ModelCode $seed.ModelCode -ProductName $seed.ProductName -ExtraAliases $seed.Aliases
    $aliases = $aliases | Sort-Object -Unique
    $modelSlug = ($seed.ModelCode -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
    $normalizedProduct = $seed.ProductName.ToLowerInvariant()
    $normalizedBrand = $seed.Brand.ToLowerInvariant()
    $normalizedName = if ($normalizedProduct.StartsWith($normalizedBrand)) {
        $normalizedProduct
    }
    else {
        ("{0} {1}" -f $seed.Brand, $seed.ProductName).ToLowerInvariant()
    }

    $displays += [ordered]@{
        id = ("display_{0}_{1}" -f $seed.Brand.ToLowerInvariant(), $modelSlug)
        brand = $seed.Brand
        series = $seed.Series
        modelName = $seed.ProductName
        generation = $seed.ModelCode
        codename = $seed.ModelCode
        releaseYear = $seed.ReleaseYear
        architecture = "{0}`"" -f $seed.ScreenSizeInches
        processNode = "N/A"
        screenSizeInches = $seed.ScreenSizeInches
        panelType = $seed.PanelType
        tags = @("display", $seed.Brand.ToLowerInvariant())
        iconKey = Resolve-DisplayDbIconKey -Brand $seed.Brand -Series $seed.Series -ModelName $seed.ProductName
        aliases = $aliases
        normalizedName = $normalizedName
    }
}

Save-Db "hardware_db_displays.json" $displays

$chipsetSeeds = @(
    @{ Brand = "Intel"; Model = "Z890"; Gen = "800"; Code = "Arrow Lake PCH" },
    @{ Brand = "Intel"; Model = "Z790"; Gen = "700"; Code = "Raptor Lake PCH" },
    @{ Brand = "Intel"; Model = "Z690"; Gen = "600"; Code = "Alder Lake PCH" },
    @{ Brand = "Intel"; Model = "Z590"; Gen = "500"; Code = "Rocket Lake PCH" },
    @{ Brand = "Intel"; Model = "Z490"; Gen = "400"; Code = "Comet Lake PCH" },
    @{ Brand = "Intel"; Model = "B860"; Gen = "800"; Code = "Arrow Lake PCH" },
    @{ Brand = "Intel"; Model = "B760"; Gen = "700"; Code = "Raptor Lake PCH" },
    @{ Brand = "Intel"; Model = "B660"; Gen = "600"; Code = "Alder Lake PCH" },
    @{ Brand = "Intel"; Model = "H810"; Gen = "800"; Code = "Arrow Lake PCH" },
    @{ Brand = "Intel"; Model = "H770"; Gen = "700"; Code = "Raptor Lake PCH" },
    @{ Brand = "Intel"; Model = "H670"; Gen = "600"; Code = "Alder Lake PCH" },
    @{ Brand = "AMD"; Model = "X870E"; Gen = "AM5"; Code = "Promontory 22" },
    @{ Brand = "AMD"; Model = "X870"; Gen = "AM5"; Code = "Promontory 22" },
    @{ Brand = "AMD"; Model = "X670E"; Gen = "AM5"; Code = "Promontory 21" },
    @{ Brand = "AMD"; Model = "X670"; Gen = "AM5"; Code = "Promontory 21" },
    @{ Brand = "AMD"; Model = "X570"; Gen = "AM4"; Code = "Promontory 19" },
    @{ Brand = "AMD"; Model = "X470"; Gen = "AM4"; Code = "Promontory 14" },
    @{ Brand = "AMD"; Model = "B850"; Gen = "AM5"; Code = "Promontory 22" },
    @{ Brand = "AMD"; Model = "B650E"; Gen = "AM5"; Code = "Promontory 21" },
    @{ Brand = "AMD"; Model = "B650"; Gen = "AM5"; Code = "Promontory 21" },
    @{ Brand = "AMD"; Model = "B550"; Gen = "AM4"; Code = "Promontory 19" },
    @{ Brand = "AMD"; Model = "B450"; Gen = "AM4"; Code = "Promontory 14" },
    @{ Brand = "AMD"; Model = "A620"; Gen = "AM5"; Code = "Promontory 21" },
    @{ Brand = "AMD"; Model = "A520"; Gen = "AM4"; Code = "Promontory 19" }
)

$chipsets = Expand-Deterministic -Seeds $chipsetSeeds -TargetCount 2400 -Builder {
    param($s, $i)

    $rev = [char](65 + ($i % 6))
    $stepping = 1 + ($i % 8)
    $year = 2018 + ($i % 9)
    $normalized = ("{0} {1} rev {2}{3}" -f $s.Brand, $s.Model, $rev, $stepping).ToLower()

    [ordered]@{
        id = ("chipset_{0}_{1}_{2:0000}" -f $s.Brand.ToLower(), $s.Model.ToLower(), $i + 1)
        brand = $s.Brand
        series = $s.Model.Substring(0, 1)
        modelName = ("{0} Rev {1}{2}" -f $s.Model, $rev, $stepping)
        generation = $s.Gen
        codename = $s.Code
        releaseYear = $year
        architecture = "Desktop"
        processNode = "N/A"
        units = 0
        tags = @("chipset", $s.Brand.ToLower())
        iconKey = Resolve-ChipsetIconKey -Brand $s.Brand -Model $s.Model
        aliases = @(
            ("{0} {1}" -f $s.Brand, $s.Model).ToLower(),
            $s.Model.ToLower(),
            ("{0} {1} chipset" -f $s.Brand, $s.Model).ToLower()
        )
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_chipsets.json" $chipsets

$memorySeeds = @(
    @{ Brand = "Corsair"; Series = "Dominator Platinum"; Type = "DDR5" },
    @{ Brand = "Corsair"; Series = "Vengeance"; Type = "DDR5" },
    @{ Brand = "Corsair"; Series = "Vengeance LPX"; Type = "DDR4" },
    @{ Brand = "Kingston"; Series = "Fury Beast"; Type = "DDR5" },
    @{ Brand = "Kingston"; Series = "Fury Renegade"; Type = "DDR4" },
    @{ Brand = "G.Skill"; Series = "Trident Z5"; Type = "DDR5" },
    @{ Brand = "G.Skill"; Series = "Ripjaws S5"; Type = "DDR5" },
    @{ Brand = "Crucial"; Series = "Ballistix"; Type = "DDR4" },
    @{ Brand = "Crucial"; Series = "Pro"; Type = "DDR5" },
    @{ Brand = "Samsung"; Series = "OEM"; Type = "DDR5" },
    @{ Brand = "SK Hynix"; Series = "OEM"; Type = "DDR5" },
    @{ Brand = "TEAMGROUP"; Series = "T-Force Delta"; Type = "DDR5" },
    @{ Brand = "Patriot"; Series = "Viper Steel"; Type = "DDR4" }
)

$memoryRates = @(3200, 3600, 4000, 4800, 5200, 5600, 6000, 6400, 6800, 7200, 7600, 8000)
$memoryKits = @("2x8", "2x16", "2x24", "2x32", "4x16")

$memoryModules = Expand-Deterministic -Seeds $memorySeeds -TargetCount 4200 -Builder {
    param($s, $i)

    $rate = $memoryRates[$i % $memoryRates.Count]
    $kit = $memoryKits[$i % $memoryKits.Count]
    $parts = $kit.Split("x")
    $moduleCount = [int]$parts[0]
    $moduleSizeGb = [int]$parts[1]
    $kitSizeGb = $moduleCount * $moduleSizeGb
    $cas = 30 + ($i % 14)
    $year = 2018 + ($i % 9)
    $model = ("{0} {1}GB ({2}) {3}-{4} CL{5}" -f $s.Series, $kitSizeGb, $kit, $s.Type, $rate, $cas)
    $normalized = ("{0} {1} {2} {3}" -f $s.Brand, $s.Series, $s.Type, $rate).ToLower()

    $kitTag = if ($moduleCount -ge 4) { "quad-kit" } else { "dual-kit" }

    [ordered]@{
        id = ("mem_module_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = $s.Series
        modelName = $model
        generation = $s.Type
        codename = $s.Series
        releaseYear = $year
        architecture = if ($s.Type -eq "DDR5") { "UDIMM" } else { "UDIMM/SODIMM" }
        processNode = "N/A"
        units = $moduleCount
        memoryType = $s.Type
        maxDataRateMTs = $rate
        tags = @("module", $kitTag)
        iconKey = Resolve-MemoryModuleIconKey -Brand $s.Brand -Series $s.Series -Type $s.Type
        aliases = @(
            ("{0} {1} {2} {3}" -f $s.Brand, $s.Series, $s.Type, $rate).ToLower(),
            ("{0} {1} {2}" -f $s.Brand, $s.Type, $rate).ToLower()
        )
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_memory_modules.json" $memoryModules

$chipSeeds = @(
    @{ Brand = "Samsung"; Prefix = "K4"; Family = "DDR5" },
    @{ Brand = "SK Hynix"; Prefix = "H5"; Family = "DDR5" },
    @{ Brand = "Micron"; Prefix = "MT"; Family = "DDR5" },
    @{ Brand = "Nanya"; Prefix = "NT"; Family = "DDR4" },
    @{ Brand = "Winbond"; Prefix = "W"; Family = "DDR4" },
    @{ Brand = "Samsung"; Prefix = "K4"; Family = "GDDR6" },
    @{ Brand = "Micron"; Prefix = "MT"; Family = "GDDR6" },
    @{ Brand = "SK Hynix"; Prefix = "H5"; Family = "DDR4" }
)

$densities = @("8G", "16G", "24G", "32G")

$memoryChips = Expand-Deterministic -Seeds $chipSeeds -TargetCount 5200 -Builder {
    param($s, $i)

    $density = $densities[$i % $densities.Count]
    $speedBin = 4000 + (($i % 12) * 400)
    $part = "{0}{1}{2}{3:000}" -f $s.Prefix, $s.Family.Replace("DDR", "D"), $density, ($i % 700)
    $year = 2017 + ($i % 10)
    $normalized = ("{0} {1}" -f $s.Brand, $part).ToLower()

    [ordered]@{
        id = ("memchip_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = $s.Family
        modelName = ("{0} {1} {2}MT/s" -f $part, $density, $speedBin)
        generation = $s.Family
        codename = ("{0} {1}" -f $s.Brand, $s.Family)
        releaseYear = $year
        architecture = "DRAM"
        processNode = "N/A"
        units = 16
        vendorPartFamily = $s.Prefix
        tags = @("chip")
        iconKey = Resolve-MemoryChipIconKey -Brand $s.Brand -Family $s.Family
        aliases = @(
            ("{0} {1}" -f $s.Brand, $part).ToLower(),
            ("{0} {1}" -f $s.Brand, $s.Family).ToLower()
        )
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_memory_chips.json" $memoryChips

$storageSeeds = @(
    @{
        Brand = "Samsung"; Series = "990 PRO"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Pascal"; Nand = "TLC V-NAND"; CapacitiesGb = @(1000, 2000, 4000); ReleaseYear = 2022;
        SeqReadMbps = 7450; SeqWriteMbps = 6900; RandomReadIops = 1200; RandomWriteIops = 1550; TbwPerTb = 600;
        ExtraAliases = @("Samsung SSD 990 PRO", "990 PRO");
        CapacityAliasesByGb = @{
            "1000" = @("MZ-V9P1T0B")
            "2000" = @("MZ-V9P2T0B")
            "4000" = @("MZ-V9P4T0B")
        }
    },
    @{
        Brand = "Samsung"; Series = "980 PRO"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Elpis"; Nand = "TLC V-NAND"; CapacitiesGb = @(500, 1000, 2000); ReleaseYear = 2020;
        SeqReadMbps = 7000; SeqWriteMbps = 5100; RandomReadIops = 1000; RandomWriteIops = 1000; TbwPerTb = 600;
        ExtraAliases = @("Samsung SSD 980 PRO", "980 PRO");
        CapacityAliasesByGb = @{
            "500" = @("MZ-V8P500BW")
            "1000" = @("MZ-V8P1T0BW")
            "2000" = @("MZ-V8P2T0BW")
        }
    },
    @{
        Brand = "Samsung"; Series = "970 EVO Plus"; Type = "NVMe SSD"; Interface = "PCIe 3.0 x4"; FormFactor = "M.2 2280";
        Controller = "Phoenix"; Nand = "TLC V-NAND"; CapacitiesGb = @(250, 500, 1000, 2000); ReleaseYear = 2019;
        SeqReadMbps = 3500; SeqWriteMbps = 3300; RandomReadIops = 620; RandomWriteIops = 560; TbwPerTb = 600;
        ExtraAliases = @("Samsung SSD 970 EVO Plus", "970 EVO Plus");
        CapacityAliasesByGb = @{
            "250" = @("MZ-V7S250BW")
            "500" = @("MZ-V7S500BW")
            "1000" = @("MZ-V7S1T0BW")
            "2000" = @("MZ-V7S2T0BW")
        }
    },
    @{
        Brand = "WD"; Series = "Black SN850X"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "SanDisk NVMe"; Nand = "TLC 3D NAND"; CapacitiesGb = @(1000, 2000, 4000); ReleaseYear = 2022;
        SeqReadMbps = 7300; SeqWriteMbps = 6600; RandomReadIops = 1200; RandomWriteIops = 1100; TbwPerTb = 600;
        ExtraAliases = @("WD_BLACK SN850X", "SN850X");
        CapacityAliasesByGb = @{
            "1000" = @("WDBB9G0010BNC-WRSN")
            "2000" = @("WDBB9G0020BNC-WRSN")
            "4000" = @("WDBB9G0040BNC-WRSN")
        }
    },
    @{
        Brand = "WD"; Series = "Blue SN580"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "SanDisk DRAM-less"; Nand = "TLC 3D NAND"; CapacitiesGb = @(500, 1000, 2000); ReleaseYear = 2023;
        SeqReadMbps = 4150; SeqWriteMbps = 4150; RandomReadIops = 600; RandomWriteIops = 750; TbwPerTb = 600;
        ExtraAliases = @("WD Blue SN580", "SN580");
        CapacityAliasesByGb = @{
            "500" = @("WDS500G3B0E")
            "1000" = @("WDS100T3B0E")
            "2000" = @("WDS200T3B0E")
        }
    },
    @{
        Brand = "Crucial"; Series = "P5 Plus"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Micron DM02A1"; Nand = "Micron TLC NAND"; CapacitiesGb = @(500, 1000, 2000); ReleaseYear = 2021;
        SeqReadMbps = 6600; SeqWriteMbps = 5000; RandomReadIops = 720; RandomWriteIops = 700; TbwPerTb = 600;
        ExtraAliases = @("Crucial P5 Plus", "P5 Plus");
        CapacityAliasesByGb = @{
            "500" = @("CT500P5PSSD8")
            "1000" = @("CT1000P5PSSD8")
            "2000" = @("CT2000P5PSSD8")
        }
    },
    @{
        Brand = "Crucial"; Series = "MX500"; Type = "SATA SSD"; Interface = "SATA 6Gb/s"; FormFactor = "2.5-inch";
        Controller = "SM2258"; Nand = "Micron 3D TLC"; CapacitiesGb = @(500, 1000, 2000, 4000); ReleaseYear = 2018;
        SeqReadMbps = 560; SeqWriteMbps = 510; RandomReadIops = 95000; RandomWriteIops = 90000; TbwPerTb = 360;
        ExtraAliases = @("Crucial MX500", "MX500");
        CapacityAliasesByGb = @{
            "500" = @("CT500MX500SSD1")
            "1000" = @("CT1000MX500SSD1")
            "2000" = @("CT2000MX500SSD1")
            "4000" = @("CT4000MX500SSD1")
        }
    },
    @{
        Brand = "Kingston"; Series = "KC3000"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Phison E18"; Nand = "TLC 3D NAND"; CapacitiesGb = @(1000, 2000, 4000); ReleaseYear = 2021;
        SeqReadMbps = 7000; SeqWriteMbps = 7000; RandomReadIops = 1000; RandomWriteIops = 1000; TbwPerTb = 800;
        ExtraAliases = @("Kingston KC3000", "KC3000");
        CapacityAliasesByGb = @{
            "1000" = @("SKC3000S/1024G")
            "2000" = @("SKC3000D/2048G")
            "4000" = @("SKC3000D/4096G")
        }
    },
    @{
        Brand = "Corsair"; Series = "MP600 PRO LPX"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Phison E18"; Nand = "TLC 3D NAND"; CapacitiesGb = @(1000, 2000, 4000); ReleaseYear = 2022;
        SeqReadMbps = 7100; SeqWriteMbps = 6800; RandomReadIops = 1000; RandomWriteIops = 1200; TbwPerTb = 700;
        ExtraAliases = @("Corsair MP600 PRO LPX", "MP600 PRO LPX", "MP600");
        CapacityAliasesByGb = @{
            "1000" = @("CSSD-F1000GBMP600PLP")
            "2000" = @("CSSD-F2000GBMP600PLP")
            "4000" = @("CSSD-F4000GBMP600PLP")
        }
    },
    @{
        Brand = "Kioxia"; Series = "Exceria G2"; Type = "NVMe SSD"; Interface = "PCIe 3.0 x4"; FormFactor = "M.2 2280";
        Controller = "BiCS Flash"; Nand = "3D TLC"; CapacitiesGb = @(1000, 2000); ReleaseYear = 2021;
        SeqReadMbps = 2100; SeqWriteMbps = 1700; RandomReadIops = 400; RandomWriteIops = 400; TbwPerTb = 400;
        ExtraAliases = @("KIOXIA EXCERIA G2", "EXCERIA G2");
        CapacityAliasesByGb = @{
            "1000" = @("LRC20Z001TG8")
            "2000" = @("LRC20Z002TG8")
        }
    },
    @{
        Brand = "Seagate"; Series = "FireCuda 530"; Type = "NVMe SSD"; Interface = "PCIe 4.0 x4"; FormFactor = "M.2 2280";
        Controller = "Phison E18"; Nand = "TLC 3D NAND"; CapacitiesGb = @(1000, 2000, 4000); ReleaseYear = 2021;
        SeqReadMbps = 7300; SeqWriteMbps = 6900; RandomReadIops = 1000; RandomWriteIops = 1000; TbwPerTb = 1275;
        ExtraAliases = @("Seagate FireCuda 530", "FireCuda 530");
        CapacityAliasesByGb = @{
            "1000" = @("ZP1000GM3A013")
            "2000" = @("ZP2000GM3A013")
            "4000" = @("ZP4000GM3A013")
        }
    },
    @{
        Brand = "Seagate"; Series = "Barracuda"; Type = "HDD"; Interface = "SATA 6Gb/s"; FormFactor = "3.5-inch";
        Controller = "CMR/SMR"; Nand = "Magnetic"; CapacitiesGb = @(1000, 2000, 4000, 8000); ReleaseYear = 2018;
        SeqReadMbps = 220; SeqWriteMbps = 210; RandomReadIops = 0; RandomWriteIops = 0; TbwPerTb = 0;
        ExtraAliases = @("Seagate Barracuda", "Barracuda");
        CapacityAliasesByGb = @{
            "1000" = @("ST1000DM010")
            "2000" = @("ST2000DM008")
            "4000" = @("ST4000DM004")
        }
    }
)

$storageControllers = Expand-Deterministic -Seeds $storageSeeds -TargetCount 4800 -Builder {
    param($s, $i)

    $seedInstance = [int]($i / $storageSeeds.Count)
    $capacityGb = $s.CapacitiesGb[$seedInstance % $s.CapacitiesGb.Count]
    $capacityLabels = @(Get-StorageCapacityAliasLabels -CapacityGb $capacityGb)
    $capacityLabel = $capacityLabels[0]
    $queueDepth = if ($s.Interface -like "SATA*") { 32 } elseif ($s.Interface -like "USB*") { 64 } else { 65535 }
    $tbw = if ($s.TbwPerTb -gt 0) { [int]([Math]::Round(($capacityGb / 1000.0) * $s.TbwPerTb)) } else { 0 }
    $typeTag = if ($s.Type -like "*NVMe*") { "nvme" } elseif ($s.Type -like "*HDD*") { "hdd" } else { "ssd" }
    $modelName = ("{0} {1} {2}" -f $s.Brand, $s.Series, $capacityLabel).Trim()
    $year = $s.ReleaseYear + ($seedInstance % 2)
    $normalized = $modelName.ToLower()

    $aliases = New-Object System.Collections.Generic.List[string]
    $aliases.Add($modelName.ToLower())
    $aliases.Add(("{0} {1}" -f $s.Brand, $s.Series).ToLower())
    $aliases.Add($s.Series.ToLower())
    foreach ($label in $capacityLabels) {
        $aliases.Add(("{0} {1}" -f $s.Series, $label).ToLower())
        $aliases.Add(("{0} {1} {2}" -f $s.Brand, $s.Series, $label).ToLower())
    }
    if ($s.Type -like "*SSD*") {
        $aliases.Add(("{0} SSD {1}" -f $s.Brand, $s.Series).ToLower())
        foreach ($label in $capacityLabels) {
            $aliases.Add(("{0} SSD {1} {2}" -f $s.Brand, $s.Series, $label).ToLower())
        }
    }
    foreach ($extraAlias in ($s.ExtraAliases | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $aliases.Add($extraAlias.ToLower())
        foreach ($label in $capacityLabels) {
            $aliases.Add(("{0} {1}" -f $extraAlias, $label).ToLower())
        }
    }
    if ($s.ContainsKey("CapacityAliasesByGb") -and $null -ne $s.CapacityAliasesByGb) {
        $capacityKey = [string]$capacityGb
        if ($s.CapacityAliasesByGb.ContainsKey($capacityKey)) {
            foreach ($capacityAlias in ($s.CapacityAliasesByGb[$capacityKey] | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
                Add-StorageAliasVariants -Aliases $aliases -Brand $s.Brand -Type $s.Type -Alias $capacityAlias
            }
        }
    }
    $aliases = $aliases | Sort-Object -Unique

    [ordered]@{
        id = ("storctrl_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = $s.Series
        modelName = $modelName
        generation = $s.Type
        codename = $s.Controller
        releaseYear = $year
        architecture = $s.FormFactor
        processNode = $s.Nand
        units = 1
        interface = $s.Interface
        maxQueueDepth = $queueDepth
        tags = @("storage", "product", $typeTag, $s.Brand.ToLower())
        iconKey = Resolve-StorageControllerIconKey -Brand $s.Brand -Model $modelName -Interface $s.Interface
        aliases = $aliases
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_storage_controllers.json" $storageControllers

$usbSeeds = @(
    @{ Brand = "ASMedia"; Model = "ASM3142"; Standard = "USB 3.2 Gen2" },
    @{ Brand = "ASMedia"; Model = "ASM3242"; Standard = "USB4 40Gbps" },
    @{ Brand = "Renesas"; Model = "uPD720202"; Standard = "USB 3.0" },
    @{ Brand = "Intel"; Model = "Thunderbolt USB4"; Standard = "USB4 40Gbps" },
    @{ Brand = "AMD"; Model = "Promontory USB"; Standard = "USB 3.2 Gen2" },
    @{ Brand = "VIA"; Model = "VL805"; Standard = "USB 3.0" },
    @{ Brand = "Fresco Logic"; Model = "FL1100"; Standard = "USB 3.0" }
)

$usbControllers = Expand-Deterministic -Seeds $usbSeeds -TargetCount 3600 -Builder {
    param($s, $i)

    $rev = [char](65 + ($i % 4))
    $year = 2016 + ($i % 11)
    $normalized = ("{0} {1} rev {2}" -f $s.Brand, $s.Model, $rev).ToLower()

    [ordered]@{
        id = ("usbctrl_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = "USB Controller"
        modelName = ("{0} Rev {1}" -f $s.Model, $rev)
        generation = $s.Standard
        codename = $s.Model
        releaseYear = $year
        architecture = "USB"
        processNode = "N/A"
        units = 2
        usbStandard = $s.Standard
        tags = @("usb", "controller")
        iconKey = Resolve-UsbIconKey -Brand $s.Brand -Model $s.Model -Standard $s.Standard
        aliases = @(
            ("{0} {1}" -f $s.Brand, $s.Model).ToLower(),
            $s.Model.ToLower()
        )
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_usb_controllers.json" $usbControllers

$motherboardSeeds = @(
    @{ Brand = "ASUS"; Series = "ROG"; Chipset = "Z790"; Socket = "LGA1700"; Models = @("ROG STRIX Z790-E GAMING WIFI", "ROG STRIX Z790-F GAMING WIFI", "ROG STRIX Z790-A GAMING WIFI D4", "ROG MAXIMUS Z790 HERO", "ROG MAXIMUS Z790 DARK HERO") },
    @{ Brand = "ASUS"; Series = "TUF Gaming"; Chipset = "B650"; Socket = "AM5"; Models = @("TUF GAMING B650-PLUS WIFI", "TUF GAMING B650-E WIFI", "TUF GAMING B650M-PLUS WIFI", "TUF GAMING B650-PLUS") },
    @{ Brand = "ASUS"; Series = "Prime"; Chipset = "Z690"; Socket = "LGA1700"; Models = @("PRIME Z690-A", "PRIME Z690-P WIFI", "PRIME Z690M-PLUS D4", "PRIME Z690-A WIFI") },
    @{ Brand = "MSI"; Series = "MEG"; Chipset = "Z790"; Socket = "LGA1700"; Models = @("MEG Z790 ACE", "MEG Z790 ACE MAX", "MEG Z790 GODLIKE", "MEG Z790 UNIFY-X") },
    @{ Brand = "MSI"; Series = "MAG"; Chipset = "B760"; Socket = "LGA1700"; Models = @("MAG B760 TOMAHAWK WIFI", "MAG B760 TOMAHAWK MAX WIFI", "MAG B760M MORTAR WIFI", "MAG B760M BAZOOKA") },
    @{ Brand = "MSI"; Series = "MPG"; Chipset = "X670E"; Socket = "AM5"; Models = @("MPG X670E CARBON WIFI", "MPG X670E CARBON MAX WIFI", "MPG X670E EDGE WIFI", "MPG X670E GAMING PLUS WIFI") },
    @{ Brand = "Gigabyte"; Series = "AORUS"; Chipset = "Z790"; Socket = "LGA1700"; Models = @("Z790 AORUS ELITE AX", "Z790 AORUS ELITE X WIFI7", "Z790 AORUS PRO X", "Z790 AORUS MASTER X") },
    @{ Brand = "Gigabyte"; Series = "AORUS"; Chipset = "B650"; Socket = "AM5"; Models = @("B650 AORUS ELITE AX", "B650E AORUS ELITE X AX ICE", "B650 AORUS PRO AX", "B650I AORUS ULTRA") },
    @{ Brand = "Gigabyte"; Series = "Gaming"; Chipset = "B650"; Socket = "AM5"; Models = @("B650 GAMING X AX", "B650 GAMING X AX V2", "B650M GAMING X AX", "B650M GAMING WIFI6") },
    @{ Brand = "ASRock"; Series = "Phantom Gaming"; Chipset = "B650E"; Socket = "AM5"; Models = @("B650E PG Riptide WiFi", "B650E PG-ITX WiFi", "B650E Phantom Gaming 4", "B650E PG Lightning") },
    @{ Brand = "ASRock"; Series = "Taichi"; Chipset = "X870E"; Socket = "AM5"; Models = @("X870E Taichi", "X870E Taichi Lite", "X870E Taichi Carrara") },
    @{ Brand = "ASRock"; Series = "Steel Legend"; Chipset = "B550"; Socket = "AM4"; Models = @("B550 Steel Legend", "B550M Steel Legend", "B550 Steel Legend WiFi") },
    @{ Brand = "Biostar"; Series = "Racing"; Chipset = "B650"; Socket = "AM5"; Models = @("RACING B650GTQ", "B650EGTQ", "B650MP-E PRO") },
    @{ Brand = "EVGA"; Series = "Dark"; Chipset = "Z790"; Socket = "LGA1700"; Models = @("Z790 DARK K|NGP|N", "Z790 CLASSIFIED") },
    @{ Brand = "Supermicro"; Series = "Workstation"; Chipset = "W680"; Socket = "LGA1700"; Models = @("X13SAE-F", "X13SWA-TF", "X13SRA-TF") }
)

$formFactors = @("ATX", "mATX", "Mini-ITX")
$motherboardSeedCount = $motherboardSeeds.Count

$motherboards = Expand-Deterministic -Seeds $motherboardSeeds -TargetCount 5400 -Builder {
    param($s, $i)

    $seedCycleIndex = [int][math]::Floor($i / $motherboardSeedCount)
    $form = $formFactors[$i % $formFactors.Count]
    $revision = 1 + ($i % 5)
    $year = 2019 + ($i % 8)
    $models = @($s.Models)
    if ($models.Count -gt 0) {
        $modelName = $models[$seedCycleIndex % $models.Count]
    }
    else {
        $modelName = ("{0} {1}" -f $s.Chipset, $s.Series)
    }

    $aliases = New-Object System.Collections.Generic.List[string]
    Add-MotherboardAliasVariants -Aliases $aliases -Brand $s.Brand -Series $s.Series -Chipset $s.Chipset -ModelName $modelName
    $aliases = $aliases | Sort-Object -Unique
    $normalized = ("{0} {1} rev {2}" -f $s.Brand, $modelName, $revision).ToLower()

    [ordered]@{
        id = ("mb_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = $s.Series
        modelName = $modelName
        generation = $s.Socket
        codename = $s.Chipset
        releaseYear = $year
        architecture = $form
        processNode = "N/A"
        units = 4
        chipset = $s.Chipset
        socket = $s.Socket
        tags = @("motherboard")
        iconKey = Resolve-MotherboardIconKey -Brand $s.Brand -Series $s.Series
        aliases = $aliases
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_motherboards.json" $motherboards

$networkSeeds = @(
    @{ Brand = "Intel"; Model = "I219-V"; Gen = "Ethernet 1GbE" },
    @{ Brand = "Intel"; Model = "I225-V"; Gen = "Ethernet 2.5GbE" },
    @{ Brand = "Intel"; Model = "I226-V"; Gen = "Ethernet 2.5GbE" },
    @{ Brand = "Intel"; Model = "AX200"; Gen = "Wi-Fi 6" },
    @{ Brand = "Intel"; Model = "AX201"; Gen = "Wi-Fi 6" },
    @{ Brand = "Intel"; Model = "AX210"; Gen = "Wi-Fi 6E" },
    @{ Brand = "Intel"; Model = "AX211"; Gen = "Wi-Fi 6E" },
    @{ Brand = "Realtek"; Model = "RTL8111H"; Gen = "Ethernet 1GbE" },
    @{ Brand = "Realtek"; Model = "RTL8125B"; Gen = "Ethernet 2.5GbE" },
    @{ Brand = "Realtek"; Model = "RTL8852BE"; Gen = "Wi-Fi 6" },
    @{ Brand = "Killer"; Model = "E2600"; Gen = "Ethernet 2.5GbE" },
    @{ Brand = "Killer"; Model = "E3100G"; Gen = "Ethernet 2.5GbE" },
    @{ Brand = "Broadcom"; Model = "BCM57781"; Gen = "Ethernet 1GbE" },
    @{ Brand = "MediaTek"; Model = "MT7921"; Gen = "Wi-Fi 6" },
    @{ Brand = "Qualcomm"; Model = "NCM865"; Gen = "Wi-Fi 7" }
)

$networkAdapters = Expand-Deterministic -Seeds $networkSeeds -TargetCount 4200 -Builder {
    param($s, $i)

    $rev = [char](65 + ($i % 5))
    $lanClass = if ($s.Gen -like "Wi-Fi*") { "WLAN" } else { "LAN" }
    $year = 2014 + ($i % 12)
    $normalized = ("{0} {1} rev {2}" -f $s.Brand, $s.Model, $rev).ToLower()

    [ordered]@{
        id = ("net_{0:00000}" -f ($i + 1))
        brand = $s.Brand
        series = $s.Gen
        modelName = ("{0} Rev {1}" -f $s.Model, $rev)
        generation = $s.Gen
        codename = $lanClass
        releaseYear = $year
        architecture = "NIC"
        processNode = "N/A"
        units = 1
        tags = @("network")
        iconKey = Resolve-NetworkIconKey -Brand $s.Brand -Model $s.Model -Generation $s.Gen
        aliases = @(
            ("{0} {1}" -f $s.Brand, $s.Model).ToLower(),
            $s.Model.ToLower(),
            ("{0} nic" -f $s.Brand).ToLower(),
            ("{0} {1}" -f $s.Brand, $s.Gen).ToLower()
        )
        normalizedName = $normalized
    }
}

Save-Db "hardware_db_network_adapters.json" $networkAdapters

$size = (Get-ChildItem $base -File | Measure-Object -Property Length -Sum).Sum
Write-Output ("DB_TOTAL_BYTES=" + $size)
