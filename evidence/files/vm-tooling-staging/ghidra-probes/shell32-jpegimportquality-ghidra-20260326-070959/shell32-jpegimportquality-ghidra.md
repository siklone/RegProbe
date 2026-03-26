# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/shell32.dll`
- Name: `shell32.dll`
- Patterns: `JPEGImportQuality`

## Pattern: `JPEGImportQuality`

### String @ `180647a80`

`JPEGImportQuality`

- Reference count: `1`
- References:
  - `1800bfd19` in `FUN_1800bf050`

#### Function `FUN_1800bf050` @ `1800bf050`

```c
/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800bf050(longlong *param_1,undefined8 param_2,undefined8 param_3,uint *param_4,
                 uint *param_5,undefined8 param_6,float *param_7)

{
  uint uVar1;
  bool bVar2;
  bool bVar3;
  bool bVar4;
  bool bVar5;
  longlong lVar6;
  char cVar7;
  HRESULT HVar8;
  uint uVar9;
  uint uVar10;
  int iVar11;
  undefined *puVar12;
  longlong *plVar13;
  uint uVar14;
  bool bVar15;
  double dVar16;
  double dVar17;
  undefined8 unaff_retaddr;
  undefined1 auStackY_1e8 [32];
  undefined8 local_198;
  undefined8 local_190;
  wchar_t *local_188;
  undefined8 local_180;
  undefined8 uStack_178;
  longlong *local_170;
  uint local_168 [2];
  undefined8 local_160;
  uint local_158 [2];
  longlong *local_150;
  longlong *local_148;
  longlong *local_140;
  longlong *local_138;
  longlong *local_130;
  longlong *local_128;
  longlong *local_120;
  float local_118 [2];
  longlong *local_110;
  longlong *local_108;
  longlong *local_100;
  longlong *local_f8 [2];
  undefined8 local_e8;
  undefined8 uStack_e0;
  longlong *local_d8;
  longlong *local_d0;
  undefined8 local_c8;
  longlong lStack_c0;
  double local_b8;
  double local_b0;
  longlong local_a8;
  ulonglong uStack_a0;
  undefined8 local_98;
  undefined4 local_88;
  undefined4 uStack_84;
  undefined4 uStack_80;
  undefined4 uStack_7c;
  undefined8 local_78;
  undefined8 uStack_70;
  undefined8 local_68;
  undefined8 uStack_60;
  ulonglong local_58;
  
  local_58 = DAT_1806e4a00 ^ (ulonglong)auStackY_1e8;
  local_e8 = param_7;
  local_b8 = 0.0;
  local_b0 = 0.0;
  local_118[0] = 0.0;
  if (param_4 != (uint *)0x0) {
    *param_4 = 0;
  }
  if (param_5 != (uint *)0x0) {
    *param_5 = 0;
  }
// ... trimmed ...
```

