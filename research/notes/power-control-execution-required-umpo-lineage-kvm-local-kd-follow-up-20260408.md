# power control execution-required UMPO lineage - KVM local-KD follow-up - 2026-04-08

## Summary

- A retained KVM local-KD follow-up disassembled `nt!PopUmpoSendPowerRequestOverrideQuery` for the execution-required power-request pair.
- The current-build function first calls `nt!PoStoreRequester`, allocates an `Umpo`-tagged buffer with `nt!ExAllocatePool2`, writes message metadata, and then dispatches `nt!PopUmpoSendPowerMessage`.
- The same wildcard pass exposed the adjacent UMPO family:
  - `nt!PopUmpoSendPowerRequestAction`
  - `nt!PopUmpoSendPowerRequestCreate`
  - `nt!PopUmpoSendPowerRequestOverrideCleanup`
  - `nt!PopUmpoSendPowerRequestOverrideQuery`
- The visible current-build override path remains UMPO message/query-centric rather than a demonstrated registry read.

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.log`

## Interpretation

- current-build visible family now includes:
  - `PopPowerRequestExecutionRequiredSettingCallback`
  - `PopPowerRequestInitialize`
  - `PopPowerRequestOverrideInitialize`
  - `PopUmpoSendPowerRequestOverrideQuery`
  - `PopUmpoSendPowerRequestCreate`
  - `PopUmpoSendPowerRequestAction`
  - `PopUmpoSendPowerRequestOverrideCleanup`
  - `PopExecutionRequiredTimeout`
  - `PopPowerRequestHandleExecutionEnablementUpdate`
  - `PopPowerRequestEvaluateExecutionRequiredStatus`
  - `PopPowerRequestCallbackExecutionRequired`
- narrowed conclusion:
  - the visible current-build callback/init/UMPO query family still does not show a registry read
  - the remaining unresolved question is whether any separate `Control\Power` registry seeding path exists elsewhere on modern builds
