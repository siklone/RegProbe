# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/diagtrack.dll`
- Name: `diagtrack.dll`
- Patterns: `TimeStampInterval`, `Reliability`

## Pattern: `TimeStampInterval`

### String @ `1803b9778`

`TimeStampInterval`

- Reference count: `1`
- References:
  - `18038fce0` in `<no function>`

## Pattern: `Reliability`

### String @ `1803ab678`

`UTC:SleepReliabilityDiag`

- Reference count: `1`
- References:
  - `18037b100` in `<no function>`

### String @ `1803b90e0`

`UpdatePolicyScenarioReliabilityAggregator.dll`

- Reference count: `1`
- References:
  - `1800121e4` in `FUN_1800121e0`

#### Function `FUN_1800121e0` @ `1800121e0`

```c
void FUN_1800121e0(void)

{
  FUN_18005ca64(&DAT_18046a5d0,L"UpdatePolicyScenarioReliabilityAggregator.dll");
  FUN_18005ca64(&DAT_18046a5f0,L"UpdateReboot.dll");
  FUN_18005ca64(&DAT_18046a610,L"MpeCm.dll");
  FUN_18005ca64(&DAT_18046a630,L"MediaFoundationAggregator.dll");
  FUN_18005ca64(&DAT_18046a650,L"CodeIntegrityAggregator.dll");
  atexit(FUN_180375560);
  return;
}
```

### String @ `1803b97f0`

`SOFTWARE\Microsoft\Reliability Analysis\RAC`

- Reference count: `6`
- References:
  - `18026bfc0` in `FUN_18026bf84`
  - `18026c075` in `FUN_18026bf84`
  - `1801703b9` in `FUN_180170218`
  - `18038fd20` in `<no function>`
  - `180391f20` in `<no function>`
  - `1803923f0` in `<no function>`

#### Function `FUN_18026bf84` @ `18026bf84`

```c
double FUN_18026bf84(void)

{
  HKEY hKey;
  uint uVar1;
  int iVar2;
  undefined8 unaff_retaddr;
  uint local_res8 [2];
  DWORD local_res10 [2];
  HKEY local_res18;
  undefined1 local_res20 [8];

  local_res8[0] = 0;
  local_res10[0] = 4;
  uVar1 = RegGetValueW((HKEY)0xffffffff80000002,L"SOFTWARE\\Microsoft\\Reliability Analysis\\RAC",
                       L"RacSampleNumber",0x18,(LPDWORD)0x0,local_res8,local_res10);
  if (0 < (int)uVar1) {
    uVar1 = uVar1 & 0xffff | 0x80070000;
  }
  if (((int)uVar1 < 0) || (99999999 < local_res8[0] - 1)) {
    local_res18 = (HKEY)0x0;
    iVar2 = FUN_18018d4dc(uVar1,100000000);
    hKey = local_res18;
    local_res8[0] = iVar2 + 1;
    if (local_res18 != (HKEY)0x0) {
      FUN_18015514c(local_res20);
      RegCloseKey(hKey);
      FUN_1801564c8(local_res20);
    }
    local_res18 = (HKEY)0x0;
    uVar1 = RegCreateKeyExW((HKEY)0xffffffff80000002,
                            L"SOFTWARE\\Microsoft\\Reliability Analysis\\RAC",0,(LPWSTR)0x0,0,
                            0x20006,(LPSECURITY_ATTRIBUTES)0x0,&local_res18,(LPDWORD)0x0);
    if (0 < (int)uVar1) {
      uVar1 = uVar1 & 0xffff | 0x80070000;
    }
    if ((int)uVar1 < 0) {
                    /* WARNING: Subroutine does not return */
      FUN_18017768c(unaff_retaddr,0x44,
                    "onecore\\base\\telemetry\\utc\\service\\include\\samplingnumber.hpp",uVar1);
    }
    uVar1 = RegSetValueExW(local_res18,L"RacSampleNumber",0,4,(BYTE *)local_res8,4);
    if (0 < (int)uVar1) {
      uVar1 = uVar1 & 0xffff | 0x80070000;
    }
    if ((int)uVar1 < 0) {
                    /* WARNING: Subroutine does not return */
      FUN_18017768c(unaff_retaddr,0x4c,
                    "onecore\\base\\telemetry\\utc\\service\\include\\samplingnumber.hpp",uVar1);
    }
    FUN_180144a04(&local_res18);
  }
  return ((double)local_res8[0] / 100000000.0) * 100.0;
}
```

