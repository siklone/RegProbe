# power.disable-cpu-idle-states decision gate review - 2026-04-12

## Summary

`power.disable-cpu-idle-states` was still blocked by the generic `documentation-first-review` gate even though the retained record, verdict, and VM evidence already describe a converged Class A package.

The earlier failed rebooted benchmark and write-diagnostics attempts remain important incident history, but they are superseded for the gate decision by the Defender-excluded stepwise package on `RegProbe-Baseline-20260328`. That package completed baseline read, candidate write, rebooted post-boot read, WPR start and stop, guest ETL existence check, host copy-back, restore, and post-restore verification.

## Decision

Promote the existing app-mapped profile as an advanced-only surface.

This does not make the tweak a general-use posture. The raw registry bundle can raise heat and power use, and the Windows default remains the safer profile for general users and battery-sensitive systems. The decision is narrower: the app already ships this advanced profile, the registry apply/restore story is machine-checkable, and the final VM package removes the earlier tooling/orchestration blocker.

## Resulting policy

- `decision.apply_allowed = true`
- `recommended_for_general_users = false`
- `current-app-profile.apply_allowed = true`
- Windows default stays unset and remains the general-user profile
