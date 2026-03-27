# audio.show-disconnected-devices

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr`
- Tools: `etw, procmon, ghidra, wpr`

The app writes HKCU/Software/Microsoft/Multimedia/Audio/DeviceCpl/ShowDisconnectedDevices = 1 to expose disconnected audio devices in the classic Sound control panel. Procmon captures on 2026-03-14 confirmed that rundll32.exe launching mmsys.cpl queries this exact value and reads both Data:1 and Data:0 when the value is toggled. A Ghidra headless pass on 2026-03-26 against mmsys.cpl also decompiled the handler that calls SHGetValueW and SHSetValueW for ShowDisconnectedDevices under the DeviceCpl branch. A focused v3.1 runtime lane on 2026-03-27 then re-ran the missing -> 1 -> missing cycle in Win25H2Clean, launched the classic Sound control panel, captured a WPR GeneralProfile placeholder trace, and ended with healthy shell state. That keeps the record aligned with the v3.1 contract without changing the underlying class basis: Procmon plus Ghidra remain the class-driving proof, and the runtime lane adds explicit rollback and VM-safe runner coverage.

## Current verdict

The registry path is validated as a live runtime preference because the classic Sound control panel queried ShowDisconnectedDevices with both 1 and 0 in reversible Procmon captures on this build, Ghidra decompiled the mmsys.cpl handlers that read and write the same DeviceCpl flag, and the app writes the same show-disconnected state with a clean restore story. The 2026-03-27 v3.1 runtime lane adds an explicit missing -> 1 -> missing VM cycle with a control-panel launch, WPR placeholder trace, and healthy shell recovery, but the class basis still rests on the Procmon plus Ghidra convergence rather than on the runtime lane alone.

## Artifact refs

- `audio-devicecpl-query-20260314-pml.md` -> evidence/files/procmon/audio.show-disconnected-devices/audio-devicecpl-query-20260314-pml.md
- `audio-devicecpl-query-zero-20260314-pml.md` -> evidence/files/procmon/audio.show-disconnected-devices/audio-devicecpl-query-zero-20260314-pml.md
- `audio-devicecpl-ghidra.md` -> evidence/files/ghidra/audio.show-disconnected-devices/audio-devicecpl-ghidra.md
- `summary.json` -> evidence/files/vm-tooling-staging/audio-devicecpl-runtime-showdisconnecteddevices-20260327-104736/summary.json
- `audio-devicecpl.etl.md` -> evidence/files/vm-tooling-staging/audio-devicecpl-runtime-showdisconnecteddevices-20260327-104736/audio-devicecpl.etl.md
- `audio-show-disconnected-devices-v31-reaudit-20260327.md` -> research/notes/audio-show-disconnected-devices-v31-reaudit-20260327.md
