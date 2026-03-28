# power.session-watchdog-timeouts boot trace - 2026-03-28

## Summary

- The watchdog timeout pair now has a successful boot-trace lane on `Win25H2Clean`.
- The successful run used the `baseline-20260327-regprobe-visible-shell-stable` snapshot.
- A host-driven `vmrun stop soft` plus `start` cycle was required; guest-initiated reboot attempts did not produce a reliable observed reboot cycle under this VMware build.

## Successful run

- Probe folder: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631`
- Summary: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/summary.json`
- ETL placeholder: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/watchdog-timeouts-boot.etl.md`

## Result

- `status = ok`
- `stop_mode = soft`
- Previous boot: `2026-03-25T14:14:21.5000000+03:00`
- Current boot: `2026-03-28T09:09:02.5000000+03:00`
- Raw ETL copied to host staging successfully and represented in-repo by the placeholder markdown
- Shell health stayed good before and after the trace

## Registry state

Baseline and after-boot values matched:

- `WatchdogResumeTimeout = 120`
- `WatchdogSleepTimeout = 300`
- `PowerSettingProfile = 0`

This gives the watchdog lane a clean reboot-verified baseline export on top of the earlier existence, string-hit, and Ghidra evidence.

## Failed attempts kept as evidence

These earlier attempts remain useful because they document the VMware-specific failure modes:

- `watchdog-timeouts-boottrace-20260328-080733`
  - reboot cycle was not observed because the original wait logic only treated exceptions as "tools went away"
- `watchdog-timeouts-boottrace-20260328-083218`
  - guest restart helper path did not lead to an observable reboot; VMware Tools stayed `running`
- `watchdog-timeouts-boottrace-20260328-084847`
  - `restartGuest` is not recognized by the current `vmrun` build

## Next step

The next useful pass is a targeted runtime/static merge for the `power.session-watchdog-timeouts` lane:

- use the new boot-trace baseline as the runtime/behavior anchor
- keep `WatchdogResumeTimeout` and `WatchdogSleepTimeout` together as one pair
- defer the `Additional*WorkerThreads` lane until after the watchdog pair is classified
