# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ResourcePolicyServer.dll`
- Name: `ResourcePolicyServer.dll`
- Patterns: `GameDVR_FSEBehavior`, `GameDVR_FSEBehaviorMode`, `GameDVR_HonorUserFSEBehaviorMode`, `GameDVR_DXGIHonorFSEWindowsCompatible`, `GameConfigStore`

## Pattern: `GameDVR_FSEBehavior`

### String @ `18001dcd8`

`GameDVR_FSEBehavior`

- Reference count: `0`
- No direct references resolved by Ghidra

### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

## Pattern: `GameDVR_FSEBehaviorMode`

### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

## Pattern: `GameDVR_HonorUserFSEBehaviorMode`

### String @ `18001de10`

`GameDVR_HonorUserFSEBehaviorMode`

- Reference count: `0`
- No direct references resolved by Ghidra

## Pattern: `GameDVR_DXGIHonorFSEWindowsCompatible`

### String @ `18001de60`

`GameDVR_DXGIHonorFSEWindowsCompatible`

- Reference count: `0`
- No direct references resolved by Ghidra

## Pattern: `GameConfigStore`

### String @ `18001d730`

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
  - `180005e66` in `FUN_180005d9c`
  - `180005f04` in `FUN_180005d9c`
  - `180006a21` in `FUN_180006980`
  - `180007632` in `FUN_1800075b0`
  - `18000726e` in `FUN_1800071f0`
  - `180006e6c` in `FUN_180006e00`
  - `1800076f2` in `FUN_180007670`
  - `180006b55` in `FUN_180006a60`
  - `18000731b` in `FUN_1800072b0`
  - `180006efe` in `FUN_180006ea0`
  - `180006f9d` in `FUN_180006f30`
  - `18000779b` in `FUN_180007730`
  - `1800073c4` in `FUN_180007360`
  - `180006c0d` in `FUN_180006b90`
  - `18000783d` in `FUN_1800077d4`
  - `180006890` in `FUN_1800067d0`
  - `180007046` in `FUN_180006fe0`

#### Function `FUN_180007400` @ `180007400`

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
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar6);
LAB_1800074d0:
  FUN_180005984(local_478[0]);
  return uVar6;
}
```

#### Function `FUN_180006c40` @ `180006c40`

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
      uVar4 = 0x150;
    }
  }
  FUN_180006678(unaff_retaddr,uVar4,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,uVar2);
LAB_180006d28:
  FUN_180005984(local_50);
  return uVar3;
}
```

#### Function `FUN_18000606c` @ `18000606c`

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
          FUN_180005af8(puVar6,&local_38,(longlong *)&local_res8,(longlong *)local_res18);
          FUN_180005968((undefined8 *)0x0);
          _Memory = (void *)0x0;
        }
      }
    }
    free(_Memory);
  }
  else {
    lVar1 = *(longlong *)(*plVar3 + 0x28);
    uVar2 = FUN_180008590(lVar1);
    uVar7 = (ulonglong)uVar2;
    if ((int)uVar2 < 0) {
      FUN_180006678(unaff_retaddr,0x75,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                    ,uVar2);
    }
    else {
      *param_2 = lVar1;
    }
// ... trimmed ...
```

#### Function `FUN_180007884` @ `180007884`

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

#### Function `FUN_180007080` @ `180007080`

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

#### Function `FUN_1800068e0` @ `1800068e0`

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

#### Function `FUN_180007510` @ `180007510`

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

#### Function `FUN_180007140` @ `180007140`

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

#### Function `FUN_180006d60` @ `180006d60`

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

#### Function `FUN_180005d9c` @ `180005d9c`

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
                            ,(uint)uVar4);
            }
            goto LAB_180005f23;
          }
        }
        uVar6 = 0x8007000e;
        goto LAB_180005f23;
      }
      uVar5 = 0xbf;
      goto LAB_180005e0f;
    }
    local_48[0] = '\0';
    iVar2 = CapabilityCheck(local_40,L"gameConfigStoreManagement",local_48);
    if (-1 < iVar2) {
      if (local_48[0] == '\0') {
        FUN_180005cb0(local_40);
        FUN_180005c14(&local_30);
        uVar6 = 0x80070005;
        goto LAB_180005f23;
      }
// ... trimmed ...
```

#### Function `FUN_180006980` @ `180006980`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006980(RPC_BINDING_HANDLE param_1,undefined8 *param_2,uint *param_3,
                       longlong param_4)

{
  longlong lVar1;
  longlong *plVar2;
  uint uVar3;
  ulonglong uVar4;
  uint uVar5;
  undefined8 unaff_retaddr;
  longlong *local_28;
  undefined4 uStack_20;
  undefined4 uStack_1c;
  undefined8 *local_18 [2];
  
  local_28 = (longlong *)0x0;
  local_18[0] = (undefined8 *)0x0;
  uVar4 = FUN_180005d9c(param_1,'\x01',local_18,(longlong *)&local_28);
  plVar2 = local_28;
  uVar3 = (uint)uVar4;
  uVar4 = uVar4 & 0xffffffff;
  if ((int)uVar3 < 0) {
    uVar5 = 0x130;
  }
  else {
    if ((param_3 == (uint *)0x0) || (param_4 == 0)) {
      uVar4 = 0x80070057;
      goto LAB_180006a30;
    }
    uStack_20 = *(undefined4 *)(param_2 + 1);
    uStack_1c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_28;
    local_28 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x18))(plVar2,&local_28,param_4,param_3);
    uVar4 = (ulonglong)uVar3;
    if ((int)uVar3 < 0) {
      uVar5 = 0x135;
    }
    else {
      uVar4 = FUN_18000cfc8(param_4,(ulonglong)*param_3);
      uVar3 = (uint)uVar4;
      uVar4 = uVar4 & 0xffffffff;
      if (-1 < (int)uVar3) goto LAB_180006a30;
      uVar5 = 0x138;
    }
  }
  FUN_180006678(unaff_retaddr,uVar5,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,uVar3);
LAB_180006a30:
  FUN_180005984(local_18[0]);
  return uVar4;
}
```

#### Function `FUN_1800075b0` @ `1800075b0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_1800075b0(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 param_3,
                       undefined4 param_4,undefined8 param_5)

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
    uVar3 = 0x216;
  }
  else {
    uStack_10 = *(undefined4 *)(param_2 + 1);
    uStack_c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_18;
    local_18 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x48))(plVar2,&local_18,param_3,param_5,param_4);
    uVar5 = (ulonglong)uVar3;
    if (-1 < (int)uVar3) goto LAB_180007641;
    uVar3 = 0x218;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar5);
LAB_180007641:
  FUN_180005984(local_28[0]);
  return uVar5;
}
```

#### Function `FUN_1800071f0` @ `1800071f0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_1800071f0(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 param_3,
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
    uVar3 = 0x1fa;
  }
  else {
    uStack_10 = *(undefined4 *)(param_2 + 1);
    uStack_c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_18;
    local_18 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x40))(plVar2,&local_18,param_3,0,param_4);
    uVar5 = (ulonglong)uVar3;
    if (-1 < (int)uVar3) goto LAB_18000727d;
    uVar3 = 0x1fc;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar5);
LAB_18000727d:
  FUN_180005984(local_28[0]);
  return uVar5;
}
```

#### Function `FUN_180006e00` @ `180006e00`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006e00(RPC_BINDING_HANDLE param_1,undefined4 param_2,undefined4 *param_3)

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
    uVar1 = 0x1b8;
  }
  else {
    *param_3 = 0;
    param_3[1] = 0;
    param_3[2] = 0;
    param_3[3] = 0;
    uVar1 = (**(code **)(*local_18[0] + 0x20))(local_18[0],param_2,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006e7b;
    uVar1 = 0x1bd;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180006e7b:
  FUN_180005984(local_res20);
  return uVar3;
}
```

#### Function `FUN_180007670` @ `180007670`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007670(RPC_BINDING_HANDLE param_1,undefined8 *param_2,undefined4 param_3,
                       undefined4 param_4,undefined8 param_5)

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
    uVar3 = 0x24a;
  }
  else {
    uStack_10 = *(undefined4 *)(param_2 + 1);
    uStack_c = *(undefined4 *)((longlong)param_2 + 0xc);
    lVar1 = *local_18;
    local_18 = (longlong *)*param_2;
    uVar3 = (**(code **)(lVar1 + 0x68))(plVar2,&local_18,param_3,param_5,param_4);
    uVar5 = (ulonglong)uVar3;
    if (-1 < (int)uVar3) goto LAB_180007701;
    uVar3 = 0x24c;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar5);
LAB_180007701:
  FUN_180005984(local_28[0]);
  return uVar5;
}
```

#### Function `FUN_180006a60` @ `180006a60`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006a60(RPC_BINDING_HANDLE param_1,uint *param_2,longlong param_3)

{
  longlong *plVar1;
  uint uVar2;
  ulonglong uVar3;
  uint uVar4;
  undefined8 unaff_retaddr;
  undefined1 auStack_98 [48];
  ulong local_68 [2];
  undefined8 *local_60;
  undefined8 local_58;
  undefined4 uStack_50;
  undefined4 uStack_4c;
  undefined4 local_48;
  undefined4 uStack_44;
  undefined4 uStack_40;
  undefined4 uStack_3c;
  ulonglong local_38;
  
  local_38 = DAT_1800241c0 ^ (ulonglong)auStack_98;
  local_58 = (longlong *)0x0;
  local_60 = (undefined8 *)0x0;
  uVar3 = FUN_180005d9c(param_1,'\0',&local_60,&local_58);
  uVar2 = (uint)uVar3;
  uVar3 = uVar3 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar4 = 0x15a;
  }
  else {
    local_68[0] = 0;
    uVar2 = I_RpcBindingInqLocalClientPID(param_1,local_68);
    plVar1 = local_58;
    if (uVar2 != 0) {
      if (0 < (int)uVar2) {
        uVar2 = uVar2 & 0xffff | 0x80070000;
      }
      uVar3 = (ulonglong)uVar2;
      goto LAB_180006b64;
    }
    local_48 = 0;
    uStack_44 = 0;
    uStack_40 = 0;
    uStack_3c = 0;
    uVar2 = (**(code **)(*local_58 + 0x20))(local_58,local_68[0],&local_48);
    uVar3 = (ulonglong)uVar2;
    if ((int)uVar2 < 0) {
      uVar4 = 0x166;
    }
    else {
      local_58 = (longlong *)CONCAT44(uStack_44,local_48);
      uStack_50 = uStack_40;
      uStack_4c = uStack_3c;
      uVar2 = (**(code **)(*plVar1 + 0x18))(plVar1,&local_58,param_3,param_2);
      uVar3 = (ulonglong)uVar2;
      if ((int)uVar2 < 0) {
        uVar4 = 0x169;
      }
      else {
        uVar3 = FUN_18000cfc8(param_3,(ulonglong)*param_2);
        uVar2 = (uint)uVar3;
        uVar3 = uVar3 & 0xffffffff;
        if (-1 < (int)uVar2) goto LAB_180006b64;
        uVar4 = 0x16b;
      }
    }
  }
  FUN_180006678(unaff_retaddr,uVar4,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,uVar2);
LAB_180006b64:
  FUN_180005984(local_60);
  return uVar3;
}
```

#### Function `FUN_1800072b0` @ `1800072b0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_1800072b0(RPC_BINDING_HANDLE param_1,undefined4 param_2,undefined8 param_3,
                       undefined8 param_4)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_18;
  longlong *local_10;
  
  local_10 = (longlong *)0x0;
  local_18 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\0',&local_18,(longlong *)&local_10);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x22f;
  }
  else {
    uVar1 = (**(code **)(*local_10 + 0x50))(local_10,param_2,param_4,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_18000732a;
    uVar1 = 0x231;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_18000732a:
  FUN_180005984(local_18);
  return uVar3;
}
```

#### Function `FUN_180006ea0` @ `180006ea0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006ea0(RPC_BINDING_HANDLE param_1,undefined4 *param_2)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_res18;
  longlong *local_res20;
  
  local_res20 = (longlong *)0x0;
  local_res18 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_res18,(longlong *)&local_res20);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x1c5;
  }
  else {
    *param_2 = 0;
    uVar1 = (**(code **)(*local_res20 + 0x30))(local_res20,0,param_2);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006f0d;
    uVar1 = 0x1ca;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180006f0d:
  FUN_180005984(local_res18);
  return uVar3;
}
```

#### Function `FUN_180006f30` @ `180006f30`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006f30(RPC_BINDING_HANDLE param_1,undefined8 param_2,undefined8 param_3,
                       undefined8 param_4)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_18;
  longlong *local_10;
  
  local_10 = (longlong *)0x0;
  local_18 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_18,(longlong *)&local_10);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x1ed;
  }
  else {
    uVar1 = (**(code **)(*local_10 + 0x38))(local_10,param_2,param_4,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006fac;
    uVar1 = 0x1ef;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180006fac:
  FUN_180005984(local_18);
  return uVar3;
}
```

