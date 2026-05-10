# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/mmsys.cpl`
- Name: `mmsys.cpl`
- Probe: `peripheral-audio-ducking-ghidra-20260507`
- Timestamp: `2026-05-07T19:03:00.944623Z`
- Patterns: `UserDuckingPreference`

## Pattern Summary

### Pattern: `UserDuckingPreference`

#### String @ `180049e80`

`UserDuckingPreference`

- Reference count: `2`
- References:
  - `18001d89c` in `FUN_18001d874`
  - `18001db21` in `FUN_18001dafc`

## Match Analysis

## Match @ `18001d89c`

- Function: `FUN_18001d874`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `59`

```c
undefined8
FUN_18001d874(longlong param_1,undefined8 param_2,undefined8 param_3,undefined8 param_4,
             undefined4 *param_5)

{
  HWND hWnd;
  LSTATUS LVar1;
  int iVar2;
  undefined8 *puVar3;
  HICON hIcon;
  int local_res10 [2];
  undefined8 local_res18;
  DWORD local_res20;
  undefined4 uStackX_24;

  local_res10[0] = 0;
  _local_res20 = CONCAT44((int)((ulonglong)param_4 >> 0x20),4);
  local_res18 = param_3;
  LVar1 = RegGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio",
                       L"UserDuckingPreference",0x10,(LPDWORD)0x0,local_res10,&local_res20);
  if (LVar1 == 0) {
    iVar2 = local_res10[0];
    if (3 < local_res10[0]) {
      iVar2 = 1;
      local_res10[0] = 1;
    }
  }
  else {
    iVar2 = 1;
  }
  *(int *)(param_1 + 0xb4) = iVar2;
  *(int *)(param_1 + 0xb0) = iVar2;
  puVar3 = FUN_180019bb8((undefined8 *)(param_1 + 8),&local_res18,0x47e);
  hWnd = (HWND)*puVar3;
  hIcon = LoadIconW(DAT_180057ad0,(LPCWSTR)0x72);
  SendMessageW(hWnd,0x170,(WPARAM)hIcon,0);
  if (hIcon != (HICON)0x0) {
    DestroyIcon(hIcon);
  }
  iVar2 = *(int *)(param_1 + 0xb4);
  if (iVar2 == 0) {
    iVar2 = 0x47f;
  }
  else if (iVar2 == 2) {
    iVar2 = 0x480;
  }
  else if (iVar2 == 1) {
    iVar2 = 0x481;
  }
  else {
    if (iVar2 != 3) goto LAB_18001d9b2;
    iVar2 = 0x482;
  }
  CheckDlgButton(*(HWND *)(param_1 + 8),iVar2,1);
LAB_18001d9b2:
  FUN_18001dbf8(param_1);
  *param_5 = 0;
  return 1;
}
```

## Match @ `18001db21`

- Function: `FUN_18001dafc`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `39`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

uint FUN_18001dafc(longlong param_1)

{
  uint uVar1;
  undefined4 uVar2;
  int local_res8 [2];
  longlong *local_res10 [3];

  local_res8[0] = *(int *)(param_1 + 0xb4);
  uVar1 = RegSetKeyValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio",
                          L"UserDuckingPreference",4,local_res8,4);
  if (0 < (int)uVar1) {
    uVar1 = uVar1 & 0xffff | 0x80070000;
  }
  if (-1 < (int)uVar1) {
    local_res10[0] = (longlong *)0x0;
    uVar1 = CoCreateInstance((IID *)&DAT_1800495c0,(LPUNKNOWN)0x0,0x17,(IID *)&DAT_180049560,
                             local_res10);
    if (-1 < (int)uVar1) {
      if (local_res8[0] == 0) {
        uVar2 = 0xc2c00000;
      }
      else if (local_res8[0] == 1) {
        uVar2 = 0xc1900000;
      }
      else if (local_res8[0] == 2) {
        uVar2 = 0xc0c00000;
      }
      else {
        uVar2 = 0;
      }
      uVar1 = (**(code **)(*local_res10[0] + 0x90))(local_res10[0],L"Comm",uVar2);
    }
    FUN_18000a6ec((longlong *)local_res10);
  }
  return uVar1;
}
```
