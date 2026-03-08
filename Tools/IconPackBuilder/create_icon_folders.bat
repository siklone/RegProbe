@echo off
REM Icon Pack Folder Structure Generator
REM Run this script to create the icon folder structure

setlocal enabledelayedexpansion

set "ROOT=..\Assets\Icons"

REM Create category folders
for %%c in (cpu gpu motherboard chipset memory storage network usb display brand os) do (
    if not exist "%ROOT%\%%c" mkdir "%ROOT%\%%c"
)

REM Create default icons (placeholders - add actual PNG files)
REM CPU
echo. > "%ROOT%\cpu_default.png.placeholder"
echo. > "%ROOT%\cpu_intel.png.placeholder"
echo. > "%ROOT%\cpu_intel_ultra.png.placeholder"
echo. > "%ROOT%\cpu_i3.png.placeholder"
echo. > "%ROOT%\cpu_i5.png.placeholder"
echo. > "%ROOT%\cpu_i7.png.placeholder"
echo. > "%ROOT%\cpu_i9.png.placeholder"
echo. > "%ROOT%\cpu_xeon.png.placeholder"
echo. > "%ROOT%\cpu_xeon_w.png.placeholder"
echo. > "%ROOT%\cpu_intel_pentium.png.placeholder"
echo. > "%ROOT%\cpu_intel_celeron.png.placeholder"
echo. > "%ROOT%\cpu_amd.png.placeholder"
echo. > "%ROOT%\amd_ryzen_cpu.png.placeholder"
echo. > "%ROOT%\cpu_ryzen3.png.placeholder"
echo. > "%ROOT%\cpu_ryzen5.png.placeholder"
echo. > "%ROOT%\cpu_ryzen7.png.placeholder"
echo. > "%ROOT%\cpu_ryzen9.png.placeholder"
echo. > "%ROOT%\cpu_threadripper.png.placeholder"
echo. > "%ROOT%\cpu_epyc.png.placeholder"
echo. > "%ROOT%\cpu_amd_athlon.png.placeholder"
echo. > "%ROOT%\cpu_amd_phenom.png.placeholder"
echo. > "%ROOT%\cpu_amd_fx.png.placeholder"

REM GPU
echo. > "%ROOT%\gpu_default.png.placeholder"
echo. > "%ROOT%\gpu_nvidia.png.placeholder"
echo. > "%ROOT%\gpu_gtx.png.placeholder"
echo. > "%ROOT%\gpu_gtx10.png.placeholder"
echo. > "%ROOT%\gpu_gtx16.png.placeholder"
echo. > "%ROOT%\gpu_rtx.png.placeholder"
echo. > "%ROOT%\gpu_rtx20.png.placeholder"
echo. > "%ROOT%\gpu_rtx30.png.placeholder"
echo. > "%ROOT%\gpu_rtx40.png.placeholder"
echo. > "%ROOT%\gpu_rtx50.png.placeholder"
echo. > "%ROOT%\gpu_quadro.png.placeholder"
echo. > "%ROOT%\amd_gpu.png.placeholder"
echo. > "%ROOT%\gpu_radeon.png.placeholder"
echo. > "%ROOT%\gpu_radeon_pro.png.placeholder"
echo. > "%ROOT%\gpu_rx5000.png.placeholder"
echo. > "%ROOT%\gpu_rx6000.png.placeholder"
echo. > "%ROOT%\gpu_rx7000.png.placeholder"
echo. > "%ROOT%\gpu_rx9000.png.placeholder"
echo. > "%ROOT%\gpu_rdna4.png.placeholder"
echo. > "%ROOT%\gpu_intel_arc.png.placeholder"

