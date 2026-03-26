# system.disable-startup-delay

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `procmon, ghidra, wpr, reboot`

Win25H2Clean reversible proof now covers the full current-build story for StartupDelayInMSec: the observed baseline is missing, the app profile writes 0, Explorer shell restart traces read that path live, a bounded WPR lane captured both `missing` and `0`, and the value restores back to missing cleanly.

## Current verdict

Win25H2Clean reversible proof, a live Explorer Procmon trace, a Ghidra string hit, and a bounded WPR shell-restart lane all line up on StartupDelayInMSec. That is enough to treat the missing baseline and value 0 as the current build contract for this project and keep the tweak app-ready.
