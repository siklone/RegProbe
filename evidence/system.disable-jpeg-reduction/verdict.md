# system.disable-jpeg-reduction

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra`
- Tools: `procmon, ghidra`

The current build evidence for JPEGImportQuality now lines up across decomp, Procmon, and a Win25H2Clean reversible probe: shell32.dll carries the JPEGImportQuality transcode path, Explorer.EXE queried JPEGImportQuality = 100 during a shell-driven wallpaper apply, the missing baseline restores cleanly, and the current app writes the same 100 state.

## Current verdict

shell32.dll decompiled to the JPEGImportQuality transcode path, Explorer.EXE queried JPEGImportQuality = 100 in a shell-driven Procmon pass on Win25H2Clean, the reversible VM probe confirmed missing -> 100 -> missing, and the current app writes the same value with a clean restore story. That is enough to treat the current value 100 profile as app-ready even though the setting is not published by Microsoft as a registry contract.
