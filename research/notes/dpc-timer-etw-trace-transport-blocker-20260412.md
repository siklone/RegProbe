# DPC/Timer ETW Trace Transport Blocker - 2026-04-12

## Scope

The DPC/timer runtime trace lane targets the remaining kernel timing records that still need exact runtime registry-read proof, including `system.kernel-long-dpc-threshold-cluster`, `system.kernel.force-bugcheck-for-dpc-watchdog`, and `system.kernel.timer-check-flags`.

## What Changed

`scripts/vm/run-dpc-timer-etw-trace-guest.ps1` now supports bridge uploads through `-UploadBaseUrl` and `-UploadPrefix`, writes a canonical `trace-summary.json`, and emits a small `target-hits.txt` file when the tracerpt XML contains target strings such as `TimerCheckFlags`, `LongDpc`, `ForceBugcheck`, or `DpcWatchdog`.

A short ISO launcher was added at `scripts/vm/run-dpc-timer-etw-trace-launcher-guest.ps1` so the guest can run the trace without typing the full bridge URL and parameter set through the fragile keyboard transport.

## Transport Result

The VM was running and the bridge at `http://10.0.2.2:8766` was healthy from the host side. The attached ISO was rebuilt with the trace runner and launcher.

QGA is reachable enough for `guest-ping`, but the installed agent reports version `0.12.1` and does not expose `guest-exec` or guest file transfer commands. That means we cannot use QGA as a reliable command execution lane for this trace on the current guest image.

The keyboard lane is currently unreliable for long commands. The first ISO invocation failed because the mounted CD-ROM was not `D:`. The bridge-download invocation then appeared to enter a malformed or stuck command line in the elevated PowerShell window, and the admin-shell recovery helper timed out without publishing its marker.

The blocker was worked around by using the ISO-visible 8.3 names:

```powershell
powershell -ExecutionPolicy Bypass -File D:\extras\RUN_DPC_TIMER_ETW_TRACE_LAU.PS1 -UploadPrefix dpc-timer-etw-20260412-fixed
```

That run completed successfully. It produced a 5.7 MB ETL and a 57 MB tracerpt XML, uploaded both to the active guest bridge root, and wrote the retained summary under `evidence/files/vm-tooling-staging/dpc-timer-etw-20260412-fixed/trace-summary.json`.

The result is a clean no-target-hit runtime trace: `target_hits_count = 0` for `TimerCheckFlags`, `LongDpc`, `ForceBugcheck`, `DpcWatchdog`, `DpcWatchdogProfile`, `DpcWatchdogPeriod`, and `KeTimerCheckFlags`.

## Next Step

Use a targeted trigger or boot/reboot lane next. This trace proves the ETW collection lane works, but it does not close the `runtime_no_read` blockers for the DPC/timer records.

Preferred manual guest command after the ISO is attached:

```powershell
powershell -ExecutionPolicy Bypass -File D:\extras\RUN_DPC_TIMER_ETW_TRACE_LAU.PS1 -UploadPrefix dpc-timer-etw-YYYYMMDD-label
```

Expected host output path:

```text
/tmp/regprobe-dpc-timer-upload/dpc-timer-etw-20260412/
```
