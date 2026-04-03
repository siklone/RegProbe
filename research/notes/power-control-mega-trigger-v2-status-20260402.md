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

## Single-Key WinDbg Lane

The lane has now been narrowed to one key at a time to answer the earlier parser/bloat criticism more directly.

What is now implemented:

- `configure-kernel-debug-baseline.ps1` now enforces a snapshot gate and adds a revert guard path
- runner outputs are now emitted as `raw + sanitized` pairs instead of only one public log surface
- the `WinDbg` generator and executor now support:
  - `singlekey-smoke`
  - `singlekey-firsthit`
  - `singlekey-rawbounded`
- the first canonical bundle is now:
  - `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`

The first single-key target is:

- `AllowSystemRequiredPowerRequests`

Support proof that the current guest is still debug-enabled:

- `registry-research-framework/audit/bcd-current-20260403.txt`

Current outcome for the first single-key lane:

- the lane is now parser-safe and low-noise at the script level
- but the actual cold-boot attach still fails at transport level before target execution becomes trustworthy
- the observed host-side failure is:
  - `Failed to write breakin packet`
  - `WARNING: The HOST cannot communicate with the TARGET!`

That means:

- this is **not** a valid `no-hit`
- `AllowSystemRequiredPowerRequests` is still in `WinDbg` escalation, not yet classified by the final arbiter
- the current blocker is debugger transport/reconnect, not parser syntax or documentation bloat

## Guest-Restart Serial Matrix

The first focused serial-config matrix narrowed the transport issue further.

What is now true:

- `guest-restart` + `kd` reached `kernel_connected=true` in `4/4` tested variants
- `3/4` variants finished `transport_ok`
- the weakest variant so far was:
  - `break_on_connect=none`
  - `tryNoRxLoss=TRUE`
  - outcome `transport_error`

This shifts the blocker again:

- the problem is no longer "guest-restart transport is generically broken"
- the new blocker is reproducible command/break-in roundtrip on the winning `guest-restart` serial base
- that is transport engineering, not key semantics

Direct follow-up on the strongest current base:

- profile:
  - `guest-restart`
  - `kd`
  - `breakin-once`
  - `break_on_connect=bonc`
  - `tryNoRxLoss=FALSE`
- outcome:
  - `kernel_connected=true`
  - `transport_error=false`
  - `shell_recovered=true`
  - final status `attach-ok-command-not-executed`

That means the attach side is now stronger than before, but the real break-in command roundtrip is still not classification-grade.

Thin queued-command follow-up:

- profile:
  - `guest-restart`
  - `kd`
  - `roundtrip-once`
  - `break_on_connect=bonc`
  - `tryNoRxLoss=FALSE`
- outcome:
  - post-restart queued commands resumed
  - `post_restart_roundtrip_success_count=2`
  - but they resumed at a fatal-system-error break
  - final status `boot-unsafe`
  - VM required snapshot recovery afterward

So the lane just got more precise:

- queued command continuation is **possible**
- but the current attach/start-order lands us in the wrong stop state
- the next fix is not parser syntax; it is attach/start-order plus break policy

Start-order follow-up on the same guest-restart base:

- profiles tested:
  - `bonc` with lead `3`, `10`, `20`
  - `none` with lead `10`, `20`
  - `b` with lead `10`
- outcome:
  - `6/6` kept `kernel_connected=true`
  - `6/6` kept `transport_error=false`
  - `6/6` recovered shell health
  - `0/6` produced any post-restart queued command roundtrip
  - all `6/6` ended `attach-ok-command-not-executed`

That narrows the blocker again:

- current `guest-restart` attach lead timing is no longer the main unknown
- current `bonc` / `none` / `b` break-policy variants are also not enough by themselves
- the next phase should move to reconnect-time command injection or pipe endpoint experiments

Reconnect-command follow-up on the same guest-restart base:

- profiles tested:
  - the same `bonc` / `none` / `b` variants and `3` / `10` / `20` lead timings
  - but this time with host-side `breakin-once` injection instead of queued roundtrip markers
