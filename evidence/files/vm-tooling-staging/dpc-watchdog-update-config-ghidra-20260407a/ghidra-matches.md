# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `dpc-watchdog-update-config-ghidra-20260407a`
- Timestamp: `2026-04-07T19:56:35.950284900Z`
- Patterns: `sym:KeUpdateDpcWatchdogConfiguration`, `sym:KiCreateDpcLimitsProcessorConfiguration`

## Pattern Summary

### Pattern: `sym:KeUpdateDpcWatchdogConfiguration`

#### Symbol @ `1405b4998`

- Symbol: `KeUpdateDpcWatchdogConfiguration`
- Type: `Function`

- Reference count: `1`
- References:
  - `140ae2ec9` in `NtSetSystemInformation` via `symbol:KeUpdateDpcWatchdogConfiguration`

### Pattern: `sym:KiCreateDpcLimitsProcessorConfiguration`

#### Symbol @ `1405b4bac`

- Symbol: `KiCreateDpcLimitsProcessorConfiguration`
- Type: `Function`

- Reference count: `2`
- References:
  - `140b566a0` in `KiInitializeProcessor` via `symbol:KiCreateDpcLimitsProcessorConfiguration`
  - `1405b4b2d` in `KeUpdateDpcWatchdogConfiguration` via `symbol:KiCreateDpcLimitsProcessorConfiguration`

## Match Analysis

## Match @ `140ae2ec9`

- Function: `NtSetSystemInformation`
- Via: `symbol:KeUpdateDpcWatchdogConfiguration`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */
/* WARNING: Type propagation algorithm not settling */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void * NtSetSystemInformation(int param_1,ulonglong *param_2,uint param_3)

{
  short sVar1;
  undefined1 auVar2 [16];
  void *pvVar3;
  bool bVar4;
  char cVar5;
  char cVar6;
  int iVar7;
  uint uVar8;
  longlong lVar9;
  void *pvVar10;
  longlong lVar11;
  code *pcVar12;
  undefined8 uVar13;
  ulonglong *puVar14;
  longlong *plVar15;
  undefined1 *puVar16;
  byte bVar17;
  ulonglong uVar18;
  void *pvVar19;
  uint uVar20;
  undefined4 extraout_XMM0_Da;
  undefined1 auStack_4f8 [32];
  void **local_4d8;
  ulonglong *local_4d0;
  byte local_4c8;
  char local_4c7 [8];
  char local_4bf;
  undefined1 local_4be [30];
  ulonglong *local_4a0;
  byte local_498;
  byte local_497;
  int local_494;
  undefined8 local_490;
  wchar_t *pwStack_488;
  void *local_480 [2];
  ulonglong local_470;
  uint local_468;
  uint local_464;
  ulonglong local_460;
  ulonglong local_458;
  void *local_450;
  ulonglong local_448;
  ulonglong local_440 [2];
  wchar_t *local_430;
  ulonglong local_428;
  undefined1 *puStack_420;
  uint local_418;
  uint local_414;
  ulonglong local_408;
  ulonglong local_400;
  uint local_3f4;
// ... trimmed ...
```

## Match @ `140b566a0`

- Function: `KiInitializeProcessor`
- Via: `symbol:KiCreateDpcLimitsProcessorConfiguration`
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

## Match @ `1405b4b2d`

- Function: `KeUpdateDpcWatchdogConfiguration`
- Via: `symbol:KiCreateDpcLimitsProcessorConfiguration`
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

