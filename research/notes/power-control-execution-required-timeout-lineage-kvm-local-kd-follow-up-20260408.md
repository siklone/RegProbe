# power control execution-required timeout lineage - KVM local-KD follow-up - 2026-04-08

## Summary

- A retained KVM local-KD follow-up resolved the timeout timer/worker chain behind the execution-required setting callback family.
- `nt!PopPowerRequestSetExecutionRequiredTimeoutTimer` reads `nt!PopExecutionRequiredTimeout`, compares it against `nt!PopExecutionRequiredContext`, and arms `nt!PopPowerRequestExecutionRequiredTimeoutTimer` with `nt!KeSetTimer2`.
- `nt!PopPowerRequestExecutionRequiredTimeoutCallback` does not touch registry state; it simply queues work through `nt!PopQueueWorkItem`.
- `nt!PopPowerRequestExecutionRequiredTimeoutWorker` acquires the power-request push lock, calls `nt!PopPowerRequestHandleExecutionEnablementUpdate`, and then advances the work-item queue with `nt!PopOkayToQueueNextWorkItem`.
- The visible timeout family remains runtime state/timer/worker driven rather than a demonstrated registry reader.

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-timeoutlineage-20260408a/local-kd-powerrequest-timeoutlineage-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-timeoutlineage-20260408a/local-kd-powerrequest-timeoutlineage-20260408a.log`

## Interpretation

- visible current-build timeout lineage now includes:
  - `PopPowerRequestExecutionRequiredSettingCallback`
  - `PopExecutionRequiredTimeout`
  - `PopPowerRequestSetExecutionRequiredTimeoutTimer`
  - `PopPowerRequestExecutionRequiredTimeoutTimer`
  - `PopPowerRequestExecutionRequiredTimeoutCallback`
  - `PopPowerRequestExecutionRequiredTimeoutWorker`
  - `PopPowerRequestHandleExecutionEnablementUpdate`
- narrowed conclusion:
  - the visible current-build family is callback/init/timeout-setting/timer-worker/UMPO centric
  - the remaining unresolved question for the `Allow*ExecutionRequired*` pair is still whether any separate `Control\Power` registry seeding path exists elsewhere on modern builds
