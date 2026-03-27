# audio.show-disconnected-devices

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra`
- Tools: `wpr, procmon, ghidra`

The app writes HKCU/Software/Microsoft/Multimedia/Audio/DeviceCpl/ShowDisconnectedDevices = 1 to expose disconnected audio devices in the classic Sound control panel. Procmon captures on 2026-03-14 confirmed that rundll32.exe launching mmsys.cpl queries this exact value and reads both Data:1 and Data:0 when the value is toggled. A Ghidra headless pass on 2026-03-26 against mmsys.cpl also decompiled the handler that calls SHGetValueW and SHSetValueW for ShowDisconnectedDevices under the DeviceCpl branch. A 2026-03-27 v3.1 runtime runner then re-ran the missing -> 1 -> missing cycle with a WPR GeneralProfile placeholder trace and healthy shell state. That keeps the record aligned with the v3.1 contract without changing the class basis.

## Current verdict

The registry path is validated as a live runtime preference because the classic Sound control panel queried ShowDisconnectedDevices with both 1 and 0 in reversible Procmon captures on this build, Ghidra decompiled the mmsys.cpl handlers that read and write the same DeviceCpl flag, and the app writes the same show-disconnected state with a clean restore story. The v3.1 runtime lane adds rollback and VM-safe-runner coverage, but the Class A basis still rests on the Procmon plus Ghidra convergence.
