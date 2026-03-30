# Power-Control Lightweight Runtime Follow-Up for Remaining Two Candidates

Date: 2026-03-30
Snapshot: `RegProbe-Baseline-ToolsHardened-20260330`
Tool: `registry-research-framework/tools/run-power-control-lightweight-runtime-followup.ps1`

## Scope

This follow-up reran the two remaining docs-first power-control candidates that were still below `A` after the earlier tools-hardened lightweight ETW pass:

- `power.control.mf-buffering-threshold`
- `power.control.timer-rebase-threshold-on-drips-exit`

The lane reused the same tools-hardened baseline and lightweight ETW structure:

- short trigger ETW
- split trace start
- split trigger
- split trace stop

`TimerRebaseThresholdOnDripsExit` added a capability gate before any runtime trigger so the VM could prove whether Modern Standby / DRIPS was even available.

## Results

### `power.control.mf-buffering-threshold`

- Trigger profile: `mf-io-burst-short`
- Runtime result: `exact-hit`
- Exact query hits: `1`
- Exact line count: `3`
- Guest shell health: healthy before and after

The new disk I/O burst trigger produced the first exact runtime read for `MfBufferingThreshold` on the tools-hardened baseline. This is sufficient to promote the record to `Class A`.

Canonical artifacts:

- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/summary.json`
- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/results.json`
- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/power-control-mf-buffering-threshold/summary.json`

### `power.control.timer-rebase-threshold-on-drips-exit`

- Capability profile: `modern-standby-capability-check`
- Runtime result: `vm-standby-limitation`
- Modern standby supported: `false`
- Available sleep state on the VM: `Standby (S1)`
- Unavailable states include `Standby (S3)` and `Hibernate`

The current VMware baseline does not expose S0 Low Power Idle / Modern Standby, so the intended DRIPS-exit trigger cannot be exercised on this VM. This is an environment limitation, not a dead-flag conclusion. The record should move to `Class B` with `vm_standby_limitation`.

Canonical artifacts:

- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/summary.json`
- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/results.json`
- `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-033416/power-control-timer-rebase-threshold-on-drips-exit/summary.json`

## Transport Notes

`guestVar` publication through `vmtoolsd info-set` remains unreliable on this baseline and still returns the same argument-shape error. That no longer blocks the lane because the host successfully recovers the compact phase summaries from the guest filesystem after the runtime pass finishes.
