# power.session-watchdog-timeouts KVM local-KD registry-helper follow-up - 2026-04-07

## Summary

- A Linux KVM local-KD symbol sweep and disassembly follow-up narrowed the watchdog lane from a generic "exact read missing" state to a concrete current-build helper family.
- The live guest now resolves a generic power-manager registry helper chain:
  - `PopOpenPowerKey`
  - `PopReadRegKeyValue`
  - `PopReadUlongPowerKey`
- `uf nt!PopReadRegKeyValue` showed a current-build helper that:
  - opens the requested key with `ZwOpenKey`
  - initializes the requested value name with `RtlInitUnicodeString`
  - queries the value with `ZwQueryValueKey`
  - allocates a larger buffer on `STATUS_BUFFER_TOO_SMALL`
  - optionally checks the value type before copying the payload to caller storage
- `uf nt!PopOpenPowerKey` showed that the power-manager root is still opened through `PopRegKey` via `PopOpenKey`.
- `x nt!*Watchdog*Reg*` did not resolve a Pop-specific watchdog registry-name symbol on this build. It only surfaced unrelated `PnpWatchdog*RegName` symbols.
- A remap of the older Ghidra fallback offsets `nt+0xc635d8` and `nt+0xc63608` landed in `PipInitializeCoreDriversByGroup` / `PipInitializeEarlyLaunchDrivers`, not in a watchdog reader.

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-symbol-sweep-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-symbol-sweep-20260407a.log`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-reader-disasm-20260407b-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-reader-disasm-20260407b.log`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-xref-remap-20260407c-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-reg-helpers-20260407abc/watchdog-xref-remap-20260407c.log`

## Why this matters

The checked-in-build story is now sharper in two ways:

- the watchdog lane no longer depends on hand-wavy "some registry helper probably exists" language because the live guest now exposes the generic power-manager reader surface directly
- the older unresolved Ghidra fallback offsets are no longer trustworthy watchdog evidence because they remap to unrelated PnP initialization code on the checked-in build

That still does not close the lane. The remaining exact-read gap is now narrower:

- identify the actual watchdog caller that feeds `WatchdogResumeTimeout` / `WatchdogSleepTimeout` into `PopReadRegKeyValue`, or
- capture the exact read directly in a runtime trace lane

## Short take

- new proof gained: current-build generic power-registry helper family
- stale proof downgraded: old fallback xref offsets
- remaining blocker: exact watchdog caller or exact live read
