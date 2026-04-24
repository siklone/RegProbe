# Ghidra String/Xref Export

- Program: `/C:/Windows/System32/ntoskrnl.exe`
- Name: `ntoskrnl.exe`
- Probe: `executive-worker-addrseed-kvm-20260406b`
- Timestamp: `2026-04-06T10:55:13.595462900Z`
- Patterns: `addr:140c62b88`, `addr:140c62bb8`

## Pattern Summary

### Pattern: `addr:140c62b88`

- Address seed: `140c62b88`

### Pattern: `addr:140c62bb8`

- Address seed: `140c62bb8`

## Match Analysis

## Unresolved Block @ `140c62b88`

- Function: `FUN_140c62b88`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `8`

```c
/* WARNING: Control flow encountered bad instruction data */

void FUN_140c62b88(void)

{
                    /* WARNING: Bad instruction - Truncating control flow here */
  halt_baddata();
}
```

## Unresolved Block @ `140c62bb8`

- Function: `FUN_140c62bb8`
- Forced boundary: `true`
- Naturally resolved: `false`
- Decompile success: `true`
- Output kind: `decompile`
- Output lines: `23`

```c
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined8 FUN_140c62bb8(longlong param_1)

{
  undefined8 uStack0000000000000028;
  undefined8 uStack0000000000000030;

  uStack0000000000000030 = 0;
  uStack0000000000000028 = 0;
  FUN_14046c968(*(undefined8 *)(param_1 + 0x20),0x11,0,0,0);
  if (_DAT_140e0a964 == 0) {
    FUN_140c22720();
  }
  uStack0000000000000030 = 0;
  uStack0000000000000028 = 0;
  FUN_14046c968(*(undefined8 *)(_DAT_140f8b6a8 + 0x20),0x12,0,0,0);
  if (_DAT_140f8c168 != 0) {
    FUN_140c207a4();
  }
  FUN_14043b7c4(&UNK_14001c628,0,0);
  return 0;
}
```
