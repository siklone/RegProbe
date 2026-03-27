# system.disable-startup-delay

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `procmon, ghidra, wpr, reboot`

Win25H2Clean reversible proof still anchors the current-build story for StartupDelayInMSec: the observed baseline is missing, the app profile writes 0, the record retains Procmon shell-restart corroboration for Explorer/Serialize, a bounded WPR lane exercised both `missing` and `0`, and the value restores back to missing cleanly.

## Current verdict

The current v3.1 record stays Class A on a converged-vm basis: the reversible probe confirms missing -> 0 -> missing, the record still carries Procmon shell-restart corroboration for the same path, the Ghidra search confirms the Explorer Serialize string path, and the bounded WPR lane exercised both missing and 0 with clean shell recovery. Microsoft still does not publish the raw registry contract, so this is build-specific runtime evidence rather than official-policy evidence.

## Artifact refs

- `vm-batch-probe-20260320.json` -> evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json
- `procmon-startup-delay.pml.md` -> evidence/files/procmon/system.disable-startup-delay/procmon-startup-delay.pml.md
- `ghidra_explorer_serialize.txt` -> evidence/files/ghidra/system.disable-startup-delay/ghidra_explorer_serialize.txt
- `startup-delay-wpr-summary.json` -> evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/startup-delay-wpr-summary.json
- `startup-delay-0.summary.json` -> evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/0/startup-delay-0.summary.json
- `startup-delay-wpr-trace-20260326.md` -> research/notes/startup-delay-wpr-trace-20260326.md
