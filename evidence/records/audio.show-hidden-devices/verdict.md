# audio.show-hidden-devices

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra`
- Tools: `procmon, ghidra`

The app writes HKCU/Software/Microsoft/Multimedia/Audio/DeviceCpl/ShowHiddenDevices = 1 to expose hidden audio devices in the classic Sound control panel. Procmon captures on 2026-03-14 confirmed that rundll32.exe launching mmsys.cpl queries this exact value and reads both Data:1 and Data:0 when the value is toggled. A Ghidra headless pass on 2026-03-26 against mmsys.cpl also decompiled the handler that calls SHGetValueW and SHSetValueW for ShowHiddenDevices under the DeviceCpl branch. That gives this record both runtime and code-side proof on this build even though a primary Microsoft documentation page for the DeviceCpl contract was not captured.

## Current verdict

The registry path is validated as a live runtime preference because the classic Sound control panel queried ShowHiddenDevices with both 1 and 0 in reversible Procmon captures on this build, Ghidra decompiled the mmsys.cpl handlers that read and write the same DeviceCpl flag, and the app writes the same show-hidden state with a clean restore story.
