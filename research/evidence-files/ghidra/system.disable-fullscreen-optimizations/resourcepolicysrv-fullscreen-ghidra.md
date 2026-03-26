# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ResourcePolicyServer.dll`
- Name: `ResourcePolicyServer.dll`
- Probe: `resourcepolicysrv-fullscreen-ghidra`
- Timestamp: `2026-03-26T18:19:46.895664300Z`
- Patterns: `GameDVR_FSEBehavior`, `GameDVR_FSEBehaviorMode`, `GameDVR_HonorUserFSEBehaviorMode`, `GameDVR_DXGIHonorFSEWindowsCompatible`, `GameConfigStore`

## Pattern Summary

### Pattern: `GameDVR_FSEBehavior`

#### String @ `18001dcd8`

`GameDVR_FSEBehavior`

- Reference count: `0`
- No direct references resolved by Ghidra

#### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

### Pattern: `GameDVR_FSEBehaviorMode`

#### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

### Pattern: `GameDVR_HonorUserFSEBehaviorMode`

#### String @ `18001de10`

`GameDVR_HonorUserFSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

### Pattern: `GameDVR_DXGIHonorFSEWindowsCompatible`

#### String @ `18001de60`

`GameDVR_DXGIHonorFSEWindowsCompatible`

- Reference count: `0`
- No direct references resolved by Ghidra

### Pattern: `GameConfigStore`

#### String @ `18001d730`

`onecore\base\appmodel\resourcepolicy\gameconfigstore\server\gameconfigstorerpcserver.cpp`

- Reference count: `29`
- References:
  - `1800074c1` in `FUN_180007400`
  - `180006d19` in `FUN_180006c40`
  - `1800060dc` in `FUN_18000606c`
  - `180006136` in `FUN_18000606c`
  - `1800061ad` in `FUN_18000606c`
  - `1800078b7` in `FUN_180007884`
  - `180007103` in `FUN_180007080`
  - `180006943` in `FUN_1800068e0`
  - `180007574` in `FUN_180007510`
  - `1800071b9` in `FUN_180007140`
  - `180006dce` in `FUN_180006d60`
  - `180005e13` in `FUN_180005d9c`
  - `... 17 more references omitted ...`

#### String @ `18001d790`

`gameConfigStoreManagement`

- Reference count: `1`
- References:
  - `180005e47` in `FUN_180005d9c`

#### String @ `18001d9c0`

`System\GameConfigStore\Parents`

- Reference count: `1`
- References:
  - `180008a60` in `FUN_180008a60`

#### String @ `18001da00`

`System\GameConfigStore\Children`

- Reference count: `1`
- References:
  - `180008a50` in `FUN_180008a50`

_Stopped after `4` matching strings for `GameConfigStore` to keep the export bounded._

## Match Analysis

## Match @ `1800074c1`

- Function: `FUN_180007400`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007400(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 *param_3)

{
  longlong lVar1;
  longlong *plVar2;
  uint uVar3;
  ulonglong uVar4;
  undefined8 uVar5;
  ulonglong uVar6;
  undefined8 unaff_retaddr;
  undefined1 auStack_498 [32];
  undefined8 *local_478 [2];
  longlong *local_468;
  undefined4 uStack_460;
  undefined4 uStack_45c;
  undefined8 local_458;
  undefined8 uStack_450;
  undefined8 local_448;
  undefined8 uStack_440;
  undefined8 local_438;
  undefined1 local_428 [1024];
  ulonglong local_28;

  local_28 = DAT_1800241c0 ^ (ulonglong)auStack_498;
  local_468 = (longlong *)0x0;
  local_478[0] = (undefined8 *)0x0;
  uVar5 = CONCAT71((int7)((ulonglong)param_2 >> 8),1);
  uVar4 = FUN_180005d9c(param_1,'\x01',local_478,(longlong *)&local_468);
  uVar6 = uVar4 & 0xffffffff;
  if ((int)uVar4 < 0) {
    uVar3 = 0x175;
  }
  else {
    local_438 = 0;
    local_458 = 0;
    uStack_450 = 0;
    local_448 = 0;
    uStack_440 = 0;
    uVar4 = FUN_18000d0c4(param_3,uVar5,(undefined4 *)&local_458,local_428);
    plVar2 = local_468;
    uVar6 = uVar4 & 0xffffffff;
    if ((int)uVar4 < 0) {
      uVar3 = 0x17b;
    }
    else {
      uStack_460 = *(undefined4 *)(param_2 + 1);
      uStack_45c = *(undefined4 *)((longlong)param_2 + 0xc);
      lVar1 = *local_468;
      local_468 = (longlong *)*param_2;
      uVar3 = (**(code **)(lVar1 + 0x10))(plVar2,&local_468,&local_458);
      uVar6 = (ulonglong)uVar3;
      if (-1 < (int)uVar3) goto LAB_1800074d0;
      uVar3 = 0x17e;
    }
  }
  FUN_180006678(unaff_retaddr,uVar3,
// ... trimmed ...
```

