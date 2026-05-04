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

## Cleanup Rerun
- a follow-up run added explicit Procmon boot-residue cleanup before capture and repeated the same `watchdog-s1-callout` lane as `win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b`
- cleanup removed stale Procmon registry state under `HKCU:\Software\Sysinternals\Process Monitor`: `Logfile`, `SourcePath`, and `FlightRecorder`
- no stale bootlog files were present, and no cleanup errors were reported
- the rerun did not surface the previous boot-log modal, but `Procmon SaveAs` timed out after 180 seconds
- no CSV, hits CSV, or normalized bundle were produced
- the result text again reported `RESTORED={"path_exists":true,"value_exists":false,...}`, preserving the missing-value baseline

## Cleanup Rerun Artifacts
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b-launcher-stage.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b-probe-stage.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b-summary.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/host-review.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-procmon-kvm-s1-20260412b/procmon-s1-b-final.png`

## Updated Take
- stale Procmon boot-log residue is no longer the leading explanation for this lane
- the remaining blocker is Procmon GUI/SaveAs export reliability after the S1 trigger
- the next runtime attempt should bypass Procmon export entirely, for example with a logman/WPR registry ETW lane around the same S1 trigger

## Non-Procmon ETW Follow-up
- added `scripts/vm/run-win32k-callout-watchdog-etw-guest.ps1` to collect `Microsoft-Windows-Kernel-Registry` events with logman around the same `watchdog-s1-callout` trigger
- first ETW smoke attempt showed this Windows build rejects multiple `-p` provider declarations in one `logman create trace` command, so the runner was narrowed to the registry provider only
- second ETW attempt proved logman could start/stop but produced no ETL because no event was forced into the session
- third ETW attempt added a non-target sentinel registry query, produced `win32k-etw-s1-20260412c_000001.etl` at 2,867,200 bytes, and guest-side `tracerpt` completed successfully
- sentinel hits were present (`sentinel_hits_count = 50`), proving the provider and XML search path were live
- target hits remained absent (`hits_count = 0`) for `Win32kCalloutWatchdogTimeoutSeconds` and the `Control\Power` target fragments during this bounded S1 trigger

## ETW Artifacts
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/win32k-etw-s1-20260412c-summary.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/win32k-etw-s1-20260412c.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/win32k-etw-s1-20260412c.etl`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/win32k-etw-s1-20260412c-tracerpt.stdout.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/win32k-etw-s1-20260412c-tracerpt.stderr.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-etw-kvm-s1-20260412c/host-review.json`

## ETW Take
- the S1 trigger remains relevant for exercising the callout path, but the registry read is probably not happening during this bounded S1 window
- runtime-read proof likely needs a boot-time registry trace, WPR boot scenario, or a KD/descriptor consumer breakpoint rather than another Procmon replay
