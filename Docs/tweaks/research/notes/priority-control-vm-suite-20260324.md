# PriorityControl VM Suite (2026-03-24)

## Setting

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl`
- Value: `Win32PrioritySeparation`
- Type: `REG_DWORD`

## What This Pass Did

- Wrote baseline `2`
- Rebooted the guest and confirmed the value after boot
- Ran `WinSAT CPU + WPR`
- Ran `WinSAT mem + WPR`
- Wrote candidate `38`
- Rebooted the guest and confirmed the value after boot
- Ran the same CPU and memory passes
- Restored baseline `2`
- Rebooted the guest and confirmed restore

## Runtime Evidence

```text
Summary file:
H:\Temp\vm-tooling-staging\priority-control-20260324-201011\summary.json

Observed after reboot:
- baseline: 2
- candidate: 38
- restore: 2

Bounded benchmark durations:
- baseline CPU: 35.28s
- candidate CPU: 26.04s
- baseline mem: 25.05s
- candidate mem: 25.11s
```

## Artifact Bundles

```text
H:\Temp\vm-tooling-staging\priority-control-20260324-201011\baseline-cpu.zip
H:\Temp\vm-tooling-staging\priority-control-20260324-201011\baseline-mem.zip
H:\Temp\vm-tooling-staging\priority-control-20260324-201011\candidate-cpu.zip
H:\Temp\vm-tooling-staging\priority-control-20260324-201011\candidate-mem.zip
```

## Notes

- This pass proves the raw value round-trip on a live Win25H2Clean guest with real reboots.
- It also proves that the guest reached real bounded CPU and memory workloads under both states.
- It does not prove the modern Microsoft semantics of raw bitmask `0x26`.
- The CPU delta is interesting enough to keep, but it is still a single bounded VM pass, not a publishable performance claim.
