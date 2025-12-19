# Tweak Implementation Guide

## Overview
Tweaks implement `ITweak` and expose four actions: Detect, Apply, Verify, and Rollback. The execution pipeline is handled by `TweakExecutionPipeline`, which logs every step and supports DryRun/Preview by default.

## Safety guarantees (Detect -> Apply -> Verify -> Rollback)
- Detect always runs first to capture current configuration.
- Apply runs only when Detect succeeds and DryRun is false.
- Verify runs after Apply when `VerifyAfterApply` is enabled.
- Rollback runs automatically when Apply or Verify fails (default) or when the user requests it.

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
- Rollback: call `ITweak.RollbackAsync` directly or rely on automatic rollback when a step fails.
