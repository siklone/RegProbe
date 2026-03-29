# power.disable-cpu-idle-states stepwise orchestration review - 2026-03-29

## Summary

- The excluded baseline `RegProbe-Baseline-20260328` is now the canonical starting point for reboot and WPR-sensitive runtime lanes.
- On that baseline, the CPU idle runtime lane completed all explicit substeps:
  - `A`: baseline read + candidate write
  - `B`: reboot + post-boot state confirmation
  - `C1`: WPR start
  - `C2`: WPR stop
  - `C3`: guest-side ETL existence check
  - `C4`: ETL copy-back to the host
  - `D`: restore baseline + post-restore confirmation

## Final session

- Runtime summary:
  - `evidence/files/vm-tooling-staging/cpu-idle-runtime-20260329-015521/summary.json`
- Session manifest:
  - `evidence/files/vm-tooling-staging/cpu-idle-stepwise-20260329-015521/session.json`

## What the final session proved

- The candidate bundle still applied cleanly before reboot:
  - `DisableIdleStatesAtBoot=1`
  - `IdleStateTimeout=0`
  - `ExitLatencyCheckEnabled=1`
- The post-boot read still showed the candidate bundle in place after reboot.
- `WPR start` and `WPR stop` both completed successfully.
- The ETL existed in the guest after `WPR stop`.
- The ETL was copied back to the host successfully.
- The restore phase returned the bundle to the observed baseline, with all three values missing again after the final reboot.

## What changed versus the earlier stepwise package

- Earlier 2026-03-28 stepwise runs exposed and fixed three concrete orchestration issues:
  - the candidate write path
  - the reboot primitive
  - the shell wait threshold after reboot
- On the excluded baseline, those fixes held together and the full chain completed.

## Remaining decision gate

The remaining blocker is no longer guest execution, candidate write, reboot, `WPR stop`, ETL existence, or host copy-back.

The record stays `Class B` because it is still a raw power-manager registry bundle without a supported Microsoft app-ready control surface. The evidence package is now strong and reproducible, but the publish decision is still a product and supportability gate rather than a runtime-orchestration gap.