#### Function `FUN_180007730` @ `180007730`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007730(RPC_BINDING_HANDLE param_1,undefined4 param_2,undefined4 param_3,
                       undefined8 param_4)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_18;
  longlong *local_10;
  
  local_10 = (longlong *)0x0;
  local_18 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_18,(longlong *)&local_10);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x23c;
  }
  else {
    uVar1 = (**(code **)(*local_10 + 0x58))(local_10,param_2,param_4,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_1800077aa;
    uVar1 = 0x23e;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_1800077aa:
  FUN_180005984(local_18);
  return uVar3;
}
```

#### Function `FUN_180007360` @ `180007360`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180007360(RPC_BINDING_HANDLE param_1,undefined4 param_2,undefined8 param_3)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined8 *local_res20;
  longlong *local_18 [2];
  
  local_18[0] = (longlong *)0x0;
  local_res20 = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\0',&local_res20,(longlong *)local_18);
  uVar3 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x222;
  }
  else {
    uVar1 = (**(code **)(*local_18[0] + 0x50))(local_18[0],param_2,0,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_1800073d3;
    uVar1 = 0x224;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_1800073d3:
  FUN_180005984(local_res20);
  return uVar3;
}
```

#### Function `FUN_180006b90` @ `180006b90`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006b90(RPC_BINDING_HANDLE param_1,undefined4 *param_2,longlong param_3)

{
  uint uVar1;
  ulonglong uVar2;
  uint uVar3;
  undefined8 unaff_retaddr;
  longlong *local_res20;
  undefined8 *local_28 [2];
  undefined4 local_18;
  undefined4 uStack_14;
  undefined4 uStack_10;
  undefined4 uStack_c;
  
  local_res20 = (longlong *)0x0;
  local_28[0] = (undefined8 *)0x0;
  uVar2 = FUN_180005d9c(param_1,'\x01',local_28,(longlong *)&local_res20);
  uVar1 = (uint)uVar2;
  uVar2 = uVar2 & 0xffffffff;
  if ((int)uVar1 < 0) {
    uVar3 = 0x121;
  }
  else {
    if (param_3 == 0) {
      uVar2 = 0x80070057;
      goto LAB_180006c1c;
    }
    local_18 = *param_2;
    uStack_14 = param_2[1];
    uStack_10 = param_2[2];
    uStack_c = param_2[3];
    uVar1 = (**(code **)(*local_res20 + 0x18))(local_res20,&local_18,0,param_3);
    uVar2 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180006c1c;
    uVar3 = 0x125;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,uVar1);
LAB_180006c1c:
  FUN_180005984(local_28[0]);
  return uVar2;
}
```

#### Function `FUN_1800077d4` @ `1800077d4`

```c
uint FUN_1800077d4(undefined8 param_1,undefined8 param_2,undefined8 param_3,undefined8 param_4)

{
  uint uVar1;
  void *pvVar2;
  undefined8 unaff_retaddr;
  undefined8 local_18 [2];
  
  RtlInitializeCriticalSection(&DAT_1800249e0);
  FUN_180005be0(local_18);
  pvVar2 = FUN_180002654(0x28);
  if (pvVar2 == (void *)0x0) {
    DAT_180024a20 = 0;
  }
  else {
    DAT_180024a20 = FUN_180005bc8((longlong)pvVar2);
    if (DAT_180024a20 != 0) {
      uVar1 = FUN_180014c08(L"AllowLpacAppExperience",param_2,(RPC_BINDING_VECTOR *)&DAT_18001b760,
                            param_4,0);
      if ((int)uVar1 < 0) {
        FUN_180006678(unaff_retaddr,0xe5,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                      ,uVar1);
      }
      else {
        DAT_180024220 = 0;
      }
      goto LAB_180007869;
    }
  }
  uVar1 = 0x8007000e;
LAB_180007869:
  FUN_180005c60(local_18);
  return uVar1;
}
```

#### Function `FUN_1800067d0` @ `1800067d0`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_1800067d0(RPC_BINDING_HANDLE param_1,undefined4 *param_2,undefined8 *param_3)

{
  uint uVar1;
  ulonglong uVar2;
  undefined8 uVar3;
  ulonglong uVar4;
  undefined8 unaff_retaddr;
  undefined1 auStack_488 [32];
  undefined8 *local_468;
  undefined8 *local_460;
  undefined8 local_458;
  undefined8 uStack_450;
  undefined8 local_448;
  undefined8 uStack_440;
  undefined8 local_438;
  undefined1 local_428 [1024];
  ulonglong local_28;
  
  local_28 = DAT_1800241c0 ^ (ulonglong)auStack_488;
  local_460 = (undefined8 *)0x0;
  local_468 = (undefined8 *)0x0;
  uVar3 = CONCAT71((int7)((ulonglong)param_2 >> 8),1);
  uVar2 = FUN_180005d9c(param_1,'\x01',&local_468,(longlong *)&local_460);
  uVar4 = uVar2 & 0xffffffff;
  if ((int)uVar2 < 0) {
    uVar1 = 0x188;
  }
  else {
    local_438 = 0;
    *param_3 = 0;
    param_3[1] = 0;
    local_458 = 0;
    uStack_450 = 0;
    local_448 = 0;
    uStack_440 = 0;
    uVar2 = FUN_18000d0c4(param_2,uVar3,(undefined4 *)&local_458,local_428);
    uVar4 = uVar2 & 0xffffffff;
    if ((int)uVar2 < 0) {
      uVar1 = 0x191;
    }
    else {
      uVar1 = (**(code **)*local_460)(local_460,&local_458,param_3);
      uVar4 = (ulonglong)uVar1;
      if (-1 < (int)uVar1) goto LAB_18000689f;
      uVar1 = 0x194;
    }
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar4);
LAB_18000689f:
  FUN_180005984(local_468);
  return uVar4;
}
```

#### Function `FUN_180006fe0` @ `180006fe0`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

ulonglong FUN_180006fe0(RPC_BINDING_HANDLE param_1,undefined8 param_2,undefined8 param_3)

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
    uVar1 = 0x1e0;
  }
  else {
    uVar1 = (**(code **)(*local_18[0] + 0x38))(local_18[0],param_2,0,param_3);
    uVar3 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) goto LAB_180007055;
    uVar1 = 0x1e2;
  }
  FUN_180006678(unaff_retaddr,uVar1,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstorerpcserver.cpp"
                ,(uint)uVar3);
LAB_180007055:
  FUN_180005984(local_res20);
  return uVar3;
}
```

### String @ `18001d790`

`gameConfigStoreManagement`

- Reference count: `1`
- References:
  - `180005e47` in `FUN_180005d9c`

#### Function `FUN_180005d9c` @ `180005d9c`

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
                            ,(uint)uVar4);
            }
            goto LAB_180005f23;
          }
        }
        uVar6 = 0x8007000e;
        goto LAB_180005f23;
      }
      uVar5 = 0xbf;
      goto LAB_180005e0f;
    }
    local_48[0] = '\0';
    iVar2 = CapabilityCheck(local_40,L"gameConfigStoreManagement",local_48);
    if (-1 < iVar2) {
      if (local_48[0] == '\0') {
        FUN_180005cb0(local_40);
        FUN_180005c14(&local_30);
        uVar6 = 0x80070005;
        goto LAB_180005f23;
      }
// ... trimmed ...
```

### String @ `18001d9c0`

`System\GameConfigStore\Parents`

- Reference count: `1`
- References:
  - `180008a60` in `FUN_180008a60`

#### Function `FUN_180008a60` @ `180008a60`

```c
wchar_t * FUN_180008a60(void)

{
  return L"System\\GameConfigStore\\Parents";
}
```

### String @ `18001da00`

`System\GameConfigStore\Children`

- Reference count: `1`
- References:
  - `180008a50` in `FUN_180008a50`

#### Function `FUN_180008a50` @ `180008a50`

```c
wchar_t * FUN_180008a50(void)

{
  return L"System\\GameConfigStore\\Children";
}
```

### String @ `18001da40`

`System\GameConfigStore`

- Reference count: `1`
- References:
  - `180008a70` in `FUN_180008a70`

#### Function `FUN_180008a70` @ `180008a70`

```c
wchar_t * FUN_180008a70(void)

{
  return L"System\\GameConfigStore";
}
```

### String @ `18001def0`

`onecore\base\appmodel\resourcepolicy\gameconfigstore\server\gameconfigstoreserver.cpp`

- Reference count: `91`
- References:
  - `180008436` in `FUN_180008400`
  - `1800084a8` in `FUN_180008400`
  - `18000a0ca` in `FUN_18000a038`
  - `18000a126` in `FUN_18000a038`
  - `18000a185` in `FUN_18000a038`
  - `18000a1de` in `FUN_18000a038`
  - `18000a59c` in `FUN_18000a038`
  - `18000a477` in `FUN_18000a038`
  - `18000a4d4` in `FUN_18000a038`
  - `18000a531` in `FUN_18000a038`
  - `180008cab` in `FUN_180008c40`
  - `18000cced` in `FUN_18000cc94`
  - `18000cd6b` in `FUN_18000cc94`
  - `180008184` in `FUN_1800080d8`
  - `1800081f4` in `FUN_1800080d8`
  - `1800082a2` in `FUN_1800080d8`
  - `1800082e2` in `FUN_1800080d8`
  - `180007d3d` in `FUN_180007cec`
  - `180007dc1` in `FUN_180007cec`
  - `180007ed9` in `FUN_180007cec`
  - `180008dc4` in `FUN_180008ce0`
  - `1800089bb` in `FUN_180008940`
  - `180008601` in `FUN_180008590`
  - `180009a4f` in `FUN_180009988`
  - `18000925e` in `FUN_1800091b0`
  - `18000b6b3` in `FUN_18000b610`
  - `180009ec9` in `FUN_180009e14`
  - `180008e84` in `FUN_180008e38`
  - `18000a671` in `FUN_18000a630`
  - `18000a6a9` in `FUN_18000a630`
  - `18000a6de` in `FUN_18000a630`
  - `18000c67d` in `FUN_18000c630`
  - `18000c6dc` in `FUN_18000c630`
  - `18000c790` in `FUN_18000c630`
  - `18000c7f6` in `FUN_18000c630`
  - `18000c889` in `FUN_18000c630`
  - `18000c91f` in `FUN_18000c630`
  - `18000c9c8` in `FUN_18000c630`
  - `18000cb50` in `FUN_18000c630`
  - `18000cbad` in `FUN_18000c630`
  - `18000be9c` in `FUN_18000be40`
  - `180009b61` in `FUN_180009a98`
  - `18000aec2` in `FUN_18000ae8c`
  - `18000af4f` in `FUN_18000ae8c`
  - `180008af6` in `FUN_180008a80`
  - `180008f60` in `FUN_180008eb0`
  - `180009014` in `FUN_180008eb0`
  - `18000905e` in `FUN_180008eb0`
  - `18000c331` in `FUN_18000c2d0`
  - `18000bf7f` in `FUN_18000bec8`
  - `18000c03b` in `FUN_18000bec8`
  - `18000c077` in `FUN_18000bec8`
  - `18000974e` in `FUN_1800096f8`
  - `180009f89` in `FUN_180009f14`
  - `180007b6f` in `FUN_180007b00`
  - `180007c35` in `FUN_180007b00`
  - `18000b7f9` in `FUN_18000b730`
  - `18000b862` in `FUN_18000b730`
  - `18000b951` in `FUN_18000b730`
  - `18000b9df` in `FUN_18000b730`
  - `18000bad1` in `FUN_18000b730`
  - `18000bb15` in `FUN_18000b730`
  - `18000bbb0` in `FUN_18000b730`
  - `18000bc39` in `FUN_18000b730`
  - `18000bce2` in `FUN_18000b730`
  - `18000bd4a` in `FUN_18000b730`
  - `18000bd9b` in `FUN_18000b730`
  - `18000a7ca` in `FUN_18000a740`
  - `18000a8f9` in `FUN_18000a740`
  - `18000a945` in `FUN_18000a740`
  - `18000ab6b` in `FUN_18000a740`
  - `18000a845` in `FUN_18000a740`
  - `18000c3c8` in `FUN_18000c360`
  - `18000b00e` in `FUN_18000af84`
  - `18000b07d` in `FUN_18000af84`
  - `18000b0df` in `FUN_18000af84`
  - `18000880f` in `FUN_180008780`
  - `180008827` in `FUN_180008780`
  - `18000b142` in `FUN_18000af84`
  - `18000b1d2` in `FUN_18000af84`
  - `1800088cd` in `FUN_180008780`
  - `18000b329` in `FUN_18000af84`
  - `18000b3b1` in `FUN_18000af84`
  - `180009ff4` in `FUN_180009fbc`
  - `18000c499` in `FUN_18000c3f4`
  - `180009452` in `FUN_1800093ec`
  - `1800080a5` in `FUN_180007ff0`
  - `1800094c4` in `FUN_1800093ec`
  - `18000acc2` in `FUN_18000abe0`
  - `18000ad21` in `FUN_18000abe0`
  - `18000ad6f` in `FUN_18000abe0`

