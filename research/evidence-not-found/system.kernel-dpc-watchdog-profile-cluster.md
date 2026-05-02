# system.kernel-dpc-watchdog-profile-cluster

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

- `repo-doc` Repo system docs list explicit DpcWatchdogProfile defaults -> Docs/system/system.md
- `vm-test` Dedicated KVM local-KD bundle reads live DPC watchdog profile globals -> evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt
- `vm-test` Dedicated KVM local-KD disassembly shows DPC watchdog reader and validator path -> evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/dpc-watchdog-config-readers-kd-20260407a.stdout.txt
- `vm-test` Dedicated KVM local-KD disassembly excludes KiApplyProcessorDpcLimits as seeding caller -> evidence/files/vm-tooling-staging/dpc-watchdog-apply-limits-kd-20260407b/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-apply-limits-kd-20260407b/dpc-watchdog-apply-limits-kd-20260407b.stdout.txt
- `vm-test` Dedicated KVM local-KD disassembly excludes KiSetProcessorDpcLimits as seeding caller -> evidence/files/vm-tooling-staging/dpc-watchdog-set-limits-kd-20260407a/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-set-limits-kd-20260407a/dpc-watchdog-set-limits-kd-20260407a.stdout.txt
- `vm-test` Dedicated KVM local-KD disassembly shows explicit DPC watchdog writer path -> evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/summary.json and evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/dpc-watchdog-update-config-kd-20260407a.stdout.txt
