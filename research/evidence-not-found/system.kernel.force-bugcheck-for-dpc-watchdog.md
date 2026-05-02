# system.kernel.force-bugcheck-for-dpc-watchdog

- Class: `B`
- Record status: `draft`
- Tested build: `26200.8246`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26200.8246: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `ghidra, wpr, reboot`

## Why it stays negative

Cross-layer evidence is strong, but an explicit policy or supportability gate still blocks promotion.

## Attached references

- `repo-doc` Repo system docs assign ForceBugcheckForDpcWatchdog = 0 -> Docs/system/system.md and research/notes/kernel-power-96-key-routing-20260327.md
- `registry-observation` Observed baseline existence for ForceBugcheckForDpcWatchdog -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `inference` Current-build string hit for ForceBugcheckForDpcWatchdog -> evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json
- `vm-test` Lightweight runtime batch wrote ForceBugcheckForDpcWatchdog = 1 and rebooted once -> research/notes/session-manager-kernel-batch-lightweight-runtime-20260331.md and evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json and evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json
- `vm-test` Dedicated KVM local-KD symbol family bundle resolves KiForceBugcheckForDpcWatchdog = 0 -> evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-force-bugcheck-kd-20260407a/dpc-watchdog-force-bugcheck-kd-20260407a.stdout.txt
- `vm-test` Boot WPR captured Session Manager Kernel QueryValue context but no exact runtime read for ForceBugcheckForDpcWatchdog -> evidence/files/vm-tooling-staging/kernel-timing-wpr-boot-registry-20260412/analysis-notes.md and evidence/files/vm-tooling-staging/kernel-timing-wpr-boot-registry-20260412/session-manager-kernel-context.txt and evidence/files/vm-tooling-staging/kernel-timing-wpr-boot-registry-20260412/findstr-filter-summary.json
