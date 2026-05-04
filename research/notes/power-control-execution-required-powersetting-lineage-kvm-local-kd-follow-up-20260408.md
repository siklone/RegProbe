## Summary
- A retained KVM local-KD wildcard pass exposed the generic current-build power-setting callback infrastructure behind the execution-required power-request family.
- `x nt!*PowerSetting*` returned `PopInitializePowerSettingCallbacks`, `PopInitializePowerSettings`, `PoRegisterPowerSettingCallback`, `PoUnregisterPowerSettingCallback`, `PopDispatchPowerSettingCallbacks`, `PopCallPowerSettingCallback`, `PopQueryPowerSettingUlong`, `PopSetPowerSettingValue`, and `PopRegisteredPowerSettingCallbacks`.
- `x nt!*ExecutionRequired*Setting*` still returned only `nt!PopPowerRequestExecutionRequiredSettingCallback`.
- `uf nt!PoRegisterPowerSettingCallback` showed the generic registration path allocating a callback object, copying the target GUID and callback pointer, and resolving the backing setting entry through `PopFindPowerSettingConfiguration`.

## Evidence
- `evidence/files/vm-tooling-staging/local-kd-powersetting-lineage-20260408a/local-kd-powersetting-lineage-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powersetting-lineage-20260408a/local-kd-powersetting-lineage-20260408a.log`

## Interpretation
- The visible current-build execution-required family now sits on top of a broader generic power-setting callback subsystem rather than a bespoke `PowerRequest*Register*` helper.
- This is stronger than the earlier callback-only picture because the running kernel now exposes the generic registration, dispatch, and callback tables that such settings use.
- It is still not exact proof that `PopPowerRequestExecutionRequiredSettingCallback` is bound in a visible checked-in-build init site, and it is still not a `Control\\Power` registry seeding path.

## Next Questions
- Where is `PopPowerRequestExecutionRequiredSettingCallback` actually registered into the generic power-setting subsystem on checked-in builds?
- Does that registration ultimately come from a policy-init path, or is it entirely decoupled from `Control\\Power` registry reads for the `Allow*ExecutionRequired*` pair?
