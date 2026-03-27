# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/mmsys.cpl`
- Name: `mmsys.cpl`
- Patterns: `ShowHiddenDevices`, `ShowDisconnectedDevices`

## Pattern: `ShowHiddenDevices`

### String @ `180047f48`

`ShowHiddenDevices`

- Reference count: `2`
- References:
  - `18000a036` in `FUN_18000a004`
  - `180008ec0` in `FUN_180008eb0`

#### Function `FUN_18000a004` @ `18000a004`

```c
void FUN_18000a004(void)

{
  LSTATUS LVar1;
  DWORD local_res8 [2];
  uint local_res10 [2];
  DWORD local_res18 [2];
  int local_res20 [2];

  local_res18[0] = 4;
  local_res8[0] = 0;
  local_res10[0] = 0;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"ShowHiddenDevices",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_1800559ec = local_res10[0];
  }
  local_res18[0] = 4;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"ShowDisconnectedDevices",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_1800559e8 = local_res10[0];
  }
  local_res18[0] = 4;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"VolumeUnits",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_180056a08 = local_res10[0];
  }
  local_res20[0] = 1;
  SystemParametersInfoW(0x1042,0,local_res20,0);
  DAT_180056a0c = (uint)(local_res20[0] == 0);
  return;
}
```

#### Function `FUN_180008eb0` @ `180008eb0`

```c
void FUN_180008eb0(void)

{
  undefined4 local_res8 [2];

  local_res8[0] = DAT_1800559ec;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"ShowHiddenDevices",4,local_res8,4);
  local_res8[0] = DAT_1800559e8;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"ShowDisconnectedDevices",4,local_res8,4);
  local_res8[0] = DAT_180056a08;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"VolumeUnits",4,local_res8,4);
  return;
}
```

## Pattern: `ShowDisconnectedDevices`

### String @ `180047fd0`

`ShowDisconnectedDevices`

- Reference count: `2`
- References:
  - `18000a08c` in `FUN_18000a004`
  - `180008f04` in `FUN_180008eb0`

#### Function `FUN_18000a004` @ `18000a004`

```c
void FUN_18000a004(void)

{
  LSTATUS LVar1;
  DWORD local_res8 [2];
  uint local_res10 [2];
  DWORD local_res18 [2];
  int local_res20 [2];

  local_res18[0] = 4;
  local_res8[0] = 0;
  local_res10[0] = 0;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"ShowHiddenDevices",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_1800559ec = local_res10[0];
  }
  local_res18[0] = 4;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"ShowDisconnectedDevices",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_1800559e8 = local_res10[0];
  }
  local_res18[0] = 4;
  LVar1 = SHGetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
                      L"VolumeUnits",local_res8,local_res10,local_res18);
  if (((LVar1 == 0) && (local_res8[0] == 4)) && (local_res10[0] < 2)) {
    DAT_180056a08 = local_res10[0];
  }
  local_res20[0] = 1;
  SystemParametersInfoW(0x1042,0,local_res20,0);
  DAT_180056a0c = (uint)(local_res20[0] == 0);
  return;
}
```

#### Function `FUN_180008eb0` @ `180008eb0`

```c
void FUN_180008eb0(void)

{
  undefined4 local_res8 [2];

  local_res8[0] = DAT_1800559ec;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"ShowHiddenDevices",4,local_res8,4);
  local_res8[0] = DAT_1800559e8;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"ShowDisconnectedDevices",4,local_res8,4);
  local_res8[0] = DAT_180056a08;
  SHSetValueW((HKEY)0xffffffff80000001,L"Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl",
              L"VolumeUnits",4,local_res8,4);
  return;
}
```
