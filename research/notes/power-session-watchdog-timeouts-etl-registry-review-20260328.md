# power.session-watchdog-timeouts ETL registry review - 2026-03-28

## Summary

- The host-side ETL parse now proves boot-time access to `Session Manager\Power`.
- The strongest readers in this pass were `System (PID 4)` during boot and `svchost.exe (PID 980)` later in the session.
- Adjacent value traffic was recovered for `SystemPowerPolicy` and `ShutdownOccurred`.
- The exact watchdog pair names still did not appear in the parsed event payloads.

## Source artifacts

- ETL placeholder: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/watchdog-timeouts-boot.etl.md`
- Filtered review: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/registry-dump-watchdog-session-manager-power.txt`

## Counts

- `Session Manager\Power`: `81`
- `SystemPowerPolicy`: `19`
- `ShutdownOccurred`: `1`
- `WatchdogResumeTimeout`: `0`
- `WatchdogSleepTimeout`: `0`

## Why this matters

This closes the old ambiguity around whether the boot trace only preserved the baseline values or whether the path was actually touched during boot. The parsed ETL now shows repeated `RegCreateKey`, `RegOpenKey`, `RegQueryValue`, and `RegSetValue` activity on `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`.

That is stronger than a plain before/after export, but it is still not a direct live read of `WatchdogResumeTimeout` or `WatchdogSleepTimeout`. The lane therefore stays draft-only and should not be promoted to a shipped app mapping yet.

## Representative findings

- `System (PID 4)` created/opened `Session Manager\Power` repeatedly during boot.
- `System (PID 4)` queried `SystemPowerPolicy` multiple times under the same path.
- `System (PID 4)` also queried `ShutdownOccurred` once from the same path.
- `svchost.exe (PID 980)` later opened `SYSTEM\CurrentControlSet\Control\Session Manager\Power` multiple times.

## Next step

- Keep the watchdog pair together as one candidate lane.
- If this lane is revisited, the next useful pass is a more targeted live-read attempt for the exact watchdog pair rather than another generic path-level trace.
