# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `longdpc-queue-threshold-ghidra-20260408b`
- Timestamp: `2026-04-08T04:20:24.566698800Z`
- Patterns: `sym:KiLongDpcQueueThreshold`

## Pattern Summary

### Pattern: `sym:KiLongDpcQueueThreshold`

#### Symbol @ `140fc41d0`

- Symbol: `KiLongDpcQueueThreshold`
- Type: `Label`

- Reference count: `3`
- References:
  - `140255414` in `KiExecuteAllDpcs` via `symbol:KiLongDpcQueueThreshold`
  - `140c60fc2` in `KeInitSystem` via `symbol:KiLongDpcQueueThreshold`
  - `140c60fcb` in `KeInitSystem` via `symbol:KiLongDpcQueueThreshold`

## Match Analysis

## Match @ `140255414`

- Function: `KiExecuteAllDpcs`
- Via: `symbol:KiLongDpcQueueThreshold`
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
/* WARNING: Enum "_NT_IORING_OP_FLAGS": Some values do not have unique names */
/* WARNING: Enum "_POWER_LIMIT_TYPES": Some values do not have unique names */
/* WARNING: Enum "_POOL_TYPE": Some values do not have unique names */
/* WARNING: Struct "_MMSUPPORT_FLAGS": ignoring multiple overlapping fields */

undefined8 KiExecuteAllDpcs(_KPRCB *param_1,_KPRIORITY_STATE *param_2,uint *param_3,uint param_4)

{
  ulong64 *puVar1;
  ulong64 *puVar2;
  undefined1 *puVar3;
  int *piVar4;
  _M128A *p_Var5;
  short *psVar6;
  ulong64 *puVar7;
  ulong64 uVar8;
  uint *puVar9;
  _KTHREAD *p_Var10;
  _KI_RESCHEDULE_CONTEXT *_Dst;
  byte *pbVar11;
  undefined8 uVar12;
  byte bVar13;
  char cVar14;
  uchar uVar15;
  undefined2 uVar16;
  longlong lVar17;
  undefined4 uVar20;
  undefined4 extraout_var;
  undefined4 extraout_var_00;
  void *pvVar18;
  undefined4 extraout_var_01;
  undefined4 extraout_var_02;
  undefined4 extraout_var_03;
  _KI_RESCHEDULE_CONTEXT_ENTRY *p_Var19;
  _LIST_ENTRY *p_Var21;
  _KTHREAD *p_Var22;
  uint uVar23;
  ulonglong uVar24;
  ulonglong *puVar25;
  ulonglong *puVar26;
  _KPRCB *p_Var27;
  _KPRIORITY_STATE *p_Var28;
  byte bVar29;
  uint uVar30;
  undefined8 *puVar31;
  ulonglong *puVar32;
  ulong uVar33;
  ulonglong *puVar34;
  ulonglong uVar35;
  _KI_RESCHEDULE_CONTEXT_ENTRY *p_Var36;
  _KTHREAD *p_Var37;
  int iVar38;
  ulonglong uVar39;
  _KPRIORITY_STATE *p_Var40;
  bool bVar41;
// ... trimmed ...
```

## Match @ `140c60fc2`

- Function: `KeInitSystem`
- Via: `symbol:KiLongDpcQueueThreshold`
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

## Match @ `140c60fcb`

- Function: `KeInitSystem`
- Via: `symbol:KiLongDpcQueueThreshold`
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