#### Function `FUN_180170218` @ `180170218`

```c
int FUN_180170218(longlong param_1,undefined8 param_2)

{
  int iVar1;
  char cVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  wchar_t *pwVar6;
  undefined4 uVar7;
  undefined2 *puVar8;
  int local_118;
  int local_114;
  _FILETIME local_110;
  undefined2 *local_108;
  undefined2 *local_100;
  undefined2 local_f8 [8];
  undefined2 *local_e8;
  undefined2 *local_e0;
  undefined2 local_d8 [8];
  undefined2 *local_c8;
  undefined2 *local_c0;
  undefined2 local_b8 [8];
  undefined2 *local_a8;
  undefined2 *local_a0;
  undefined2 local_98 [8];
  undefined2 *local_88;
  undefined2 *local_80;
  undefined2 local_78 [8];
  undefined2 *local_68;
  undefined2 *local_60;
  undefined2 local_58 [8];
  _FILETIME local_48;

  local_68 = local_58;
  local_110.dwLowDateTime = 0;
  local_110.dwHighDateTime = 0;
  local_c8 = local_b8;
  local_c0 = local_b8;
  local_b8[0] = 0;
  local_e8 = local_d8;
  local_e0 = local_d8;
  local_d8[0] = 0;
  uVar7 = 0;
  local_58[0] = 0;
  local_108 = local_f8;
  local_100 = local_f8;
  local_f8[0] = 0;
  local_88 = local_78;
  local_80 = local_78;
  local_78[0] = 0;
  local_a8 = local_98;
  local_a0 = local_98;
  local_98[0] = 0;
  local_60 = local_68;
  GetSystemTimeAsFileTime(&local_110);
  local_48 = local_110;
  iVar3 = FUN_1801091e0(0xffffffff80000002,L"SOFTWARE\\Microsoft\\SQMClient",L"MachineId",
                        L"{BADF00DB-ADF0-0DBA-DF00-DBADF00DBADF}",&local_c8,1,1);
  if (iVar3 < 0) goto LAB_18017078e;
  iVar4 = FUN_180108530(0xffffffff80000002,L"SOFTWARE\\Policies\\Microsoft\\SQMClient",
                        L"MSFTInternal",0,0);
  if (iVar4 == 0) {
    iVar4 = FUN_180108530(0xffffffff80000002,L"SOFTWARE\\Microsoft\\SQMClient",L"MSFTInternal",0,0);
  }
  iVar5 = *(int *)(param_1 + 0x268);
  if (iVar5 == 0) {
    iVar5 = FUN_180108530(0xffffffff80000002,L"SOFTWARE\\Microsoft\\SQMClient",L"IsTest",0,0);
  }
  local_118 = FUN_180108530(0xffffffff80000002,L"SOFTWARE\\Microsoft\\Reliability Analysis\\RAC",
                            L"RacSampleNumber",0,0);
  if (local_118 == 0) {
    FUN_1802ded8c(&local_118);
  }
  iVar1 = local_118;
  FUN_1802deb2c();
  iVar3 = FUN_1801091e0(0xffffffff80000002,L"SOFTWARE\\Policies\\Microsoft\\SQMClient",
                        L"CorporateSQMURL",0,&local_e8,1,1);
  if ((iVar3 < 0) || (local_e8 == local_e0)) {
// ... trimmed ...
```

### String @ `1803bd620`

`Reliability-Mode`

- Reference count: `2`
- References:
  - `18016a1a2` in `FUN_180169fd8`
  - `18016a1a9` in `FUN_180169fd8`

#### Function `FUN_180169fd8` @ `180169fd8`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_180169fd8(longlong param_1,longlong param_2,char param_3,wchar_t **param_4,
                 undefined4 param_5,longlong *param_6,undefined8 param_7)

