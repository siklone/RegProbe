# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ExplorerFrame.dll`
- Name: `ExplorerFrame.dll`
- Patterns: `UseCompactMode`

## Pattern: `UseCompactMode`

### String @ `18023d708`

`UseCompactMode`

- Reference count: `6`
- References:
  - `1801935cd` in `FUN_1801935c0`
  - `180031a1d` in `FUN_18003198c`
  - `180031a7a` in `FUN_18003198c`
  - `1800c0f3c` in `FUN_1800c0e10`
  - `180030796` in `FUN_1800303e0`
  - `1800307ef` in `FUN_1800303e0`

#### Function `FUN_1801935c0` @ `1801935c0`

```c
void FUN_1801935c0(longlong param_1,ulonglong param_2)

{
  int iVar1;
  ulonglong uVar2;
  undefined8 unaff_retaddr;
  ulonglong local_res10 [3];
  
  local_res10[0] = param_2;
  iVar1 = FUN_1800bf528(L"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                        L"UseCompactMode",0);
  if (*(int *)(param_1 + 0x10) != iVar1) {
    local_res10[0] = local_res10[0] & 0xffffffff00000000;
    *(int *)(param_1 + 0x10) = iVar1;
    FUN_18018feac(local_res10,L"SendingAdvancedSettingsChange");
    uVar2 = SendMessageW(*(HWND *)(param_1 + 8),0x1a,0,0x180250378);
    if ((int)uVar2 != 0) {
      FUN_18019c21c(unaff_retaddr,0x4d2,"shell\\explorerframe\\explorerframep.cpp",
                    uVar2 & 0xffffffff);
    }
  }
  return;
}
```

#### Function `FUN_18003198c` @ `18003198c`

```c
bool FUN_18003198c(void)

{
  undefined4 uVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  uint local_res8 [2];
  DWORD local_res10 [2];
  DWORD local_res18 [4];
  
  local_res8[0] = 0;
  local_res10[0] = 0;
  uVar1 = RtlQueryWnfStateData(local_res10,0xf850339a3bc0835,FUN_1800ee4d0,local_res8,0);
  uVar2 = RtlNtStatusToDosError(uVar1);
  if (0 < (int)uVar2) {
    uVar2 = uVar2 & 0xffff | 0x80070000;
  }
  if ((int)uVar2 < 0) {
    FUN_180020204(unaff_retaddr,0x47,
                  "onecoreuap\\internal\\shell\\inc\\private\\TabletModeHelpers.h",uVar2);
  }
  else if (local_res8[0] == 1) {
    return true;
  }
  local_res10[0] = local_res10[0] & 0xffffff00;
  local_res8[0] = local_res8[0] & 0xffffff00;
  FUN_180031b04();
  if (((char)local_res10[0] != '\0') && ((char)local_res8[0] != '\0')) {
    return true;
  }
  local_res8[0] = 0;
  local_res10[0] = 4;
  uVar2 = RegGetValueW((HKEY)0xffffffff80000001,
                       L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                       L"UseCompactMode",0x10,(LPDWORD)0x0,local_res8,local_res10);
  if (0 < (int)uVar2) {
    uVar2 = uVar2 & 0xffff | 0x80070000;
  }
  if ((int)uVar2 < 0) {
    local_res18[0] = 4;
    uVar2 = RegGetValueW((HKEY)0xffffffff80000002,
                         L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                         L"UseCompactMode",0x10,(LPDWORD)0x0,local_res8,local_res18);
    if (0 < (int)uVar2) {
      uVar2 = uVar2 & 0xffff | 0x80070000;
    }
    if ((int)uVar2 < 0) {
      local_res8[0] = 0;
    }
  }
  return local_res8[0] == 0;
}
```

#### Function `FUN_1800c0e10` @ `1800c0e10`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800c0e10(longlong param_1)

