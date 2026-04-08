# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `powerrequest-executionrequired-binding-ghidra-20260408a`
- Timestamp: `2026-04-08T14:01:06.835108300Z`
- Patterns: `sym:PopPowerRequestExecutionRequiredSettingCallback`, `sym:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`

## Pattern Summary

### Pattern: `sym:PopPowerRequestExecutionRequiredSettingCallback`

#### Symbol @ `140aacf80`

- Symbol: `PopPowerRequestExecutionRequiredSettingCallback`
- Type: `Function`

- Reference count: `0`
- No direct or bounded indirect references resolved by Ghidra

### Pattern: `sym:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`

#### Symbol @ `14001e558`

- Symbol: `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
- Type: `Label`

- Reference count: `1`
- References:
  - `140aacfae` in `PopPowerRequestExecutionRequiredSettingCallback` via `symbol:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`

## Match Analysis

## Match @ `140aacfae`

- Function: `PopPowerRequestExecutionRequiredSettingCallback`
- Via: `symbol:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `23`

```c
undefined8
PopPowerRequestExecutionRequiredSettingCallback(longlong *param_1,undefined4 *param_2,int param_3)

{
  undefined8 uVar1;
  longlong lVar2;
  
  uVar1 = 0xc000000d;
  PopAcquireRwLockExclusive(&PopPowerRequestLock);
  lVar2 = 0x4e037e983166bc41 - *param_1;
  if (lVar2 == 0) {
    lVar2 = -0x71ded4a0f013b14d - param_1[1];
  }
  if (((lVar2 == 0) && (param_3 == 4)) && (param_2 != (undefined4 *)0x0)) {
    KeCancelTimer2(&PopPowerRequestExecutionRequiredTimeoutTimer,0);
    PopExecutionRequiredTimeout = *param_2;
    PopPowerRequestSetExecutionRequiredTimeoutTimer();
    PopPowerRequestHandleExecutionEnablementUpdate();
    uVar1 = 0;
  }
  PopReleaseRwLock(&PopPowerRequestLock);
  return uVar1;
}
```

