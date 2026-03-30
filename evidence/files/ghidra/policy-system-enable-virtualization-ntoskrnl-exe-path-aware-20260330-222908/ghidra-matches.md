# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `policy-system-enable-virtualization-ntoskrnl-exe-path-aware`
- Timestamp: `2026-03-30T19:55:28.038926200Z`
- Patterns: `EnableVirtualization`, `EnableLUA`, `EnableInstallerDetection`

## Pattern Summary

### Pattern: `EnableVirtualization`

#### String @ `140adf610`

`EnableVirtualization`

- Reference count: `2`
- References:
  - `140761e9d` in `FUN_140761df8`
  - `140761ead` in `FUN_140761df8`

### Pattern: `EnableLUA`

#### String @ `140adf5f0`

`EnableLUA`

- Reference count: `2`
- References:
  - `140761e8b` in `FUN_140761df8`
  - `140761e96` in `FUN_140761df8`

### Pattern: `EnableInstallerDetection`

#### String @ `140adf640`

`EnableInstallerDetection`

- Reference count: `2`
- References:
  - `140761eb1` in `FUN_140761df8`
  - `140761eb8` in `FUN_140761df8`

## Match Analysis

## Match @ `140761e9d`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```

## Match @ `140761ead`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```

## Match @ `140761e8b`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```

## Match @ `140761e96`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```

## Match @ `140761eb1`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```

## Match @ `140761eb8`

- Function: `FUN_140761df8`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_140761df8(undefined8 param_1,undefined8 param_2,undefined8 param_3)

{
  byte *pbVar1;
  bool bVar2;
  undefined1 uVar3;
  int iVar4;
  int iVar5;
  undefined8 uVar6;
  longlong lVar7;
  uint *puVar8;
  uint uVar9;
  undefined4 extraout_XMM0_Da;
  undefined4 uVar11;
  undefined1 auStack_148 [32];
  undefined4 local_128;
  undefined4 *local_120;
  longlong local_108;
  undefined4 local_100;
  int local_fc;
  undefined8 local_f8;
  wchar_t *local_f0;
  undefined8 local_e8;
  wchar_t *local_e0;
  undefined8 local_d8;
  wchar_t *local_d0;
  undefined8 local_c8;
  wchar_t *local_c0;
  undefined4 local_b8;
  undefined4 local_b4;
  undefined8 local_b0;
  undefined8 *local_a8;
  undefined4 local_a0;
  undefined4 local_9c;
  undefined8 local_98;
  undefined8 uStack_90;
  undefined8 local_88;
  undefined8 uStack_80;
  undefined4 local_78 [2];
  wchar_t *local_70;
  uint local_68 [2];
  undefined4 local_60;
  wchar_t *local_58;
  undefined4 local_50;
  undefined4 local_48;
  wchar_t *local_40;
  undefined4 local_38;
  ulonglong local_28;
  ulonglong uVar10;

  local_28 = DAT_140e0a580 ^ (ulonglong)auStack_148;
  uVar10 = 0;
  local_f8 = 0x840082;
  local_fc = 0;
  local_f0 = L"\\Registry\\Machine\\SYSTEM\\CurrentControlSet\\Control\\LsaInformation";
  local_108 = 0;
  local_e0 = L"UACInstalled";
// ... trimmed ...
```