{
  undefined8 uVar1;
  undefined8 uVar2;
  undefined8 uVar3;
  wchar_t **ppwVar4;
  undefined8 uVar5;
  wchar_t *pwVar6;
  undefined8 uVar7;
  char cVar8;
  undefined1 uVar9;
  int iVar10;
  ULONGLONG UVar11;
  undefined8 uVar12;
  undefined4 *puVar13;
  longlong lVar14;
  undefined8 *puVar15;
  longlong lVar16;
  undefined7 uVar17;
  ulonglong uVar18;
  bool bVar19;
  undefined4 extraout_XMM0_Da;
  undefined4 extraout_XMM0_Da_00;
  undefined4 extraout_XMM0_Da_01;
  undefined4 extraout_XMM0_Da_02;
  undefined8 unaff_retaddr;
  undefined1 auStack_948 [32];
  undefined8 local_928;
  undefined4 *local_920;
  wchar_t **local_918;
  wchar_t **local_910;
  wchar_t ***local_908;
  int *local_900;
  undefined8 *local_8f8;
  undefined8 *local_8f0;
  undefined8 local_8e8;
  undefined1 *local_8d8;
  undefined4 *local_8d0;
  longlong *local_8c8;
  longlong local_8c0;
  undefined1 local_8b8;
  undefined4 local_8b4;
  undefined4 local_8b0;
  undefined4 local_8ac;
  undefined4 local_8a8;
  undefined4 local_8a4;
  int local_8a0 [2];
  wchar_t **local_898;
  undefined8 local_890;
  longlong local_888;
  longlong lStack_880;
  longlong local_878;
  undefined1 local_870 [8];
  undefined8 local_868 [2];
  wchar_t *local_858;
  undefined8 uStack_850;
  wchar_t *local_848;
  undefined8 uStack_840;
  undefined4 local_838;
  undefined4 local_834;
  undefined4 local_830;
  undefined4 local_82c;
  undefined8 local_828;
  undefined8 local_820;
  longlong *local_818;
  wchar_t *local_808;
  undefined8 uStack_800;
  wchar_t *local_7f8;
  undefined8 uStack_7f0;
  wchar_t *local_7e8;
  undefined8 uStack_7e0;
  wchar_t *local_7d8;
  undefined8 uStack_7d0;
  wchar_t *local_7c8;
  undefined8 uStack_7c0;
// ... trimmed ...
```

### String @ `1803c7300`

`SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability\PBR`

- Reference count: `2`
- References:
  - `1802626d8` in `FUN_180262664`
  - `1803921a0` in `<no function>`

#### Function `FUN_180262664` @ `180262664`

```c
undefined8 * FUN_180262664(undefined8 *param_1)

{
  PHKEY phkResult;
  HKEY hKey;
  uint uVar1;
  undefined8 unaff_retaddr;
  undefined1 local_res10 [8];

  FUN_1802621b4();
  *param_1 = &PTR_FUN_18038a430;
  param_1[5] = 0;
  *(undefined4 *)(param_1 + 1) = 0x11;
  phkResult = (PHKEY)(param_1 + 3);
  hKey = *phkResult;
  if (hKey != (HKEY)0x0) {
    FUN_18015514c(local_res10);
    RegCloseKey(hKey);
    FUN_1801564c8(local_res10);
  }
  *phkResult = (HKEY)0x0;
  uVar1 = RegOpenKeyExW((HKEY)0xffffffff80000002,
                        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Reliability\\PBR",0,
                        0x80000000,phkResult);
  if (0 < (int)uVar1) {
    uVar1 = uVar1 & 0xffff | 0x80070000;
  }
  if ((int)uVar1 < 0) {
                    /* WARNING: Subroutine does not return */
    FUN_18017768c(unaff_retaddr,0xf,
                  "onecore\\base\\telemetry\\utc\\service\\metadata\\lastsuccessfulrefreshtimemetadata.cpp"
                  ,uVar1);
  }
  FUN_180262780(param_1);
  return param_1;
}
```
