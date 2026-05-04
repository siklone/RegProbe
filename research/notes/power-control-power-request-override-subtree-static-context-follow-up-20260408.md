# Power Request Override Subtree Static Context Follow-Up - 2026-04-08

This follow-up does not claim an exact leaf binding for `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`.

It narrows the current-build static story around the subtree using already-retained current-build analysis:

- symbol-seeded Ghidra decompilation:
  - `powerrequest-executionrequired-binding-ghidra-20260408a`
- local-KD power-setting query/store disassembly:
  - `local-kd-powersetting-query-20260408a`
- retained override-lineage KD pass:
  - `local-kd-powerrequest-reglineage-20260408a`

## Observed static context

- The only naturally resolved power-request setting callback in retained Ghidra is still timeout-specific:
  - `PopPowerRequestExecutionRequiredSettingCallback`
  - keyed to `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
  - writes `PopExecutionRequiredTimeout`
  - rearms the timeout timer
  - calls `PopPowerRequestHandleExecutionEnablementUpdate`
- The retained wildcard KD pass still shows no visible `nt!*PowerRequest*Reg*` helper on the checked-in build.
- The retained local-KD query/store disassembly shows the generic power-setting layer operating through:
  - `PopFindPowerSettingConfiguration`
  - `PopSettingLock`
  - `PopPowerSettingChangeStamp`
  - `PopRegisteredPowerSettingCallbacks`
  - `PoRegisterPowerSettingCallback`
- In that retained path, the visible generic power-setting query/store layer is in-memory and callback-table driven rather than exposing a direct registry API.

## What this resolves

- The subtree no longer lacks all static context.
- There is now bounded retained current-build static context around the same power-request family:
  - timeout-specific callback path from Ghidra
  - generic query/store and callback-table path from local-KD
  - visible override-family symbols from wildcard KD

## What remains unresolved

- No retained static artifact yet binds `PowerRequestOverride` leaf names directly to a naturally resolved registry-reading function.
- No retained static artifact yet proves stable semantics for the `Driver`, `Process`, or `Service` leaves.
- The subtree therefore remains a draft research lane with unresolved leaf semantics, even though the "missing static context entirely" gap is now closed.
