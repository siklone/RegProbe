# Kernel/Power 96 Remaining Groups

Date: 2026-03-30

Scope: residual routing after the first completed value-exists lanes from the 96-key broad batch.

## Current broad-batch state

- Total candidates remain `96`.
- Completed or decision-gated from the original broad batch: `11`.
- Remaining safe `value-exists` queue for a generic static/string pass: `5`.
- Remaining `value-exists` candidates that need path-sensitive or policy-specific handling: `2`.
- Remaining `path-only` hold: `73`.
- `path-missing` hold: `2`.

## Completed or decision-gated broad-batch values

These no longer belong in the next generic queue.

- Docs-first 7 already turned into real records:
  - `power.control.class1-initial-unpark-count`
  - `power.control.hibernate-enabled`
  - `power.control.hibernate-enabled-default`
  - `power.control.lid-reliability-state`
  - `power.control.mf-buffering-threshold`
  - `power.control.perf-calculate-actual-utilization`
  - `power.control.timer-rebase-threshold-on-drips-exit`
- Net-new Executive pair already collapsed into `system.executive-additional-worker-threads`:
  - `system.executive-additional-critical-worker-threads`
  - `system.executive-additional-delayed-worker-threads`
- Net-new watchdog pair already collapsed into `power.session-watchdog-timeouts`:
  - `power.session-watchdog-resume-timeout`
  - `power.session-watchdog-sleep-timeout`

## Covered value-exists hold

These are not good next-batch candidates unless we explicitly decide to split existing bundles or re-audit them.

- `power.control.energy-estimation-enabled`
- `power.control.event-processor-enabled`
- `power.session.hiberboot-enabled`

## Recommended next queue

These are the remaining value-exists candidates that still fit the generic batch string-first lane cleanly.

- `power.customize-during-setup`
- `power.source-settings-version`
- `power.session-power-setting-profile`
- `system.executive-uuid-sequence-number`
- `power.control.hiber-file-size-percent`

## Path-sensitive follow-up queue

These still exist on the baseline, but the generic string-first lane is too collision-prone for them.

- `policy.system.enable-virtualization`
  Needs a policy/UAC-specific lane because exact string search can collapse into `EnableVirtualizationBasedSecurity`.
- `system.io-allow-remote-dasd`
  Needs exact-path or path-aware follow-up because earlier static routing collided with the removable-storage policy path.

## Hold groups

- Docs-first `Control\\Power` path-only: `33`
- Docs-first `Session Manager\\Kernel` path-only: `21`
- Remaining path-only and missing-hold groups stay unchanged from the 2026-03-29 phase-0 follow-up manifest.

## Execution note

The next concrete step is a generic static/string-first pass over the 5-candidate safe queue from the tools-hardened baseline, then a separate manual lane for `EnableVirtualization` and `AllowRemoteDASD`.