{
  char cVar1;
  int iVar2;
  undefined4 uVar3;
  uint uVar4;
  undefined8 uVar5;
  undefined8 *puVar6;
  undefined8 unaff_retaddr;
  undefined1 auStackY_188 [32];
  HKEY local_138 [2];
  undefined8 local_128;
  undefined4 uStack_120;
  undefined4 uStack_11c;
  undefined **local_118;
  undefined8 local_110;
  undefined ***local_108;
  undefined8 local_100;
  undefined4 local_f8;
  undefined8 local_f0;
  undefined1 local_e8 [8];
  undefined1 local_e0 [16];
  undefined1 local_d0 [56];
  undefined1 local_98 [8];
  undefined **local_90;
  undefined8 local_88;
  undefined ***local_28;
  ulonglong local_20;
  
  local_20 = DAT_180294ec0 ^ (ulonglong)auStackY_188;
  local_118 = &PTR_FUN_18021c430;
  local_110 = 0;
  local_108 = &local_118;
  local_100 = 0;
  local_f8 = 0;
  local_f0 = 0;
  FUN_1800c0c3c(local_e8);
  cVar1 = FUN_18002f284();
  if (cVar1 != '\0') {
    cVar1 = FUN_180116710(param_1 + 0x380);
    if (cVar1 != '\0') {
      FUN_18012efa0();
      uVar5 = FUN_18005921c(param_1 + 0x380,local_d0);
      FUN_1800c0934(&local_118,uVar5);
      FUN_1800c0d50(local_d0);
    }
  }
  cVar1 = FUN_18002f2d4();
  if (cVar1 == '\0') {
LAB_1800c0ee6:
    FUN_1800c0844(param_1,1,0);
    iVar2 = FUN_1800c1310(*(undefined8 *)(param_1 + 0x298),*(undefined8 *)(param_1 + 0x2c0),
                          *(int *)(param_1 + 0x198) != 0);
    if (iVar2 < 0) {
      FUN_1800c0478(param_1);
    }
    FUN_1800c0844(param_1,2,0);
  }
  else {
    cVar1 = FUN_1800c6204();
    if (cVar1 != '\0') goto LAB_1800c0ee6;
    cVar1 = FUN_1800fe204();
    iVar2 = 0;
    if (cVar1 == '\0') {
      FUN_1801289f4(&DAT_1802985b0,1);
      iVar2 = 0;
      if (*(char *)(param_1 + 0x2b8) == '\0') goto LAB_1800c0ee6;
    }
  }
  uVar3 = FUN_1800bf528(L"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                        L"UseCompactMode",0);
  puVar6 = (undefined8 *)FUN_180191314(local_e0,*(undefined8 *)(param_1 + 8),uVar3);
  local_128 = *puVar6;
  uStack_120 = *(undefined4 *)(puVar6 + 1);
  uStack_11c = *(undefined4 *)((longlong)puVar6 + 0xc);
// ... trimmed ...
```

#### Function `FUN_1800303e0` @ `1800303e0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

undefined8 FUN_1800303e0(Element *param_1,uint param_2)

{
  Element *pEVar1;
  longlong lVar2;
  char cVar3;
  ushort uVar4;
  int iVar5;
  undefined4 uVar6;
  uint uVar7;
  int iVar8;
  ushort *puVar9;
  Element *pEVar10;
  Value *pVVar11;
  Element *pEVar12;
  ulonglong uVar13;
  longlong *plVar14;
  bool bVar15;
  Element *pEVar16;
  int iVar17;
  undefined4 extraout_XMM0_Da;
  undefined8 unaff_retaddr;
  longlong *local_res8;
  longlong *local_res18;
  LPCWSTR local_res20;
  undefined8 local_b8;
  undefined8 uStack_b0;
  undefined8 local_a8;
  Element *local_a0;
  undefined4 local_98;
  undefined4 local_94;
  DWORD local_90 [2];
  Element *local_88;
  ulong local_80 [2];
  undefined2 local_78 [12];
  Event local_60 [8];
  undefined *local_58;
  uint local_40;
  
  pEVar12 = (Element *)0x0;
  local_80[0] = 0;
  local_88 = param_1;
  DirectUI::Element::StartDefer(param_1,local_80);
  pEVar1 = param_1 + 0xd8;
  if (*(code **)(*(longlong *)param_1 + 0x1e0) == FUN_18008aa80) {
    plVar14 = *(longlong **)pEVar1;
    if (plVar14 != (longlong *)0x0) {
      local_b8 = 0;
      uStack_b0 = 0;
      local_a8 = 0;
      local_res8 = (longlong *)((ulonglong)local_res8 & 0xffffffff00000000);
      iVar5 = (**(code **)(*plVar14 + 0x18))(plVar14,0,&DAT_18023d468,&local_b8,&local_res8);
      if (-1 < iVar5) {
        puVar9 = (ushort *)PropVariantToStringWithDefault(&local_b8,0);
        DirectUI::Element::SetAccName(param_1,puVar9);
      }
      PropVariantClear((PROPVARIANT *)&local_b8);
    }
  }
  else {
    (**(code **)(*(longlong *)param_1 + 0x1e0))(param_1);
  }
  if (*(longlong *)pEVar1 == 0) goto LAB_1800309a8;
  local_58 = PTR_DAT_1802947a8;
  local_40 = param_2;
  DirectUI::Element::BroadcastEvent(param_1,local_60);
  pEVar10 = DirectUI::Element::GetTopLevel(param_1);
  pEVar16 = pEVar12;
  if (pEVar10 != (Element *)0x0) {
    plVar14 = DAT_1802973c0;
    if (*(code **)(*(longlong *)pEVar10 + 0x118) != FUN_180027ab0) {
      plVar14 = (longlong *)(**(code **)(*(longlong *)pEVar10 + 0x118))(pEVar10);
    }
    cVar3 = (**(code **)(*plVar14 + 0x50))(plVar14);
    if (cVar3 != '\0') {
      pEVar16 = pEVar10;
    }
// ... trimmed ...
```

