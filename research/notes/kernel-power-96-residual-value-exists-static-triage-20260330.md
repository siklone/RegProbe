# Kernel/Power 96 Residual Value-Exists Static Triage

Date: 2026-03-30

Source queue: `registry-research-framework/audit/kernel-power-96-residual-value-exists-string-probe-20260330.json`

Source run:

- `evidence/files/vm-tooling-staging/registry-batch-string-20260330-141213/summary.json`

## Result

- Total candidates: `5`
- Candidates with hits: `2`
- Candidates without hits: `3`
- Shell stayed healthy before and after the batch string probe.

## Positive candidates

- `power.control.hiber-file-size-percent`
  - Exact unicode string hit in `C:\Windows\System32\ntoskrnl.exe`
  - Existing repo context already exists via the hibernation-support notes under `power.hide-hibernate-option`
  - Best next lane: docs/static follow-up, then decide whether a VM runtime gate is meaningful on the current baseline

- `system.executive-uuid-sequence-number`
  - Exact unicode string hit in `C:\Windows\System32\ntoskrnl.exe`
  - This stays separate from `system.executive-additional-worker-threads`; the current result is a string hit only, not proof that the Executive worker-thread lane should absorb it
  - Best next lane: path-aware static follow-up

## Negative candidates

- `power.customize-during-setup`
- `power.source-settings-version`
- `power.session-power-setting-profile`

These three produced no exact string hit in the chosen primary binaries on the tools-hardened baseline. That does not make them dead flags, but it does remove them from the strongest immediate queue.

## Recommended next queue

1. `power.control.hiber-file-size-percent`
2. `system.executive-uuid-sequence-number`

## Execution note

`policy.system.enable-virtualization` and `system.io-allow-remote-dasd` remain outside the generic string-first lane because they still need path-aware handling to avoid substring or path-collision false positives.
