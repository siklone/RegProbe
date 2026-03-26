# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `kernel-serialize-timer-expiration-ghidra`
- Timestamp: `2026-03-26T17:34:40.257729Z`
- Patterns: `addr:140c63068`, `SerializeTimerExpiration`

## Pattern Summary

### Pattern: `addr:140c63068`

- Address seed: `140c63068`

### Pattern: `SerializeTimerExpiration`

_No matching strings found for `SerializeTimerExpiration`._

## Match Analysis

## Unresolved Block @ `140c63068`

- Function: `FUN_140c63068`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Instruction at (ram,0x000140c63375) overlaps instruction at (ram,0x000140c63373)
    */

void FUN_140c63068(longlong param_1,longlong param_2)

{
  byte *pbVar1;
  char *pcVar2;
  byte bVar3;
  longlong lVar4;
  longlong lVar5;
  byte bVar6;
  char cVar7;
  char cVar8;
  int iVar9;
  int iVar10;
  byte *in_RAX;
  int *piVar11;
  byte *pbVar12;
  byte bVar13;
  undefined1 unaff_BL;
  undefined7 unaff_00000019;
  longlong *plVar14;
  longlong unaff_RSI;
  longlong unaff_RDI;
  undefined2 in_DS;
  char in_AF;
  longlong lStack_c6a7;

  cVar8 = (char)param_2;
  bVar13 = (byte)((ulonglong)param_1 >> 8);
  iVar9 = (int)in_RAX;
  *(int *)in_RAX = *(int *)in_RAX + iVar9;
  bVar6 = (byte)in_RAX;
  *in_RAX = *in_RAX + bVar6;
  *(int *)in_RAX = *(int *)in_RAX + iVar9;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  in_RAX[0x140c6] = in_RAX[0x140c6] + cVar8;
  *in_RAX = *in_RAX + bVar6;
  *(undefined1 *)(unaff_RDI + 0x140c6) = unaff_BL;
  *in_RAX = *in_RAX + bVar6;
  *(int *)in_RAX = *(int *)in_RAX + iVar9;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
  *in_RAX = *in_RAX + bVar6;
// ... trimmed ...
```
