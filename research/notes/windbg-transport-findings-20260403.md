# WinDbg Transport Findings

Date: `2026-04-03`

## Core Finding

- `attach-after-shell` is not an arbiter-capable attach path right now.
- The corrected one-off run reclassified it as `attach_ok_no_debuggee`.
- In practice this means the debugger process opens the pipe and stays in `Waiting to reconnect...` without a live kernel debuggee.

## What We Learned

- `kd` + `guest-restart` can briefly reach `Kernel Debugger connection established`, but the session still degrades into transport failure.
- `kd` + `attach-after-shell` preserves VM shell health, but that path is only a `no_debuggee` waiting state.
- `cdb` did not improve the situation; the first two frontend trials ended as `missing-log` / `transport_unstable`.

## Practical Outcome

- Do not widen the single-key WinDbg arbiter lane yet.
- Do not interpret `attach-after-shell` as a successful live debug transport.
- The next infrastructure phase should be a `serial-config-matrix` plus attach/start-order testing, not more key-specific RE.

## Evidence

- Corrected `attach-after-shell` one-off: [summary.json](C:\r\evidence\files\vm-tooling-staging\windbg-boot-registry-trace-20260403-124230\summary.json)
- KD guest-restart transport error: [summary.json](C:\r\evidence\files\vm-tooling-staging\windbg-boot-registry-trace-20260403-122047\summary.json)
- CDB guest-restart frontend trial: [summary.json](C:\r\evidence\files\vm-tooling-staging\windbg-boot-registry-trace-20260403-123648\summary.json)
- CDB cold-boot frontend trial: [summary.json](C:\r\evidence\files\vm-tooling-staging\windbg-boot-registry-trace-20260403-123909\summary.json)