#### Function `FUN_180008400` @ `180008400`

```c
uint FUN_180008400(short *param_1,undefined8 *param_2)

{
  ulonglong uVar1;
  undefined1 auVar2 [16];
  int iVar3;
  DATA_BLOB *pDVar4;
  uint uVar5;
  DWORD DVar6;
  BOOL BVar7;
  size_t sVar8;
  wchar_t *_String;
  undefined8 unaff_retaddr;
  DATA_BLOB *local_res18;
  longlong local_res20;
  DATA_BLOB **local_48;
  undefined1 local_40;
  DATA_BLOB local_38;
  
  local_res20 = 0;
  uVar5 = FUN_18000c5d4(param_1,0x7fffffff,&local_res20);
  if ((int)uVar5 < 0) {
    FUN_180006678(unaff_retaddr,0x17a,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar5);
    return uVar5;
  }
  local_38._4_4_ = 0;
  local_res18 = (DATA_BLOB *)0x0;
  iVar3 = (int)local_res20;
  uVar1 = local_res20 + 1;
  auVar2._8_8_ = 0;
  auVar2._0_8_ = uVar1;
  sVar8 = SUB168(ZEXT816(2) * auVar2,0);
  if (SUB168(ZEXT816(2) * auVar2,8) != 0) {
    sVar8 = 0xffffffffffffffff;
  }
  _String = (wchar_t *)thunk_FUN_1800026ac(sVar8);
  if (_String != (wchar_t *)0x0) {
    DVar6 = FUN_180005054(_String,uVar1,param_1);
    if ((int)DVar6 < 0) {
      FUN_180006678(unaff_retaddr,0x184,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,DVar6);
      goto LAB_18000856b;
    }
    _wcslwr(_String);
    local_48 = &local_res18;
    local_40 = 1;
    local_38.cbData = iVar3 * 2;
    local_38.pbData = (BYTE *)_String;
    local_res18 = (DATA_BLOB *)FUN_1800026ac(0x10);
    if (local_res18 != (DATA_BLOB *)0x0) {
      local_res18->pbData = (BYTE *)0x0;
      local_res18->cbData = 0;
      BVar7 = CryptProtectData(&local_38,(LPCWSTR)0x0,(DATA_BLOB *)0x0,(PVOID)0x0,
                               (CRYPTPROTECT_PROMPTSTRUCT *)0x0,4,local_res18);
      pDVar4 = local_res18;
      if (BVar7 == 0) {
        DVar6 = FUN_1800061e0();
      }
      else {
        local_res18 = (DATA_BLOB *)0x0;
        *param_2 = pDVar4;
      }
      FUN_1800079fc(&local_48);
      goto LAB_18000856b;
    }
    FUN_1800079fc(&local_48);
  }
  DVar6 = 0x8007000e;
LAB_18000856b:
  free(_String);
  return DVar6;
}
```

#### Function `FUN_18000a038` @ `18000a038`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

uint FUN_18000a038(longlong *param_1,DWORD param_2,undefined8 *param_3)

