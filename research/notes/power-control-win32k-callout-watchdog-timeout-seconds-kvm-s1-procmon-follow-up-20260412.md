# Power Control Win32kCalloutWatchdogTimeoutSeconds KVM S1 Procmon Follow-up

Date: 2026-04-12
Candidate: `power.control.win32k-callout-watchdog-timeout-seconds`
Guest: `regprobe-win11-25h2-session`

## Objective
- try the remaining runtime-trace lane for `Win32kCalloutWatchdogTimeoutSeconds`
- add a reusable KVM-side `watchdog-s1-callout` trigger profile instead of reusing the broader watchdog power burst
- separate “we have not tried a bounded sleep/resume lane” from “the current Procmon export path still fails before CSV review”

## Result
- the dedicated KVM S1 Procmon replay reached live guest execution through the new `watchdog-s1-callout` trigger
- Procmon surfaced a stale boot-time-data prompt from a previous instance and then a later dialog saying `The specified log file does not exist.`
- the uploaded probe stage ended at `exception` with `Procmon SaveAs exited with code 1.`
- no CSV, hits CSV, or normalized bundle were produced
- the guest-side result text still reported `RESTORED={"path_exists":true,"value_exists":false,...}`, so the lane completed its restore step and preserved the missing-value baseline

## Artifacts
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a-launcher-stage.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a-probe-stage.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a-summary.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412a/host-review.json`

## Short Take
- the candidate is no longer missing a bounded KVM S1 Procmon attempt
- the remaining runtime blocker is now narrower than generic trigger selection: current Procmon export stability on this S1 watchdog lane is still broken
- the next attempt should clean or bypass Procmon boot-log residue before repeating the same lane
