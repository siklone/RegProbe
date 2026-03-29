# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `power-control-docs-first-ntoskrnl`
- Timestamp: `2026-03-29T11:05:39.871860200Z`
- Patterns: `Class1InitialUnparkCount`, `HibernateEnabled`, `HibernateEnabledDefault`, `LidReliabilityState`, `MfBufferingThreshold`, `PerfCalculateActualUtilization`, `TimerRebaseThresholdOnDripsExit`

## Pattern Summary

### Pattern: `Class1InitialUnparkCount`

#### String @ `140c6b248`

`Class1InitialUnparkCount`

- Reference count: `1`
- References:
  - `140c64148` in `<no function>`

### Pattern: `HibernateEnabled`

#### String @ `140028948`

`HibernateEnabled`

- Reference count: `1`
- References:
  - `140e07500` in `<no function>`

#### String @ `14004902b`

`HibernateEnabled`

- Reference count: `0`
- No direct references resolved by Ghidra

#### String @ `140c6ada0`

`HibernateEnabledDefault`

- Reference count: `1`
- References:
  - `140c63728` in `<no function>`

### Pattern: `HibernateEnabledDefault`

#### String @ `140c6ada0`

`HibernateEnabledDefault`

- Reference count: `1`
- References:
  - `140c63728` in `<no function>`

### Pattern: `LidReliabilityState`

#### String @ `140028a00`

`LidReliabilityState`

- Reference count: `2`
- References:
  - `1405cf133` in `FUN_1405cf0ec`
  - `140747fd8` in `FUN_140747fb8`

### Pattern: `MfBufferingThreshold`

#### String @ `140c6b548`

`MfBufferingThreshold`

- Reference count: `1`
- References:
  - `140c63ed8` in `<no function>`

### Pattern: `PerfCalculateActualUtilization`

#### String @ `140c6b2e0`

`PerfCalculateActualUtilization`

- Reference count: `1`
- References:
  - `140c640b8` in `<no function>`

### Pattern: `TimerRebaseThresholdOnDripsExit`

#### String @ `140c6c980`

`TimerRebaseThresholdOnDripsExit`

- Reference count: `1`
- References:
  - `140c64ec8` in `<no function>`

## Match Analysis

## Unresolved Block @ `140c64148`

- Function: `FUN_140c6413e`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c6413e(undefined8 param_1,undefined8 param_2)

