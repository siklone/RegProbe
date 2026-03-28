# power.session-watchdog-timeouts sleep capability - 2026-03-28

## Summary

- `Win25H2Clean` currently exposes only `Standby (S1)` for the watchdog lane.
- `Standby (S3)`, `Hibernate`, and `Standby (S0 Low Power Idle)` are not available in this VMware baseline.
- Shell health stayed clean before and after the probe.

## Source artifacts

- Summary: `evidence/files/vm-tooling-staging/watchdog-sleep-capability-20260328-104406/summary.json`
- Raw text: `evidence/files/vm-tooling-staging/watchdog-sleep-capability-20260328-104406/powercfg-a.txt`

## Result

Available:

- `Standby (S1)`

Unavailable:

- `Standby (S2)`
- `Standby (S3)`
- `Hibernate`
- `Standby (S0 Low Power Idle)`
- `Hybrid Sleep`
- `Fast Startup`

## Why this matters

The watchdog value names suggest that `sleep` and `resume` transitions are the most promising next trigger for an exact live-read attempt. This probe narrows that plan: on `Win25H2Clean`, the realistic in-VM path is an `S1` transition, not `S3`, hibernate, or modern standby.

That means a deeper sleep-state live-read lane may require either:

- an `S1`-specific runtime experiment inside this VM, or
- a different validation environment if `S3` or hibernate semantics become necessary.
