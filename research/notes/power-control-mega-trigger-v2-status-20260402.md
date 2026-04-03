# Power-Control Mega-Trigger v2 Status

Date: `2026-04-03`

## What Is Now Working

- the safe 5-key mega-trigger pilot is terminal and recovery-safe
- both validated pilot runs still end `no-hit`:
  - `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-221106/summary.json`
  - `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-223411/summary.json`
- the `WinDbg` lane now executes real command scripts instead of only attaching:
  - `-cfr` is now used for session scripts
  - `-bonc` forces an initial debugger prompt
  - the runner now resolves `.logopen /t` suffix logs instead of assuming a fixed filename

## Current WinDbg Status

The `WinDbg` lane is now `partial`, not blocked.

What is validated:

- host debugger is installed and `kd.exe` is usable
- the secondary VM still boots from `RegProbe-Baseline-Debug-20260402`
- public symbol discovery works:
  - `nt!CmQueryValueKey`
  - `nt!CmQueryValueKeyCallout`
  - `nt!NtQueryValueKey`
  - `nt!ZwQueryValueKey`
- the first successful public-symbol search is captured here:
  - `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-014256/windbg-registry-trace_59e0_2026-04-03_01-44-20-117.log`
- the first successful argument-shape probe is captured here:
  - `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-014903/windbg-registry-trace_01ac_2026-04-03_01-50-38-815.log`

What that first-hit probe proved:

- `nt!CmpQueryValueKey` is not usable from public symbols
- `nt!CmQueryValueKey` is usable from public symbols
- for `CmQueryValueKey`, the queried value name is exposed through the `UNICODE_STRING*` in `@rdx`
- the first captured queried name was `Disable Performance Counters`

## What Still Fails

Two automated `WinDbg` watch styles are still too rough to be the final arbiter:

1. raw value-name logging
   - run:
     - `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-015149/results.json`
   - outcome:
     - large `CmQueryValueKey` value-name log
     - no hits for the 5 target names
     - shell-health timed out before the run closed cleanly

2. conditional filtered watch
   - run:
     - `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-023157/results.json`
   - outcome:
     - the filtered `bs/.if/@@c++` expression still does not parse cleanly in the current command form
     - no real hit blocks were emitted
     - shell-health timed out before the run closed cleanly

The secondary VM was recovered after the heavy watch attempts and is healthy again:

- `registry-research-framework/audit/configure-kernel-debug-baseline.json`

## Repo Truth

These 5 no-hit candidates are still the active escalation set:

- `power.control.allow-audio-to-enable-execution-required-power-requests`
- `power.control.allow-system-required-power-requests`
- `power.control.always-compute-qos-hints`
- `power.control.coalescing-flush-interval`
- `power.control.idle-processors-require-qos-management`

The current attach bundle has been corrected to public-symbol reality:

- `registry-research-framework/audit/power-control-windbg-boot-registry-trace-20260402.json`
- `registry-research-framework/audit/windbg-registry-watch.txt`

## Next Follow-Up

1. replace the failing conditional `bs/.if` command with a parser-safe filtered command form for `nt!CmQueryValueKey`
2. keep the watch lightweight enough that shell health returns without needing a post-run recovery
3. only after a clean filtered `WinDbg` pass should these 5 values move to early-boot-read vs true dead/no-hit decisioning
