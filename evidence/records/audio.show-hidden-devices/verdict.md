# audio.show-hidden-devices

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra`
- Tools: `etw, procmon, ghidra`

The app writes HKCU/Software/Microsoft/Multimedia/Audio/DeviceCpl/ShowHiddenDevices = 1 to expose hidden audio devices in the classic Sound control panel. The repo preserves a normalized Procmon summary from the 2026-03-14 capture showing rundll32.exe querying this exact value with both Data:1 and Data:0, while the checked-in Procmon markdown files are placeholders because the raw PML is off-git. A Ghidra headless pass on 2026-03-26 against mmsys.cpl decompiled the corresponding SHGetValueW and SHSetValueW handlers, and a v3.1 runtime lane on 2026-03-27 cleanly applied, launched the Sound control panel, and restored the value on Win25H2Clean. That keeps the record aligned with the current v3.1 cross-layer contract even though no primary Microsoft documentation page for the DeviceCpl contract was captured.

## Current verdict

The record keeps Class A under the current v3.1 matrix because the repo still preserves converged runtime and static proof: a normalized Procmon summary from the 2026-03-14 Sound control panel capture shows rundll32.exe querying ShowHiddenDevices with both 1 and 0, the checked-in Ghidra export decompiles the mmsys.cpl handlers that read and write the same DeviceCpl flag, the 2026-03-27 runtime lane cleanly applied and restored the value on Win25H2Clean, and the app writes the same show-hidden state with a clean restore story. This remains a runtime-observed preference rather than a Microsoft-documented contract.
