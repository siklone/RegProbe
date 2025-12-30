# Tweak Implementation Guide
> Update (2025-12-30): LegacyTweakProvider restored missing tweaks; verify this doc against the current catalog.

## Overview
Tweaks implement `ITweak` and expose four actions: Detect, Apply, Verify, and Rollback. The execution pipeline is handled by `TweakExecutionPipeline`, which logs every step and supports DryRun/Preview by default.

> **Note (2025-12-27):** Rollback currently relies on the last Detect snapshot within the same app session. Durable rollback across app restarts is not implemented yet.

## Safety guarantees (Detect -> Apply -> Verify -> Rollback)
- Detect always runs first to capture current configuration.
- Apply runs only when Detect succeeds and DryRun is false.
- Verify runs after Apply when `VerifyAfterApply` is enabled.
- Rollback runs automatically when Apply or Verify fails (default) or when the user requests it.

## Elevation requirements
- Tweaks that touch HKLM/HKCR, services, drivers, scheduled tasks, BCD, or system directories must run elevated.
- HKCU and user-profile tweaks can run without elevation.
- Each tweak doc section includes a `Requires elevation:` line to indicate the expected privilege.

### ElevatedHost discovery (dev runs)
When running via `dotnet run`, you can override the elevated host location with:
`WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`.

## Monitoring system

### Logging
- Every step writes to the app log and the structured CSV log (`tweak-log.csv`).
- CSV fields include `timestamp`, `tweak_id`, `tweak_name`, `action`, `status`, `message`, and `error`.

### Real-time monitoring
- The pipeline reports `TweakExecutionUpdate` for each step with action, status, message, and timestamp.
- UI can render live indicators for Detect, Apply, Verify, and Rollback based on these updates.

### Export logs
- `ITweakLogStore.ExportCsvAsync(path)` copies the CSV log to a user-selected destination.

## How to apply/verify/rollback tweaks in the app
- Preview (default): run the pipeline with `DryRun = true` to see what would change.
- Apply: run the pipeline with `DryRun = false`.
- Verify: keep `VerifyAfterApply = true` or call `ITweak.VerifyAsync` explicitly.
- Rollback: restores values captured by the last Detect (same app session) and is also used automatically when Apply/Verify fails.
