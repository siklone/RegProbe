# Kernel/Power Net-New Existence Probe

Date: 2026-03-28

Source routing note: `research/notes/kernel-power-96-key-routing-20260327.md`

Manifest: `registry-research-framework/audit/kernel-power-net-new-candidates-20260328.json`

Batch results:

- Summary: `evidence/files/vm-tooling-staging/registry-batch-existence-20260328-000828/summary.json`
- JSON results: `evidence/files/vm-tooling-staging/registry-batch-existence-20260328-000828/results.json`
- CSV results: `evidence/files/vm-tooling-staging/registry-batch-existence-20260328-000828/results.csv`
- Follow-up queue: `registry-research-framework/audit/kernel-power-net-new-follow-up-20260328.json`

## Why this pass exists

Before promoting any of Hooke's net-new kernel and power values into the heavy VM queue, the repo needed a safe baseline pass that answered one simple question:

- does the parent path exist?
- does the value exist?
- if it exists, what is its current type and baseline preview?

This lane stayed read-only. It did not write candidate values, did not reboot the guest, and did not run ETW, Procmon, or WPR.

## Execution details

- VM: `Win25H2Clean`
- Snapshot: `baseline-20260327-regprobe-visible-shell-stable`
- Probe script: `scripts/vm/run-registry-batch-existence-probe.ps1`
- Manifest size: `20` net-new candidates
- Result: `status = ok`
- Shell outcome: healthy before and after the batch
- Incident log impact: none

## High-level result

- Total candidates: `20`
- Parent path exists: `20 / 20`
- Value exists: `9 / 20`
- Read errors: `0`

Family split:

- `policy-system`: `0 / 1` values exist
- `power-control`: `2 / 2` values exist
- `session-manager-executive`: `3 / 7` values exist
- `session-manager-io`: `1 / 4` values exist
- `session-manager-power`: `3 / 6` values exist

## Values that already exist on the clean baseline

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\CustomizeDuringSetup = 1`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion = 4`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads = 0`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalDelayedWorkerThreads = 0`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber = 25272111`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD = 0`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\PowerSettingProfile = 0`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout = 120`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout = 300`

## Values whose parent path exists but the value is absent by default

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\KernelWorkerTestFlags`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\MaximumKernelWorkerThreads`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride`
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled`

## Recommended next queue

The best first follow-up set is the `9` values that already exist on the clean baseline. They are the easiest candidates to move into documentation, static analysis, and later runtime confirmation without guessing a synthetic write first.

Recommended order:

1. `CustomizeDuringSetup`
2. `SourceSettingsVersion`
3. `PowerSettingProfile`
4. `WatchdogResumeTimeout`
5. `WatchdogSleepTimeout`
6. `AdditionalCriticalWorkerThreads`
7. `AdditionalDelayedWorkerThreads`
8. `UuidSequenceNumber`
9. `AllowRemoteDASD`

The `11` path-only values should stay in docs/static triage until we have a stronger reason to write them on the VM.

## Guardrails carried forward

- Treat every `HKLM\SYSTEM` value in this batch as a kernel or boot-adjacent lane.
- Do not use Frida for those values.
- Escalate to ETW, WPR, or deeper static work only after the existence-first queue has been narrowed.
