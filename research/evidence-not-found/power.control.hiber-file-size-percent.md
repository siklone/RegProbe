# power.control.hiber-file-size-percent

- Class: `B`
- Record status: `validated`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, wpr, reboot`

## Why it stays negative

Docs, current-build static evidence, KVM local-KD, repeated runtime replays, and a reboot-backed KVM observation now agree on the live Control\\Power path and HiberFileSizePercent state. Promotion stays decision-gated...

## Attached references

- `repo-doc` Repo power notes for HiberFileSizePercent -> Docs/power/power.md
- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `repo-doc` Residual value-exists string triage for HiberFileSizePercent -> evidence/files/vm-tooling-staging/registry-batch-string-20260330-141213/results.json and research/notes/kernel-power-96-residual-value-exists-static-triage-20260330.md
- `etw-trace` Tools-hardened lightweight ETW follow-up for HiberFileSizePercent -> evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/summary.json and evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/results.json and research/notes/power-control-hiber-file-size-percent-lightweight-runtime-20260330.md
- `vm-test` Linux KVM local-KD follow-up for HiberFileSizePercent -> evidence/files/vm-tooling-staging/local-kd-hiber-symbols-20260406a/local-kd-hiber-symbols-20260406a-summary.json and evidence/files/vm-tooling-staging/local-kd-hiber-symbols-20260406a/local-kd-hiber-symbols-20260406a.log and evidence/files/vm-tooling-staging/local-kd-hiber-disasm-20260406a/local-kd-hiber-disasm-20260406a-summary.json and evidence/files/vm-tooling-staging/local-kd-hiber-disasm-20260406a/local-kd-hiber-disasm-20260406a.log and evidence/files/vm-tooling-staging/local-kd-hiber-strings-20260406a/local-kd-hiber-strings-20260406a-summary.json and evidence/files/vm-tooling-staging/local-kd-hiber-strings-20260406a/local-kd-hiber-strings-20260406a.log and research/notes/power-control-hiber-file-size-percent-kvm-local-kd-follow-up-20260406.md
- `vm-test` Linux KVM Procmon runtime replay for HiberFileSizePercent -> evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/hiberfilesizepercent-procmon-kvm-20260407c-summary.json and evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/hiberfilesizepercent-procmon-kvm-20260407c.txt and evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/host-review.json and research/notes/power-control-hiber-file-size-percent-kvm-procmon-runtime-20260407.md
