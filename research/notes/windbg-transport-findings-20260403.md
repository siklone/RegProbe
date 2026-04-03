# WinDbg Transport Findings

Date: `2026-04-03`

## Core Finding

- `attach-after-shell` is not an arbiter-capable attach path right now.
- The corrected one-off run reclassified it as `attach_ok_no_debuggee`.
- In practice this means the debugger process opens the pipe and stays in `Waiting to reconnect...` without a live kernel debuggee.
- A first serial-config matrix narrowed the real transport problem:
  - `guest-restart` + `kd` now reaches a live kernel connection in `4/4` tested variants
  - `3/4` variants finished `transport_ok`
  - the weakest variant so far is `break_on_connect=none` + `tryNoRxLoss=TRUE`

## What We Learned

- `kd` + `guest-restart` can briefly reach `Kernel Debugger connection established`, but the session still degrades into transport failure.
- `kd` + `attach-after-shell` preserves VM shell health, but that path is only a `no_debuggee` waiting state.
- `cdb` did not improve the situation; the first two frontend trials ended as `missing-log` / `transport_unstable`.
- The serial-config sweep shows the transport problem is no longer "any guest restart is broken".
- The narrower question is now which `guest-restart` serial variant preserves command/break-in roundtrips reproducibly.
- A direct `breakin-once` run on the strongest current base (`guest-restart` + `kd` + `bonc` + `tryNoRxLoss=FALSE`) still ended `attach_ok_command_not_executed`.
- That means kernel connectivity is now the stronger part of the lane; command/break-in roundtrip is the remaining weak point.

## Practical Outcome

- Do not widen the single-key WinDbg arbiter lane yet.
- Do not interpret `attach-after-shell` as a successful live debug transport.
- Treat `guest-restart` as the current best transport base, not `attach-after-shell`.
- The next infrastructure phase should stay on `guest-restart` break-in/command-roundtrip validation plus pipe endpoint/start-order testing, not more key-specific RE.

## Evidence

- transport findings surface: [windbg-transport-findings-20260403.json](C:\r\registry-research-framework\audit\windbg-transport-findings-20260403.json)
- transport matrix baseline: [windbg-transport-matrix-20260403.json](C:\r\registry-research-framework\audit\windbg-transport-matrix-20260403.json)
- serial-config matrix: [windbg-serial-config-matrix-20260403.json](C:\r\registry-research-framework\audit\windbg-serial-config-matrix-20260403.json)
- serial-config execution detail: [windbg-serial-config-execution-20260403.json](C:\r\registry-research-framework\audit\windbg-serial-config-execution-20260403.json)
- guest-restart breakin-once bundle: [windbg-transport-bundle-guest-restart-breakin-once-kd-bonc-rxloss-false-20260403.json](C:\r\registry-research-framework\audit\windbg-transport-bundle-guest-restart-breakin-once-kd-bonc-rxloss-false-20260403.json)
- guest-restart breakin-once summary: [summary.json](C:\r\evidence\files\vm-tooling-staging\windbg-boot-registry-trace-20260403-145133\summary.json)