REM Motherboard
echo. > "%ROOT%\motherboard_default.png.placeholder"
echo. > "%ROOT%\mb_asus.png.placeholder"
echo. > "%ROOT%\mb_asus_rog.png.placeholder"
echo. > "%ROOT%\mb_asus_tuf.png.placeholder"
echo. > "%ROOT%\mb_asus_prime.png.placeholder"
echo. > "%ROOT%\mb_msi.png.placeholder"
echo. > "%ROOT%\mb_msi_meg.png.placeholder"
echo. > "%ROOT%\mb_msi_mpg.png.placeholder"
echo. > "%ROOT%\mb_msi_mag.png.placeholder"
echo. > "%ROOT%\mb_gigabyte.png.placeholder"
echo. > "%ROOT%\mb_gigabyte_aorus.png.placeholder"
echo. > "%ROOT%\mb_gigabyte_gaming.png.placeholder"
echo. > "%ROOT%\mb_asrock.png.placeholder"
echo. > "%ROOT%\mb_asrock_taichi.png.placeholder"
echo. > "%ROOT%\mb_asrock_phantom.png.placeholder"
echo. > "%ROOT%\mb_biostar.png.placeholder"
echo. > "%ROOT%\mb_supermicro.png.placeholder"
echo. > "%ROOT%\mb_evga.png.placeholder"

REM Chipset
echo. > "%ROOT%\chipset_default.png.placeholder"
echo. > "%ROOT%\chipset_intel.png.placeholder"
echo. > "%ROOT%\chipset_amd.png.placeholder"

REM Memory
echo. > "%ROOT%\memory_default.png.placeholder"
echo. > "%ROOT%\memory_ddr4.png.placeholder"
echo. > "%ROOT%\memory_ddr5.png.placeholder"
echo. > "%ROOT%\memory_corsair.png.placeholder"
echo. > "%ROOT%\memory_corsair_dominator.png.placeholder"
echo. > "%ROOT%\memory_corsair_vengeance.png.placeholder"
echo. > "%ROOT%\memory_kingston.png.placeholder"
echo. > "%ROOT%\memory_kingston_fury.png.placeholder"
echo. > "%ROOT%\memory_gskill.png.placeholder"
echo. > "%ROOT%\memory_gskill_trident.png.placeholder"
echo. > "%ROOT%\memory_gskill_ripjaws.png.placeholder"
echo. > "%ROOT%\memory_crucial.png.placeholder"
echo. > "%ROOT%\memory_crucial_ballistix.png.placeholder"
echo. > "%ROOT%\memory_samsung.png.placeholder"
echo. > "%ROOT%\memory_hynix.png.placeholder"
echo. > "%ROOT%\memory_micron.png.placeholder"

REM Storage
echo. > "%ROOT%\storage_default.png.placeholder"
echo. > "%ROOT%\storage_hdd.png.placeholder"
echo. > "%ROOT%\storage_ssd.png.placeholder"
echo. > "%ROOT%\storage_nvme.png.placeholder"

REM Network
echo. > "%ROOT%\network_default.png.placeholder"
echo. > "%ROOT%\network_intel.png.placeholder"
echo. > "%ROOT%\network_realtek.png.placeholder"
echo. > "%ROOT%\network_killer.png.placeholder"
echo. > "%ROOT%\network_broadcom.png.placeholder"
echo. > "%ROOT%\network_mediatek.png.placeholder"

REM USB
echo. > "%ROOT%\usb_default.png.placeholder"

REM Display
echo. > "%ROOT%\display_default.png.placeholder"

REM Brand
echo. > "%ROOT%\asus.png.placeholder"
echo. > "%ROOT%\msi.png.placeholder"
echo. > "%ROOT%\gigabyte.png.placeholder"
echo. > "%ROOT%\asrock.png.placeholder"
echo. > "%ROOT%\intel_core.png.placeholder"
echo. > "%ROOT%\nvidia.png.placeholder"
echo. > "%ROOT%\amd_ryzen.png.placeholder"

REM OS
echo. > "%ROOT%\windows10.png.placeholder"
echo. > "%ROOT%\windows11.png.placeholder"
echo. > "%ROOT%\os\windows10.png.placeholder"
echo. > "%ROOT%\os\windows11.png.placeholder"

echo Icon folder structure created!
echo Please replace .placeholder files with actual PNG icons (48x48 or 64x64 recommended)

endlocal
