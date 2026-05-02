# power.control.perf-calculate-actual-utilization

- Class: `B`
- Record status: `validated`
- Tested build: `26100.1.amd64fre.ge_release.240331-1435`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100.1.amd64fre.ge_release.240331-1435: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, wpr, reboot`

## Why it stays negative

This record is strong enough to show, but it still needs a tighter policy edge before it becomes Class A.

## Attached references

- `repo-doc` Repo power notes for docs-first power-control values -> Docs/power/power.md
- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `repo-doc` Shared docs-first string triage for current-build ntoskrnl -> evidence/files/vm-tooling-staging/power-control-docs-first-string-20260329-102348/results.json and research/notes/power-control-docs-first-value-exists-static-triage-20260329.md
- `decompilation` Shared Ghidra xref batch for docs-first power-control values -> evidence/raw/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/ghidra-matches.md and evidence/raw/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/evidence.json and research/notes/power-control-docs-first-ghidra-review-20260329.md
- `procmon-trace` Shared clean-baseline guest-processed stepwise Procmon boot log for docs-first power-control values -> evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json and evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/results.json and research/notes/power-control-docs-first-stepwise-runtime-capture-20260329.md and evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/exact-hits.csv
- `procmon-trace` Guest-processed post-boot Procmon trigger batch for remaining docs-first power-control values -> evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/power-control-perf-calculate-actual-utilization/summary.json and evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/power-control-perf-calculate-actual-utilization/power-control-perf-calculate-actual-utilization-postboot-trigger.hits.csv and research/notes/power-control-docs-first-postboot-trigger-capture-20260329.md
