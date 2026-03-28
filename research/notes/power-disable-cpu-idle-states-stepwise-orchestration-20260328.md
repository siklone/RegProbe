# power.disable-cpu-idle-states stepwise orchestration review - 2026-03-28

## Summary

- The monolithic CPU idle runtime chain was split into four independent stages:
  - Step A: baseline + `WPR start` + candidate write
  - Step B: reboot + settle + post-boot read + `WPR stop`
  - Step C: collect artifacts to the host and repo evidence root
  - Step D: restore baseline + reboot + restore verification
- This made the failing stage visible instead of hiding it behind one large runtime script.

## What broke first

- The first stepwise run failed in Step A at `set-candidate`.
- The guest-side error payload showed the actual failure:
  - `New-Item -Path Registry::HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Power -Force`
  - error: `Cannot delete a subkey tree because the subkey does not exist.`
- That was fixed by replacing the candidate write inside the stepwise payload with `reg.exe add` calls instead of the PowerShell provider path creation.

## What broke second

- After Step A was fixed, the next failure moved to Step B.
- The original reboot primitive failed in two ways:
  - `vmrun restartGuest` returned `Unrecognized command`
  - guest-initiated reboot attempts did not complete a reliable reboot cycle
- That was fixed by switching Step B to the host-driven reboot pattern already proven in the watchdog boot-trace lane:
  - `vmrun stop <vm> soft`
  - fallback `hard` stop if needed
  - `vmrun start`
  - wait for VMware Tools and shell health again

## Final stepwise result

- Final successful session:
  - `evidence/files/vm-tooling-staging/cpu-idle-stepwise-20260328-225327/session.json`
- Step A:
  - candidate bundle applied successfully before reboot
  - `DisableIdleStatesAtBoot=1`
  - `IdleStateTimeout=0`
  - `ExitLatencyCheckEnabled=1`
- Step B:
  - host-driven reboot succeeded with `reboot_mode=soft`
  - post-boot read still showed the candidate bundle in place
  - `WPR stop` returned success
  - but `etl_exists=false`
- Step C:
  - all JSON manifests copied back successfully
  - ETL copy remained negative
- Step D:
  - restore returned the bundle to missing
  - post-restore reboot also kept the baseline state clean

## Why this matters

This pass narrows the remaining blocker again.

The unresolved problem is no longer:

- generic VMware guest execution
- generic host copy-back
- direct registry bundle writes
- the candidate write stage after `WPR start`
- the reboot primitive itself

The remaining ambiguity is now specifically the trace side:

- `WPR stop` reports success
- but the expected ETL file does not materialize at the guest path
- and there is therefore no raw trace to collect to the host

That means the next meaningful CPU idle escalation is no longer another broad runtime retry. It is a focused WPR or trace-materialization investigation, ideally reusable for other rebooted lanes.