{
  char *pcVar1;
  byte bVar2;
  byte bVar3;
  char cVar4;
  int iVar5;
  byte *in_RAX;
  char *pcVar6;
  char cVar7;
  byte bVar8;
  char cVar9;
  longlong unaff_RBX;
  longlong unaff_RSI;
  longlong unaff_retaddr;

  cVar4 = (char)((ulonglong)param_2 >> 8);
  bVar8 = (byte)((ulonglong)param_1 >> 8);
  cVar7 = (char)param_1;
  bVar3 = (byte)in_RAX;
  *in_RAX = *in_RAX + bVar3;
  *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | bVar8;
  *in_RAX = *in_RAX + bVar3;
  cVar9 = -0x3a;
  iVar5 = (int)in_RAX;
  *(int *)in_RAX = *(int *)in_RAX + iVar5;
  *in_RAX = *in_RAX + bVar3;
  *(int *)in_RAX = *(int *)in_RAX + iVar5;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *(byte *)(unaff_retaddr + 0x140c6) = *(byte *)(unaff_retaddr + 0x140c6) | bVar8;
  bVar2 = *in_RAX;
  *in_RAX = *in_RAX + bVar3;
  pcVar1 = (char *)(CONCAT62((int6)((ulonglong)param_2 >> 0x10),CONCAT11(cVar4,0xc6)) + 0x140c6);
  *pcVar1 = (*pcVar1 - cVar4) - CARRY1(bVar2,bVar3);
  *in_RAX = *in_RAX + bVar3;
  *(byte *)(unaff_RBX + -4) = bVar3;
  *(int *)in_RAX = *(int *)in_RAX + iVar5;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
  *in_RAX = *in_RAX + bVar3;
// ... trimmed ...
```

## Unresolved Block @ `140e07500`

- Function: `FUN_140e07510`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `15`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140e07510(void)

{
  char in_AL;
  undefined7 in_register_00000001;

  *(char *)CONCAT71(in_register_00000001,in_AL) =
       *(char *)CONCAT71(in_register_00000001,in_AL) + in_AL;
  *(char *)CONCAT71(in_register_00000001,in_AL) =
       *(char *)CONCAT71(in_register_00000001,in_AL) + in_AL;
                    /* WARNING: Bad instruction - Truncating control flow here */
  halt_baddata();
}
```

## Unresolved Block @ `140c63728`

- Function: `FUN_140c6371e`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `49`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c6371e(undefined8 param_1)

{
  char cVar1;
  undefined4 in_EAX;
  int iVar2;
  undefined4 in_register_00000004;
  longlong unaff_RSI;

  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + (char)in_EAX;
  *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | (byte)((ulonglong)param_1 >> 8);
  *(char *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + (char)in_EAX;
  cVar1 = cRam800000000140c6ad;
  iVar2 = CONCAT31((int3)((uint)in_EAX >> 8),cRam800000000140c6ad);
  *(int *)CONCAT44(in_register_00000004,iVar2) =
       *(int *)CONCAT44(in_register_00000004,iVar2) + iVar2;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
  *(char *)CONCAT44(in_register_00000004,iVar2) =
       *(char *)CONCAT44(in_register_00000004,iVar2) + cVar1;
                    /* WARNING: Bad instruction - Truncating control flow here */
  halt_baddata();
}
```

## Match @ `1405cf133`

- Function: `FUN_1405cf0ec`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `44`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_1405cf0ec(void)

{
  int iVar1;
  bool bVar2;
  undefined1 auStack_78 [32];
  undefined4 local_58;
  undefined4 *local_50;
  longlong local_48;
  undefined4 local_40 [2];
  undefined8 local_38;
  undefined8 uStack_30;
  undefined8 local_28;
  undefined8 uStack_20;
  undefined4 local_18;
  ulonglong local_10;

  local_10 = DAT_140e0a580 ^ (ulonglong)auStack_78;
  local_48 = 0;
  local_40[0] = 0;
  local_38 = 0;
  uStack_30 = 0;
  local_18 = 0;
  bVar2 = true;
  local_28 = 0;
  uStack_20 = 0;
  iVar1 = FUN_14073d560(0,&local_48);
  if (-1 < iVar1) {
    RtlInitUnicodeString(&local_38,L"LidReliabilityState");
    local_50 = local_40;
    local_58 = 0x14;
    iVar1 = ZwQueryValueKey(local_48,&local_38,2,&local_28);
    if (-1 < iVar1) {
      bVar2 = uStack_20._4_1_ != '\0';
    }
    if (local_48 != 0) {
      ZwClose(local_48);
    }
  }
  FUN_140747ee0(bVar2);
  return;
}
```

## Match @ `140747fd8`

- Function: `FUN_140747fb8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `21`

```c
void FUN_140747fb8(void)

{
  int iVar1;
  longlong local_res8 [4];
  undefined8 local_18;
  undefined8 uStack_10;

  local_res8[0] = 0;
  local_18 = 0;
  uStack_10 = 0;
  iVar1 = FUN_14073d560(0,local_res8);
  if (-1 < iVar1) {
    RtlInitUnicodeString(&local_18,L"LidReliabilityState");
    ZwSetValueKey(local_res8[0],&local_18,0,4,&DAT_140e0b3c8,4);
    if (local_res8[0] != 0) {
      ZwClose(local_res8[0]);
    }
  }
  return;
}
```

## Unresolved Block @ `140c63ed8`

- Function: `FUN_140c63ece`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `41`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c63ece(undefined8 param_1)

{
  char cVar1;
  int *in_RAX;
  byte bVar2;
  longlong unaff_RSI;
  int *unaff_retaddr;
  undefined8 uStackX_8;

  bVar2 = (byte)((ulonglong)param_1 >> 8);
  cVar1 = (char)in_RAX;
  *(char *)in_RAX = (char)*in_RAX + cVar1;
  *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | bVar2;
  *(char *)in_RAX = (char)*in_RAX + cVar1;
  *in_RAX = *in_RAX + (int)in_RAX;
  *(char *)in_RAX = (char)*in_RAX + cVar1;
  *unaff_retaddr = *unaff_retaddr + (int)unaff_retaddr;
  cVar1 = (char)unaff_retaddr;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *(byte *)(uStackX_8 + 0x140c6) = *(byte *)(uStackX_8 + 0x140c6) | bVar2;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
  *unaff_retaddr = *unaff_retaddr + (int)unaff_retaddr;
  *(char *)unaff_retaddr = (char)*unaff_retaddr + cVar1;
                    /* WARNING: Bad instruction - Truncating control flow here */
  halt_baddata();
}
```

## Unresolved Block @ `140c640b8`

- Function: `FUN_140c640ae`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Control flow encountered bad instruction data */
/* WARNING: Instruction at (ram,0x000140c64133) overlaps instruction at (ram,0x000140c64132)
    */

void FUN_140c640ae(longlong param_1,longlong param_2)

{
  char *pcVar1;
  byte bVar2;
  char cVar3;
  int iVar4;
  int *in_RAX;
  byte *pbVar5;
  char *pcVar6;
  longlong lVar7;
  char cVar9;
  byte bVar10;
  longlong unaff_RBX;
  longlong unaff_RSI;
  longlong unaff_retaddr;
  byte bVar8;

  bVar10 = (byte)((ulonglong)param_2 >> 8);
  while( true ) {
    cVar3 = (char)in_RAX;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(byte *)(unaff_RSI + 0x140c6) =
         *(byte *)(unaff_RSI + 0x140c6) | (byte)((ulonglong)param_1 >> 8);
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    lVar7 = param_1 + -1;
    bVar8 = (byte)((ulonglong)lVar7 >> 8);
    if (lVar7 == 0 || (char)*in_RAX == '\0') break;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | bVar8;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(byte *)(unaff_RBX + 0x140c6) = *(byte *)(unaff_RBX + 0x140c6) & bVar10;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    in_RAX = (int *)((ulonglong)in_RAX | 0x41);
    *in_RAX = *in_RAX + (int)in_RAX;
    cVar3 = (char)in_RAX;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
    *(char *)in_RAX = (char)*in_RAX + cVar3;
// ... trimmed ...
```

## Unresolved Block @ `140c64ec8`

- Function: `FUN_140c64ebe`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c64ebe(ulonglong param_1,longlong param_2)

{
  byte bVar1;
  byte bVar2;
  char cVar3;
  uint in_EAX;
  undefined4 in_register_00000004;
  byte bVar5;
  longlong lVar4;
  longlong unaff_RBX;
  longlong unaff_RSI;

  bVar2 = (byte)in_EAX;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  bVar5 = (byte)(param_1 >> 8);
  *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | bVar5;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(uint *)CONCAT44(in_register_00000004,in_EAX) =
       *(int *)CONCAT44(in_register_00000004,in_EAX) + in_EAX;
  bVar1 = *(byte *)CONCAT44(in_register_00000004,in_EAX);
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(char *)(unaff_RBX + -4) = (*(char *)(unaff_RBX + -4) - bVar2) - CARRY1(bVar1,bVar2);
  *(uint *)CONCAT44(in_register_00000004,in_EAX) =
       *(int *)CONCAT44(in_register_00000004,in_EAX) + in_EAX;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
  *(byte *)(unaff_RSI + 0x140c6) = *(byte *)(unaff_RSI + 0x140c6) | bVar5;
  *(byte *)CONCAT44(in_register_00000004,in_EAX) =
       *(char *)CONCAT44(in_register_00000004,in_EAX) + bVar2;
// ... trimmed ...
```
