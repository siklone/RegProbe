# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `powerrequest-executionrequired-ghidra-20260408a`
- Timestamp: `2026-04-08T12:27:51.746378600Z`
- Patterns: `sym:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`, `sym:PopPowerRequestExecutionRequiredSettingCallback`, `sym:PopExecutionRequiredTimeout`

## Pattern Summary

### Pattern: `sym:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`

#### Symbol @ `14001e558`

- Symbol: `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
- Type: `Label`

- Reference count: `1`
- References:
  - `140aacfae` in `PopPowerRequestExecutionRequiredSettingCallback` via `symbol:GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`

### Pattern: `sym:PopPowerRequestExecutionRequiredSettingCallback`

#### Symbol @ `140aacf80`

- Symbol: `PopPowerRequestExecutionRequiredSettingCallback`
- Type: `Function`

- Reference count: `0`
- No direct or bounded indirect references resolved by Ghidra

### Pattern: `sym:PopExecutionRequiredTimeout`

#### Symbol @ `140fd70b0`

- Symbol: `PopExecutionRequiredTimeout`
- Type: `Label`

- Reference count: `5`
- References:
  - `140a3bd2f` in `PopPowerRequestEvaluateExecutionRequiredStatus` via `symbol:PopExecutionRequiredTimeout`
  - `140a3bd71` in `PopPowerRequestEvaluateExecutionRequiredStatus` via `symbol:PopExecutionRequiredTimeout`
  - `140749e1c` in `PopPowerRequestSetExecutionRequiredTimeoutTimer` via `symbol:PopExecutionRequiredTimeout`
  - `140749e32` in `PopPowerRequestSetExecutionRequiredTimeoutTimer` via `symbol:PopExecutionRequiredTimeout`
  - `140aacfe4` in `PopPowerRequestExecutionRequiredSettingCallback` via `symbol:PopExecutionRequiredTimeout`

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

## Match @ `140a3bd2f`

- Function: `PopPowerRequestEvaluateExecutionRequiredStatus`
- Via: `symbol:PopExecutionRequiredTimeout`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `21`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

bool PopPowerRequestEvaluateExecutionRequiredStatus(void)

{
  bool bVar1;
  
  bVar1 = false;
  if (PopExecutionRequiredTimeout != 0) {
    if ((DAT_140f0e083 == '\0') ||
       (((PopPowerRequestActiveAudioEnablesExecutionRequired != 0 && (DAT_140f0e082 != '\0')) ||
        (DAT_140f0e081 != '\0')))) {
      bVar1 = true;
    }
    else {
      bVar1 = (ulonglong)(_DAT_fffff78000000008 - _DAT_140f0e088) <
              (ulonglong)PopExecutionRequiredTimeout * 10000000;
    }
  }
  return bVar1;
}
```

## Match @ `140a3bd71`

- Function: `PopPowerRequestEvaluateExecutionRequiredStatus`
- Via: `symbol:PopExecutionRequiredTimeout`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `21`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

bool PopPowerRequestEvaluateExecutionRequiredStatus(void)

{
  bool bVar1;
  
  bVar1 = false;
  if (PopExecutionRequiredTimeout != 0) {
    if ((DAT_140f0e083 == '\0') ||
       (((PopPowerRequestActiveAudioEnablesExecutionRequired != 0 && (DAT_140f0e082 != '\0')) ||
        (DAT_140f0e081 != '\0')))) {
      bVar1 = true;
    }
    else {
      bVar1 = (ulonglong)(_DAT_fffff78000000008 - _DAT_140f0e088) <
              (ulonglong)PopExecutionRequiredTimeout * 10000000;
    }
  }
  return bVar1;
}
```

## Match @ `140749e1c`

- Function: `PopPowerRequestSetExecutionRequiredTimeoutTimer`
- Via: `symbol:PopExecutionRequiredTimeout`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `24`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void PopPowerRequestSetExecutionRequiredTimeoutTimer(void)

{
  longlong lVar1;
  undefined8 local_18;
  undefined8 local_10;
  
  if ((DAT_140f0e083 != '\0') && (PopExecutionRequiredTimeout != 0)) {
    if ((ulonglong)(_DAT_fffff78000000008 - _DAT_140f0e088) <
        (ulonglong)PopExecutionRequiredTimeout * 10000000) {
      lVar1 = (ulonglong)PopExecutionRequiredTimeout * 10000000 -
              (_DAT_fffff78000000008 - _DAT_140f0e088);
    }
    else {
      lVar1 = 10000000;
    }
    local_10 = 0xffffffffffffffff;
    local_18 = 0;
    KeSetTimer2(&PopPowerRequestExecutionRequiredTimeoutTimer,-lVar1,0,&local_18);
  }
  return;
}
```

## Match @ `140749e32`

- Function: `PopPowerRequestSetExecutionRequiredTimeoutTimer`
- Via: `symbol:PopExecutionRequiredTimeout`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `24`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void PopPowerRequestSetExecutionRequiredTimeoutTimer(void)

{
  longlong lVar1;
  undefined8 local_18;
  undefined8 local_10;
  
  if ((DAT_140f0e083 != '\0') && (PopExecutionRequiredTimeout != 0)) {
    if ((ulonglong)(_DAT_fffff78000000008 - _DAT_140f0e088) <
        (ulonglong)PopExecutionRequiredTimeout * 10000000) {
      lVar1 = (ulonglong)PopExecutionRequiredTimeout * 10000000 -
              (_DAT_fffff78000000008 - _DAT_140f0e088);
    }
    else {
      lVar1 = 10000000;
    }
    local_10 = 0xffffffffffffffff;
    local_18 = 0;
    KeSetTimer2(&PopPowerRequestExecutionRequiredTimeoutTimer,-lVar1,0,&local_18);
  }
  return;
}
```

## Match @ `140aacfe4`

- Function: `PopPowerRequestExecutionRequiredSettingCallback`
- Via: `symbol:PopExecutionRequiredTimeout`
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

