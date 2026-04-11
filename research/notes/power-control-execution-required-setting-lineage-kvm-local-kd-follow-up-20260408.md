# power control execution-required setting lineage - KVM local-KD follow-up - 2026-04-08

## Summary

- A retained KVM local-KD wildcard pass narrowed the current-build setting lineage for the execution-required power-request family.
- `x nt!*PowerRequest*Setting*` returned only `nt!PopPowerRequestExecutionRequiredSettingCallback`.
- `x nt!*PowerRequest*Init*` returned `nt!PopPowerRequestInitialize`, `nt!PopPowerRequestOverrideInitialize`, and adjacent power-request init helpers.
- `x nt!*ExecutionRequiredTimeout*` returned the `PopExecutionRequiredTimeout` state variable plus its timeout timer/worker/callback helpers.
- A later retained KVM local-KD disassembly pass showed `PopPowerRequestInitialize` only zeroing per-object fields, while `PopPowerRequestOverrideInitialize` iterated `PopPowerRequestObjectList` and called `PopUmpoSendPowerRequestOverrideQuery`.
- Combined with the earlier reader disassembly pass, the visible current-build lineage is callback/init-centric rather than a demonstrated registry reader.

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a.log`

## Interpretation

- current-build lineage now visible:
  - `PopPowerRequestExecutionRequiredSettingCallback`
  - `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
  - `PopExecutionRequiredTimeout`
  - `PopPowerRequestInitialize`
  - `PopPowerRequestOverrideInitialize`
  - `PopUmpoSendPowerRequestOverrideQuery`
  - `PopPowerRequestHandleExecutionEnablementUpdate`
  - `PopPowerRequestEvaluateExecutionRequiredStatus`
  - `PopPowerRequestCallbackExecutionRequired`
- narrowed conclusion:
  - the execution-required pair is no longer blocked on a generic current-build reader hunt
  - the visible init/override path does not currently show a registry read
  - the remaining unresolved question is whether any `Control\Power` registry seeding path still feeds this callback/init family elsewhere on modern builds
