# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/diagtrack.dll`
- Name: `diagtrack.dll`
- Probe: `diagtrack-reliability-ghidra`
- Timestamp: `2026-03-26T17:37:34.225204900Z`
- Patterns: `addr:18038fce0`, `addr:18026bfc0`, `TimeStampInterval`

## Pattern Summary

### Pattern: `addr:18038fce0`

- Address seed: `18038fce0`

### Pattern: `addr:18026bfc0`

- Address seed: `18026bfc0`

### Pattern: `TimeStampInterval`

_No matching strings found for `TimeStampInterval`._

## Match Analysis

## Unresolved Block @ `18038fce0`

- Function: `FUN_18038fce0`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Control flow encountered bad instruction data */
/* WARNING: Instruction at (ram,0x00018038fce6) overlaps instruction at (ram,0x00018038fce2)
    */

void FUN_18038fce0(undefined1 *param_1,undefined8 param_2,char param_3)

{
  byte bVar1;
  undefined4 uVar2;
  byte bVar3;
  char cVar4;
  uint uVar5;
  byte *pbVar6;
  byte *in_RAX;
  char *pcVar7;
  undefined1 *puVar8;
  char cVar10;
  char cVar11;
  undefined1 unaff_BL;
  char unaff_BH;
  undefined6 unaff_0000001a;
  char *unaff_RSI;
  byte *unaff_RDI;
  bool bVar12;
  undefined1 in_ZF;
  undefined1 in_SF;
  uint7 uVar9;

  cVar11 = (char)((ulonglong)param_2 >> 8);
  cVar10 = (char)param_2;
  do {
    puVar8 = (undefined1 *)register0x00000020;
    if (!(bool)in_SF) {
code_r0x00018038fce2:
      register0x00000020 = (BADSPACEBASE *)puVar8;
      *(uint *)in_RAX = *(int *)in_RAX + (uint)in_RAX + (uint)((uint)in_RAX < *(uint *)(in_RAX + 1))
      ;
      *in_RAX = *in_RAX + (char)in_RAX;
      *in_RAX = *in_RAX + (char)in_RAX;
LAB_18038fcee:
      *in_RAX = *in_RAX + (char)in_RAX;
      cVar4 = DAT_2700000001803b97;
      uVar9 = (uint7)((ulonglong)in_RAX >> 8);
      pcVar7 = (char *)CONCAT71(uVar9,DAT_2700000001803b97);
      *pcVar7 = *pcVar7 + DAT_2700000001803b97;
      *pcVar7 = *pcVar7 + cVar4;
      *pcVar7 = *pcVar7 + cVar4;
      pcVar7[0x32] = pcVar7[0x32] + cVar10;
      puVar8 = (undefined1 *)((ulonglong)uVar9 << 8);
      *puVar8 = *puVar8;
      *puVar8 = *puVar8;
      *puVar8 = *puVar8;
      *(undefined8 *)((longlong)register0x00000020 + -8) = 0x1803a6f;
      *puVar8 = *puVar8;
      *unaff_RDI = *unaff_RDI + cVar10;
      *puVar8 = *puVar8;
      *puVar8 = *puVar8;
      *puVar8 = *puVar8;
      uVar5 = (int)unaff_RDI - *(int *)((ulonglong)unaff_RDI & 0xffffffff);
// ... trimmed ...
```

## Unresolved Block @ `18026bfc0`

- Function: `FUN_18026bfc0`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
double FUN_18026bfc0(void)

{
  longlong lVar1;
  code *pcVar2;
  uint uVar3;
  int iVar4;
  uint uVar5;
  longlong unaff_RBP;
  undefined8 unaff_RDI;
  char cVar6;
  double dVar7;
  undefined8 in_stack_00000020;
  undefined4 uVar8;

  uVar8 = (undefined4)((ulonglong)in_stack_00000020 >> 0x20);
  uVar3 = RegGetValueW(0xffffffff80000002,&UNK_1803b97f0);
  if (0 < (int)uVar3) {
    uVar3 = uVar3 & 0xffff | 0x80070000;
  }
  uVar5 = *(uint *)(unaff_RBP + 0x20);
  cVar6 = (char)unaff_RDI;
  if (99999999 < uVar5 - 1) {
    cVar6 = '\x01';
  }
  if (((int)uVar3 < 0) || (cVar6 != '\0')) {
    *(undefined8 *)(unaff_RBP + 0x30) = unaff_RDI;
    iVar4 = FUN_18018d4dc(uVar3,100000000);
    *(int *)(unaff_RBP + 0x20) = iVar4 + 1;
    lVar1 = *(longlong *)(unaff_RBP + 0x30);
    if (lVar1 != 0) {
      FUN_18015514c(unaff_RBP + 0x38);
      RegCloseKey(lVar1);
      FUN_1801564c8(unaff_RBP + 0x38);
    }
    *(undefined8 *)(unaff_RBP + 0x30) = unaff_RDI;
    uVar3 = RegCreateKeyExW(0xffffffff80000002,&UNK_1803b97f0,0,0,CONCAT44(uVar8,(int)unaff_RDI));
    if (0 < (int)uVar3) {
      uVar3 = uVar3 & 0xffff | 0x80070000;
    }
    if ((int)uVar3 < 0) {
      FUN_18017768c(*(undefined8 *)(unaff_RBP + 0x18),0x44,&UNK_1803c7bb8,uVar3);
      pcVar2 = (code *)swi(3);
      dVar7 = (double)(*pcVar2)();
      return dVar7;
    }
    uVar3 = RegSetValueExW(*(undefined8 *)(unaff_RBP + 0x30),&UNK_1803b9848,0,4,unaff_RBP + 0x20);
    if (0 < (int)uVar3) {
      uVar3 = uVar3 & 0xffff | 0x80070000;
    }
    if ((int)uVar3 < 0) {
      FUN_18017768c(*(undefined8 *)(unaff_RBP + 0x18),0x4c,&UNK_1803c7bb8,uVar3);
      pcVar2 = (code *)swi(3);
      dVar7 = (double)(*pcVar2)();
      return dVar7;
    }
    FUN_180144a04(unaff_RBP + 0x30);
    uVar5 = *(uint *)(unaff_RBP + 0x20);
  }
// ... trimmed ...
```
