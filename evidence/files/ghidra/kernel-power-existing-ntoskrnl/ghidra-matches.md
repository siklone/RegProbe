# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `kernel-power-existing-ntoskrnl`
- Timestamp: `2026-03-27T21:54:48.090218500Z`
- Patterns: `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`, `UuidSequenceNumber`, `AllowRemoteDASD`

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

### Pattern: `UuidSequenceNumber`

#### String @ `1400387d8`

`UuidSequenceNumber`

- Reference count: `0`
- No direct references resolved by Ghidra

### Pattern: `AllowRemoteDASD`

#### String @ `1406b8040`

`AllowRemoteDASD`

- Reference count: `2`
- References:
  - `1404cb643` in `FUN_1404cb634`
  - `1404cb6b0` in `FUN_1404cb634`

## Match Analysis

## Unresolved Block @ `140c63608`

- Function: `<no function>`
- Forced boundary: `false`
- Naturally resolved: `false`
- Decompile success: `false`
- Output kind: `disassembly`
- Output lines: `1`

```asm
// no disassembly available in range
```

## Unresolved Block @ `140c635d8`

- Function: `<no function>`
- Forced boundary: `false`
- Naturally resolved: `false`
- Decompile success: `false`
- Output kind: `disassembly`
- Output lines: `1`

```asm
// no disassembly available in range
```

## Unresolved Block @ `140c62b88`

- Function: `<no function>`
- Forced boundary: `false`
- Naturally resolved: `false`
- Decompile success: `false`
- Output kind: `disassembly`
- Output lines: `1`

```asm
// no disassembly available in range
```

## Unresolved Block @ `140c62bb8`

- Function: `<no function>`
- Forced boundary: `false`
- Naturally resolved: `false`
- Decompile success: `false`
- Output kind: `disassembly`
- Output lines: `1`

```asm
// no disassembly available in range
```

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
