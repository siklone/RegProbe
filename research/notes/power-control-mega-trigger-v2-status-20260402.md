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

## Current Blocker

The remaining blocker is post-trace parsing.

Observed current behavior:

- `tracerpt` falls back with `exit_code = -2147024894`
- the `Get-WinEvent -Path` fallback still does not produce terminal parsed output quickly enough
- the host now reports this deterministically as a stalled `parsing` phase instead of stranding the VM in a broken state

## Why This Still Matters

This is no longer a VM survivability problem.

It is now a narrow ETL parsing problem:

- the VM recovers cleanly
- the runner does not leave stale `armed` probes behind
- the trigger suite itself is viable

## Next Follow-Up

1. add an ETL readiness check after trace stop
2. replace or harden the current fallback parser
3. only after terminal `results.json` is reliable, widen beyond the 5-key pilot
