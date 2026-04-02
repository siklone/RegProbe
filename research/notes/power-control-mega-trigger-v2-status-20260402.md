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

The remaining VM survivability blocker is gone, and the pilot lane is now terminal.

The validated run is:

- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-221106/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-221106/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-223411/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-223411/results.json`

What changed to make it reliable:

- host storage preflight now blocks unsafe VM paths before runtime collection starts
- the guest now resolves ETL output from `C:\Windows\System32\<TraceName>.etl` when `logman` ignores the requested output path
- the pilot lane now uses deterministic ETL binary fallback parsing instead of stalling in `tracerpt`
- terminal completion now writes `summary.json` first so the host runner can observe terminal status without waiting on later artifact writes

## Why This Still Matters

This is now a usable runtime triage lane again:

- the VM recovers cleanly
- the runner does not leave stale `armed` probes behind
- the trigger suite is viable
- the pilot now finishes with a deterministic terminal outcome instead of stalling in `parsing`

## Next Follow-Up

1. take the persistent 5-key `no-hit` set to the `WinDbg` lane
2. only widen beyond the 5-key pilot after the `WinDbg` pass tells us whether these are early-boot reads or true dead/no-hit candidates
3. if `WinDbg` still shows no read activity, keep them in the negative-evidence / dead-flag decision path
