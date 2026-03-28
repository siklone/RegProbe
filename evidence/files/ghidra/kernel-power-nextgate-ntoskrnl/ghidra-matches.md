# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `kernel-power-nextgate-ntoskrnl`
- Timestamp: `2026-03-28T00:56:49.568025400Z`
- Patterns: `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`

## Pattern Summary

### Pattern: `WatchdogResumeTimeout`

#### String @ `140c6a618`

`WatchdogResumeTimeout`

- Reference count: `1`
- References:
  - `140c63608` in `<no function>`

### Pattern: `WatchdogSleepTimeout`

#### String @ `140c6a648`

`WatchdogSleepTimeout`

- Reference count: `1`
- References:
  - `140c635d8` in `<no function>`

### Pattern: `AdditionalCriticalWorkerThreads`

#### String @ `140c6a210`

`AdditionalCriticalWorkerThreads`

- Reference count: `1`
- References:
  - `140c62b88` in `<no function>`

### Pattern: `AdditionalDelayedWorkerThreads`

#### String @ `140c6a1c8`

`AdditionalDelayedWorkerThreads`

- Reference count: `1`
- References:
  - `140c62bb8` in `<no function>`

## Match Analysis

## Unresolved Block @ `140c63608`

- Function: `FUN_140c635fe`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
void FUN_140c635fe(undefined8 param_1,undefined2 param_2)

{
  undefined1 *puVar1;
  undefined1 uVar2;
  code *pcVar3;
  char cVar4;
  undefined4 in_EAX;
  undefined3 uVar8;
  undefined4 in_register_00000004;
  int *piVar6;
  char *pcVar7;
  char cVar10;
  char unaff_BL;
  undefined7 unaff_00000019;
  longlong unaff_RSI;
  longlong unaff_RDI;
  undefined4 uVar5;
  undefined4 uVar9;

  cVar10 = (char)((ulonglong)param_1 >> 8);
  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + (char)in_EAX;
  piVar6 = (int *)func_0x00014206fcab();
  cVar4 = (char)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + unaff_BL;
  *(char *)((longlong)piVar6 + 1) = '\0';
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  uVar2 = in(param_2);
  *(undefined1 *)(unaff_RDI + 1) = uVar2;
  *piVar6 = *piVar6 + (int)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  pcVar7 = (char *)func_0x00014206fcdb();
  uVar9 = (undefined4)((ulonglong)pcVar7 >> 0x20);
  *pcVar7 = *pcVar7 + (char)pcVar7;
  uVar8 = (undefined3)((ulonglong)pcVar7 >> 8);
  cVar4 = (char)pcVar7 + (char)((ushort)param_2 >> 8);
  uVar5 = CONCAT31(uVar8,cVar4);
  *(undefined4 *)(unaff_RDI + 2) = *(undefined4 *)(unaff_RSI + 1);
  *(undefined1 *)(CONCAT44(uVar9,uVar5) + 1) = 0;
  *(char *)CONCAT44(uVar9,uVar5) = *(char *)CONCAT44(uVar9,uVar5) + cVar4;
  puVar1 = (undefined1 *)(CONCAT71(unaff_00000019,unaff_BL) + 0x140e0);
  *puVar1 = *puVar1;
  *(char *)CONCAT44(uVar9,uVar5) = *(char *)CONCAT44(uVar9,uVar5) + cVar4;
  *(char *)CONCAT44(uVar9,uVar5) = *(char *)CONCAT44(uVar9,uVar5) + cVar4;
  *(char *)CONCAT44(uVar9,uVar5) = *(char *)CONCAT44(uVar9,uVar5) + cVar4;
// ... trimmed ...
```

## Unresolved Block @ `140c635d8`

- Function: `FUN_140c635ce`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
void FUN_140c635ce(undefined8 param_1,undefined2 param_2)

{
  undefined1 *puVar1;
  undefined1 uVar2;
  code *pcVar3;
  char cVar4;
  undefined4 in_EAX;
  undefined3 uVar8;
  undefined4 in_register_00000004;
  int *piVar6;
  char *pcVar7;
  char cVar10;
  char cVar11;
  char unaff_BL;
  undefined7 unaff_00000019;
  longlong unaff_RSI;
  longlong unaff_RDI;
  undefined4 uVar5;
  undefined4 uVar9;

  cVar11 = (char)((ulonglong)param_1 >> 8);
  cVar10 = (char)param_1;
  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + (char)in_EAX;
  piVar6 = (int *)func_0x00014206fc7b();
  cVar4 = (char)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)((longlong)piVar6 + -0x5a) = *(char *)((longlong)piVar6 + -0x5a) + cVar10;
  *(char *)((longlong)piVar6 + 1) = '\0';
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *piVar6 = *piVar6 + (int)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  piVar6 = (int *)func_0x00014206fcab();
  cVar4 = (char)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + unaff_BL;
  *(char *)((longlong)piVar6 + 1) = '\0';
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  uVar2 = in(param_2);
  *(undefined1 *)(unaff_RDI + 1) = uVar2;
  *piVar6 = *piVar6 + (int)piVar6;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
  *(char *)piVar6 = (char)*piVar6 + cVar4;
// ... trimmed ...
```

## Unresolved Block @ `140c62b88`

- Function: `FUN_140c62b7e`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c62b7e(undefined8 param_1,undefined2 param_2)

{
  byte in_AL;
  byte bVar1;
  undefined1 in_AH;
  undefined2 in_register_00000002;
  undefined4 in_register_00000004;

  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  bRama2100000000140c6 = in_AL;
  *(undefined1 *)
   (CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) + 1) = 0;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
  *(byte *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL))) =
       *(char *)CONCAT44(in_register_00000004,CONCAT22(in_register_00000002,CONCAT11(in_AH,in_AL)))
       + in_AL;
// ... trimmed ...
```

## Unresolved Block @ `140c62bb8`

- Function: `FUN_140c62bae`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `55`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c62bae(undefined8 param_1,undefined2 param_2)

{
  char cVar1;
  uint in_EAX;
  uint uVar2;
  undefined4 in_register_00000004;

  cVar1 = (char)in_EAX;
  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + cVar1;
  cRama1c80000000140c6 = cVar1;
  *(undefined1 *)(CONCAT44(in_register_00000004,in_EAX) + 1) = 0;
  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + cVar1;
  uVar2 = in_EAX ^ 0xa1;
  out(param_2,uVar2);
  *(uint *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(int *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + uVar2;
  cVar1 = (char)uVar2;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
  cRama1900000000140c6 = cVar1;
  *(undefined1 *)((CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + 1) = 0;
  *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) =
       *(char *)(CONCAT44(in_register_00000004,in_EAX) ^ 0xa1) + cVar1;
                    /* WARNING: Bad instruction - Truncating control flow here */
  halt_baddata();
}
```
