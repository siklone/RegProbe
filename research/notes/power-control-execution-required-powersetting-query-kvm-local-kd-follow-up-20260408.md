# Execution-Required Power-Setting Query/Store KVM Local-KD Follow-Up

Date: 2026-04-08
Target binary: `C:\Windows\System32\ntoskrnl.exe`
Probe: `local-kd-powersetting-query-20260408a`

## Outcome

- A retained local-KD disassembly pass completed for `PopQueryPowerSettingUlong`, `PopGetPowerSettingValue`, and `PopSetPowerSettingValue`.
- `PopQueryPowerSettingUlong` and `PopGetPowerSettingValue` both operate under `PopSettingLock` and resolve their backing data through `PopFindPowerSettingConfiguration`.
- The visible `PopSetPowerSettingValue` path updates in-memory setting data, bumps `PopPowerSettingChangeStamp`, walks `PopRegisteredPowerSettingCallbacks`, and schedules `PopSetNotificationWork`.
- The retained disassembly did not expose a visible registry API in this generic query/store layer.

## Artifacts

- `evidence/files/vm-tooling-staging/local-kd-powersetting-query-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powersetting-query-20260408a/host-review.json`
- `evidence/files/vm-tooling-staging/local-kd-powersetting-query-20260408a/local-kd.log`
- `evidence/files/vm-tooling-staging/local-kd-powersetting-query-20260408a/local-kd.txt`

## Interpretation

- The visible current-build generic power-setting query/store subsystem appears to be in-memory and callback-table driven rather than directly registry-driven at the point of query/set.
- This tightens the execution-required power-request pair in a useful way: the open question is no longer whether the visible generic `PopQueryPowerSettingUlong` / `PopSetPowerSettingValue` layer performs an obvious registry read or write.
- The remaining question is earlier seeding: were `Control\Power` values such as `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` loaded earlier into `PopFindPowerSettingConfiguration` objects, or is the modern current-build family entirely runtime-driven?
