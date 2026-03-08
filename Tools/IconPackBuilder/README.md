# Icon Pack Builder

Generates icon manifest and folder structure for Windows Optimizer hardware database.

## Generated Files

| File | Description |
|------|-------------|
| `HardwareIconMap.json` | Icon mapping rules with priority |
| `HardwareIconMap.schema.json` | JSON schema for validation |
| `IconPackManifest.json` | Required icons list |
| `IconPackManifest.schema.json` | Manifest schema |

## Icon Resolution Order

```
1. Series Match (priority 90-100)  → e.g., "rtx 50" → gpu_rtx50
2. Vendor Match (priority 70-89)   → e.g., "corsair" → memory_corsair  
3. Type Match (priority 40-69)     → e.g., "intel" → cpu_intel
4. Fallback                         → e.g., cpu_default
```

## Folder Structure

```
Assets/Icons/
├── cpu/
│   ├── cpu_default.png
│   ├── cpu_intel.png
│   ├── cpu_i5.png
│   └── ...
├── gpu/
│   ├── gpu_default.png
│   ├── gpu_nvidia.png
│   ├── gpu_rtx.png
│   └── ...
├── motherboard/
├── chipset/
├── memory/
├── storage/
├── network/
├── usb/
├── display/
├── brand/
│   ├── asus.png
│   ├── msi.png
│   └── ...
└── os/
    ├── windows10.png
    └── windows11.png
```

## Icon Specifications

- **Format**: PNG with transparency
- **Size**: 48x48 or 64x64 pixels recommended
- **Style**: Flat or minimal, consistent with Windows 11 design

## Usage in Code

```csharp
// Using V2 resolver with priority-based matching
var iconKey = HardwareIconResolverV2.ResolveIconKey("cpu", "AMD Ryzen 9 7950X");
// Returns: "cpu_ryzen9"

var iconPath = HardwareIconResolverV2.GetCpuIcon("Intel Core i9-14900K");
// Returns: pack URI for cpu_i9.png

// Direct icon resolution
var image = HardwareIconResolverV2.ResolveIcon("gpu_rtx50", "gpu_default");
```

## Hardware Coverage

| Category | Vendors/Series |
|----------|----------------|
| **CPU** | Intel Core i3/i5/i7/i9/Ultra, Xeon, Pentium, Celeron; AMD Ryzen 3/5/7/9, Threadripper, EPYC, Athlon, Phenom, FX |
| **GPU** | NVIDIA GTX/RTX (10-50 series), Quadro; AMD Radeon RX (500-9000 series), Pro; Intel Arc |
| **Motherboard** | ASUS (ROG/TUF/Prime), MSI (MEG/MPG/MAG), Gigabyte (AORUS), ASRock (Taichi/Phantom), Biostar, Supermicro, EVGA |
| **Chipset** | Intel Z/B/H series (Z890, B760, etc.); AMD X/B/A series (X870, B650, etc.) |
| **Memory** | Corsair (Dominator/Vengeance), Kingston (Fury), G.Skill (Trident/Ripjaws), Crucial (Ballistix), Samsung, SK Hynix, Micron |
| **Storage** | Samsung (990/980/970 Pro), WD (Black/Blue), Seagate (FireCuda/Barracuda), Crucial (P5), Kingston (KC3000), Corsair (MP600) |
| **Network** | Intel (I225/I226/AX200-211), Realtek (RTL8111/8125), Killer (E2600/E3000), Broadcom, MediaTek |
| **USB** | Intel, AMD, ASMedia, Renesas, VIA; USB 2.0/3.0/3.1/3.2/USB4 |
| **Display** | ASUS (ROG/ProArt), Acer (Predator), LG (UltraGear), Samsung (Odyssey), Dell (Alienware/UltraSharp), BenQ, ViewSonic, AOC |

## Adding New Icons

1. Add PNG file to `Assets/Icons/` (flat) or category subfolder
2. Add entry to `HardwareIconMap.json`:
```json
{
  "match": "new hardware pattern",
  "icon": "icon_filename",
  "priority": 85
}
```
3. Add entry to `IconPackManifest.json` for documentation
4. Icons are auto-discovered at runtime via pack URIs
