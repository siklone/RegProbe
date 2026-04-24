# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/Taskbar.dll`
- Name: `Taskbar.dll`
- Patterns: `TaskbarAnimations`

## Pattern: `TaskbarAnimations`

### String @ `1801c7fd0`

`TaskbarAnimations`

- Reference count: `1`
- References:
  - `1800571b1` in `FUN_180057100`

#### Function `FUN_180057100` @ `180057100`

```c
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

ulonglong FUN_180057100(void)

{
  int iVar1;
  ulonglong uVar2;
  wchar_t *pwVar3;
  undefined1 auStack_258 [32];
  char local_238 [4];
  uint local_234 [3];
  wchar_t local_228 [264];
  ulonglong local_18;
  
  local_18 = DAT_18023d040 ^ (ulonglong)auStack_258;
  local_234[0] = 0;
  iVar1 = Ordinal_190(&DAT_1801d2e70);
  if (iVar1 != 0) {
    return (ulonglong)local_234[0];
  }
  SystemParametersInfoW(0x1042,0,local_234,0);
  if (local_234[0] != 0) {
    if (DAT_180241bfc == 0) {
      local_238[0] = '\0';
      WinStationIsSessionRemoteable(0,0xffffffff,local_238);
      DAT_180241bfc = 2 - (uint)(local_238[0] != '\0');
    }
    if ((DAT_180241bfc == 1) || (iVar1 = GetSystemMetrics(0x2001), iVar1 != 0)) {
      FUN_180057db0(local_228,0x104,
                    L"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Remote\\%d",
                    *(undefined4 *)((longlong)ProcessEnvironmentBlock + 0x2c0));
      pwVar3 = local_228;
    }
    else {
      pwVar3 = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
    }
    uVar2 = Ordinal_123(pwVar3,L"TaskbarAnimations",1);
    return uVar2;
  }
  return 0;
}
```

