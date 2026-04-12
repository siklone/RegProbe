# Power Session Win32CalloutWatchdogBugcheckEnabled VM Neutral Perf Bench

Date: 2026-04-12
Candidate: `power.session-win32-callout-watchdog-bugcheck-enabled`
Guest: `regprobe-win11-25h2-session`

## Objective
- close the `bench-not-run` lane without exercising a behavior-changing watchdog bugcheck value
- use a semantically neutral DWORD `0`, matching the live KD-observed `nt!PopWin32CalloutWatchdogBugcheckEnabled = 0`
- verify apply and rollback mechanics on the VM while preserving the missing-value baseline

## Result
- applied `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled = DWORD 0`
- verified the value after apply
- measured 3 relative VM samples using service query latency plus registry query latency
- restored the baseline with `restore-baseline`, which deleted the value because it was absent before the run
- verified rollback: the value was absent after restore

## Median Fix
- the first run exposed a bug in `run-perf-bench-guest.ps1`: for 3 samples the median index used `[int]($count / 2)`, and PowerShell rounded `1.5` to `2`
- fixed the median calculation to use `[Math]::Floor($count / 2)`
- reran the bench as `win32-callout-bugcheck-neutral-perf-20260412b`

## Bench Numbers
- baseline median: `7.028 ms`
- applied median: `3.723 ms`
- delta: `-3.305 ms / -47.026%`
- reliability: `relative-neutral-value`

These numbers are not a performance-improvement claim. The samples are intentionally small and VM-local. The useful result is that a neutral-value apply and rollback completed cleanly, so the record no longer needs to be treated as missing any VM bench execution at all.

## Artifacts
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-neutral-perf-20260412b/win32-callout-bugcheck-neutral-perf-20260412b-summary.json`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-neutral-perf-20260412b/win32-callout-bugcheck-neutral-perf-20260412b.json`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-neutral-perf-20260412b/win32-callout-bugcheck-neutral-perf-20260412b.txt`
- `evidence/files/vm-tooling-staging/win32-callout-watchdog-bugcheck-neutral-perf-20260412b/host-review.json`

## Take
- `bench-not-run` can be replaced with a bounded neutral-value VM bench note
- non-default semantics remain unproven because the bench deliberately avoided DWORD `1`
- runtime-read proof remains unresolved and likely belongs to boot/init tracing rather than another Procmon GUI replay
