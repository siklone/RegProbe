# Dashboard Hardware Live Check Checklist

Date: 2026-02-27
Scope: Dashboard cards and hardware detail windows (OS, Motherboard, CPU, GPU, Memory, Storage, Displays, Network, USB).

## Preconditions

1. Launch app: `dotnet run --project WindowsOptimizer.App\\WindowsOptimizer.App.csproj`
2. Wait until dashboard loading overlay disappears.
3. Confirm all 9 hardware cards are visible and clickable.

## Card Smoke Checks

1. OS card
- Icon must render (not blank/transparent).
- Subtitle should be OS name (Windows 10/11 variant), not `Loading...`.
- Click opens detail window.

2. Motherboard card
- Icon must render.
- Subtitle should show product/model, not empty.
- Click opens detail window.

3. CPU card
- Icon must render.
- Subtitle should show CPU model.
- Click opens detail window.

4. GPU card
- Icon must render.
- Subtitle should show GPU model.
- Click opens detail window.

5. Memory card
- Icon must render.
- Subtitle should show total RAM.
- Click opens detail window.

6. Storage card
- Icon must render.
- Subtitle should show drive count.
- Click opens detail window.

7. Displays card
- Icon must render.
- Subtitle should show display count.
- Click opens detail window.

8. Network card
- Icon must render.
- Subtitle should show adapter count.
- Click opens detail window.

9. USB card
- Icon must render.
- Subtitle should show USB device count.
- Click opens detail window.

## Detail Window Quality Checks

1. OS detail
- Must include: Product Name, Edition, Build, UBR, Architecture, BIOS Mode, Secure Boot, TPM, Uptime.
- Should include source hints block (Build Source / Version Source etc.) when available.

2. Motherboard detail
- Must include: Manufacturer, Model or Product, BIOS Version, BIOS Vendor, BIOS Date.
- Should include: Chipset, Chassis Type, Expansion Slots, Serial/Asset Tag when available.

3. CPU detail
- Must include: Name, Manufacturer, Cores, Threads, Max Clock.
- Should include: Socket, Bus Speed, L1/L2/L3 cache, Family/Model/Stepping, Microcode.

4. GPU detail
- Must include: Name, Vendor, Driver Version, VRAM.
- Should include: PCI Vendor ID, PCI Device ID, VRAM Type, Driver Date, Resolution/Refresh, PNP Device ID.

5. Memory detail
- Must include: Total Capacity, Modules, Frequency, Memory Type.
- Should include: Configured Speed, Form Factor, Min Voltage, Primary Manufacturer/Module.

6. Storage detail
- Must include: Total Capacity, Drive Count, Primary Drive.
- Should include per-disk rows (`Disk 1 Model`, `Disk 1 Size`, `Disk 1 Interface`, ...).

7. Displays detail
- Must include: Primary Monitor, Resolution, Refresh Rate, Monitor Count.
- Should include: Color Depth, Virtual Desktop, Manufacturer/Model, GPU Output.

8. Network detail
- Must include: Primary Adapter, Type, IPv4, Link Speed.
- Should include: MAC Address (formatted), Gateway, DNS, DHCP, Lease timestamps, adapter counts.

9. USB detail
- Must include: Primary Controller, Controllers, Total Devices.
- Should include: Removable Drives, Vendor ID, Product ID, Device ID, Status.

## Regression Flags (Fail If Any)

1. Any card icon is blank while card text exists.
2. Detail window opens but only shows 1-2 rows where data is expected.
3. Storage detail does not show per-disk rows despite multiple disks.
4. Network MAC shown as raw compact 12-char string instead of colon format.
5. Any detail window keeps skeleton/loading forever.

## Notes

- Icon catalog currently has more key definitions than actual PNGs; missing keys must still fall back to existing default icons, never blank.
- If a field is truly unavailable from WMI/API, it may show as `Unknown`, but the section should still render.
