# audio.show-hidden-devices v3.1 Retroactive Re-Audit

Date: 2026-03-27

Scope: local repo evidence only. No new VM capture was run in this pass.

## Local evidence used

1. Procmon carry-forward evidence

- The checked-in Procmon files are placeholders, not raw filtered hits:
  - `evidence/files/procmon/audio.show-hidden-devices/audio-devicecpl-query-20260314-pml.md`
  - `evidence/files/procmon/audio.show-hidden-devices/audio-devicecpl-query-zero-20260314-pml.md`
- The normalized hit summary retained in the research record is:
  - `audio_devicecpl_query_20260314.pml: rundll32.exe RegQueryValue HKCU\Software\Microsoft\Multimedia\Audio\DeviceCpl\ShowHiddenDevices Data:1`
  - `audio_devicecpl_query_zero_20260314.pml: rundll32.exe RegQueryValue HKCU\Software\Microsoft\Multimedia\Audio\DeviceCpl\ShowHiddenDevices Data:0`
- This is enough to preserve the prior runtime-consumer claim, but the raw PML is off-git.

2. Static proof

- `evidence/files/ghidra/audio.show-hidden-devices/audio-devicecpl-ghidra.md` decompiles:
  - `FUN_18000a004`, which calls `SHGetValueW(..., L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl", L"ShowHiddenDevices", ...)`
  - `FUN_180008eb0`, which calls `SHSetValueW(..., L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl", L"ShowHiddenDevices", ...)`
- That is direct code-side proof that `mmsys.cpl` reads and writes this DeviceCpl flag on the tested build.

3. v3.1 runtime-lane output

- `evidence/files/vm-tooling-staging/audio-devicecpl-runtime-showhiddendevices-20260327-103758/summary.json` shows a clean `missing -> 1 -> missing` apply/restore cycle on `Win25H2Clean`.
- The same summary shows the Sound control panel launched and exited successfully during the probe.
- The ETL placeholder in that folder confirms the lane ran, but the checked-in repo does not preserve parsed registry-hit details from that ETL.

## v3.1 verdict

- Keep the record at Class `A` under the current v3.1 matrix.
- Reason: the record still has converged non-official evidence across runtime and static layers, and the app mapping plus restore story remain exact.
- Caveat: this remains a runtime-observed DeviceCpl preference, not a Microsoft-documented contract, and the Procmon proof is preserved as a normalized summary rather than as raw checked-in PML output.