## Match @ `180006d19`

- Function: `FUN_180006c40`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006c40(RPC_BINDING_HANDLE param_1,undefined8 param_2)

{
  longlong *plVar1;
  uint uVar2;
  ulonglong uVar3;
  uint uVar4;
  undefined8 unaff_retaddr;
  undefined1 auStack_88 [48];
  ulong local_58 [2];
  undefined8 *local_50;
  undefined8 local_48;
  undefined4 uStack_40;
  undefined4 uStack_3c;
  undefined4 local_38;
  undefined4 uStack_34;
  undefined4 uStack_30;
  undefined4 uStack_2c;
  ulonglong local_28;

  local_28 = DAT_1800241c0 ^ (ulonglong)auStack_88;
  local_48 = (longlong *)0x0;
  local_50 = (undefined8 *)0x0;
  uVar3 = FUN_180005d9c(param_1,'\0',&local_50,&local_48);
  uVar2 = (uint)uVar3;
  uVar3 = uVar3 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar4 = 0x141;
  }
  else {
    local_58[0] = 0;
    uVar2 = I_RpcBindingInqLocalClientPID(param_1,local_58);
    plVar1 = local_48;
    if (uVar2 != 0) {
      if (0 < (int)uVar2) {
        uVar2 = uVar2 & 0xffff | 0x80070000;
      }
      uVar3 = (ulonglong)uVar2;
      goto LAB_180006d28;
    }
    local_38 = 0;
    uStack_34 = 0;
    uStack_30 = 0;
    uStack_2c = 0;
    uVar2 = (**(code **)(*local_48 + 0x20))(local_48,local_58[0],&local_38);
    uVar3 = (ulonglong)uVar2;
    if ((int)uVar2 < 0) {
      uVar4 = 0x14d;
    }
    else {
      local_48 = (longlong *)CONCAT44(uStack_34,local_38);
      uStack_40 = uStack_30;
      uStack_3c = uStack_2c;
      uVar2 = (**(code **)(*plVar1 + 0x18))(plVar1,&local_48,0,param_2);
      uVar3 = (ulonglong)uVar2;
      if (-1 < (int)uVar2) goto LAB_180006d28;
// ... trimmed ...
```

## Match @ `1800060dc`

- Function: `FUN_18000606c`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
ulonglong FUN_18000606c(short *param_1,longlong *param_2)

{
  longlong lVar1;
  uint uVar2;
  longlong *plVar3;
  ulonglong uVar4;
  undefined8 *puVar5;
  undefined8 *puVar6;
  void *_Memory;
  ulonglong uVar7;
  undefined8 unaff_retaddr;
  void *local_res8;
  undefined8 *local_res18 [2];
  short *local_38;
  longlong local_30;

  local_30 = -1;
  do {
    local_30 = local_30 + 1;
  } while (param_1[local_30] != 0);
  local_38 = param_1;
  plVar3 = FUN_180005a3c(DAT_180024a20,(longlong *)&local_res8,(longlong *)&local_38);
  if (*plVar3 == DAT_180024a20) {
    local_res8 = (void *)0x0;
    uVar4 = FUN_18000ce90(param_1,&local_res8);
    uVar7 = uVar4 & 0xffffffff;
    if ((int)(uint)uVar4 < 0) {
      FUN_180006678(unaff_retaddr,0x62,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                    ,(uint)uVar4);
      _Memory = local_res8;
    }
    else {
      puVar5 = FUN_180002654(0x28);
      if (puVar5 == (undefined8 *)0x0) {
        FUN_180005968((undefined8 *)0x0);
        uVar7 = 0x8007000e;
        _Memory = local_res8;
      }
      else {
        *puVar5 = &PTR_FUN_18001a030;
        puVar5[1] = 0;
        puVar5[2] = 0;
        puVar5[3] = 0;
        puVar5[4] = 0;
        puVar6 = puVar5;
        uVar4 = FUN_18000a630((longlong)puVar5,param_1);
        uVar7 = uVar4 & 0xffffffff;
        if ((int)(uint)uVar4 < 0) {
          FUN_180006678(unaff_retaddr,0x67,
                        "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                        ,(uint)uVar4);
          FUN_180005968(puVar5);
          _Memory = local_res8;
        }
        else {
          *param_2 = (longlong)puVar5;
          local_res18[0] = puVar5;
// ... trimmed ...
```

## Match @ `180006136`

- Function: `FUN_18000606c`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
ulonglong FUN_18000606c(short *param_1,longlong *param_2)

{
  longlong lVar1;
  uint uVar2;
  longlong *plVar3;
  ulonglong uVar4;
  undefined8 *puVar5;
  undefined8 *puVar6;
  void *_Memory;
  ulonglong uVar7;
  undefined8 unaff_retaddr;
  void *local_res8;
  undefined8 *local_res18 [2];
  short *local_38;
  longlong local_30;

  local_30 = -1;
  do {
    local_30 = local_30 + 1;
  } while (param_1[local_30] != 0);
  local_38 = param_1;
  plVar3 = FUN_180005a3c(DAT_180024a20,(longlong *)&local_res8,(longlong *)&local_38);
  if (*plVar3 == DAT_180024a20) {
    local_res8 = (void *)0x0;
    uVar4 = FUN_18000ce90(param_1,&local_res8);
    uVar7 = uVar4 & 0xffffffff;
    if ((int)(uint)uVar4 < 0) {
      FUN_180006678(unaff_retaddr,0x62,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                    ,(uint)uVar4);
      _Memory = local_res8;
    }
    else {
      puVar5 = FUN_180002654(0x28);
      if (puVar5 == (undefined8 *)0x0) {
        FUN_180005968((undefined8 *)0x0);
        uVar7 = 0x8007000e;
        _Memory = local_res8;
      }
      else {
        *puVar5 = &PTR_FUN_18001a030;
        puVar5[1] = 0;
        puVar5[2] = 0;
        puVar5[3] = 0;
        puVar5[4] = 0;
        puVar6 = puVar5;
        uVar4 = FUN_18000a630((longlong)puVar5,param_1);
        uVar7 = uVar4 & 0xffffffff;
        if ((int)(uint)uVar4 < 0) {
          FUN_180006678(unaff_retaddr,0x67,
                        "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                        ,(uint)uVar4);
          FUN_180005968(puVar5);
          _Memory = local_res8;
        }
        else {
          *param_2 = (longlong)puVar5;
          local_res18[0] = puVar5;
// ... trimmed ...
```

## Match @ `1800061ad`

- Function: `FUN_18000606c`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
ulonglong FUN_18000606c(short *param_1,longlong *param_2)

{
  longlong lVar1;
  uint uVar2;
  longlong *plVar3;
  ulonglong uVar4;
  undefined8 *puVar5;
  undefined8 *puVar6;
  void *_Memory;
  ulonglong uVar7;
  undefined8 unaff_retaddr;
  void *local_res8;
  undefined8 *local_res18 [2];
  short *local_38;
  longlong local_30;

  local_30 = -1;
  do {
    local_30 = local_30 + 1;
  } while (param_1[local_30] != 0);
  local_38 = param_1;
  plVar3 = FUN_180005a3c(DAT_180024a20,(longlong *)&local_res8,(longlong *)&local_38);
  if (*plVar3 == DAT_180024a20) {
    local_res8 = (void *)0x0;
    uVar4 = FUN_18000ce90(param_1,&local_res8);
    uVar7 = uVar4 & 0xffffffff;
    if ((int)(uint)uVar4 < 0) {
      FUN_180006678(unaff_retaddr,0x62,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                    ,(uint)uVar4);
      _Memory = local_res8;
    }
    else {
      puVar5 = FUN_180002654(0x28);
      if (puVar5 == (undefined8 *)0x0) {
        FUN_180005968((undefined8 *)0x0);
        uVar7 = 0x8007000e;
        _Memory = local_res8;
      }
      else {
        *puVar5 = &PTR_FUN_18001a030;
        puVar5[1] = 0;
        puVar5[2] = 0;
        puVar5[3] = 0;
        puVar5[4] = 0;
        puVar6 = puVar5;
        uVar4 = FUN_18000a630((longlong)puVar5,param_1);
        uVar7 = uVar4 & 0xffffffff;
        if ((int)(uint)uVar4 < 0) {
          FUN_180006678(unaff_retaddr,0x67,
                        "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                        ,(uint)uVar4);
          FUN_180005968(puVar5);
          _Memory = local_res8;
        }
        else {
          *param_2 = (longlong)puVar5;
          local_res18[0] = puVar5;
// ... trimmed ...
```

## Match @ `1800078b7`

- Function: `FUN_180007884`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `33`

```c
uint FUN_180007884(void)

{
  uint uVar1;
  undefined8 uVar2;
  undefined8 *puVar3;
  uint uVar4;
  undefined8 unaff_retaddr;
  undefined8 local_18 [2];

  puVar3 = local_18;
  FUN_180005be0(puVar3);
  DAT_180024220 = 1;
  uVar1 = FUN_180014f90(puVar3,(RPC_BINDING_VECTOR **)&DAT_18001b760);
  if ((int)uVar1 < 0) {
    uVar4 = 0xf8;
  }
  else {
    uVar2 = FUN_180005cfc();
    uVar1 = (uint)uVar2;
    if (-1 < (int)uVar1) {
      FUN_180005c60(local_18);
      RtlDeleteCriticalSection(&DAT_1800249e0);
      return uVar1;
    }
    uVar4 = 0xfb;
  }
  FUN_180006678(unaff_retaddr,uVar4,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,uVar1);
  FUN_180005c60(local_18);
  return uVar1;
}
```

## Match @ `180007103`

- Function: `FUN_180007080`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `42`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007080(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 param_3,
                       undefined8 param_4,undefined8 param_5)

{
  longlong lVar1;
  longlong *plVar2;
  uint uVar3;
  ulonglong uVar4;
  ulonglong uVar5;
  undefined8 unaff_retaddr;
  undefined8 *local_28 [2];
  longlong *local_18;
  undefined4 uStack_10;
  undefined4 uStack_c;

  local_18 = (longlong *)0x0;
  local_28[0] = (undefined8 *)0x0;
  uVar4 = FUN_180005d9c(param_1,'\x01',local_28,(longlong *)&local_18);
  plVar2 = local_18;
  uVar5 = uVar4 & 0xffffffff;
  if ((int)uVar4 < 0) {
    uVar3 = 0x208;
  }
  else {
    uStack_10 = *(undefined4 *)(param_2 + 1);
    uStack_c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_18;
    local_18 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x40))(plVar2,&local_18,param_3,param_5,param_4);
    uVar5 = (ulonglong)uVar3;
    if (-1 < (int)uVar3) goto LAB_180007112;
    uVar3 = 0x20a;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar5);
LAB_180007112:
  FUN_180005984(local_28[0]);
  return uVar5;
}
```

## Match @ `180006943`

- Function: `FUN_1800068e0`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `32`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_1800068e0(RPC_BINDING_HANDLE param_1,undefined8 param_2,undefined8 param_3)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_res20;
  longlong *local_18 [2];

  local_18[0] = (longlong *)0x0;
  local_res20 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_res20,(longlong *)local_18);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x1d4;
  }
  else {
    uVar1 = (**(code **)(*local_18[0] + 0x30))(local_18[0],param_3,param_2);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006952;
    uVar1 = 0x1d6;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180006952:
  FUN_180005984(local_res20);
  return uVar3;
}
```

## Match @ `180007574`

- Function: `FUN_180007510`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `40`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007510(RPC_BINDING_HANDLE param_1,undefined4 *param_2)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_res18;
  longlong *local_res20;
  undefined4 local_18;
  undefined4 uStack_14;
  undefined4 uStack_10;
  undefined4 uStack_c;

  local_res20 = (longlong *)0x0;
  local_res18 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_res18,(longlong *)&local_res20);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x19d;
  }
  else {
    local_18 = *param_2;
    uStack_14 = param_2[1];
    uStack_10 = param_2[2];
    uStack_c = param_2[3];
    uVar1 = (**(code **)(*local_res20 + 8))(local_res20,&local_18);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180007583;
    uVar1 = 0x19f;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180007583:
  FUN_180005984(local_res18);
  return uVar3;
}
```

## Match @ `1800071b9`

- Function: `FUN_180007140`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `42`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007140(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 param_3,
                       undefined8 param_4)

{
  longlong lVar1;
  longlong *plVar2;
  uint uVar3;
  ulonglong uVar4;
  ulonglong uVar5;
  undefined8 unaff_retaddr;
  undefined8 *local_28 [2];
  longlong *local_18;
  undefined4 uStack_10;
  undefined4 uStack_c;

  local_18 = (longlong *)0x0;
  local_28[0] = (undefined8 *)0x0;
  uVar4 = FUN_180005d9c(param_1,'\x01',local_28,(longlong *)&local_18);
  plVar2 = local_18;
  uVar5 = uVar4 & 0xffffffff;
  if ((int)uVar4 < 0) {
    uVar3 = 599;
  }
  else {
    uStack_10 = *(undefined4 *)(param_2 + 1);
    uStack_c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_18;
    local_18 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x70))(plVar2,&local_18,param_3,param_4);
    uVar5 = (ulonglong)uVar3;
    if (-1 < (int)uVar3) goto LAB_1800071c8;
    uVar3 = 0x259;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar5);
LAB_1800071c8:
  FUN_180005984(local_28[0]);
  return uVar5;
}
```

## Match @ `180006dce`

- Function: `FUN_180006d60`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `36`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006d60(RPC_BINDING_HANDLE param_1,undefined8 param_2,undefined4 *param_3)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_res20;
  longlong *local_18 [2];

  local_18[0] = (longlong *)0x0;
  local_res20 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_res20,(longlong *)local_18);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x1a9;
  }
  else {
    *param_3 = 0;
    param_3[1] = 0;
    param_3[2] = 0;
    param_3[3] = 0;
    uVar1 = (**(code **)(*local_18[0] + 0x28))(local_18[0],param_2,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006ddd;
    uVar1 = 0x1ae;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180006ddd:
  FUN_180005984(local_res20);
  return uVar3;
}
```

## Match @ `180005e13`

- Function: `FUN_180005d9c`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
ulonglong FUN_180005d9c(RPC_BINDING_HANDLE param_1,char param_2,undefined8 *param_3,
                       longlong *param_4)

{
  uint uVar1;
  int iVar2;
  undefined8 *puVar3;
  ulonglong uVar4;
  uint uVar5;
  ulonglong uVar6;
  undefined8 unaff_retaddr;
  RPC_BINDING_HANDLE local_res8;
  char local_48 [8];
  HANDLE local_40;
  short *local_38;
  RPC_BINDING_HANDLE *local_30;
  undefined1 local_28;

  local_38 = (short *)0x0;
  local_res8 = param_1;
  uVar1 = RpcImpersonateClient(param_1);
  uVar6 = (ulonglong)uVar1;
  if (uVar1 != 0) {
    if (0 < (int)uVar1) {
      uVar6 = (ulonglong)(uVar1 & 0xffff | 0x80070000);
    }
    goto LAB_180005f23;
  }
  local_40 = (HANDLE)0x0;
  local_30 = &local_res8;
  local_28 = 1;
  uVar1 = FUN_18000601c(&local_40);
  if ((int)uVar1 < 0) {
    uVar5 = 0xac;
LAB_180005e0f:
    FUN_180006678(unaff_retaddr,uVar5,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                  ,uVar1);
  }
  else {
    if (param_2 != '\x01') {
LAB_180005ea0:
      uVar1 = FUN_180005f4c(local_40,&local_38);
      if (-1 < (int)uVar1) {
        FUN_180005cb0(local_40);
        FUN_180005c14(&local_30);
        puVar3 = FUN_180002654(0x10);
        if (puVar3 == (undefined8 *)0x0) {
          *param_3 = 0;
        }
        else {
          puVar3 = FUN_180005be0(puVar3);
          *param_3 = puVar3;
          if (puVar3 != (undefined8 *)0x0) {
            uVar4 = FUN_18000606c(local_38,param_4);
            uVar6 = uVar4 & 0xffffffff;
            if ((int)(uint)uVar4 < 0) {
              FUN_180006678(unaff_retaddr,0xc9,
                            "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
// ... trimmed ...
```

## Match @ `180005e47`

- Function: `FUN_180005d9c`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `60`

```c
ulonglong FUN_180005d9c(RPC_BINDING_HANDLE param_1,char param_2,undefined8 *param_3,
                       longlong *param_4)

{
  uint uVar1;
  int iVar2;
  undefined8 *puVar3;
  ulonglong uVar4;
  uint uVar5;
  ulonglong uVar6;
  undefined8 unaff_retaddr;
  RPC_BINDING_HANDLE local_res8;
  char local_48 [8];
  HANDLE local_40;
  short *local_38;
  RPC_BINDING_HANDLE *local_30;
  undefined1 local_28;

  local_38 = (short *)0x0;
  local_res8 = param_1;
  uVar1 = RpcImpersonateClient(param_1);
  uVar6 = (ulonglong)uVar1;
  if (uVar1 != 0) {
    if (0 < (int)uVar1) {
      uVar6 = (ulonglong)(uVar1 & 0xffff | 0x80070000);
    }
    goto LAB_180005f23;
  }
  local_40 = (HANDLE)0x0;
  local_30 = &local_res8;
  local_28 = 1;
  uVar1 = FUN_18000601c(&local_40);
  if ((int)uVar1 < 0) {
    uVar5 = 0xac;
LAB_180005e0f:
    FUN_180006678(unaff_retaddr,uVar5,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                  ,uVar1);
  }
  else {
    if (param_2 != '\x01') {
LAB_180005ea0:
      uVar1 = FUN_180005f4c(local_40,&local_38);
      if (-1 < (int)uVar1) {
        FUN_180005cb0(local_40);
        FUN_180005c14(&local_30);
        puVar3 = FUN_180002654(0x10);
        if (puVar3 == (undefined8 *)0x0) {
          *param_3 = 0;
        }
        else {
          puVar3 = FUN_180005be0(puVar3);
          *param_3 = puVar3;
          if (puVar3 != (undefined8 *)0x0) {
            uVar4 = FUN_18000606c(local_38,param_4);
            uVar6 = uVar4 & 0xffffffff;
            if ((int)(uint)uVar4 < 0) {
              FUN_180006678(unaff_retaddr,0xc9,
                            "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
// ... trimmed ...
```

## Match @ `180008a60`

- Function: `FUN_180008a60`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `5`

```c
wchar_t * FUN_180008a60(void)

{
  return L"System\\GameConfigStore\\Parents";
}
```

## Match @ `180008a50`

- Function: `FUN_180008a50`
- Forced boundary: `false`
- Naturally resolved: `true`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `5`

```c
wchar_t * FUN_180008a50(void)

{
  return L"System\\GameConfigStore\\Children";
}
```
