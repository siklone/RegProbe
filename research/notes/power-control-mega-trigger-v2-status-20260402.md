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

## WinDbg Escalation Status

The next lane is now prepared and host-side debugger installation is no longer the blocker:

- `registry-research-framework/audit/power-control-windbg-boot-registry-trace-20260402.json`
- `registry-research-framework/audit/configure-kernel-debug-baseline.json`
- `registry-research-framework/audit/windbg-registry-watch.txt`

What is now true for the `WinDbg` lane:

- the secondary VM has a working debug baseline snapshot: `RegProbe-Baseline-Debug-20260402`
- guest boot debugging is enabled
- the VMX serial pipe is configured for `\\.\pipe\regprobe_debug`
- the guest rebooted after `bcdedit` and returned to healthy shell state
- the attach bundle is generated for the 5 persistent `no-hit` values
- `kd.exe` is now installed on the host and detected by the wrapper

What the first live execution showed:

- a real `kd` boot-trace session now connects to the guest kernel over the VMware pipe
- the debugger reaches the target and shows `Kernel Debugger connection established`
- but the guest then stops in a fatal system-error / debugger-break state before shell recovery
- the current lane is therefore not a clean evidence pass yet; it is a `boot-unsafe` blocker under the present VM/debug configuration
- after the failed execution, the VM had to be recovered manually by reverting to `RegProbe-Baseline-Debug-20260402`, removing a stale `.lck`, and starting it again

## Why This Still Matters

This is now a usable runtime triage lane again:

- the VM recovers cleanly
- the runner does not leave stale `armed` probes behind
- the trigger suite is viable
- the pilot now finishes with a deterministic terminal outcome instead of stalling in `parsing`

## Next Follow-Up

1. harden the `kd` execution lane to classify `boot-unsafe` runs without leaving the VM unhealthy
2. investigate why kernel debug enters a fatal system-error / break state under the current VMware serial-pipe setup
3. only after a stable `kd` pass should the 5-key set move to early-boot-read vs true dead/no-hit decisioning
4. keep widening on hold until the debug lane is stable
