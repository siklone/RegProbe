# audio.show-disconnected-devices v3.1 re-audit

Target: `audio.show-disconnected-devices`
Path: `HKCU\Software\Microsoft\Multimedia\Audio\DeviceCpl`
Value: `ShowDisconnectedDevices`
Baseline: `missing` on the 2026-03-27 runtime lane; separate 2026-03-14 Procmon captures cover the `0` and `1` read states
Candidate: `1`

Observed:
- Procmon on 2026-03-14 captured `rundll32.exe` launching `shell32.dll,Control_RunDLL mmsys.cpl,,0` and querying `HKCU\Software\Microsoft\Multimedia\Audio\DeviceCpl\ShowDisconnectedDevices` with `Data:1` and `Data:0` in separate reversible captures.
- Ghidra on 2026-03-26 decompiled the `mmsys.cpl` DeviceCpl flag handlers and showed `SHGetValueW` / `SHSetValueW` usage for `ShowDisconnectedDevices` under the same registry branch.
- The mapped v3.1 runtime runner on 2026-03-27 re-ran `missing -> 1 -> missing`, launched the classic Sound control panel, wrote a WPR `GeneralProfile` placeholder trace, and ended with healthy shell state.

Restore:
- The Procmon-backed control-panel probe covered the reversible `1 -> 0 -> 1` read cycle.
- The 2026-03-27 runtime lane restored the original missing-value baseline after the control-panel launch.

Contract:
- Class `A` is preserved.
- The class-driving proof remains the existing Procmon plus Ghidra convergence.
- The v3.1 runtime lane strengthens rollback and VM-safe-runner coverage only. It does not replace the Procmon consumer proof and it does not prove the missing-state semantics of the control panel.

Artifacts:
- `evidence/files/procmon/audio.show-disconnected-devices/audio-devicecpl-query-20260314-pml.md`
- `evidence/files/procmon/audio.show-disconnected-devices/audio-devicecpl-query-zero-20260314-pml.md`
- `evidence/files/ghidra/audio.show-disconnected-devices/audio-devicecpl-ghidra.md`
- `evidence/files/vm-tooling-staging/audio-devicecpl-runtime-showdisconnecteddevices-20260327-104736/summary.json`
- `evidence/files/vm-tooling-staging/audio-devicecpl-runtime-showdisconnecteddevices-20260327-104736/audio-devicecpl.etl.md`
