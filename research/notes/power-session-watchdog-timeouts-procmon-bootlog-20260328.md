# power.session-watchdog-timeouts Procmon boot log - 2026-03-28

## Summary

- A baseline-only Procmon boot-log lane now works on `Win25H2Clean`.
- The hidden `EnableBootLogging` CLI variants still returned non-zero in the arm phase.
- Even so, the post-boot `ConvertBootLog` plus `OpenLog` and `SaveAs` flow produced a real boot-log `PML` and `CSV`.
- The filtered boot-log hits stayed adjacent to the watchdog pair: `SystemPowerPolicy` and `ShutdownOccurred`.
- There were still no direct `WatchdogResumeTimeout` or `WatchdogSleepTimeout` reads in the filtered boot-log view.

## Source artifacts

- Runner summary: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/summary.json`
- Arm phase: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/summary-arm.json`
- Collect phase: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/summary-collect.json`
- Filtered hits: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/watchdog-procmon-bootlog.hits.csv`
- Raw placeholder: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/watchdog-procmon-bootlog.pml.md`

## Result

- Shell health was clean before and after the run.
- The host-driven reboot cycle advanced successfully.
- `ConvertBootLog` returned `0` and produced:
  - `PML` length: `913,983,143`
  - `CSV` length: `377,411,218`
- The filtered hit set contained `18` entries.

Observed filtered boot-log hits:

- `System (PID 4)` queried `SystemPowerPolicy`
- `System (PID 4)` queried `ShutdownOccurred`
- `System (PID 4)` repeatedly wrote `SystemPowerPolicy`

Not observed in the filtered boot-log hits:

- `WatchdogResumeTimeout`
- `WatchdogSleepTimeout`
- `PowerSettingProfile`
- `svchost.exe`

## Why this matters

This closes an important tooling question for the watchdog lane. Procmon boot logging is usable here, but the strongest signal still matches the ETL review rather than replacing it. The boot-log capture confirms that `Session Manager\Power` is active during boot and that the adjacent `SystemPowerPolicy` and `ShutdownOccurred` values are visible from `System (PID 4)`.

That sharpens the remaining gap: the lane is no longer missing a Procmon attempt, it is missing an exact live read of the watchdog pair itself. The next runtime step should therefore be more targeted than another generic boot capture.
