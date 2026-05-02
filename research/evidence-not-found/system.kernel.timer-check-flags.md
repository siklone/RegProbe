# system.kernel.timer-check-flags

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

- `repo-doc` Repo system docs assign TimerCheckFlags = 1 -> Docs/system/system.md and research/notes/kernel-power-96-key-routing-20260327.md
- `registry-observation` Observed baseline existence for TimerCheckFlags -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `inference` Current-build string hit for TimerCheckFlags -> evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json
- `analysis-output` WRK source-enrichment retains KeTimerCheckFlags initialization and bit-test semantics -> registry-research-framework/enrichment/outputs/source-enrichment-20260403-192135/master-enrichment.json
- `vm-test` Lightweight runtime batch wrote TimerCheckFlags = 1 and rebooted once -> research/notes/session-manager-kernel-batch-lightweight-runtime-20260331.md and evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json and evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json
- `vm-test` Dedicated KVM local-KD bundle resolves KeTimerCheckFlags = 1 -> evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a-summary.json and evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a.stdout.txt
