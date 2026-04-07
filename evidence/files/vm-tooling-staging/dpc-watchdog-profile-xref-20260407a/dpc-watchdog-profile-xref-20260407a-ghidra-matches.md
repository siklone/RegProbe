# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `dpc-watchdog-profile-xref-20260407a`
- Timestamp: `2026-04-07T19:30:10.171013100Z`
- Patterns: `sym:KeDpcWatchdogProfileOffsetMs`, `sym:KeDpcWatchdogProfileSingleDpcThresholdMs`, `sym:KeDpcWatchdogProfileCumulativeDpcThresholdMs`, `sym:KeDpcWatchdogProfileBufferSizeBytes`, `sym:KeDpcWatchdogPeriodMs`

## Pattern Summary

### Pattern: `sym:KeDpcWatchdogProfileOffsetMs`

#### Symbol @ `140fc5fd4`

- Symbol: `KeDpcWatchdogProfileOffsetMs`
- Type: `Label`

- Reference count: `1`
- References:
  - `140c2855c` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileOffsetMs`

### Pattern: `sym:KeDpcWatchdogProfileSingleDpcThresholdMs`

#### Symbol @ `140fc4028`

- Symbol: `KeDpcWatchdogProfileSingleDpcThresholdMs`
- Type: `Label`

- Reference count: `16`
- References:
  - `140c60e2a` in `KeInitSystem` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `1405b5feb` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `1405b5ff4` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `1405b4c3c` in `KiCreateDpcLimitsProcessorConfiguration` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c28188` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c281b1` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c281e2` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c281f3` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c28599` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c28607` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c28612` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `140c28629` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `... 4 more references omitted ...`

### Pattern: `sym:KeDpcWatchdogProfileCumulativeDpcThresholdMs`

#### Symbol @ `140fc4024`

- Symbol: `KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Type: `Label`

- Reference count: `16`
- References:
  - `140c60e23` in `KeInitSystem` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `1405b5ffa` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `1405b6003` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `1405b4c18` in `KiCreateDpcLimitsProcessorConfiguration` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c281b7` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c281d7` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c281f9` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c28206` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c285a6` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c285d7` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c285e9` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `140c2863e` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `... 4 more references omitted ...`

### Pattern: `sym:KeDpcWatchdogProfileBufferSizeBytes`

#### Symbol @ `140fc4048`

- Symbol: `KeDpcWatchdogProfileBufferSizeBytes`
- Type: `Label`

- Reference count: `10`
- References:
  - `140b565eb` in `KiInitializeProcessor` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `140c2820c` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `140c28219` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `140c2823d` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `140c28644` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `140c2865e` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `1405b4863` in `KeQueryDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `1405b4ac5` in `KeUpdateDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `1405b4af3` in `KeUpdateDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`
  - `1405b4e8d` in `KiValidateDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogProfileBufferSizeBytes`

### Pattern: `sym:KeDpcWatchdogPeriodMs`

#### Symbol @ `140fc4030`

- Symbol: `KeDpcWatchdogPeriodMs`
- Type: `Label`

- Reference count: `10`
- References:
  - `140c60e07` in `KeInitSystem` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b5faf` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b5fb8` in `KiApplyDpcVerificationScaleSettings` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b4bd0` in `KiCreateDpcLimitsProcessorConfiguration` via `symbol:KeDpcWatchdogPeriodMs`
  - `140c280f4` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogPeriodMs`
  - `140c2810f` in `KiInitDpcThresholds` via `symbol:KeDpcWatchdogPeriodMs`
  - `140c2856c` in `KiInitializeLegacyWatchdogProfileThresholds` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b488b` in `KeQueryDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b4a83` in `KeUpdateDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogPeriodMs`
  - `1405b4e42` in `KiValidateDpcWatchdogConfiguration` via `symbol:KeDpcWatchdogPeriodMs`

## Match Analysis

## Match @ `140c2855c`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileOffsetMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c60e2a`

- Function: `KeInitSystem`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined8 KeInitSystem(undefined8 param_1,ulonglong param_2)

