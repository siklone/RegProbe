# performance.disable-taskbar-animations

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot`
- Tools: `procmon, ghidra, reboot`

A guest-side reversible probe on Win25H2Clean confirmed that HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarAnimations = 0 disables taskbar animations and = 1 enables them on this build. Procmon then captured explorer.exe querying the same value with Data:0 and Data:1 in separate reversible passes, and a Ghidra headless pass on Taskbar.dll decompiled a code path that reads TaskbarAnimations from Explorer\Advanced. The app's current write matches the observed disabled state.

## Current verdict

The guest-side reversible probe on Win25H2Clean confirmed the 0 / 1 mapping for TaskbarAnimations, Procmon captured explorer.exe reading both states from the same value, Taskbar.dll decompiled to a direct TaskbarAnimations read under Explorer\Advanced, and the current app write matches the observed disabled state with a clean restore story.
