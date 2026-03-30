# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `system-io-allow-remote-dasd-ntoskrnl-exe-path-aware`
- Timestamp: `2026-03-30T17:09:59.173708800Z`
- Patterns: `AllowRemoteDASD`, `RemovableStorageDevices`

## Pattern Summary

### Pattern: `AllowRemoteDASD`

#### String @ `1406b8040`

`AllowRemoteDASD`

- Reference count: `2`
- References:
  - `1404cb643` in `FUN_1404cb634`
  - `1404cb6b0` in `FUN_1404cb634`

### Pattern: `RemovableStorageDevices`

#### String @ `1406b7fa0`

`\REGISTRY\MACHINE\SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices`

- Reference count: `2`
- References:
  - `1404cb657` in `FUN_1404cb634`
  - `1404cb661` in `FUN_1404cb634`

## Match Analysis

## Match @ `1404cb643`

- Function: `FUN_1404cb634`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `43`

```c
bool FUN_1404cb634(void)

{
  int iVar1;
  size_t sVar2;
  ulonglong uVar3;
  bool bVar4;
  undefined8 local_res8;
  longlong local_res10;
  short local_18;
  short local_16;
  undefined4 local_14;
  wchar_t *local_10;

  bVar4 = false;
  local_res8 = 0;
  local_res10 = 0;
  wcslen(L"AllowRemoteDASD");
  local_14 = 0;
  local_10 = L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
  ;
  sVar2 = wcslen(
                L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
                );
  uVar3 = sVar2 * 2;
  if (0xfffd < uVar3) {
    uVar3 = 0xfffc;
  }
  local_18 = (short)uVar3;
  local_16 = local_18 + 2;
  iVar1 = FUN_140a74fb0(&local_res8,0,&local_18,0x20019,0);
  if (-1 < iVar1) {
    iVar1 = FUN_1409b29dc(local_res8,L"AllowRemoteDASD",0,&local_res10);
    if (-1 < iVar1) {
      if (*(int *)(local_res10 + 0xc) != 0) {
        bVar4 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10) != 0;
      }
      ExFreePoolWithTag(local_res10,0);
    }
    ZwClose(local_res8);
  }
  return bVar4;
}
```

## Match @ `1404cb6b0`

- Function: `FUN_1404cb634`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `43`

```c
bool FUN_1404cb634(void)

{
  int iVar1;
  size_t sVar2;
  ulonglong uVar3;
  bool bVar4;
  undefined8 local_res8;
  longlong local_res10;
  short local_18;
  short local_16;
  undefined4 local_14;
  wchar_t *local_10;

  bVar4 = false;
  local_res8 = 0;
  local_res10 = 0;
  wcslen(L"AllowRemoteDASD");
  local_14 = 0;
  local_10 = L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
  ;
  sVar2 = wcslen(
                L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
                );
  uVar3 = sVar2 * 2;
  if (0xfffd < uVar3) {
    uVar3 = 0xfffc;
  }
  local_18 = (short)uVar3;
  local_16 = local_18 + 2;
  iVar1 = FUN_140a74fb0(&local_res8,0,&local_18,0x20019,0);
  if (-1 < iVar1) {
    iVar1 = FUN_1409b29dc(local_res8,L"AllowRemoteDASD",0,&local_res10);
    if (-1 < iVar1) {
      if (*(int *)(local_res10 + 0xc) != 0) {
        bVar4 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10) != 0;
      }
      ExFreePoolWithTag(local_res10,0);
    }
    ZwClose(local_res8);
  }
  return bVar4;
}
```

## Match @ `1404cb657`

- Function: `FUN_1404cb634`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `43`

```c
bool FUN_1404cb634(void)

{
  int iVar1;
  size_t sVar2;
  ulonglong uVar3;
  bool bVar4;
  undefined8 local_res8;
  longlong local_res10;
  short local_18;
  short local_16;
  undefined4 local_14;
  wchar_t *local_10;

  bVar4 = false;
  local_res8 = 0;
  local_res10 = 0;
  wcslen(L"AllowRemoteDASD");
  local_14 = 0;
  local_10 = L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
  ;
  sVar2 = wcslen(
                L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
                );
  uVar3 = sVar2 * 2;
  if (0xfffd < uVar3) {
    uVar3 = 0xfffc;
  }
  local_18 = (short)uVar3;
  local_16 = local_18 + 2;
  iVar1 = FUN_140a74fb0(&local_res8,0,&local_18,0x20019,0);
  if (-1 < iVar1) {
    iVar1 = FUN_1409b29dc(local_res8,L"AllowRemoteDASD",0,&local_res10);
    if (-1 < iVar1) {
      if (*(int *)(local_res10 + 0xc) != 0) {
        bVar4 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10) != 0;
      }
      ExFreePoolWithTag(local_res10,0);
    }
    ZwClose(local_res8);
  }
  return bVar4;
}
```

## Match @ `1404cb661`

- Function: `FUN_1404cb634`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `43`

```c
bool FUN_1404cb634(void)

{
  int iVar1;
  size_t sVar2;
  ulonglong uVar3;
  bool bVar4;
  undefined8 local_res8;
  longlong local_res10;
  short local_18;
  short local_16;
  undefined4 local_14;
  wchar_t *local_10;

  bVar4 = false;
  local_res8 = 0;
  local_res10 = 0;
  wcslen(L"AllowRemoteDASD");
  local_14 = 0;
  local_10 = L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
  ;
  sVar2 = wcslen(
                L"\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices"
                );
  uVar3 = sVar2 * 2;
  if (0xfffd < uVar3) {
    uVar3 = 0xfffc;
  }
  local_18 = (short)uVar3;
  local_16 = local_18 + 2;
  iVar1 = FUN_140a74fb0(&local_res8,0,&local_18,0x20019,0);
  if (-1 < iVar1) {
    iVar1 = FUN_1409b29dc(local_res8,L"AllowRemoteDASD",0,&local_res10);
    if (-1 < iVar1) {
      if (*(int *)(local_res10 + 0xc) != 0) {
        bVar4 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10) != 0;
      }
      ExFreePoolWithTag(local_res10,0);
    }
    ZwClose(local_res8);
  }
  return bVar4;
}
```