{
  undefined8 uVar1;
  longlong lVar2;
  void *pvVar3;
  char cVar4;
  int iVar5;
  uint uVar6;
  uint uVar7;
  ulonglong uVar8;
  ulonglong uVar9;
  undefined8 *puVar10;
  longlong lVar11;
  undefined1 auStack_c8 [32];
  undefined8 local_a8;
  undefined1 *local_a0;
  longlong local_98;
  longlong local_90;
  longlong local_88 [2];
  undefined1 local_78 [32];
  longlong *local_58;
  undefined8 local_50;
  longlong *local_48;
  undefined8 local_40;
  longlong *local_38;
  undefined8 local_30;
  ulonglong local_28;
  
  pvVar3 = FiberData;
  local_28 = __security_cookie ^ (ulonglong)auStack_c8;
  uVar9 = 0;
  iVar5 = (int)param_1;
  if (iVar5 == 0) {
    if ((((KeFeatureBits2 & 0x8000) != 0) && (KiDisableTsx != 0)) &&
       (uVar9 = *(ulonglong *)((longlong)FiberData + 0x2d00), param_2 = uVar9,
       ((byte)uVar9 & 3) != 3)) {
      param_1 = 0x122;
      *(ulonglong *)((longlong)FiberData + 0x2d00) = uVar9 | 3;
      param_2 = uVar9 >> 0x20;
      wrmsr(0x122,param_2 << 0x20 | uVar9 & 0xffffffff | 3);
    }
    KiTsxSupported = KiDetectTsx(param_1,param_2);
    KiRcuSystemInitialize(pvVar3);
    KeInitializeSchedulerAssist(pvVar3);
    KeInitializeCatRegisters();
    iVar5 = KeInitializeTimerTable(pvVar3);
    if (iVar5 < 0) {
      local_a8 = 0;
                    /* WARNING: Subroutine does not return */
      KeBugCheckEx(0x31,(longlong)iVar5,1);
    }
    KiInitializeVelocity();
  }
  else {
    if (iVar5 == 1) {
// ... trimmed ...
```

## Match @ `1405b5feb`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b5ff4`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b4c3c`

- Function: `KiCreateDpcLimitsProcessorConfiguration`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiCreateDpcLimitsProcessorConfiguration
               (undefined8 *param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  ulonglong uVar2;
  
  *param_1 = 0;
  param_1[1] = 0;
  param_1[2] = 0;
  *(undefined4 *)((longlong)param_1 + 0x1c) = param_2;
  *(undefined4 *)(param_1 + 3) = param_3;
  uVar2 = (ulonglong)KeMaximumIncrement;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogPeriodMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 4) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)param_1 = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileCumulativeDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0x14) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileSingleDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 2) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 1) = (int)uVar1;
  uVar2 = ((uVar2 - 1) + (ulonglong)KeDpcCumulativeSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar2) {
    uVar2 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0xc) = (int)uVar2;
  return;
}
```

## Match @ `140c28188`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c281b1`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c281e2`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c281f3`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c28599`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c28607`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c28612`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c28629`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileSingleDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c60e23`

- Function: `KeInitSystem`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined8 KeInitSystem(undefined8 param_1,ulonglong param_2)

{
  undefined8 uVar1;
  longlong lVar2;
  void *pvVar3;
  char cVar4;
  int iVar5;
  uint uVar6;
  uint uVar7;
  ulonglong uVar8;
  ulonglong uVar9;
  undefined8 *puVar10;
  longlong lVar11;
  undefined1 auStack_c8 [32];
  undefined8 local_a8;
  undefined1 *local_a0;
  longlong local_98;
  longlong local_90;
  longlong local_88 [2];
  undefined1 local_78 [32];
  longlong *local_58;
  undefined8 local_50;
  longlong *local_48;
  undefined8 local_40;
  longlong *local_38;
  undefined8 local_30;
  ulonglong local_28;
  
  pvVar3 = FiberData;
  local_28 = __security_cookie ^ (ulonglong)auStack_c8;
  uVar9 = 0;
  iVar5 = (int)param_1;
  if (iVar5 == 0) {
    if ((((KeFeatureBits2 & 0x8000) != 0) && (KiDisableTsx != 0)) &&
       (uVar9 = *(ulonglong *)((longlong)FiberData + 0x2d00), param_2 = uVar9,
       ((byte)uVar9 & 3) != 3)) {
      param_1 = 0x122;
      *(ulonglong *)((longlong)FiberData + 0x2d00) = uVar9 | 3;
      param_2 = uVar9 >> 0x20;
      wrmsr(0x122,param_2 << 0x20 | uVar9 & 0xffffffff | 3);
    }
    KiTsxSupported = KiDetectTsx(param_1,param_2);
    KiRcuSystemInitialize(pvVar3);
    KeInitializeSchedulerAssist(pvVar3);
    KeInitializeCatRegisters();
    iVar5 = KeInitializeTimerTable(pvVar3);
    if (iVar5 < 0) {
      local_a8 = 0;
                    /* WARNING: Subroutine does not return */
      KeBugCheckEx(0x31,(longlong)iVar5,1);
    }
    KiInitializeVelocity();
  }
  else {
    if (iVar5 == 1) {
// ... trimmed ...
```

## Match @ `1405b5ffa`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b6003`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b4c18`

- Function: `KiCreateDpcLimitsProcessorConfiguration`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiCreateDpcLimitsProcessorConfiguration
               (undefined8 *param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  ulonglong uVar2;
  
  *param_1 = 0;
  param_1[1] = 0;
  param_1[2] = 0;
  *(undefined4 *)((longlong)param_1 + 0x1c) = param_2;
  *(undefined4 *)(param_1 + 3) = param_3;
  uVar2 = (ulonglong)KeMaximumIncrement;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogPeriodMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 4) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)param_1 = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileCumulativeDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0x14) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileSingleDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 2) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 1) = (int)uVar1;
  uVar2 = ((uVar2 - 1) + (ulonglong)KeDpcCumulativeSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar2) {
    uVar2 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0xc) = (int)uVar2;
  return;
}
```

## Match @ `140c281b7`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c281d7`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c281f9`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c28206`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c285a6`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c285d7`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c285e9`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c2863e`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileCumulativeDpcThresholdMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140b565eb`

- Function: `KiInitializeProcessor`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `34`

```c
void KiInitializeProcessor(longlong param_1)

{
  undefined4 uVar1;
  undefined8 local_28;
  undefined8 uStack_20;
  undefined8 local_18;
  undefined8 uStack_10;
  
  uVar1 = KeDpcWatchdogProfileBufferSizeBytes;
  local_28 = 0;
  uStack_20 = 0;
  local_18 = 0;
  uStack_10 = 0;
  if (KeThreadDpcEnable != 0) {
    KeInitializeGate(param_1 + 0x8340,0);
    KiInitializeDpcList(param_1 + 0x3870);
    *(undefined8 *)(param_1 + 0x3880) = 0;
    *(undefined4 *)(param_1 + 0x3888) = 0;
  }
  KeInitializeThreadedDpc(param_1 + 0x8ad8,KiDpcWatchdog,*(undefined4 *)(param_1 + 0x24));
  *(undefined1 *)(param_1 + 0x8ad9) = 2;
  if (*(longlong *)(param_1 + 0x8b10) == 0) {
    *(short *)(param_1 + 0x8ada) = (short)*(undefined4 *)(param_1 + 0x24) + 0x800;
  }
  KeInitializeDpc(param_1 + 0xa150,KiFreeTemporaryStacks,0);
  *(undefined1 *)(param_1 + 0xa151) = 2;
  if (*(longlong *)(param_1 + 0xa188) == 0) {
    *(short *)(param_1 + 0xa152) = (short)*(undefined4 *)(param_1 + 0x24) + 0x800;
  }
  KiCreateDpcLimitsProcessorConfiguration(&local_28,0,uVar1);
  KiApplyProcessorDpcLimits(param_1,&local_28);
  return;
}
```

## Match @ `140c2820c`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c28219`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c2823d`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c28644`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `140c2865e`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `1405b4863`

- Function: `KeQueryDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined8 KeQueryDpcWatchdogConfiguration(void *param_1,uint param_2,int param_3)

{
  longlong lVar1;
  bool bVar2;
  uint local_38;
  int iStack_34;
  undefined8 uStack_30;
  undefined8 local_28;
  undefined8 uStack_20;
  
  uStack_30 = 0;
  local_28 = 0;
  uStack_20 = 0;
  iStack_34 = 0;
  if (param_3 == 0xe4) {
    if (param_2 != 0x14) {
      return 0xc0000004;
    }
    local_38 = 1;
  }
  else {
    if (param_3 != 0xe5) {
      return 0xc000000d;
    }
    if (param_2 != 0x20) {
      return 0xc0000004;
    }
    local_38 = 2;
  }
  *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) =
       *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) + -1;
  lVar1 = KeAbPreAcquire(&KiDpcWatchdogConfigurationLock,0,0);
  LOCK();
  bVar2 = KiDpcWatchdogConfigurationLock == 0;
  if (bVar2) {
    KiDpcWatchdogConfigurationLock = 0x11;
  }
  UNLOCK();
  if (!bVar2) {
    ExfAcquirePushLockSharedEx
              (&KiDpcWatchdogConfigurationLock,0,lVar1,&KiDpcWatchdogConfigurationLock);
  }
  if (lVar1 != 0) {
    *(undefined1 *)(lVar1 + 10) = 1;
  }
  if (param_3 != 0xe4) {
    if (param_3 != 0xe5) goto LAB_1405b48c7;
    if (KeDpcWatchdogProfileSingleDpcThresholdMs != 0) {
      local_38 = local_38 | 0x1000;
      local_28 = CONCAT44(KeDpcWatchdogProfileSingleDpcThresholdMs,(undefined4)local_28);
    }
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0) {
      local_38 = local_38 | 0x2000;
      uStack_20 = CONCAT44(uStack_20._4_4_,KeDpcWatchdogProfileCumulativeDpcThresholdMs);
    }
    if (KeDpcWatchdogProfileBufferSizeBytes != 0) {
      local_38 = local_38 | 0x4000;
      uStack_20 = CONCAT44(KeDpcWatchdogProfileBufferSizeBytes,(undefined4)uStack_20);
// ... trimmed ...
```

## Match @ `1405b4ac5`

- Function: `KeUpdateDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

int KeUpdateDpcWatchdogConfiguration(undefined8 param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  uint uVar2;
  int iVar3;
  longlong lVar4;
  byte bVar5;
  uint uVar6;
  undefined1 in_CR8;
  undefined8 uVar7;
  undefined1 auStack_98 [32];
  undefined8 local_78;
  undefined8 uStack_70;
  undefined8 local_68;
  undefined8 uStack_60;
  undefined8 local_58;
  undefined8 uStack_50;
  undefined8 local_48;
  undefined8 uStack_40;
  ulonglong local_38;
  
  local_38 = __security_cookie ^ (ulonglong)auStack_98;
  local_58 = 0;
  uStack_50 = 0;
  local_48 = 0;
  uStack_40 = 0;
  *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) =
       *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) + -1;
  local_78 = 0;
  uStack_70 = 0;
  local_68 = 0;
  uStack_60 = 0;
  lVar4 = KeAbPreAcquire(&KiDpcWatchdogConfigurationLock,0,0);
  LOCK();
  uVar1 = KiDpcWatchdogConfigurationLock & 1;
  KiDpcWatchdogConfigurationLock = KiDpcWatchdogConfigurationLock | 1;
  UNLOCK();
  if (uVar1 != 0) {
    ExfAcquirePushLockExclusiveEx
              (&KiDpcWatchdogConfigurationLock,lVar4,&KiDpcWatchdogConfigurationLock);
  }
  if (lVar4 != 0) {
    *(undefined1 *)(lVar4 + 10) = 1;
  }
  iVar3 = KiValidateDpcWatchdogConfiguration(param_1,param_2,param_3,&local_78);
  if (-1 < iVar3) {
    uVar7 = 2;
    if (KiIrqlFlags != 0) {
      KiRaiseIrqlProcessIrqlFlags(in_CR8,2);
    }
    uVar2 = KeDpcWatchdogProfileBufferSizeBytes;
    if (((uint)local_78 >> 8 & 1) != 0) {
      KeDpcTimeoutMs = local_78._4_4_;
    }
    if (((uint)local_78 >> 9 & 1) != 0) {
// ... trimmed ...
```

## Match @ `1405b4af3`

- Function: `KeUpdateDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

int KeUpdateDpcWatchdogConfiguration(undefined8 param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  uint uVar2;
  int iVar3;
  longlong lVar4;
  byte bVar5;
  uint uVar6;
  undefined1 in_CR8;
  undefined8 uVar7;
  undefined1 auStack_98 [32];
  undefined8 local_78;
  undefined8 uStack_70;
  undefined8 local_68;
  undefined8 uStack_60;
  undefined8 local_58;
  undefined8 uStack_50;
  undefined8 local_48;
  undefined8 uStack_40;
  ulonglong local_38;
  
  local_38 = __security_cookie ^ (ulonglong)auStack_98;
  local_58 = 0;
  uStack_50 = 0;
  local_48 = 0;
  uStack_40 = 0;
  *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) =
       *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) + -1;
  local_78 = 0;
  uStack_70 = 0;
  local_68 = 0;
  uStack_60 = 0;
  lVar4 = KeAbPreAcquire(&KiDpcWatchdogConfigurationLock,0,0);
  LOCK();
  uVar1 = KiDpcWatchdogConfigurationLock & 1;
  KiDpcWatchdogConfigurationLock = KiDpcWatchdogConfigurationLock | 1;
  UNLOCK();
  if (uVar1 != 0) {
    ExfAcquirePushLockExclusiveEx
              (&KiDpcWatchdogConfigurationLock,lVar4,&KiDpcWatchdogConfigurationLock);
  }
  if (lVar4 != 0) {
    *(undefined1 *)(lVar4 + 10) = 1;
  }
  iVar3 = KiValidateDpcWatchdogConfiguration(param_1,param_2,param_3,&local_78);
  if (-1 < iVar3) {
    uVar7 = 2;
    if (KiIrqlFlags != 0) {
      KiRaiseIrqlProcessIrqlFlags(in_CR8,2);
    }
    uVar2 = KeDpcWatchdogProfileBufferSizeBytes;
    if (((uint)local_78 >> 8 & 1) != 0) {
      KeDpcTimeoutMs = local_78._4_4_;
    }
    if (((uint)local_78 >> 9 & 1) != 0) {
// ... trimmed ...
```

## Match @ `1405b4e8d`

- Function: `KiValidateDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogProfileBufferSizeBytes`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined4 KiValidateDpcWatchdogConfiguration(void *param_1,uint param_2,int param_3,uint *param_4)

{
  uint uVar1;
  uint uVar2;
  uint uVar3;
  bool bVar4;
  
  if (param_3 == 0xe4) {
    bVar4 = param_2 == 0x14;
  }
  else {
    if (param_3 != 0xe5) goto LAB_1405b4dcb;
    bVar4 = param_2 == 0x20;
  }
  if (!bVar4) {
    return 0xc0000004;
  }
LAB_1405b4dcb:
  param_4[0] = 0;
  param_4[1] = 0;
  param_4[2] = 0;
  param_4[3] = 0;
  param_4[4] = 0;
  param_4[5] = 0;
  param_4[6] = 0;
  param_4[7] = 0;
  RtlCopyMemory(param_4,param_1,(ulonglong)param_2);
  uVar1 = *param_4;
  uVar3 = uVar1 & 0xff;
  if (uVar3 - 1 < 2) {
    if (param_3 == 0xe4) {
      if (uVar3 != 1) {
        return 0xc000000d;
      }
      if ((uVar1 & 0x7000) != 0) {
        return 0xc000000d;
      }
    }
    else if ((param_3 == 0xe5) && (uVar3 != 2)) {
      return 0xc000000d;
    }
    if ((uVar1 >> 8 != 0) && (uVar1 < 0x8000)) {
      if ((uVar1 >> 8 & 1) == 0) {
        param_4[1] = KeDpcTimeoutMs;
      }
      if ((uVar1 >> 9 & 1) == 0) {
        param_4[2] = KeDpcWatchdogPeriodMs;
      }
      if ((uVar1 >> 10 & 1) == 0) {
        param_4[3] = KeDpcSoftTimeoutMs;
      }
      if ((uVar1 >> 0xb & 1) == 0) {
        param_4[4] = KeDpcCumulativeSoftTimeoutMs;
      }
      if ((uVar1 >> 0xc & 1) == 0) {
        param_4[5] = KeDpcWatchdogProfileSingleDpcThresholdMs;
      }
      if ((uVar1 >> 0xd & 1) == 0) {
// ... trimmed ...
```

## Match @ `140c60e07`

- Function: `KeInitSystem`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined8 KeInitSystem(undefined8 param_1,ulonglong param_2)

{
  undefined8 uVar1;
  longlong lVar2;
  void *pvVar3;
  char cVar4;
  int iVar5;
  uint uVar6;
  uint uVar7;
  ulonglong uVar8;
  ulonglong uVar9;
  undefined8 *puVar10;
  longlong lVar11;
  undefined1 auStack_c8 [32];
  undefined8 local_a8;
  undefined1 *local_a0;
  longlong local_98;
  longlong local_90;
  longlong local_88 [2];
  undefined1 local_78 [32];
  longlong *local_58;
  undefined8 local_50;
  longlong *local_48;
  undefined8 local_40;
  longlong *local_38;
  undefined8 local_30;
  ulonglong local_28;
  
  pvVar3 = FiberData;
  local_28 = __security_cookie ^ (ulonglong)auStack_c8;
  uVar9 = 0;
  iVar5 = (int)param_1;
  if (iVar5 == 0) {
    if ((((KeFeatureBits2 & 0x8000) != 0) && (KiDisableTsx != 0)) &&
       (uVar9 = *(ulonglong *)((longlong)FiberData + 0x2d00), param_2 = uVar9,
       ((byte)uVar9 & 3) != 3)) {
      param_1 = 0x122;
      *(ulonglong *)((longlong)FiberData + 0x2d00) = uVar9 | 3;
      param_2 = uVar9 >> 0x20;
      wrmsr(0x122,param_2 << 0x20 | uVar9 & 0xffffffff | 3);
    }
    KiTsxSupported = KiDetectTsx(param_1,param_2);
    KiRcuSystemInitialize(pvVar3);
    KeInitializeSchedulerAssist(pvVar3);
    KeInitializeCatRegisters();
    iVar5 = KeInitializeTimerTable(pvVar3);
    if (iVar5 < 0) {
      local_a8 = 0;
                    /* WARNING: Subroutine does not return */
      KeBugCheckEx(0x31,(longlong)iVar5,1);
    }
    KiInitializeVelocity();
  }
  else {
    if (iVar5 == 1) {
// ... trimmed ...
```

## Match @ `1405b5faf`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b5fb8`

- Function: `KiApplyDpcVerificationScaleSettings`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
void KiApplyDpcVerificationScaleSettings(void)

{
  if (ViVerifierEnabled != 0) {
    KeDpcWatchdogPeriodMs = KeDpcWatchdogPeriodMs * KeVerifierDpcScalingFactor;
    KeDpcTimeoutMs = KeDpcTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcSoftTimeoutMs = KeDpcSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcCumulativeSoftTimeoutMs = KeDpcCumulativeSoftTimeoutMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileSingleDpcThresholdMs =
         KeDpcWatchdogProfileSingleDpcThresholdMs * KeVerifierDpcScalingFactor;
    KeDpcWatchdogProfileCumulativeDpcThresholdMs =
         KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeVerifierDpcScalingFactor;
  }
  return;
}
```

## Match @ `1405b4bd0`

- Function: `KiCreateDpcLimitsProcessorConfiguration`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiCreateDpcLimitsProcessorConfiguration
               (undefined8 *param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  ulonglong uVar2;
  
  *param_1 = 0;
  param_1[1] = 0;
  param_1[2] = 0;
  *(undefined4 *)((longlong)param_1 + 0x1c) = param_2;
  *(undefined4 *)(param_1 + 3) = param_3;
  uVar2 = (ulonglong)KeMaximumIncrement;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogPeriodMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 4) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)param_1 = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileCumulativeDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0x14) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcWatchdogProfileSingleDpcThresholdMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 2) = (int)uVar1;
  uVar1 = ((uVar2 - 1) + (ulonglong)KeDpcSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar1) {
    uVar1 = 0xffffffff;
  }
  *(int *)(param_1 + 1) = (int)uVar1;
  uVar2 = ((uVar2 - 1) + (ulonglong)KeDpcCumulativeSoftTimeoutMs * 10000) / uVar2;
  if (0xffffffff < uVar2) {
    uVar2 = 0xffffffff;
  }
  *(int *)((longlong)param_1 + 0xc) = (int)uVar2;
  return;
}
```

## Match @ `140c280f4`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c2810f`

- Function: `KiInitDpcThresholds`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void KiInitDpcThresholds(void)

{
  uint uVar1;
  
  if (KeDpcWatchdogPeriodMs - 1 < 1999) {
    KeDpcWatchdogPeriodMs = 2000;
  }
  if (KeDpcTimeoutMs - 1 < 0x13) {
    KeDpcTimeoutMs = 0x14;
  }
  if (KeDpcSoftTimeoutMs != 0) {
    if (KeDpcSoftTimeoutMs < 0x14) {
      KeDpcSoftTimeoutMs = 0x14;
    }
    if ((KeDpcTimeoutMs != 0) && (KeDpcTimeoutMs < KeDpcSoftTimeoutMs)) {
      KeDpcSoftTimeoutMs = KeDpcTimeoutMs;
    }
  }
  if (KeDpcCumulativeSoftTimeoutMs != 0) {
    if (KeDpcCumulativeSoftTimeoutMs < 2000) {
      KeDpcCumulativeSoftTimeoutMs = 2000;
    }
    if ((KeDpcWatchdogPeriodMs != 0) && (KeDpcWatchdogPeriodMs < KeDpcCumulativeSoftTimeoutMs)) {
      KeDpcCumulativeSoftTimeoutMs = KeDpcWatchdogPeriodMs;
    }
  }
  if ((KeDpcWatchdogProfileSingleDpcThresholdMs - 1 < 0xfffffffe) &&
     (((uVar1 = KeDpcSoftTimeoutMs, KeDpcSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcTimeoutMs, KeDpcTimeoutMs != 0)) &&
      (uVar1 < KeDpcWatchdogProfileSingleDpcThresholdMs)))) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
  }
  if (((KeDpcWatchdogProfileCumulativeDpcThresholdMs - 1 < 0xfffffffe) &&
      ((uVar1 = KeDpcCumulativeSoftTimeoutMs, KeDpcCumulativeSoftTimeoutMs != 0 ||
       (uVar1 = KeDpcWatchdogPeriodMs, KeDpcWatchdogPeriodMs != 0)))) &&
     (uVar1 < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
  }
  KiInitializeLegacyWatchdogProfileThresholds();
  if (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff) {
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0;
  }
  if (KeDpcWatchdogProfileBufferSizeBytes == 0xffffffff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0;
    if ((KeDpcWatchdogProfileSingleDpcThresholdMs != 0) ||
       (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0)) {
      KeDpcWatchdogProfileBufferSizeBytes = 0x41000;
    }
  }
  else if (KeDpcWatchdogProfileBufferSizeBytes - 1 < 0x1fff) {
    KeDpcWatchdogProfileBufferSizeBytes = 0x2000;
  }
  _DAT_140fc43c4 = KeDpcWatchdogProfileBufferSizeBytes >> 3;
// ... trimmed ...
```

## Match @ `140c2856c`

- Function: `KiInitializeLegacyWatchdogProfileThresholds`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `45`

```c
void KiInitializeLegacyWatchdogProfileThresholds(void)

{
  ulonglong uVar1;
  uint uVar2;
  
  if (((((KeDpcWatchdogProfileOffsetMs != 0) && (KeDpcWatchdogPeriodMs != 0)) &&
       (KeDpcTimeoutMs != 0)) &&
      ((KeDpcTimeoutMs < KeDpcWatchdogPeriodMs &&
       (KeDpcWatchdogProfileSingleDpcThresholdMs == 0xffffffff)))) &&
     (KeDpcWatchdogProfileCumulativeDpcThresholdMs == 0xffffffff)) {
    uVar2 = KeDpcWatchdogProfileOffsetMs;
    if (KeDpcWatchdogProfileOffsetMs < 0x3e9) {
      uVar2 = 1000;
    }
    if ((KeDpcWatchdogPeriodMs < uVar2) && (uVar2 = 10000, KeDpcWatchdogPeriodMs < 0x2711)) {
      uVar2 = 1000;
    }
    KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcWatchdogPeriodMs - uVar2;
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs < 1000) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = 1000;
    }
    uVar1 = (ulonglong)(KeDpcWatchdogProfileCumulativeDpcThresholdMs * KeDpcTimeoutMs) /
            (ulonglong)KeDpcWatchdogPeriodMs;
    KeDpcWatchdogProfileSingleDpcThresholdMs = (uint)uVar1;
    if (0xffffffff < uVar1) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0xffffffff;
    }
    if (uVar2 < KeDpcTimeoutMs - KeDpcWatchdogProfileSingleDpcThresholdMs) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = 0;
    }
    if ((KeDpcSoftTimeoutMs != 0) && (KeDpcSoftTimeoutMs < KeDpcWatchdogProfileSingleDpcThresholdMs)
       ) {
      KeDpcWatchdogProfileSingleDpcThresholdMs = KeDpcSoftTimeoutMs;
    }
    if ((KeDpcCumulativeSoftTimeoutMs != 0) &&
       (KeDpcCumulativeSoftTimeoutMs < KeDpcWatchdogProfileCumulativeDpcThresholdMs)) {
      KeDpcWatchdogProfileCumulativeDpcThresholdMs = KeDpcCumulativeSoftTimeoutMs;
    }
    if (KeDpcWatchdogProfileBufferSizeBytes == -1) {
      KeDpcWatchdogProfileBufferSizeBytes = (uVar2 / 1000) * 0x6800;
    }
  }
  return;
}
```

## Match @ `1405b488b`

- Function: `KeQueryDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined8 KeQueryDpcWatchdogConfiguration(void *param_1,uint param_2,int param_3)

{
  longlong lVar1;
  bool bVar2;
  uint local_38;
  int iStack_34;
  undefined8 uStack_30;
  undefined8 local_28;
  undefined8 uStack_20;
  
  uStack_30 = 0;
  local_28 = 0;
  uStack_20 = 0;
  iStack_34 = 0;
  if (param_3 == 0xe4) {
    if (param_2 != 0x14) {
      return 0xc0000004;
    }
    local_38 = 1;
  }
  else {
    if (param_3 != 0xe5) {
      return 0xc000000d;
    }
    if (param_2 != 0x20) {
      return 0xc0000004;
    }
    local_38 = 2;
  }
  *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) =
       *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) + -1;
  lVar1 = KeAbPreAcquire(&KiDpcWatchdogConfigurationLock,0,0);
  LOCK();
  bVar2 = KiDpcWatchdogConfigurationLock == 0;
  if (bVar2) {
    KiDpcWatchdogConfigurationLock = 0x11;
  }
  UNLOCK();
  if (!bVar2) {
    ExfAcquirePushLockSharedEx
              (&KiDpcWatchdogConfigurationLock,0,lVar1,&KiDpcWatchdogConfigurationLock);
  }
  if (lVar1 != 0) {
    *(undefined1 *)(lVar1 + 10) = 1;
  }
  if (param_3 != 0xe4) {
    if (param_3 != 0xe5) goto LAB_1405b48c7;
    if (KeDpcWatchdogProfileSingleDpcThresholdMs != 0) {
      local_38 = local_38 | 0x1000;
      local_28 = CONCAT44(KeDpcWatchdogProfileSingleDpcThresholdMs,(undefined4)local_28);
    }
    if (KeDpcWatchdogProfileCumulativeDpcThresholdMs != 0) {
      local_38 = local_38 | 0x2000;
      uStack_20 = CONCAT44(uStack_20._4_4_,KeDpcWatchdogProfileCumulativeDpcThresholdMs);
    }
    if (KeDpcWatchdogProfileBufferSizeBytes != 0) {
      local_38 = local_38 | 0x4000;
      uStack_20 = CONCAT44(KeDpcWatchdogProfileBufferSizeBytes,(undefined4)uStack_20);
// ... trimmed ...
```

## Match @ `1405b4a83`

- Function: `KeUpdateDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

int KeUpdateDpcWatchdogConfiguration(undefined8 param_1,undefined4 param_2,undefined4 param_3)

{
  ulonglong uVar1;
  uint uVar2;
  int iVar3;
  longlong lVar4;
  byte bVar5;
  uint uVar6;
  undefined1 in_CR8;
  undefined8 uVar7;
  undefined1 auStack_98 [32];
  undefined8 local_78;
  undefined8 uStack_70;
  undefined8 local_68;
  undefined8 uStack_60;
  undefined8 local_58;
  undefined8 uStack_50;
  undefined8 local_48;
  undefined8 uStack_40;
  ulonglong local_38;
  
  local_38 = __security_cookie ^ (ulonglong)auStack_98;
  local_58 = 0;
  uStack_50 = 0;
  local_48 = 0;
  uStack_40 = 0;
  *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) =
       *(short *)((longlong)SystemReserved1[0xf] + 0x1e4) + -1;
  local_78 = 0;
  uStack_70 = 0;
  local_68 = 0;
  uStack_60 = 0;
  lVar4 = KeAbPreAcquire(&KiDpcWatchdogConfigurationLock,0,0);
  LOCK();
  uVar1 = KiDpcWatchdogConfigurationLock & 1;
  KiDpcWatchdogConfigurationLock = KiDpcWatchdogConfigurationLock | 1;
  UNLOCK();
  if (uVar1 != 0) {
    ExfAcquirePushLockExclusiveEx
              (&KiDpcWatchdogConfigurationLock,lVar4,&KiDpcWatchdogConfigurationLock);
  }
  if (lVar4 != 0) {
    *(undefined1 *)(lVar4 + 10) = 1;
  }
  iVar3 = KiValidateDpcWatchdogConfiguration(param_1,param_2,param_3,&local_78);
  if (-1 < iVar3) {
    uVar7 = 2;
    if (KiIrqlFlags != 0) {
      KiRaiseIrqlProcessIrqlFlags(in_CR8,2);
    }
    uVar2 = KeDpcWatchdogProfileBufferSizeBytes;
    if (((uint)local_78 >> 8 & 1) != 0) {
      KeDpcTimeoutMs = local_78._4_4_;
    }
    if (((uint)local_78 >> 9 & 1) != 0) {
// ... trimmed ...
```

## Match @ `1405b4e42`

- Function: `KiValidateDpcWatchdogConfiguration`
- Via: `symbol:KeDpcWatchdogPeriodMs`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined4 KiValidateDpcWatchdogConfiguration(void *param_1,uint param_2,int param_3,uint *param_4)

{
  uint uVar1;
  uint uVar2;
  uint uVar3;
  bool bVar4;
  
  if (param_3 == 0xe4) {
    bVar4 = param_2 == 0x14;
  }
  else {
    if (param_3 != 0xe5) goto LAB_1405b4dcb;
    bVar4 = param_2 == 0x20;
  }
  if (!bVar4) {
    return 0xc0000004;
  }
LAB_1405b4dcb:
  param_4[0] = 0;
  param_4[1] = 0;
  param_4[2] = 0;
  param_4[3] = 0;
  param_4[4] = 0;
  param_4[5] = 0;
  param_4[6] = 0;
  param_4[7] = 0;
  RtlCopyMemory(param_4,param_1,(ulonglong)param_2);
  uVar1 = *param_4;
  uVar3 = uVar1 & 0xff;
  if (uVar3 - 1 < 2) {
    if (param_3 == 0xe4) {
      if (uVar3 != 1) {
        return 0xc000000d;
      }
      if ((uVar1 & 0x7000) != 0) {
        return 0xc000000d;
      }
    }
    else if ((param_3 == 0xe5) && (uVar3 != 2)) {
      return 0xc000000d;
    }
    if ((uVar1 >> 8 != 0) && (uVar1 < 0x8000)) {
      if ((uVar1 >> 8 & 1) == 0) {
        param_4[1] = KeDpcTimeoutMs;
      }
      if ((uVar1 >> 9 & 1) == 0) {
        param_4[2] = KeDpcWatchdogPeriodMs;
      }
      if ((uVar1 >> 10 & 1) == 0) {
        param_4[3] = KeDpcSoftTimeoutMs;
      }
      if ((uVar1 >> 0xb & 1) == 0) {
        param_4[4] = KeDpcCumulativeSoftTimeoutMs;
      }
      if ((uVar1 >> 0xc & 1) == 0) {
        param_4[5] = KeDpcWatchdogProfileSingleDpcThresholdMs;
      }
      if ((uVar1 >> 0xd & 1) == 0) {
// ... trimmed ...
```