- outcome:
  - `6/6` kept `kernel_connected=true`
  - `6/6` still ended `attach-ok-command-not-executed`
  - `0/6` produced any breakin success
  - `1/6` produced a fatal-break outlier on `none + lead20`

So the blocker narrowed once more:

- not parser syntax
- not generic guest-restart transport
- not queued command continuation alone
- not host-side reconnect command injection alone
- next phase should move to pipe endpoint or debugger launch-mode experiments

Pipe/launch follow-up on the same guest-restart base:

- profiles tested:
  - `server + quiet`
  - `server + standard`
  - `client + quiet`
  - `client + standard`
- outcome:
  - both `server` variants still reached `kernel_connected=true`
  - both `server` variants still ended `attach-ok-command-not-executed`
  - both `client` variants degraded into direct `transport_error`
  - launch mode (`quiet` vs `standard`) did not materially change the result

That gives the cleanest transport verdict so far:

- the current VMware named-pipe contract is characterized
- `server` endpoint preserves attach but not command execution
- `client` endpoint breaks transport outright
- this is no longer a parser/start-order/break-policy problem
- any next WinDbg transport step must change the transport contract itself

Canonical matrix outputs:

- `registry-research-framework/audit/windbg-serial-config-matrix-20260403.json`
- `registry-research-framework/audit/windbg-serial-config-execution-20260403.json`
- `research/notes/windbg-serial-config-matrix-20260403.md`
- `registry-research-framework/audit/windbg-transport-bundle-guest-restart-breakin-once-kd-bonc-rxloss-false-20260403.json`
- `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-145133/summary.json`
- `registry-research-framework/audit/windbg-transport-bundle-guest-restart-roundtrip-once-thin-kd-bonc-rxloss-false-20260403.json`
- `evidence/files/vm-tooling-staging/windbg-boot-registry-trace-20260403-154201/summary.json`
- `registry-research-framework/audit/windbg-start-order-matrix-20260403.json`
- `registry-research-framework/audit/windbg-start-order-execution-20260403.json`
- `research/notes/windbg-start-order-matrix-20260403.md`
- `registry-research-framework/audit/windbg-reconnect-command-matrix-20260403.json`
- `registry-research-framework/audit/windbg-reconnect-command-execution-20260403.json`
- `research/notes/windbg-reconnect-command-matrix-20260403.md`
- `registry-research-framework/audit/windbg-pipe-launch-matrix-20260403.json`
- `registry-research-framework/audit/windbg-pipe-launch-execution-20260403.json`
- `research/notes/windbg-pipe-launch-matrix-20260403.md`

## Source-Enrichment Follow-Up

The source-enrichment wave is now real instead of scaffold-only.

What is now present:

- `ReactOS`
- `WRK`
- `System Informer`
- `ADMX`

Current canonical outputs:

- `registry-research-framework/audit/source-enrichment-20260403-192135.json`
- `registry-research-framework/audit/source-enrichment-priority-queue-20260403.json`
- `research/notes/source-enrichment-20260403-192135.md`

What that changes:

- the current 5-key no-hit pilot still has **zero** source support across:
  - `ReactOS`
  - `WRK`
  - `System Informer`
  - `ADMX`
- so these five values are still valid `WinDbg` escalation candidates, but they are no longer just "runtime no-hit"; they are now "runtime no-hit + zero source support so far"
- in parallel, other kernel candidates now have strong cross-source support and can be prioritized more confidently:
  - `system.executive-additional-critical-worker-threads`
  - `system.executive-additional-delayed-worker-threads`
  - `system.executive-uuid-sequence-number`
  - `power.control.hiberboot-enabled`

That means the repo now has two honest truths at once:

- the current VMware named-pipe `WinDbg` contract is exhausted for classification-grade arbitration
- the source-enrichment queue is now good enough to drive the next runtime spend instead of guessing

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

1. treat the current VMware named-pipe WinDbg contract as characterized and exhausted for classification-grade arbitration
2. only return to single-key arbiter revalidation after the transport contract itself changes
3. until then, do not promote WinDbg no-hit/no-read conclusions for these 5 values
4. use the refreshed source-enrichment priority queue to decide which non-pilot candidates get runtime or WinDbg time next
