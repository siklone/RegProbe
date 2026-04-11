# Power Session Watchdog Timeouts KVM Procmon Power-Burst Follow-up

Date: 2026-04-07
Candidate: `power.session-watchdog-timeouts`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the Session Manager watchdog lane through Linux KVM with a reusable host-driven `watchdog-power-burst` Procmon trigger
- test whether a post-boot power-family burst can finally surface `WatchdogResumeTimeout` or `WatchdogSleepTimeout` without depending on sleep-transition stability
- turn the older one-off power/watchdog trigger idea into a reusable KVM runtime lane that can be rerun from the host

## Result
- the new `watchdog-power-burst` trigger ran end-to-end through the hardened KVM Procmon runner and exported a real `260953`-row CSV
- the replay kept the original `WatchdogSleepTimeout = 300` value intact before and after the burst, but Procmon still reported `MATCH_COUNT=0`
- host-side review also stayed fully negative: `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, `PowerSettingProfile`, `SystemPowerPolicy`, `ShutdownOccurred`, and even the broader `Session Manager\Power` fragment each returned `0` lines
- this does not weaken the lane's semantic story; it narrows the remaining blocker further toward a true missing exact registry-read transport rather than a missing KVM power-family trigger

## Artifacts
- `evidence/files/vm-tooling-staging/watchdog-procmon-kvm-power-burst-20260407a/watchdog-procmon-kvm-power-burst-20260407a.txt`
- `evidence/files/vm-tooling-staging/watchdog-procmon-kvm-power-burst-20260407a/watchdog-procmon-kvm-power-burst-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/watchdog-procmon-kvm-power-burst-20260407a/host-review.json`

## Short Take
- the reusable KVM `watchdog-power-burst` Procmon lane is now real and rerunnable from the host
- it still is not a winning exact-read transport for the watchdog pair
- the watchdog lane remains strong on path, state, and current-build code flow, but still lacks the decisive live trace that would promote it beyond Class B
