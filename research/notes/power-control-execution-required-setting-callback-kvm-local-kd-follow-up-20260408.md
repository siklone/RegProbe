# power control execution-required setting callback - KVM local-KD follow-up - 2026-04-08

## Summary

- A retained KVM local-KD disassembly pass resolved the visible current-build power-request setting callback for the execution-required family.
- `nt!PopPowerRequestExecutionRequiredSettingCallback` first compares the incoming GUID against `nt!GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`.
- The active branch requires a non-null payload and a 4-byte setting length.
- On a matching payload it:
  - cancels `nt!PopPowerRequestExecutionRequiredTimeoutTimer`
  - writes the incoming DWORD into `nt!PopExecutionRequiredTimeout`
  - calls `nt!PopPowerRequestSetExecutionRequiredTimeoutTimer`
  - calls `nt!PopPowerRequestHandleExecutionEnablementUpdate`
- The same pass also reconfirmed the timeout family:
  - `nt!PopPowerRequestExecutionRequiredTimeoutTimer`
  - `nt!PopPowerRequestSetExecutionRequiredTimeoutTimer`
  - `nt!PopPowerRequestExecutionRequiredTimeoutCallback`
  - `nt!PopPowerRequestExecutionRequiredTimeoutWorker`
  - `nt!PopExecutionRequiredTimeout`

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settingcb-20260408a/local-kd-powerrequest-settingcb-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settingcb-20260408a/local-kd-powerrequest-settingcb-20260408a.log`

## Interpretation

- visible current-build setting-callback semantics are now narrower:
  - the exposed callback is timeout-setting specific
  - it updates `PopExecutionRequiredTimeout` and then re-evaluates execution enablement
  - it does not itself demonstrate a registry read or a direct write to the `Allow*ExecutionRequired*` booleans
- narrowed conclusion:
  - the visible current-build family is callback/init/UMPO-query centric with a timeout-setting callback
  - the remaining unresolved question for the `Allow*ExecutionRequired*` pair is whether any separate `Control\Power` registry seeding path exists elsewhere on modern builds
