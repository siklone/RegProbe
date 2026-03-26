# explorer.enable-explorer-compact-mode

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot`
- Tools: `procmon, ghidra, reboot`

The app writes HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\UseCompactMode = 1, the 25H2 raw registry dump lists the same value name under both the machine and current-user Explorer\Advanced branches, and Procmon captures on 2026-03-14 confirmed that Explorer.EXE queries this exact value on shell restart with both Data:1 and Data:0 when the value is toggled. A Ghidra headless pass on 2026-03-26 against ExplorerFrame.dll also decompiled the code path that calls RegGetValueW for UseCompactMode from both HKCU and HKLM Explorer\Advanced. That validates UseCompactMode as a live runtime Explorer preference on this build and resolves the old direction mismatch: the control enables compact view rather than disabling it.

## Current verdict

UseCompactMode is validated as a live Explorer runtime preference because Explorer.EXE queried both 1 and 0 on shell restart in reversible Procmon captures on this build, ExplorerFrame.dll decompiled to direct RegGetValueW reads for the same value under Explorer\Advanced, the app writes the same enabled state, and the 25H2 raw registry dump corroborates that the value family is still present on current builds.