{
  longlong lVar1;
  HKEY _Memory;
  char cVar2;
  uint uVar3;
  ulonglong uVar4;
  wint_t *pwVar5;
  undefined8 uVar6;
  HKEY pHVar7;
  LPCWSTR _Memory_00;
  uint uVar8;
  ulonglong uVar9;
  longlong *plVar10;
  LPCWSTR pWVar11;
  HKEY pHVar12;
  LPCWSTR _Memory_01;
  LPCWSTR _Memory_02;
  undefined8 unaff_retaddr;
  undefined1 auStack_168 [48];
  char local_138;
  char local_137;
  HKEY local_130;
  uint local_128;
  HKEY local_120;
  DWORD local_118;
  uint local_114;
  uint local_110;
  uint local_10c;
  undefined8 local_108;
  LPCWSTR pWStack_100;
  undefined4 local_f8;
  longlong local_e8;
  LPCWSTR local_e0;
  LPCWSTR local_d8;
  LPCWSTR local_d0;
  longlong *local_c8;
  longlong *local_c0;
  undefined8 *local_b8;
  wchar_t local_a8 [48];
  ulonglong local_48;
  
  local_48 = DAT_1800241c0 ^ (ulonglong)auStack_168;
  _Memory_00 = (LPCWSTR)0x0;
  local_120 = (HKEY)0x0;
  local_130 = (HKEY)0x0;
  *param_3 = 0;
  param_3[1] = 0;
  local_d0 = (LPCWSTR)0x0;
  local_d8 = (LPCWSTR)0x0;
  local_e0 = (LPCWSTR)0x0;
  local_e8 = 0;
  local_138 = '\0';
  local_110 = 0;
  local_10c = 0;
  local_118 = param_2;
  local_c8 = param_1;
  local_b8 = param_3;
  uVar3 = FUN_180009988(param_2,&local_120);
  _Memory = local_120;
  if ((int)uVar3 < 0) {
    FUN_180006678(unaff_retaddr,0x83e,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar3);
    free((void *)0x0);
    free((void *)0x0);
    free((void *)0x0);
    free((void *)0x0);
    free(local_120);
  }
  else {
    uVar3 = FUN_180007cec((short *)local_120,local_a8);
    if ((int)uVar3 < 0) {
      FUN_180006678(unaff_retaddr,0x840,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
// ... trimmed ...
```

#### Function `FUN_180008c40` @ `180008c40`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_180008c40(longlong param_1,short *param_2,longlong param_3,uint *param_4)

{
  uint uVar1;
  ulonglong uVar2;
  uint uVar3;
  undefined8 unaff_retaddr;
  undefined1 auStack_b8 [32];
  wchar_t local_98 [48];
  ulonglong local_38;
  
  local_38 = DAT_1800241c0 ^ (ulonglong)auStack_b8;
  uVar1 = FUN_180007cec(param_2,local_98);
  if ((int)uVar1 < 0) {
    uVar3 = 0x700;
  }
  else {
    uVar2 = FUN_180008ce0((undefined8 *)(param_1 + 0x10),local_98,param_3,param_4);
    uVar1 = (uint)uVar2;
    if (-1 < (int)uVar1) {
      return uVar2;
    }
    uVar3 = 0x702;
  }
  FUN_180006678(unaff_retaddr,uVar3,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar1);
  return (ulonglong)uVar1;
}
```

#### Function `FUN_18000cc94` @ `18000cc94`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_18000cc94(undefined8 *param_1,longlong param_2,LPCWSTR param_3)

{
  wchar_t wVar1;
  wchar_t wVar2;
  uint uVar3;
  ulonglong uVar4;
  wchar_t *pwVar5;
  BYTE *pBVar6;
  uint uVar7;
  longlong lVar8;
  undefined8 unaff_retaddr;
  undefined1 auStack_e8 [48];
  HKEY local_b8;
  uint local_b0 [2];
  BYTE *local_a8;
  undefined4 local_a0;
  uint local_98 [2];
  short *psStack_90;
  undefined4 local_88;
  wchar_t local_78 [48];
  ulonglong local_18;
  
  local_18 = DAT_1800241c0 ^ (ulonglong)auStack_e8;
  local_b8 = (HKEY)0x0;
  local_88 = 0;
  local_a8 = (BYTE *)0x0;
  local_b0[0] = 0;
  local_a0 = 0;
  local_98[0] = 0;
  local_98[1] = 0;
  psStack_90 = (short *)0x0;
  uVar3 = FUN_180015de4(param_1,param_3,param_3,&local_b8);
  if ((int)uVar3 < 0) {
    FUN_180006678(unaff_retaddr,0x534,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar3);
    pBVar6 = (BYTE *)0x0;
  }
  else {
    uVar4 = FUN_180015968(&local_b8,L"Parent",param_3,local_b0);
    uVar3 = (uint)uVar4;
    if ((int)uVar3 < 0) {
      uVar7 = 0x536;
    }
    else {
      uVar3 = FUN_180007ff0(local_a8,local_b0[0],local_98);
      if ((int)uVar3 < 0) {
        uVar7 = 0x538;
      }
      else {
        uVar3 = FUN_180007cec(psStack_90,local_78);
        if (-1 < (int)uVar3) {
          pwVar5 = local_78;
          lVar8 = param_2 - (longlong)pwVar5;
          do {
            wVar1 = *pwVar5;
            wVar2 = *(wchar_t *)((longlong)pwVar5 + lVar8);
            if (wVar1 != wVar2) break;
            pwVar5 = pwVar5 + 1;
          } while (wVar2 != L'\0');
          pBVar6 = local_a8;
          if (wVar1 == wVar2) {
            uVar3 = 0;
          }
          else {
            uVar3 = 0x8000ffff;
          }
          goto LAB_18000cda4;
        }
        uVar7 = 0x53a;
      }
    }
    FUN_180006678(unaff_retaddr,uVar7,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar3);
    pBVar6 = local_a8;
// ... trimmed ...
```

#### Function `FUN_1800080d8` @ `1800080d8`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_1800080d8(undefined8 *param_1,LPCWSTR param_2,undefined4 *param_3,uint *param_4)

{
  undefined4 *puVar1;
  uint uVar2;
  DWORD DVar3;
  ulonglong uVar4;
  undefined8 uVar5;
  void *pvVar6;
  uint uVar7;
  LPBYTE pBVar8;
  uint *puVar9;
  undefined **ppuVar10;
  undefined8 unaff_retaddr;
  undefined1 auStack_158 [48];
  HKEY local_128;
  int local_120;
  undefined4 local_11c;
  undefined4 local_118 [2];
  uint local_110 [2];
  void *pvStack_108;
  undefined4 local_100;
  uint local_f8 [2];
  BYTE *local_f0;
  undefined4 local_e8;
  undefined4 *local_d8;
  undefined4 *local_d0;
  uint local_c8;
  uint local_c4;
  char local_c0;
  undefined4 local_b8 [12];
  uint local_88 [2];
  void *pvStack_80;
  undefined4 local_78;
  uint local_70 [2];
  undefined8 auStack_68 [5];
  ulonglong local_40;
  
  local_40 = DAT_1800241c0 ^ (ulonglong)auStack_158;
  uVar7 = 0;
  local_c8 = *param_4;
  local_c4 = 0;
  local_c0 = '\0';
  local_d8 = param_3;
  if (param_3 == (undefined4 *)0x0) {
    local_d8 = local_b8;
    local_c8 = 0x28;
  }
  uVar4 = (ulonglong)local_c8;
  local_d0 = param_3;
  memset(local_d8,0,uVar4);
  puVar1 = local_d8;
  if (local_c8 < 0x28) {
    local_c8 = 0;
    if (local_c0 == '\0') {
      return 0x80070057;
    }
  }
  else {
    local_d0 = local_d0 + 10;
    local_c8 = local_c8 - 0x28;
    local_c0 = '\x01';
    local_c4 = 0x28;
  }
  local_128 = (HKEY)0x0;
  uVar2 = FUN_180015de4(param_1,param_2,uVar4,&local_128);
  if ((int)uVar2 < 0) {
    FUN_180006678(unaff_retaddr,0x42c,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar2);
    goto LAB_180008198;
  }
  local_100 = 0;
  local_120 = 0;
  local_110[0] = 0;
  local_110[1] = 0;
  pvStack_108 = (void *)0x0;
// ... trimmed ...
```

#### Function `FUN_180007cec` @ `180007cec`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_180007cec(short *param_1,wchar_t *param_2)

{
  ulonglong uVar1;
  undefined1 auVar2 [16];
  int iVar3;
  uint uVar4;
  BOOL BVar5;
  size_t sVar6;
  wchar_t *_String;
  undefined8 uVar7;
  ulonglong uVar8;
  undefined8 unaff_retaddr;
  undefined1 auStackY_b8 [32];
  HCRYPTHASH local_88;
  DWORD local_80 [2];
  HCRYPTPROV local_78;
  longlong local_70;
  HCRYPTPROV *local_68;
  HCRYPTHASH *local_60;
  undefined1 local_58;
  byte local_50 [24];
  ulonglong local_38;
  
  local_38 = DAT_1800241c0 ^ (ulonglong)auStackY_b8;
  uVar8 = 0;
  local_70 = 0;
  uVar4 = FUN_18000c5d4(param_1,0x7fffffff,&local_70);
  if ((int)uVar4 < 0) {
    FUN_180006678(unaff_retaddr,0x136,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar4);
  }
  else {
    local_50[0x10] = 0;
    local_50[0x11] = 0;
    local_50[0x12] = 0;
    local_50[0x13] = 0;
    local_50[0] = 0;
    local_50[1] = 0;
    local_50[2] = 0;
    local_50[3] = 0;
    local_50[4] = 0;
    local_50[5] = 0;
    local_50[6] = 0;
    local_50[7] = 0;
    local_50[8] = 0;
    local_50[9] = 0;
    local_50[10] = 0;
    local_50[0xb] = 0;
    local_50[0xc] = 0;
    local_50[0xd] = 0;
    local_50[0xe] = 0;
    local_50[0xf] = 0;
    local_80[0] = 0x14;
    iVar3 = (int)local_70;
    uVar1 = local_70 + 1;
    auVar2._8_8_ = 0;
    auVar2._0_8_ = uVar1;
    sVar6 = SUB168(ZEXT416(2) * auVar2,0);
    if (SUB168(ZEXT416(2) * auVar2,8) != 0) {
      sVar6 = 0xffffffffffffffff;
    }
    _String = (wchar_t *)thunk_FUN_1800026ac(sVar6);
    if (_String == (wchar_t *)0x0) {
      uVar4 = 0x8007000e;
    }
    else {
      uVar4 = FUN_180005054(_String,uVar1,param_1);
      if ((int)uVar4 < 0) {
        FUN_180006678(unaff_retaddr,0x142,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                      ,uVar4);
      }
      else {
        _wcslwr(_String);
        local_68 = &local_78;
// ... trimmed ...
```

#### Function `FUN_180008ce0` @ `180008ce0`

```c
ulonglong FUN_180008ce0(undefined8 *param_1,LPCWSTR param_2,longlong param_3,uint *param_4)

{
  ulonglong uVar1;
  uint uVar2;
  undefined8 uVar3;
  ulonglong uVar4;
  uint uVar5;
  short *psVar6;
  undefined8 unaff_retaddr;
  short *local_res10;
  ulonglong local_38 [2];
  
  uVar4 = 0;
  local_res10 = (short *)0x0;
  local_38[0] = 0;
  if ((param_2 == (LPCWSTR)0x0) || (param_4 == (uint *)0x0)) {
    free((void *)0x0);
    uVar4 = 0x80070057;
  }
  else {
    uVar2 = FUN_1800093ec(param_1,param_2,&local_res10,(uint *)0x0);
    psVar6 = local_res10;
    if (uVar2 == 0x80070002) {
      *param_4 = 0;
    }
    else {
      if ((int)uVar2 < 0) {
        uVar5 = 0x329;
      }
      else {
        uVar2 = FUN_18000c5d4(local_res10,0x7fffffff,(longlong *)local_38);
        if ((int)uVar2 < 0) {
          uVar5 = 0x330;
        }
        else {
          do {
            uVar1 = local_38[0];
            uVar5 = (uint)uVar4;
            if (local_38[0] == 0) {
              if ((param_3 != 0) && (*param_4 < uVar5)) {
                uVar2 = 0x800700ea;
              }
              *param_4 = uVar5;
              free(local_res10);
              return (ulonglong)uVar2;
            }
            if ((param_3 != 0) && (uVar5 < *param_4)) {
              uVar3 = FUN_18001609c((longlong)psVar6,local_38[0] & 0xffffffff,
                                    (uint *)(uVar4 * 0x10 + param_3));
              uVar2 = (uint)uVar3;
              if ((int)uVar2 < 0) {
                uVar5 = 0x336;
                goto LAB_180008dbf;
              }
            }
            psVar6 = psVar6 + uVar1 + 1;
            uVar4 = (ulonglong)(uVar5 + 1);
            uVar2 = FUN_18000c5d4(psVar6,0x7fffffff,(longlong *)local_38);
          } while (-1 < (int)uVar2);
          uVar5 = 0x33a;
        }
      }
LAB_180008dbf:
      FUN_180006678(unaff_retaddr,uVar5,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar2);
      uVar4 = (ulonglong)uVar2;
    }
    free(local_res10);
  }
  return uVar4;
}
```

#### Function `FUN_180008940` @ `180008940`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_180008940(longlong param_1,uint *param_2,undefined4 *param_3,uint *param_4)

{
  uint uVar1;
  longlong lVar2;
  ulonglong uVar3;
  undefined4 extraout_var;
  ulonglong uVar4;
  undefined8 unaff_retaddr;
  undefined1 auStack_b8 [32];
  uint local_98 [4];
  WCHAR local_88 [40];
  ulonglong local_38;
  
  local_38 = DAT_1800241c0 ^ (ulonglong)auStack_b8;
  lVar2 = *(longlong *)param_2;
  local_98[0] = 0x24;
  if (lVar2 == 0) {
    lVar2 = *(longlong *)(param_2 + 2);
  }
  if ((lVar2 == 0) || (param_4 == (uint *)0x0)) {
    uVar4 = 0x80070057;
  }
  else {
    uVar3 = FUN_180016300(param_2,(ushort *)local_88,0x24,local_98);
    uVar4 = uVar3 & 0xffffffff;
    if ((int)(uint)uVar3 < 0) {
      FUN_180006678(unaff_retaddr,0x606,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,(uint)uVar3);
    }
    else {
      if (0x49 < (ulonglong)local_98[0] * 2) {
                    /* WARNING: Subroutine does not return */
        FUN_180001fd4();
      }
      local_88[local_98[0]] = L'\0';
      uVar1 = FUN_1800080d8((undefined8 *)(param_1 + 8),local_88,param_3,param_4);
      uVar4 = CONCAT44(extraout_var,uVar1);
      if ((-1 < (int)uVar1) && (param_3 != (undefined4 *)0x0)) {
        param_3[8] = param_3[8] | 1;
      }
    }
  }
  return uVar4;
}
```

#### Function `FUN_180008590` @ `180008590`

```c
uint FUN_180008590(longlong param_1)

{
  LSTATUS LVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  
  uVar2 = 0;
  LVar1 = RegQueryInfoKeyW(*(HKEY *)(param_1 + 0x18),(LPWSTR)0x0,(LPDWORD)0x0,(LPDWORD)0x0,
                           (LPDWORD)0x0,(LPDWORD)0x0,(LPDWORD)0x0,(LPDWORD)0x0,(LPDWORD)0x0,
                           (LPDWORD)0x0,(LPDWORD)0x0,(PFILETIME)0x0);
  if (LVar1 != 0) {
    uVar2 = FUN_18000ae8c(*(LPCWSTR *)(param_1 + 0x20),(PHKEY)(param_1 + 0x18),
                          (PHKEY)(param_1 + 0x10),(PHKEY)(param_1 + 8));
    if ((int)uVar2 < 0) {
      FUN_180006678(unaff_retaddr,0x8d9,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar2);
    }
  }
  return uVar2;
}
```

#### Function `FUN_180009988` @ `180009988`

```c
uint FUN_180009988(DWORD param_1,undefined8 *param_2)

{
  DWORD DVar1;
  short *_Memory;
  size_t sVar2;
  short *psVar3;
  undefined8 unaff_retaddr;
  uint local_res18 [2];
  
  local_res18[0] = 0x104;
  _Memory = (short *)thunk_FUN_1800026ac(0x208);
  if (_Memory == (short *)0x0) {
LAB_180009a6d:
    DVar1 = 0x8007000e;
  }
  else {
    free((void *)0x0);
    DVar1 = FUN_180009794(param_1,_Memory,local_res18,(short *)0x0,(uint *)0x0);
    if (DVar1 == 0x8007007a) {
      sVar2 = SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),0);
      if (SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),8) != 0) {
        sVar2 = 0xffffffffffffffff;
      }
      psVar3 = (short *)thunk_FUN_1800026ac(sVar2);
      if (_Memory != psVar3) {
        free(_Memory);
        _Memory = psVar3;
        if (psVar3 == (short *)0x0) goto LAB_180009a6d;
      }
      DVar1 = FUN_180009794(param_1,_Memory,local_res18,(short *)0x0,(uint *)0x0);
    }
    if ((int)DVar1 < 0) {
      FUN_180006678(unaff_retaddr,0x557,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,DVar1);
      goto LAB_180009a74;
    }
    *param_2 = _Memory;
  }
  _Memory = (short *)0x0;
LAB_180009a74:
  free(_Memory);
  return DVar1;
}
```

#### Function `FUN_1800091b0` @ `1800091b0`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_1800091b0(longlong param_1,uint *param_2,uint param_3,uint *param_4)

{
  uint uVar1;
  longlong lVar2;
  ulonglong uVar3;
  undefined8 uVar4;
  uint uVar5;
  undefined8 unaff_retaddr;
  undefined1 auStack_d8 [32];
  byte local_b8 [8];
  HKEY local_b0;
  uint local_a8 [4];
  WCHAR local_98 [40];
  ulonglong local_48;
  
  local_48 = DAT_1800241c0 ^ (ulonglong)auStack_d8;
  lVar2 = *(longlong *)param_2;
  *param_4 = 0;
  uVar4 = 0x24;
  local_b0 = (HKEY)0x0;
  local_a8[0] = 0x24;
  local_b8[0] = 0;
  if (lVar2 == 0) {
    lVar2 = *(longlong *)(param_2 + 2);
  }
  if ((lVar2 == 0) || (0x1d < param_3)) {
    FUN_180007968((HKEY)0x0);
    return 0x80070057;
  }
  if (((&DAT_18001aafe)[(longlong)(int)param_3 * 0x18] == '\0') ||
     ((&DAT_18001aaff)[(longlong)(int)param_3 * 0x18] == '\0')) {
LAB_1800092da:
    FUN_180007968(local_b0);
    uVar1 = 0;
  }
  else {
    uVar3 = FUN_180016300(param_2,(ushort *)local_98,0x24,local_a8);
    uVar1 = (uint)uVar3;
    if ((int)uVar1 < 0) {
      uVar5 = 0x7fd;
    }
    else {
      if (0x49 < (ulonglong)local_a8[0] * 2) {
                    /* WARNING: Subroutine does not return */
        FUN_180001fd4();
      }
      local_98[local_a8[0]] = L'\0';
      uVar1 = FUN_180015de4((undefined8 *)(param_1 + 8),local_98,uVar4,&local_b0);
      if ((int)uVar1 < 0) {
        uVar5 = 0x800;
      }
      else {
        uVar4 = FUN_180008630(&local_b0,param_3,local_b8);
        uVar1 = (uint)uVar4;
        if (-1 < (int)uVar1) {
          *param_4 = (uint)local_b8[0];
          goto LAB_1800092da;
        }
        uVar5 = 0x802;
      }
    }
    FUN_180006678(unaff_retaddr,uVar5,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar1);
    FUN_180007968(local_b0);
  }
  return uVar1;
}
```

#### Function `FUN_18000b610` @ `18000b610`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_18000b610(longlong param_1,uint *param_2)

{
  LPCWSTR pWVar1;
  uint uVar2;
  longlong lVar3;
  ulonglong uVar4;
  uint uVar5;
  undefined8 unaff_retaddr;
  undefined1 auStack_98 [32];
  uint local_78 [4];
  WCHAR local_68 [40];
  ulonglong local_18;
  
  local_18 = DAT_1800241c0 ^ (ulonglong)auStack_98;
  lVar3 = *(longlong *)param_2;
  local_78[0] = 0x24;
  if (lVar3 == 0) {
    lVar3 = *(longlong *)(param_2 + 2);
  }
  if (lVar3 == 0) {
    uVar2 = 0x80070057;
  }
  else {
    uVar4 = FUN_180016300(param_2,(ushort *)local_68,0x24,local_78);
    uVar2 = (uint)uVar4;
    if ((int)uVar2 < 0) {
      uVar5 = 0x686;
    }
    else {
      if (0x49 < (ulonglong)local_78[0] * 2) {
                    /* WARNING: Subroutine does not return */
        FUN_180001fd4();
      }
      pWVar1 = *(LPCWSTR *)(param_1 + 0x20);
      local_68[local_78[0]] = L'\0';
      uVar2 = FUN_18000af84(pWVar1,(undefined8 *)(param_1 + 0x10),(undefined8 *)(param_1 + 8),
                            local_68);
      if (-1 < (int)uVar2) {
        return uVar2;
      }
      uVar5 = 0x68a;
    }
    FUN_180006678(unaff_retaddr,uVar5,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar2);
  }
  return uVar2;
}
```

#### Function `FUN_180009e14` @ `180009e14`

```c
uint FUN_180009e14(DWORD param_1,undefined8 *param_2)

{
  uint extraout_EAX;
  uint extraout_EAX_00;
  uint uVar1;
  short *psVar2;
  size_t sVar3;
  short *psVar4;
  undefined *puVar5;
  DWORD DVar6;
  undefined8 unaff_retaddr;
  uint local_res18 [2];
  
  local_res18[0] = 0x104;
  puVar5 = &DAT_18001ccc8;
  psVar2 = (short *)thunk_FUN_1800026ac(0x208);
  if (psVar2 == (short *)0x0) {
LAB_180009ee7:
    uVar1 = 0x8007000e;
  }
  else {
    free((void *)0x0);
    DVar6 = FUN_180009bac(param_1,puVar5,psVar2,local_res18);
    psVar2 = (short *)CONCAT44((int)((ulonglong)psVar2 >> 0x20),DVar6);
    uVar1 = extraout_EAX;
    if (extraout_EAX == 0x8007007a) {
      sVar3 = SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),0);
      puVar5 = &DAT_18001ccc8;
      if (SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),8) != 0) {
        sVar3 = 0xffffffffffffffff;
      }
      psVar4 = (short *)thunk_FUN_1800026ac(sVar3);
      if (psVar2 != psVar4) {
        free(psVar2);
        psVar2 = psVar4;
        if (psVar4 == (short *)0x0) goto LAB_180009ee7;
      }
      DVar6 = FUN_180009bac(param_1,puVar5,psVar2,local_res18);
      psVar2 = (short *)CONCAT44((int)((ulonglong)psVar2 >> 0x20),DVar6);
      uVar1 = extraout_EAX_00;
    }
    if ((int)uVar1 < 0) {
      FUN_180006678(unaff_retaddr,0x5a3,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar1);
      goto LAB_180009eee;
    }
    *param_2 = psVar2;
  }
  psVar2 = (short *)0x0;
LAB_180009eee:
  free(psVar2);
  return uVar1;
}
```

#### Function `FUN_180008e38` @ `180008e38`

```c
uint FUN_180008e38(longlong param_1,wchar_t *param_2)

{
  uint uVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  
  if (*(int *)(param_1 + 0x14) == 1) {
    uVar1 = FUN_180007cec(*(short **)(param_1 + 0x18),param_2);
    if (-1 < (int)uVar1) {
      return uVar1;
    }
    uVar2 = 0x21b;
  }
  else {
    if (*(int *)(param_1 + 0x14) != 2) {
      return 0x80070057;
    }
    uVar1 = FUN_180007cec(*(short **)(param_1 + 0x18),param_2);
    if (-1 < (int)uVar1) {
      return uVar1;
    }
    uVar2 = 0x220;
  }
  FUN_180006678(unaff_retaddr,uVar2,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar1);
  return uVar1;
}
```

#### Function `FUN_18000a630` @ `18000a630`

```c
ulonglong FUN_18000a630(longlong param_1,short *param_2)

{
  uint uVar1;
  ulonglong uVar2;
  ulonglong uVar3;
  undefined4 extraout_var;
  undefined8 unaff_retaddr;
  longlong local_res10;
  
  if (param_2 == (short *)0x0) {
    uVar2 = 0x80070057;
  }
  else {
    uVar3 = FUN_18000ce90(param_2,(undefined8 *)(param_1 + 0x20));
    uVar2 = uVar3 & 0xffffffff;
    if ((int)(uint)uVar3 < 0) {
      FUN_180006678(unaff_retaddr,0x81e,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,(uint)uVar3);
    }
    else {
      uVar1 = FUN_18000ae8c(*(LPCWSTR *)(param_1 + 0x20),(PHKEY)(param_1 + 0x18),
                            (PHKEY)(param_1 + 0x10),(PHKEY)(param_1 + 8));
      if ((int)uVar1 < 0) {
        FUN_180006678(unaff_retaddr,0x820,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                      ,uVar1);
        uVar2 = (ulonglong)uVar1;
      }
      else {
        uVar1 = FUN_18000c5d4(param_2,0x7fffffff,&local_res10);
        uVar2 = CONCAT44(extraout_var,uVar1);
        if ((int)uVar1 < 0) {
          FUN_180006678(unaff_retaddr,0x822,
                        "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                        ,uVar1);
          uVar2 = (ulonglong)uVar1;
        }
      }
    }
  }
  return uVar2;
}
```

#### Function `FUN_18000c630` @ `18000c630`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

uint FUN_18000c630(longlong param_1,longlong param_2)

{
  undefined8 uVar1;
  code *pcVar2;
  longlong *plVar3;
  uint uVar4;
  int iVar5;
  undefined8 *puVar6;
  uint uVar7;
  ulonglong uVar8;
  ulonglong uVar9;
  undefined8 unaff_retaddr;
  undefined1 auStack_e8 [48];
  longlong *local_b8;
  longlong *local_b0;
  longlong *local_a8;
  longlong *local_a0;
  longlong *local_98;
  longlong *local_90;
  longlong *local_88;
  longlong local_80;
  uint local_78 [2];
  longlong *local_70;
  undefined8 local_68;
  undefined8 local_60;
  undefined1 local_58 [24];
  ulonglong local_40;
  
  local_40 = DAT_1800241c0 ^ (ulonglong)auStack_e8;
  uVar4 = RoInitialize(1);
  uVar7 = 0;
  if ((int)uVar4 < 0) {
    FUN_180006678(unaff_retaddr,0x1d6,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar4);
  }
  else {
    local_b0 = (longlong *)0x0;
    puVar6 = (undefined8 *)
             FUN_180007910((longlong)&local_60,L"Windows.Internal.StateRepository.Application");
    uVar1 = *puVar6;
    FUN_18000a714((longlong *)&local_b0);
    uVar4 = RoGetActivationFactory(uVar1,&DAT_18001d828,&local_b0);
    plVar3 = local_b0;
    if ((int)uVar4 < 0) {
      FUN_180006678(unaff_retaddr,0x1e1,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar4);
    }
    else {
      local_b8 = (longlong *)0x0;
      pcVar2 = *(code **)(*local_b0 + 0x80);
      FUN_18000a714((longlong *)&local_b8);
      uVar9 = 0xffffffffffffffff;
      uVar8 = 0xffffffffffffffff;
      do {
        uVar8 = uVar8 + 1;
      } while (*(short *)(param_2 + uVar8 * 2) != 0);
      if (0xffffffff < uVar8) {
        uVar8 = 0xffffffff;
        RaiseException(0xc000000d,1,0,(ULONG_PTR *)0x0);
      }
      WindowsCreateStringReference(param_2,uVar8 & 0xffffffff,local_58,&local_60);
      uVar4 = (*pcVar2)(plVar3,local_60,&local_b8);
      if ((int)uVar4 < 0) {
        FUN_180006678(unaff_retaddr,0x1e4,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                      ,uVar4);
      }
      else {
        local_a0 = (longlong *)0x0;
        puVar6 = (undefined8 *)
                 FUN_180007910((longlong)&local_60,L"Windows.Internal.StateRepository.PackageUser");
        uVar1 = *puVar6;
        FUN_18000a714((longlong *)&local_a0);
// ... trimmed ...
```

#### Function `FUN_18000be40` @ `18000be40`

```c
ulonglong FUN_18000be40(longlong param_1,longlong *param_2,uint param_3,int *param_4,DWORD param_5)

{
  longlong lVar1;
  ulonglong uVar2;
  undefined8 unaff_retaddr;
  longlong local_18;
  longlong lStack_10;
  
  lVar1 = *param_2;
  if (lVar1 == 0) {
    lVar1 = param_2[1];
  }
  if ((((lVar1 == 0) || (0x1d < param_3)) || (param_4 == (int *)0x0)) || (param_5 == 0)) {
    uVar2 = 0x80070057;
  }
  else {
    local_18 = *param_2;
    lStack_10 = param_2[1];
    uVar2 = FUN_18000bec8(param_1,(uint *)&local_18,param_3,param_4,param_5,'\0');
    if ((int)(uint)uVar2 < 0) {
      FUN_180006678(unaff_retaddr,0x772,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,(uint)uVar2);
      uVar2 = uVar2 & 0xffffffff;
    }
  }
  return uVar2;
}
```

#### Function `FUN_180009a98` @ `180009a98`

```c
uint FUN_180009a98(DWORD param_1,undefined8 *param_2)

{
  DWORD DVar1;
  short *_Memory;
  size_t sVar2;
  short *psVar3;
  undefined8 unaff_retaddr;
  uint local_res18 [2];
  
  local_res18[0] = 0x104;
  _Memory = (short *)thunk_FUN_1800026ac(0x208);
  if (_Memory == (short *)0x0) {
LAB_180009b7f:
    DVar1 = 0x8007000e;
  }
  else {
    free((void *)0x0);
    DVar1 = FUN_180009794(param_1,(short *)0x0,(uint *)0x0,_Memory,local_res18);
    if (DVar1 == 0x8007007a) {
      sVar2 = SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),0);
      if (SUB168(ZEXT816(2) * ZEXT416(local_res18[0]),8) != 0) {
        sVar2 = 0xffffffffffffffff;
      }
      psVar3 = (short *)thunk_FUN_1800026ac(sVar2);
      if (_Memory != psVar3) {
        free(_Memory);
        _Memory = psVar3;
        if (psVar3 == (short *)0x0) goto LAB_180009b7f;
      }
      DVar1 = FUN_180009794(param_1,(short *)0x0,(uint *)0x0,_Memory,local_res18);
    }
    if ((int)DVar1 < 0) {
      FUN_180006678(unaff_retaddr,0x586,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,DVar1);
      goto LAB_180009b86;
    }
    *param_2 = _Memory;
  }
  _Memory = (short *)0x0;
LAB_180009b86:
  free(_Memory);
  return DVar1;
}
```

#### Function `FUN_18000ae8c` @ `18000ae8c`

```c
uint FUN_18000ae8c(LPCWSTR param_1,PHKEY param_2,PHKEY param_3,PHKEY param_4)

{
  HKEY pHVar1;
  uint uVar2;
  wchar_t *pwVar3;
  uint uVar4;
  undefined8 unaff_retaddr;
  HKEY local_18 [2];
  
  local_18[0] = (HKEY)0x0;
  uVar2 = FUN_180009fbc(param_1,local_18);
  if ((int)uVar2 < 0) {
    FUN_180006678(unaff_retaddr,0x5af,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar2);
    FUN_180007968(local_18[0]);
    return uVar2;
  }
  pwVar3 = FUN_180008a70();
  pHVar1 = local_18[0];
  uVar2 = FUN_180015754(param_2,local_18[0],pwVar3);
  if ((int)uVar2 < 0) {
    uVar4 = 0x5b2;
  }
  else {
    pwVar3 = FUN_180008a60();
    uVar2 = FUN_180015754(param_3,pHVar1,pwVar3);
    if ((int)uVar2 < 0) {
      uVar4 = 0x5b3;
    }
    else {
      pwVar3 = FUN_180008a50();
      uVar2 = FUN_180015754(param_4,pHVar1,pwVar3);
      if (-1 < (int)uVar2) goto LAB_18000af5e;
      uVar4 = 0x5b4;
    }
  }
  FUN_180006678(unaff_retaddr,uVar4,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar2);
LAB_18000af5e:
  FUN_180007968(pHVar1);
  return uVar2;
}
```

#### Function `FUN_180008a80` @ `180008a80`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

uint FUN_180008a80(longlong *param_1,short *param_2,uint *param_3)

{
  uint uVar1;
  uint uVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  undefined1 auStack_128 [32];
  uint local_108;
  uint local_104 [3];
  uint local_f8;
  uint uStack_f4;
  uint uStack_f0;
  uint uStack_ec;
  WCHAR local_e8 [40];
  wchar_t local_98 [48];
  ulonglong local_38;
  
  local_38 = DAT_1800241c0 ^ (ulonglong)auStack_128;
  local_108 = 1;
  local_104[0] = 0x24;
  if ((param_2 == (short *)0x0) || (param_3 == (uint *)0x0)) {
    return 0x80070057;
  }
  param_3[0] = 0;
  param_3[1] = 0;
  param_3[2] = 0;
  param_3[3] = 0;
  uVar1 = FUN_180007cec(param_2,local_98);
  if ((int)uVar1 < 0) {
    uVar2 = 0x69d;
  }
  else {
    uVar3 = FUN_180008ce0(param_1 + 2,local_98,(longlong)param_3,&local_108);
    uVar1 = (uint)uVar3;
    if (-1 < (int)uVar1) {
      if (local_108 == 1) {
        uVar3 = FUN_180016300(param_3,(ushort *)local_e8,0x24,local_104);
        uVar1 = (uint)uVar3;
        if ((int)uVar1 < 0) {
          uVar2 = 0x6a8;
          goto LAB_180008af2;
        }
        if (0x49 < (ulonglong)local_104[0] * 2) {
                    /* WARNING: Subroutine does not return */
          FUN_180001fd4();
        }
        local_e8[local_104[0]] = L'\0';
        uVar2 = FUN_18000cc94(param_1 + 1,(longlong)local_98,local_e8);
        if (-1 < (int)uVar2) {
          return uVar1;
        }
        local_f8 = *param_3;
        uStack_f4 = param_3[1];
        uStack_f0 = param_3[2];
        uStack_ec = param_3[3];
        (**(code **)(*param_1 + 8))(param_1,&local_f8);
      }
      return 0x80070002;
    }
    uVar2 = 0x6a3;
  }
LAB_180008af2:
  FUN_180006678(unaff_retaddr,uVar2,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar1);
  return uVar1;
}
```

#### Function `FUN_180008eb0` @ `180008eb0`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_180008eb0(longlong param_1,uint *param_2,int param_3,uint *param_4,LPDWORD param_5)

{
  uint uVar1;
  DWORD DVar2;
  longlong lVar3;
  ulonglong uVar4;
  BYTE *_Memory;
  uint uVar5;
  undefined8 uVar6;
  ulonglong uVar7;
  undefined8 unaff_retaddr;
  undefined1 auStackY_118 [32];
  HKEY local_e8;
  uint local_e0;
  DWORD local_dc;
  uint local_d8 [2];
  void *pvStack_d0;
  undefined4 local_c8;
  uint local_c0 [2];
  BYTE *local_b8;
  undefined4 local_b0;
  WCHAR local_a8 [40];
  ulonglong local_58;
  
  local_58 = DAT_1800241c0 ^ (ulonglong)auStackY_118;
  lVar3 = *(longlong *)param_2;
  uVar7 = (ulonglong)param_3;
  uVar6 = 0x24;
  local_e8 = (HKEY)0x0;
  local_e0 = 0x24;
  local_dc = 0;
  if (lVar3 == 0) {
    lVar3 = *(longlong *)(param_2 + 2);
  }
  if ((((lVar3 == 0) || (param_3 < 0)) || (0x1d < uVar7)) || (param_5 == (LPDWORD)0x0)) {
    FUN_180007968((HKEY)0x0);
    return 0x80070057;
  }
  uVar4 = FUN_180016300(param_2,(ushort *)local_a8,0x24,&local_e0);
  uVar1 = (uint)uVar4;
  uVar4 = uVar4 & 0xffffffff;
  if ((int)uVar1 < 0) {
    uVar5 = 0x720;
  }
  else {
    if (0x49 < (ulonglong)local_e0 * 2) {
                    /* WARNING: Subroutine does not return */
      FUN_180001fd4();
    }
    local_a8[local_e0] = L'\0';
    uVar1 = FUN_180015de4((undefined8 *)(param_1 + 8),local_a8,uVar6,&local_e8);
    uVar4 = (ulonglong)uVar1;
    if (-1 < (int)uVar1) {
      if ((&DAT_18001aafd)[uVar7 * 0x18] == '\0') {
        uVar5 = *param_5;
        uVar1 = FUN_180015c48(&local_e8,(LPCWSTR)(&PTR_u_Revision_18001aaf0)[uVar7 * 3],
                              (LPBYTE)param_4,param_5,&local_dc);
        uVar4 = (ulonglong)uVar1;
        if ((int)uVar1 < 0) {
          uVar5 = 0x746;
          goto LAB_180008f5c;
        }
        if (local_dc == *(DWORD *)(&DAT_18001aaf8 + uVar7 * 0x18)) {
          if ((param_4 == (uint *)0x0) || ((&DAT_18001aafe)[uVar7 * 0x18] == '\0'))
          goto LAB_180008f6c;
          if ((3 < uVar5) && (local_dc == 4)) {
            if (param_3 == 9) {
              *param_4 = 1;
            }
            else {
              *param_4 = (uint)((*param_4 >> (*(uint *)(&DAT_18001ab00 + uVar7 * 0x18) & 0x1f) & 1)
                               != 0);
            }
            goto LAB_180008f6c;
          }
        }
// ... trimmed ...
```

#### Function `FUN_18000c2d0` @ `18000c2d0`

```c
ulonglong FUN_18000c2d0(longlong param_1,longlong *param_2,uint param_3,int *param_4,DWORD param_5)

{
  longlong lVar1;
  ulonglong uVar2;
  undefined8 unaff_retaddr;
  longlong local_18;
  longlong lStack_10;
  
  lVar1 = *param_2;
  if (lVar1 == 0) {
    lVar1 = param_2[1];
  }
  if ((((lVar1 == 0) || ((int)param_3 < 0)) || (0x1d < param_3)) ||
     ((param_4 == (int *)0x0 || (param_5 == 0)))) {
    uVar2 = 0x80070057;
  }
  else {
    local_18 = *param_2;
    lStack_10 = param_2[1];
    uVar2 = FUN_18000bec8(param_1,(uint *)&local_18,param_3,param_4,param_5,'\x01');
    if ((int)(uint)uVar2 < 0) {
      FUN_180006678(unaff_retaddr,0x7dd,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,(uint)uVar2);
      uVar2 = uVar2 & 0xffffffff;
    }
  }
  return uVar2;
}
```

#### Function `FUN_18000bec8` @ `18000bec8`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_18000bec8(longlong param_1,uint *param_2,uint param_3,int *param_4,DWORD param_5,
                       char param_6)

{
  char cVar1;
  bool bVar2;
  DWORD DVar3;
  LSTATUS LVar4;
  uint uVar5;
  longlong lVar6;
  ulonglong uVar7;
  undefined7 extraout_var;
  uint uVar8;
  ulonglong uVar9;
  undefined8 unaff_retaddr;
  undefined1 auStackY_148 [32];
  char local_e8 [8];
  HKEY local_e0;
  uint local_d8;
  DWORD local_d4;
  DWORD local_d0 [2];
  char *local_c8;
  HANDLE *local_c0;
  undefined1 local_b8;
  HANDLE local_b0;
  DWORD local_a8;
  uint local_a4 [3];
  WCHAR local_98 [40];
  ulonglong local_48;
  
  local_48 = DAT_1800241c0 ^ (ulonglong)auStackY_148;
  lVar6 = *(longlong *)param_2;
  uVar9 = (ulonglong)(int)param_3;
  local_d4 = 0;
  local_d8 = 0x24;
  local_e8[0] = '\0';
  local_e8[1] = 0;
  if (lVar6 == 0) {
    lVar6 = *(longlong *)(param_2 + 2);
  }
  if (((lVar6 == 0) || ((int)param_3 < 0)) || (0x1d < uVar9)) {
    return 0x80070057;
  }
  if ((&DAT_18001aafc)[uVar9 * 0x18] == '\0') {
    return 0x80070005;
  }
  uVar7 = FUN_180016300(param_2,(ushort *)local_98,0x24,&local_d8);
  if ((int)(uint)uVar7 < 0) {
    FUN_180006678(unaff_retaddr,0x8fb,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,(uint)uVar7);
    return uVar7 & 0xffffffff;
  }
  if (0x49 < (ulonglong)local_d8 * 2) {
                    /* WARNING: Subroutine does not return */
    FUN_180001fd4();
  }
  local_98[local_d8] = L'\0';
  local_b0 = (HANDLE)FUN_18000ce30();
  if (local_b0 == (HANDLE)0xffffffffffffffff) {
    DVar3 = FUN_1800061e0();
    uVar7 = (ulonglong)DVar3;
    goto LAB_18000c280;
  }
  local_c8 = local_e8 + 1;
  local_c0 = &local_b0;
  local_b8 = 1;
  local_e0 = (HKEY)0x0;
  local_a8 = 0;
  LVar4 = RegCreateKeyTransactedW
                    (*(HKEY *)(param_1 + 8),local_98,0,(LPWSTR)0x0,0,0xf003f,
                     (LPSECURITY_ATTRIBUTES)0x0,&local_e0,&local_a8,local_b0,(PVOID)0x0);
  if (LVar4 == 0) {
    uVar7 = FUN_180008630(&local_e0,param_3,local_e8);
    cVar1 = local_e8[0];
    uVar5 = (uint)uVar7;
    uVar7 = uVar7 & 0xffffffff;
// ... trimmed ...
```

#### Function `FUN_1800096f8` @ `1800096f8`

```c
uint FUN_1800096f8(DWORD param_1,undefined8 *param_2)

{
  uint extraout_EAX;
  short *_Memory;
  uint uVar1;
  undefined8 unaff_retaddr;
  uint local_res18 [4];
  
  local_res18[0] = 0x1000;
  _Memory = (short *)thunk_FUN_1800026ac(0x2000);
  if (_Memory == (short *)0x0) {
    uVar1 = 0x8007000e;
  }
  else {
    free((void *)0x0);
    FUN_180009524(param_1,_Memory,local_res18);
    uVar1 = extraout_EAX;
    if ((int)extraout_EAX < 0) {
      FUN_180006678(unaff_retaddr,0x569,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,extraout_EAX);
      goto LAB_180009773;
    }
    *param_2 = _Memory;
  }
  _Memory = (short *)0x0;
LAB_180009773:
  free(_Memory);
  return uVar1;
}
```

#### Function `FUN_180009f14` @ `180009f14`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */

uint FUN_180009f14(longlong *param_1,uint param_2,undefined8 *param_3,ulonglong param_4)

{
  uint uVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  undefined8 local_res18 [2];
  undefined8 *local_18;
  undefined1 local_10;
  
  local_res18[0] = 0;
  local_18 = local_res18;
  local_10 = 1;
  *param_3 = 0;
  param_3[1] = 0;
  uVar1 = FUN_180016afc(param_2,local_res18,param_3,param_4);
  if ((int)uVar1 < 0) {
    uVar2 = 0x8cb;
  }
  else {
    uVar1 = (**(code **)(*param_1 + 0x28))(param_1,local_res18[0],param_3);
    if (-1 < (int)uVar1) goto LAB_180009f98;
    uVar2 = 0x8ce;
  }
  FUN_180006678(unaff_retaddr,uVar2,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar1);
LAB_180009f98:
  FUN_180007a4c(&local_18);
  return uVar1;
}
```

#### Function `FUN_180007b00` @ `180007b00`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_180007b00(longlong param_1,uint *param_2,uint *param_3)

{
  RPC_STATUS RVar1;
  uint uVar2;
  undefined8 uVar3;
  ulonglong uVar4;
  uint uVar5;
  undefined8 unaff_retaddr;
  undefined1 auStackY_e8 [32];
  char local_b8;
  byte local_b7 [3];
  uint local_b4;
  UUID local_b0;
  WCHAR local_98 [40];
  ulonglong local_48;
  
  local_48 = DAT_1800241c0 ^ (ulonglong)auStackY_e8;
  local_b4 = 0x24;
  local_b0.Data1 = 0;
  local_b0.Data2 = 0;
  local_b0.Data3 = 0;
  local_b0.Data4[0] = '\0';
  local_b0.Data4[1] = '\0';
  local_b0.Data4[2] = '\0';
  local_b0.Data4[3] = '\0';
  local_b0.Data4[4] = '\0';
  local_b0.Data4[5] = '\0';
  local_b0.Data4[6] = '\0';
  local_b0.Data4[7] = '\0';
  if (param_3 != (uint *)0x0) {
    param_3[0] = 0;
    param_3[1] = 0;
    param_3[2] = 0;
    param_3[3] = 0;
  }
  uVar3 = FUN_18000cf2c((longlong)param_2);
  uVar2 = (uint)uVar3;
  if ((int)uVar2 < 0) {
    uVar5 = 0x65c;
  }
  else {
    RVar1 = UuidCreate(&local_b0);
    if ((RVar1 != 0) && (RVar1 != 0x720)) {
      return 0x8000ffff;
    }
    uVar4 = FUN_180016300(&local_b0.Data1,(ushort *)local_98,0x24,&local_b4);
    uVar2 = (uint)uVar4;
    if ((int)uVar2 < 0) {
      uVar5 = 0x664;
    }
    else {
      if (0x49 < (ulonglong)local_b4 * 2) {
                    /* WARNING: Subroutine does not return */
        FUN_180001fd4();
      }
      local_98[local_b4] = L'\0';
      local_b8 = '\0';
      local_b7[0] = 0;
      uVar4 = FUN_18000a740((undefined8 *)(param_1 + 0x10),(undefined8 *)(param_1 + 8),
                            (longlong)param_2,&local_b8,param_3,local_b7);
      uVar2 = (uint)uVar4;
      if ((int)uVar2 < 0) {
        FUN_180006678(unaff_retaddr,0x302,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                      ,uVar2);
      }
      else if (local_b8 == '\0') {
        if (local_b7[0] != 0) {
          if (param_2[5] == 2) {
            if (param_2[4] != 0) {
              return 0x80070057;
            }
            uVar2 = FUN_18000c630(*(longlong *)(param_1 + 0x20),*(longlong *)(param_2 + 6));
            if ((int)uVar2 < 0) {
              uVar5 = 0x670;
              goto LAB_180007b6b;
// ... trimmed ...
```

#### Function `FUN_18000b730` @ `18000b730`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

DWORD FUN_18000b730(undefined8 *param_1,undefined8 *param_2,LPCWSTR param_3,uint *param_4,
                   char param_5)

{
  undefined1 auVar1 [16];
  bool bVar2;
  DWORD DVar3;
  LSTATUS LVar4;
  uint uVar5;
  size_t sVar6;
  short *psVar7;
  undefined7 extraout_var;
  uint uVar8;
  LPCWSTR lpSubKey;
  undefined *puVar9;
  ulonglong uVar10;
  ulonglong uVar11;
  uint uVar12;
  longlong lVar13;
  undefined8 unaff_retaddr;
  undefined1 auStackY_198 [32];
  HKEY local_138;
  undefined1 local_130 [8];
  DWORD local_128 [2];
  HKEY local_120;
  uint local_118 [2];
  DWORD **local_110;
  undefined1 local_108;
  undefined1 *local_100;
  HANDLE *local_f8;
  undefined1 local_f0;
  uint local_e8;
  uint local_e4;
  DWORD *local_e0;
  HANDLE local_d8;
  DWORD local_d0 [2];
  longlong local_c8 [2];
  wchar_t local_b8 [48];
  ulonglong local_58;
  
  local_58 = DAT_1800241c0 ^ (ulonglong)auStackY_198;
  uVar11 = 0;
  local_130[0] = 0;
  local_d8 = (HANDLE)FUN_18000ce30();
  if (local_d8 == (HANDLE)0xffffffffffffffff) {
    DVar3 = FUN_1800061e0();
    goto LAB_18000be0a;
  }
  local_100 = local_130;
  local_f8 = &local_d8;
  local_f0 = 1;
  local_138 = (HKEY)0x0;
  local_d0[0] = 0;
  LVar4 = RegCreateKeyTransactedW
                    ((HKEY)*param_2,param_3,0,(LPWSTR)0x0,0,0xf003f,(LPSECURITY_ATTRIBUTES)0x0,
                     &local_138,local_d0,local_d8,(PVOID)0x0);
  if (LVar4 != 0) {
    DVar3 = FUN_18000b70c(unaff_retaddr,0x3ae,
                          "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                         );
LAB_18000b80f:
    FUN_180007968(local_138);
    FUN_180007a20(&local_100);
    goto LAB_18000be0a;
  }
  local_128[0] = param_4[5];
  DVar3 = FUN_180015ee8(&local_138,L"Type",(BYTE *)local_128,4,4);
  if ((int)DVar3 < 0) {
    uVar12 = 0x3b6;
LAB_18000b85b:
    FUN_180006678(unaff_retaddr,uVar12,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,DVar3);
  }
  else {
    local_128[0] = *param_4;
    DVar3 = FUN_180015ee8(&local_138,L"Revision",(BYTE *)local_128,4,4);
// ... trimmed ...
```

#### Function `FUN_18000a740` @ `18000a740`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_18000a740(undefined8 *param_1,undefined8 *param_2,longlong param_3,char *param_4,
                       uint *param_5,byte *param_6)

{
  short sVar1;
  short sVar2;
  int iVar3;
  uint uVar4;
  ulonglong uVar5;
  wint_t *pwVar6;
  ulonglong uVar7;
  short *psVar8;
  LPCWSTR _Memory;
  undefined **ppuVar9;
  longlong lVar10;
  HKEY *ppHVar11;
  LPCWSTR pWVar12;
  byte bVar13;
  uint uVar14;
  uint *puVar15;
  int iVar16;
  bool bVar17;
  bool bVar18;
  undefined8 unaff_retaddr;
  undefined1 auStackY_168 [32];
  byte local_138;
  char local_137;
  HKEY local_130 [2];
  HKEY local_120;
  int local_118;
  uint local_114;
  LPCWSTR local_110;
  LPCWSTR local_108;
  uint local_100 [2];
  wint_t *pwStack_f8;
  undefined4 local_f0;
  undefined8 *local_e8;
  uint *local_e0;
  byte *local_d8;
  char *local_d0;
  uint local_c8 [4];
  wchar_t local_b8 [48];
  ulonglong local_58;
  
  local_58 = DAT_1800241c0 ^ (ulonglong)auStackY_168;
  local_e0 = param_5;
  local_d8 = param_6;
  local_114 = 0;
  local_138 = 1;
  *param_4 = '\0';
  if (param_5 != (uint *)0x0) {
    param_5[0] = 0;
    param_5[1] = 0;
    param_5[2] = 0;
    param_5[3] = 0;
  }
  if (param_6 != (byte *)0x0) {
    *param_6 = 0;
  }
  local_e8 = param_2;
  local_d0 = param_4;
  uVar4 = FUN_180008e38(param_3,local_b8);
  if ((int)uVar4 < 0) {
    FUN_180006678(unaff_retaddr,0x268,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar4);
LAB_18000a7de:
    uVar5 = (ulonglong)uVar4;
  }
  else {
    local_130[0] = (HKEY)0x0;
    uVar4 = RegOpenKeyExW((HKEY)*param_1,local_b8,0,0x20019,local_130);
    if (uVar4 == 0) {
      bVar18 = true;
LAB_18000a861:
      FUN_180007968(local_130[0]);
      uVar5 = 0;
// ... trimmed ...
```

#### Function `FUN_18000c360` @ `18000c360`

```c
uint FUN_18000c360(longlong param_1,uint param_2,uint *param_3,DWORD param_4)

{
  uint uVar1;
  longlong lVar2;
  undefined8 unaff_retaddr;
  
  if (((param_2 < 9) && (param_3 != (uint *)0x0)) && (param_4 != 0)) {
    lVar2 = (longlong)(int)param_2;
    if (*(DWORD *)(&DAT_180024008 + lVar2 * 0x18) == 4) {
      if (param_4 != 4) goto LAB_18000c3e0;
      if ((*param_3 < *(uint *)(&DAT_18002400c + lVar2 * 0x18)) ||
         (*(uint *)(&DAT_180024010 + lVar2 * 0x18) < *param_3)) {
        return 0x8000ffff;
      }
    }
    uVar1 = FUN_180015ee8((undefined8 *)(param_1 + 0x18),
                          (LPCWSTR)(&PTR_u_GameDVR_Enabled_180024000)[lVar2 * 3],(BYTE *)param_3,
                          param_4,*(DWORD *)(&DAT_180024008 + lVar2 * 0x18));
    if ((int)uVar1 < 0) {
      FUN_180006678(unaff_retaddr,0x7c3,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar1);
    }
  }
  else {
LAB_18000c3e0:
    uVar1 = 0x80070057;
  }
  return uVar1;
}
```

#### Function `FUN_18000af84` @ `18000af84`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

uint FUN_18000af84(LPCWSTR param_1,undefined8 *param_2,undefined8 *param_3,LPCWSTR param_4)

{
  short sVar1;
  short sVar2;
  bool bVar3;
  HKEY hKey;
  bool bVar4;
  bool bVar5;
  uint uVar6;
  LSTATUS LVar7;
  ulonglong uVar8;
  ulonglong uVar9;
  short *psVar10;
  wchar_t *pwVar11;
  undefined7 extraout_var;
  BYTE *_Memory;
  short *psVar12;
  uint uVar13;
  undefined8 *puVar14;
  LPBYTE pBVar15;
  longlong lVar16;
  short *psVar17;
  int iVar18;
  undefined8 unaff_retaddr;
  undefined1 auStackY_1d8 [32];
  undefined1 local_178 [8];
  HANDLE local_170;
  DWORD local_168 [2];
  HKEY local_160;
  uint local_158 [2];
  HKEY local_150;
  HKEY local_148;
  HKEY local_140;
  HKEY local_138;
  BYTE local_130 [8];
  short *local_128;
  longlong local_120;
  undefined1 *local_118;
  HANDLE *local_110;
  undefined1 local_108;
  LPCWSTR local_100;
  uint local_f8 [2];
  short *psStack_f0;
  undefined4 local_e8;
  uint local_e0 [2];
  BYTE *local_d8;
  undefined4 local_d0;
  undefined8 *local_c8;
  wchar_t local_b8 [48];
  ulonglong local_58;
  
  local_58 = DAT_1800241c0 ^ (ulonglong)auStackY_1d8;
  uVar8 = 0;
  local_130[0] = '\0';
  local_130[1] = '\0';
  local_130[2] = '\0';
  local_130[3] = '\0';
  local_128 = (short *)0x0;
  local_158[0] = 0;
  local_120 = 0;
  bVar5 = false;
  bVar3 = false;
  local_138 = (HKEY)0x0;
  local_140 = (HKEY)0x0;
  local_148 = (HKEY)0x0;
  local_150 = (HKEY)0x0;
  local_160 = (HKEY)0x0;
  local_168[0] = 0;
  local_178[0] = 0;
  puVar14 = param_3;
  local_100 = param_4;
  local_c8 = param_2;
  uVar6 = FUN_180009fbc(param_1,&local_138);
  if ((int)uVar6 < 0) {
    FUN_180006678(unaff_retaddr,0x496,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
// ... trimmed ...
```

#### Function `FUN_180008780` @ `180008780`

```c
ulonglong FUN_180008780(longlong param_1,longlong param_2,uint *param_3)

{
  undefined8 *puVar1;
  uint uVar2;
  ulonglong uVar3;
  ulonglong uVar4;
  undefined8 uVar5;
  undefined8 unaff_retaddr;
  DWORD local_res18 [2];
  ulonglong local_38;
  longlong lStack_30;
  undefined4 local_28;
  
  if (param_3 == (uint *)0x0) {
    uVar3 = 0x80070057;
  }
  else {
    uVar5 = 0;
    local_res18[0] = 0;
    uVar2 = RegQueryInfoKeyW(*(HKEY *)(param_1 + 8),(LPWSTR)0x0,(LPDWORD)0x0,(LPDWORD)0x0,
                             local_res18,(LPDWORD)0x0,(LPDWORD)0x0,(LPDWORD)0x0,(LPDWORD)0x0,
                             (LPDWORD)0x0,(LPDWORD)0x0,(PFILETIME)0x0);
    uVar3 = (ulonglong)uVar2;
    if ((int)uVar2 < 0) {
      FUN_180006678(unaff_retaddr,0x30f,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar2);
      FUN_180006678(unaff_retaddr,0x6d0,
                    "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                    ,uVar2);
    }
    else {
      if (param_2 != 0) {
        if (*param_3 < local_res18[0]) {
          return 0x800700ea;
        }
        for (uVar2 = 0; uVar2 < local_res18[0]; uVar2 = uVar2 + 1) {
          local_28 = 0;
          local_38 = 0;
          lStack_30 = 0;
          uVar4 = FUN_1800154d8((uint *)&local_38,0xff);
          uVar3 = uVar4 & 0xffffffff;
          if ((int)uVar4 < 0) {
            uVar2 = 0x6e9;
LAB_1800088c9:
            FUN_180006678(unaff_retaddr,uVar2,
                          "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                          ,(uint)uVar3);
            FUN_180007ae0((longlong)&local_38);
            return uVar3;
          }
          uVar4 = FUN_180015808((undefined8 *)(param_1 + 8),uVar2,(uint *)&local_38,uVar5);
          uVar3 = uVar4 & 0xffffffff;
          if ((int)uVar4 < 0) {
            uVar2 = 0x6ea;
            goto LAB_1800088c9;
          }
          uVar4 = FUN_18001609c(lStack_30,local_38 & 0xffffffff,
                                (uint *)((ulonglong)uVar2 * 0x10 + param_2));
          uVar3 = uVar4 & 0xffffffff;
          if ((int)uVar4 < 0) {
            uVar2 = 0x6ec;
            goto LAB_1800088c9;
          }
          FUN_180007ae0((longlong)&local_38);
        }
        uVar2 = local_res18[0];
        if (local_res18[0] < *param_3) {
          do {
            uVar4 = (ulonglong)uVar2;
            uVar2 = uVar2 + 1;
            puVar1 = (undefined8 *)(param_2 + uVar4 * 0x10);
            *puVar1 = 0;
            puVar1[1] = 0;
          } while (uVar2 < *param_3);
        }
      }
      *param_3 = local_res18[0];
// ... trimmed ...
```

#### Function `FUN_180009fbc` @ `180009fbc`

```c
uint FUN_180009fbc(LPCWSTR param_1,undefined8 *param_2)

{
  HKEY pHVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  HKEY local_res18 [2];
  
  local_res18[0] = (HKEY)0x0;
  uVar2 = FUN_180015d70(local_res18,(HKEY)0xffffffff80000003,param_1,0xf003f);
  pHVar1 = local_res18[0];
  if ((int)uVar2 < 0) {
    FUN_180006678(unaff_retaddr,0x1c7,
                  "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                  ,uVar2);
  }
  else {
    local_res18[0] = (HKEY)0x0;
    *param_2 = pHVar1;
  }
  FUN_180007968(local_res18[0]);
  return uVar2;
}
```

#### Function `FUN_18000c3f4` @ `18000c3f4`

```c
uint FUN_18000c3f4(undefined8 *param_1,int param_2)

{
  uint uVar1;
  uint uVar2;
  undefined8 unaff_retaddr;
  uint local_res10 [2];
  uint local_res18 [2];
  
  local_res10[0] = 0;
  if ((&DAT_18001aafe)[(longlong)param_2 * 0x18] == '\0') {
    return 0x8000ffff;
  }
  uVar1 = FUN_180015c00(param_1,L"UserOverrideMask",(LPBYTE)local_res10);
  if (uVar1 == 0x80070002) {
    local_res10[0] = 0;
  }
  else if ((int)uVar1 < 0) {
    uVar2 = 0x37a;
    goto LAB_18000c494;
  }
  local_res10[0] = local_res10[0] | 1 << ((byte)param_2 & 0x1f);
  local_res18[0] = local_res10[0];
  uVar1 = FUN_180015ee8(param_1,L"UserOverrideMask",(BYTE *)local_res18,4,4);
  if (-1 < (int)uVar1) {
    return uVar1;
  }
  uVar2 = 0x37e;
LAB_18000c494:
  FUN_180006678(unaff_retaddr,uVar2,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar1);
  return uVar1;
}
```

#### Function `FUN_1800093ec` @ `1800093ec`

```c
uint FUN_1800093ec(undefined8 *param_1,LPCWSTR param_2,undefined8 *param_3,uint *param_4)

{
  undefined1 auVar1 [16];
  uint uVar2;
  size_t sVar3;
  LPBYTE _Memory;
  uint uVar4;
  undefined *puVar5;
  ulonglong uVar6;
  undefined8 unaff_retaddr;
  uint local_18 [2];
  HKEY local_10;
  
  local_18[0] = 0;
  local_10 = (HKEY)0x0;
  uVar2 = FUN_180015de4(param_1,param_2,param_3,&local_10);
  if ((int)uVar2 < 0) {
    uVar4 = 0x23c;
  }
  else {
    uVar2 = FUN_1800159a8(&local_10,param_2,(LPBYTE)0x0,local_18);
    if (-1 < (int)uVar2) {
      uVar6 = (ulonglong)local_18[0];
      auVar1 = ZEXT816(2) * ZEXT416(local_18[0] + 1);
      sVar3 = auVar1._0_8_;
      puVar5 = &DAT_18001ccc8;
      if (auVar1._8_8_ != 0) {
        sVar3 = 0xffffffffffffffff;
      }
      _Memory = (LPBYTE)thunk_FUN_1800026ac(sVar3);
      if (_Memory == (LPBYTE)0x0) {
        free((void *)0x0);
        uVar2 = 0x8007000e;
      }
      else {
        (_Memory + uVar6 * 2)[0] = '\0';
        (_Memory + uVar6 * 2)[1] = '\0';
        uVar2 = FUN_1800159a8(&local_10,puVar5,_Memory,local_18);
        if ((int)uVar2 < 0) {
          FUN_180006678(unaff_retaddr,0x247,
                        "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                        ,uVar2);
        }
        else {
          *param_3 = _Memory;
          if (param_4 != (uint *)0x0) {
            *param_4 = local_18[0];
          }
          _Memory = (LPBYTE)0x0;
        }
        free(_Memory);
      }
      goto LAB_1800094f4;
    }
    uVar4 = 0x23e;
  }
  FUN_180006678(unaff_retaddr,uVar4,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                ,uVar2);
LAB_1800094f4:
  FUN_180007968(local_10);
  return uVar2;
}
```

#### Function `FUN_180007ff0` @ `180007ff0`

```c
DWORD FUN_180007ff0(BYTE *param_1,DWORD param_2,uint *param_3)

{
  BOOL BVar1;
  DWORD DVar2;
  ulonglong uVar3;
  undefined8 unaff_retaddr;
  DATA_BLOB *local_res8;
  DATA_BLOB local_28;
  DATA_BLOB **local_18;
  undefined1 local_10;
  
  local_28._4_4_ = 0;
  local_18 = &local_res8;
  local_res8 = (DATA_BLOB *)0x0;
  local_10 = 1;
  local_28.cbData = param_2;
  local_28.pbData = param_1;
  local_res8 = (DATA_BLOB *)FUN_1800026ac(0x10);
  if (local_res8 == (DATA_BLOB *)0x0) {
    DVar2 = 0x8007000e;
  }
  else {
    local_res8->pbData = (BYTE *)0x0;
    local_res8->cbData = 0;
    BVar1 = CryptUnprotectData(&local_28,(LPWSTR *)0x0,(DATA_BLOB *)0x0,(PVOID)0x0,
                               (CRYPTPROTECT_PROMPTSTRUCT *)0x0,4,local_res8);
    if (BVar1 == 0) {
      DVar2 = FUN_1800061e0();
    }
    else {
      uVar3 = FUN_180015650(param_3,local_res8->pbData,local_res8->cbData >> 1);
      DVar2 = (DWORD)uVar3;
      if ((int)DVar2 < 0) {
        FUN_180006678(unaff_retaddr,0x1b7,
                      "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                      ,DVar2);
      }
    }
  }
  FUN_1800079fc(&local_18);
  return DVar2;
}
```

#### Function `FUN_18000abe0` @ `18000abe0`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_18000abe0(longlong param_1,uint *param_2,uint *param_3)

{
  short sVar1;
  short sVar2;
  uint uVar3;
  DWORD DVar4;
  short *psVar5;
  longlong lVar6;
  short *_Memory;
  uint uVar7;
  ulonglong uVar8;
  short *psVar9;
  bool bVar10;
  undefined8 unaff_retaddr;
  undefined1 auStackY_158 [32];
  char local_128 [4];
  uint local_124;
  short *local_120;
  longlong local_118;
  longlong local_110;
  longlong lStack_108;
  WCHAR local_f8 [40];
  wchar_t local_a8 [48];
  ulonglong local_48;
  
  local_48 = DAT_1800241c0 ^ (ulonglong)auStackY_158;
  lVar6 = *(longlong *)param_2;
  local_128[0] = '\0';
  local_120 = (short *)0x0;
  local_118 = 0;
  local_124 = 0x24;
  local_110 = 0;
  lStack_108 = 0;
  bVar10 = false;
  if (lVar6 == 0) {
    lVar6 = *(longlong *)(param_2 + 2);
  }
  if (lVar6 == 0) {
    uVar8 = 0x80070057;
  }
  else {
    uVar8 = FUN_18000cf2c((longlong)param_3);
    uVar3 = (uint)uVar8;
    uVar8 = uVar8 & 0xffffffff;
    if ((int)uVar3 < 0) {
      uVar7 = 0x625;
    }
    else {
      uVar8 = FUN_180016300(param_2,(ushort *)local_f8,0x24,&local_124);
      uVar3 = (uint)uVar8;
      uVar8 = uVar8 & 0xffffffff;
      if ((int)uVar3 < 0) {
        uVar7 = 0x627;
      }
      else {
        if (0x49 < (ulonglong)local_124 * 2) {
                    /* WARNING: Subroutine does not return */
          FUN_180001fd4();
        }
        local_f8[local_124] = L'\0';
        uVar3 = FUN_180008e38((longlong)param_3,local_a8);
        uVar8 = (ulonglong)uVar3;
        if (-1 < (int)uVar3) {
          uVar3 = FUN_1800093ec((undefined8 *)(param_1 + 0x10),local_a8,&local_120,(uint *)0x0);
          _Memory = local_120;
          if ((int)uVar3 < 0) {
            FUN_180006678(unaff_retaddr,0x62e,
                          "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\server\\gameconfigstoreserver.cpp"
                          ,uVar3);
            free(local_120);
            return (ulonglong)uVar3;
          }
          psVar9 = local_120;
          uVar3 = FUN_18000c5d4(local_120,0x7fffffff,&local_118);
          uVar8 = (ulonglong)uVar3;
          if ((int)uVar3 < 0) {
// ... trimmed ...
```

### String @ `18001df70`

`onecore\base\appmodel\resourcepolicy\gameconfigstore\shared\rpcshared.cpp`

- Reference count: `1`
- References:
  - `18000d076` in `FUN_18000cfc8`

#### Function `FUN_18000cfc8` @ `18000cfc8`

```c
ulonglong FUN_18000cfc8(longlong param_1,undefined8 param_2)

{
  ulonglong uVar1;
  uint uVar2;
  ulonglong uVar3;
  longlong lVar4;
  undefined8 unaff_retaddr;
  
  if (((*(int *)(param_1 + 0x10) != 0) && (*(longlong *)(param_1 + 8) == 0)) ||
     (uVar2 = (uint)param_2, uVar2 < 0x28)) {
    return 0x80070057;
  }
  if (*(int *)(param_1 + 0x14) == 1) {
    lVar4 = param_1;
    uVar1 = FUN_18000d09c(param_1,uVar2,(ulonglong *)(param_1 + 0x18));
    uVar3 = uVar1 & 0xffffffff;
    if ((int)uVar1 < 0) {
      uVar2 = 0xf9;
    }
    else {
      if (*(int *)(param_1 + 0x10) == 0) {
        return uVar3;
      }
      uVar1 = 0;
      while( true ) {
        uVar2 = (uint)uVar1;
        if (*(uint *)(lVar4 + 0x10) <= uVar2) break;
        uVar1 = FUN_18000d09c(lVar4,(uint)param_2,
                              (ulonglong *)(*(longlong *)(lVar4 + 8) + 8 + uVar1 * 0x10));
        uVar3 = uVar1 & 0xffffffff;
        if ((int)uVar1 < 0) {
          uVar2 = 0xff;
          goto LAB_18000d071;
        }
        uVar1 = (ulonglong)(uVar2 + 1);
      }
      uVar1 = FUN_18000d09c(lVar4,(uint)param_2,(ulonglong *)(lVar4 + 8));
      uVar3 = uVar1 & 0xffffffff;
      if (-1 < (int)uVar1) {
        return uVar3;
      }
      uVar2 = 0x101;
    }
  }
  else {
    uVar1 = FUN_18000d09c(param_1,uVar2,(ulonglong *)(param_1 + 0x18));
    uVar3 = uVar1 & 0xffffffff;
    if (-1 < (int)uVar1) {
      return uVar3;
    }
    uVar2 = 0x106;
  }
LAB_18000d071:
  FUN_180006678(unaff_retaddr,uVar2,
                "onecore\\base\\appmodel\\resourcepolicy\\gameconfigstore\\shared\\rpcshared.cpp",
                (uint)uVar3);
  return uVar3;
}
```

