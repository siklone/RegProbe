# Power-Control Mega-Trigger v2 Status

Date: `2026-04-02`

## What Is Now Working

- the host runner is recovery-first and stale-probe aware
- `-RecoverOnly` successfully reverts `Win25H2Clean` to `RegProbe-Baseline-ToolsHardened-20260330`
- shell-health checks pass after recovery
- the guest run gets through the full safe pilot trigger set:
  - `cpu_stress`
  - `power_plan_and_requests`
  - `multi_thread_burst`
  - `disk_io_burst`
  - `process_spawn_burst`
  - `foreground_background_switch`
  - `timer_resolution_change`
  - `network_activity`

## Current Status

The remaining VM survivability blocker is gone.

Post-trace parsing has now been hardened in code:

- the guest waits for the ETL to become stable after trace stop
- `tracerpt` now retries with captured stderr instead of a single fire-and-forget call
- the old `Get-WinEvent -Path` fallback has been replaced with a deterministic ETL binary token scan
- guest error handling now writes placeholder `results.json` as well as terminal `summary.json`

## Remaining Validation Gap

This needs one more live VM rerun to confirm that the new parser path produces terminal `results.json` under the same pilot run that previously stalled in `parsing`.

## Why This Still Matters

This is no longer a VM survivability problem.

It was narrowed down to a post-trace ETL parsing problem:

- the VM recovers cleanly
- the runner does not leave stale `armed` probes behind
- the trigger suite itself is viable

## Next Follow-Up

1. rerun the 5-key pilot against the hardened parser path
2. if terminal `results.json` is now reliable, widen only after one more confirmatory pass
3. only then widen beyond the 5-key pilot
