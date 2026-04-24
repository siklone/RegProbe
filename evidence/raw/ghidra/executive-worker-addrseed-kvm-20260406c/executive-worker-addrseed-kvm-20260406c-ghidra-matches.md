# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `executive-worker-addrseed-kvm-20260406c`
- Timestamp: `2026-04-06T11:13:19.765415900Z`
- Patterns: `addr:140c62b88`, `addr:140c62bb8`

## Pattern Summary

### Pattern: `addr:140c62b88`

- Address seed: `140c62b88`

### Pattern: `addr:140c62bb8`

- Address seed: `140c62bb8`

## Match Analysis

## Match @ `140c62b88`

- Function: `IopInitializeSystemDrivers`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined8 IopInitializeSystemDrivers(void)

{
  char cVar1;
  int iVar2;
  int iVar3;
  longlong *plVar4;
  longlong lVar5;
  longlong lVar6;
  longlong *plVar7;
  undefined4 local_res8 [2];
  longlong local_res10;
  undefined8 local_res18;
  undefined8 local_58;
  wchar_t *pwStack_50;
  undefined8 local_48;
  longlong lStack_40;
  undefined8 local_38;
  undefined8 uStack_30;

  local_res8[0] = 0;
  local_res18 = 0;
  local_res10 = 0;
  local_48 = 0;
  lStack_40 = 0;
  local_58 = 0;
  pwStack_50 = (wchar_t *)0x0;
  local_38 = 0;
  uStack_30 = 0;
  PnpDiagnosticTrace(&KMPnPEvt_SystemStart_Start,0,0);
  cVar1 = ExIsManufacturingModeEnabled();
  plVar4 = (longlong *)CmGetSystemDriverList(-(ulonglong)(cVar1 != '\0') & DAT_140efeb10);
  if (plVar4 != (longlong *)0x0) {
    lVar5 = *plVar4;
    plVar7 = plVar4;
    while (lVar5 != 0) {
      iVar2 = IopGetDriverNameFromKeyNode(lVar5,&local_38);
      if (iVar2 < 0) {
LAB_140c62a9b:
        local_58 = CONCAT44(local_58._4_4_,0xa0008);
        pwStack_50 = L"Enum";
        iVar2 = IopOpenRegistryKeyEx(&local_res18,*plVar7,&local_58,0x20019);
        if (-1 < iVar2) {
          iVar2 = 0;
          iVar3 = IopGetRegistryValue(local_res18,L"INITSTARTFAILED",0,&local_res10);
          if (-1 < iVar3) {
            if (*(int *)(local_res10 + 0xc) == 4) {
              iVar2 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10);
            }
            ExFreePoolWithTag(local_res10,0);
          }
          ZwClose(local_res18);
          if (iVar2 != 0) goto LAB_140c62a8e;
        }
        iVar2 = IopGetRegistryValue(*plVar7,L"Group",0,&local_res10);
        lVar5 = local_res10;
        if (iVar2 < 0) {
          lVar6 = 0;
        }
// ... trimmed ...
```

## Match @ `140c62bb8`

- Function: `IopInitializeSystemDrivers`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
undefined8 IopInitializeSystemDrivers(void)

{
  char cVar1;
  int iVar2;
  int iVar3;
  longlong *plVar4;
  longlong lVar5;
  longlong lVar6;
  longlong *plVar7;
  undefined4 local_res8 [2];
  longlong local_res10;
  undefined8 local_res18;
  undefined8 local_58;
  wchar_t *pwStack_50;
  undefined8 local_48;
  longlong lStack_40;
  undefined8 local_38;
  undefined8 uStack_30;

  local_res8[0] = 0;
  local_res18 = 0;
  local_res10 = 0;
  local_48 = 0;
  lStack_40 = 0;
  local_58 = 0;
  pwStack_50 = (wchar_t *)0x0;
  local_38 = 0;
  uStack_30 = 0;
  PnpDiagnosticTrace(&KMPnPEvt_SystemStart_Start,0,0);
  cVar1 = ExIsManufacturingModeEnabled();
  plVar4 = (longlong *)CmGetSystemDriverList(-(ulonglong)(cVar1 != '\0') & DAT_140efeb10);
  if (plVar4 != (longlong *)0x0) {
    lVar5 = *plVar4;
    plVar7 = plVar4;
    while (lVar5 != 0) {
      iVar2 = IopGetDriverNameFromKeyNode(lVar5,&local_38);
      if (iVar2 < 0) {
LAB_140c62a9b:
        local_58 = CONCAT44(local_58._4_4_,0xa0008);
        pwStack_50 = L"Enum";
        iVar2 = IopOpenRegistryKeyEx(&local_res18,*plVar7,&local_58,0x20019);
        if (-1 < iVar2) {
          iVar2 = 0;
          iVar3 = IopGetRegistryValue(local_res18,L"INITSTARTFAILED",0,&local_res10);
          if (-1 < iVar3) {
            if (*(int *)(local_res10 + 0xc) == 4) {
              iVar2 = *(int *)((ulonglong)*(uint *)(local_res10 + 8) + local_res10);
            }
            ExFreePoolWithTag(local_res10,0);
          }
          ZwClose(local_res18);
          if (iVar2 != 0) goto LAB_140c62a8e;
        }
        iVar2 = IopGetRegistryValue(*plVar7,L"Group",0,&local_res10);
        lVar5 = local_res10;
        if (iVar2 < 0) {
          lVar6 = 0;
        }
// ... trimmed ...
```
