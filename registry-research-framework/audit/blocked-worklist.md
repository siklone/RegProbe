# Blocked Worklist

Generated: `2026-04-18T16:57:00.276302Z`

Blocked candidates: `18`

## Actionability

- `hold`: 18

## Lane Summary

- `intentional-hold`: 18 | first: `policy.system.enable-virtualization` | `winopt research list-blocked --worklist --lane intentional-hold` | Treat as environment-limited or intentional hold unless a safer lane becomes available.

## Top Holds

- `policy.system.enable-virtualization` (`intentional-hold`, score=9, blockers=1)
- `power.control.hiber-file-size-percent` (`intentional-hold`, score=9, blockers=1)
- `power.control.hibernate-enabled-default` (`intentional-hold`, score=9, blockers=1)
- `power.control.timer-rebase-threshold-on-drips-exit` (`intentional-hold`, score=9, blockers=1)
- `power.control.allow-system-required-power-requests` (`intentional-hold`, score=8, blockers=2)

## Candidates

### `policy.system.enable-virtualization`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `9`
- Feature area: `Policy System Registry`
- Key path: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`
- Value name: `EnableVirtualization`
- Blockers: `enable-virtualization-research-only-raw-policy-system-value`
- Recent audit artifacts: `registry-research-framework/audit/policy-system-enable-virtualization-wpr-qga-runtime-read-20260413.json`, `registry-research-framework/audit/policy-system-enable-virtualization-path-aware-follow-up-20260331.json`, `registry-research-framework/audit/policy-system-enable-virtualization-path-aware-follow-up-20260330.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.hiber-file-size-percent`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `9`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `HiberFileSizePercent`
- Blockers: `hiber-file-size-percent-research-only-raw-power-manager-value`
- Recent audit artifacts: `registry-research-framework/audit/power-control-hiber-file-size-percent-wpr-qga-runtime-read-20260412.json`, `registry-research-framework/audit/power-control-hiber-file-size-percent-lightweight-runtime-20260330.json`, `registry-research-framework/audit/hiber-file-size-percent-stepwise-runtime-audit-20260408.md`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.hibernate-enabled-default`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `9`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `HibernateEnabledDefault`
- Blockers: `hibernate-enabled-default-hibernate-trigger-not-available-on-current-vm`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.timer-rebase-threshold-on-drips-exit`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `9`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `TimerRebaseThresholdOnDripsExit`
- Blockers: `timer-rebase-threshold-drips-trigger-not-available-on-current-vm`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.allow-system-required-power-requests`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `8`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowSystemRequiredPowerRequests`
- Blockers: `intentional-hold`, `system-execution-required-no-current-build-registry-seeding-path`
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-control-allow-system-required-wpr-qga-no-hit-20260412.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Wait for a safer environment or a clearer product surface before probing.

### `power.control.allow-audio-to-enable-execution-required-power-requests`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `7`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Blockers: `audio-execution-required-no-current-build-registry-seeding-path`, `audio-execution-required-no-primary-current-build-doc`, `intentional-hold`
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-control-allow-system-required-wpr-qga-no-hit-20260412.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.session-watchdog-timeouts`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `7`
- Feature area: `Directed Power Watchdog Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `WatchdogResumeTimeout / WatchdogSleepTimeout`
- Blockers: `power-session-watchdog-timeouts-intentional-hold-validation-environment-limitation`, `power-session-watchdog-timeouts-no-current-build-exact-registry-read`, `power-session-watchdog-timeouts-no-current-build-registry-seeding-caller`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Wait for a safer environment or a clearer product surface before probing.

### `system.kernel.global-timer-resolution-requests`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `7`
- Feature area: `Session Manager Kernel Timer Resolution`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Blockers: `global-timer-resolution-intentional-hold-no-current-build-pivot`, `global-timer-resolution-no-primary-current-build-doc`, `global-timer-resolution-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-runtime-sprint-20260418.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.control.power-watchdog-timeout-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Control Power Watchdog Defaults`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `PowerWatchdogDrvSetMonitorTimeoutMsec`
- Blockers: `powerwatchdog-timeout-family-intentional-hold-no-current-build-pivot`, `powerwatchdog-timeout-family-no-current-build-string-or-symbol-hit`, `powerwatchdog-timeout-family-no-primary-current-build-doc`, `powerwatchdog-timeout-family-runtime-read-unresolved`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.ttm-enabled`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `TtmEnabled`
- Blockers: `ttmenabled-boot-unsafe-dedicated-boot-lane-required`, `ttmenabled-boot-unsafe-on-isolated-pilot-profile`, `ttmenabled-no-primary-current-build-doc`, `ttmenabled-reader-unresolved`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.control.win32k-callout-watchdog-timeout-seconds`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Control Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `Win32kCalloutWatchdogTimeoutSeconds`
- Blockers: `win32k-callout-watchdog-bounded-s1-registry-etw-no-hit-current-build`, `win32k-callout-watchdog-intentional-hold-no-current-build-pivot`, `win32k-callout-watchdog-no-primary-current-build-doc`, `win32k-callout-watchdog-override-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.session-win32-callout-watchdog-bugcheck-enabled`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `Win32CalloutWatchdogBugcheckEnabled`
- Blockers: `win32-callout-watchdog-bugcheck-intentional-hold-adjacent-sibling-without-current-build-pivot`, `win32-callout-watchdog-bugcheck-no-primary-current-build-doc`, `win32-callout-watchdog-bugcheck-procmon-saveas-timeout-on-bounded-callout-lane`, `win32-callout-watchdog-bugcheck-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-long-dpc-threshold-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Kernel DPC Scheduling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `LongDpcQueueThreshold`
- Blockers: `long-dpc-threshold-intentional-hold-no-current-build-pivot`, `long-dpc-threshold-no-primary-current-build-doc`, `long-dpc-threshold-procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane`, `long-dpc-threshold-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.force-bugcheck-for-dpc-watchdog`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Kernel DPC Watchdog`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `ForceBugcheckForDpcWatchdog`
- Blockers: `force-bugcheck-dpc-watchdog-intentional-hold-safety-sensitive-without-current-build-pivot`, `force-bugcheck-dpc-watchdog-no-primary-current-build-doc`, `force-bugcheck-dpc-watchdog-semantics-unproven`, `force-bugcheck-dpc-watchdog-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.timer-check-flags`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Kernel Timer Diagnostics`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `TimerCheckFlags`
- Blockers: `timer-check-flags-intentional-hold-no-current-build-pivot`, `timer-check-flags-modern-bit-semantics-unproven`, `timer-check-flags-no-primary-current-build-doc`, `timer-check-flags-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-timer-check-flags-wpr-qga-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-timer-check-flags-etw-stackwalk-20260418.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.control.power-request-override-subtree`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Power Request Override Routing`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Value name: `(subtree root, Driver, Process, Service)`
- Blockers: `intentional-hold`, `powerrequestoverride-restore-story-leaf-model-unproven`, `powerrequestoverride-static-context-adjacent-not-leaf-specific`, `powerrequestoverride-subtree-leaf-semantics-unresolved`, `powerrequestoverride-subtree-not-mapped-to-supported-app-surface`
- Recent audit artifacts: `registry-research-framework/audit/power-request-override-runtime-proof-20260418.json`, `registry-research-framework/audit/power-request-override-runtime-audit-20260408.md`, `registry-research-framework/audit/power-request-override-runtime-audit-20260408.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `system.kernel-dpc-watchdog-control-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Session Manager Kernel DPC Watchdog Control Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DPCTimeout`
- Blockers: `dpc-watchdog-control-intentional-hold-no-current-build-pivot`, `dpc-watchdog-control-live-zero-state-conflicts-with-repo-docs`, `dpc-watchdog-control-no-current-build-persisted-seeding-caller-or-exact-query-arm`, `dpc-watchdog-control-no-primary-current-build-doc`, `dpc-watchdog-control-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-dpc-watchdog-profile-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Session Manager Kernel DPC Watchdog Profile`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DpcWatchdogProfileBufferSizeBytes`
- Blockers: `dpc-watchdog-profile-conditional-init-semantics-unproven`, `dpc-watchdog-profile-intentional-hold-mixed-live-state-without-current-build-pivot`, `dpc-watchdog-profile-mixed-current-build-state-conflicts-with-repo-docs`, `dpc-watchdog-profile-no-current-build-exact-registry-read`, `dpc-watchdog-profile-no-primary-current-build-doc`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.
