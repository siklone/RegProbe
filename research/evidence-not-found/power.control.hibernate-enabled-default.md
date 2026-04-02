# power.control.hibernate-enabled-default

- Class: `B`
- Record status: `draft`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr, reboot`

## Why it stays negative

Cross-layer evidence is strong, but the clean VMware baseline cannot exercise a real hibernation trigger (`vm_firmware_limitation`), so runtime promotion stays decision-gated.

## Attached references

- `repo-doc` Repo power notes for docs-first power-control values -> Docs/power/power.md
- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `repo-doc` Shared docs-first string triage for current-build ntoskrnl -> evidence/files/vm-tooling-staging/power-control-docs-first-string-20260329-102348/results.json and research/notes/power-control-docs-first-value-exists-static-triage-20260329.md
- `decompilation` Shared Ghidra xref batch for docs-first power-control values -> evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/ghidra-matches.md and evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/evidence.json and research/notes/power-control-docs-first-ghidra-review-20260329.md
- `procmon-trace` Shared clean-baseline guest-processed stepwise Procmon boot log for docs-first power-control values -> evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json and evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/results.json and research/notes/power-control-docs-first-stepwise-runtime-capture-20260329.md and evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/exact-hits.csv
- `procmon-trace` Guest-processed post-boot Procmon trigger batch for remaining docs-first power-control values -> evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/power-control-hibernate-enabled-default/summary.json and evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/power-control-hibernate-enabled-default/power-control-hibernate-enabled-default-postboot-trigger.hits.csv and research/notes/power-control-docs-first-postboot-trigger-capture-20260329.md
