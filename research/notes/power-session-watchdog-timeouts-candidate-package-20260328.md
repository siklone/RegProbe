# power.session-watchdog-timeouts candidate package - 2026-03-28

## What changed

- Added a draft research record for `power.session-watchdog-timeouts`
- Added a matching v3.1 evidence bundle under `evidence/records/power.session-watchdog-timeouts`
- Promoted the watchdog pair from a loose next-gate note to a normal repo candidate surface

## Why this is still draft-only

The watchdog pair is now evidence-backed, but it is not app-ready:

- there is no shipped RegProbe provider or UI mapping
- there is no direct live read of the exact watchdog values yet
- the strongest semantic lead still depends on repo-side PoFx pseudocode plus current-build Ghidra fallback artifacts

## Why it still matters

The lane is stronger than a speculative intake now:

- clean baseline existence is confirmed
- current-build ntoskrnl string hits and Ghidra fallback artifacts exist
- a reboot-verified boot trace preserved the same 120/300 pair after boot
- shell health stayed intact on the successful baseline

## Current recommendation

Keep the pair together as one candidate lane:

- `WatchdogResumeTimeout`
- `WatchdogSleepTimeout`

Do not split them or ship them as an end-user tweak yet.
