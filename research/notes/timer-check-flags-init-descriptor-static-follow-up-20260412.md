# TimerCheckFlags INIT Descriptor Static Follow-up

- Generated: `2026-04-12T02:08:00+03:00`
- Target: `system.kernel.timer-check-flags`
- Binary: `/tmp/regprobe-kernel-upload/uploads/ntoskrnl.exe`

## Result

`TimerCheckFlags` is no longer only a loose current-build string hit. The current `ntoskrnl.exe` image contains an `INIT` descriptor row that binds:

- key context: `Session Manager\Kernel`
- value name: `TimerCheckFlags`
- target global: `0x140E0B080`

The target global RVA is `0xE0B080`, which matches the live KVM local-KD symbol address for `nt!KeTimerCheckFlags` after subtracting the live kernel base:

- live kernel base: `0xfffff806e6e00000`
- live `nt!KeTimerCheckFlags`: `0xfffff806e7c0b080`
- live RVA: `0xE0B080`
- static descriptor target RVA: `0xE0B080`

## Host-Side Decode

- `TimerCheckFlags` string file offset: `0xBF4BF8`
- string RVA: `0xC7BBF8`
- string VA: `0x140C7BBF8`
- string section: `INIT`
- descriptor row file offset: `0xBED8D8`
- descriptor row RVA: `0xC748D8`
- descriptor row VA: `0x140C748D8`

Descriptor row:

```text
+0x00 = 0x140C7BBF8 -> TimerCheckFlags
+0x08 = 0x140E0B080 -> KeTimerCheckFlags RVA 0xE0B080
+0x10 = 0
+0x18 = 0
```

Nearby key context:

```text
Session Manager\Kernel
```

## Interpretation

This closes the static/xref gap for the record. The value name, registry key context, and live global now converge on the current build. The remaining blocker is not Ghidra/static evidence anymore; it is an exact runtime registry trace or equivalent runtime read showing the descriptor path being consumed during boot/init.

## Artifacts

- `evidence/files/vm-tooling-staging/timercheckflags-xref-20260412/init-table-binding.json`
- `evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-timercheckflags-20260408a/local-kd-timercheckflags-20260408a.stdout.txt`
