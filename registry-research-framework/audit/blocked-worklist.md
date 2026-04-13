# Blocked Worklist

Generated: `2026-04-13T05:18:24.686386Z`

Blocked candidates: `18`

## Lane Summary

- `ghidra`: 5 | first: `power.control.allow-system-required-power-requests` | `winopt research list-blocked --worklist --lane ghidra --top 5` | Continue static RE or Ghidra work until the exact reader or initializer is named.
- `intentional-hold`: 5 | first: `policy.system.enable-virtualization` | `winopt research list-blocked --worklist --lane intentional-hold` | Treat as environment-limited or intentional hold unless a safer lane becomes available.
- `restore-story`: 1 | first: `power.control.power-request-override-subtree` | `winopt research list-blocked --worklist --lane restore-story --top 5` | Prove restore or rollback behavior for the exact subtree or value.
- `runtime-trace`: 7 | first: `system.kernel.global-timer-resolution-requests` | `winopt research list-blocked --worklist --lane runtime-trace --top 5` | Retry runtime capture with a narrower trigger or a more reliable trace lane.

## Top Actionable Candidates

- `power.control.power-request-override-subtree` (`restore-story`, score=37, blockers=3)
- `power.control.allow-system-required-power-requests` (`ghidra`, score=33, blockers=2)
- `power.session-watchdog-timeouts` (`ghidra`, score=33, blockers=2)
- `power.control.allow-audio-to-enable-execution-required-power-requests` (`ghidra`, score=32, blockers=3)
- `power.control.power-watchdog-timeout-cluster` (`ghidra`, score=32, blockers=3)

## Candidates

### `power.control.power-request-override-subtree`

- Lane: `restore-story`
- Actionability: `active`
- Priority score: `37`
- Feature area: `Power Request Override Routing`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Value name: `(subtree root, Driver, Process, Service)`
- Blockers: `powerrequestoverride-restore-story-unproven-subtree-presence-only`, `powerrequestoverride-static-context-adjacent-not-leaf-specific`, `powerrequestoverride-subtree-leaf-semantics-unresolved`
- Recent audit artifacts: `registry-research-framework/audit/power-request-override-runtime-audit-20260408.md`, `registry-research-framework/audit/power-request-override-runtime-audit-20260408.json`
- Suggested command: `winopt research show-blocked power.control.power-request-override-subtree --json`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `power.control.allow-system-required-power-requests`

- Lane: `ghidra`
- Actionability: `active`
- Priority score: `33`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowSystemRequiredPowerRequests`
- Blockers: `system-execution-required-init-walker-not-symbol-resolved`, `system-execution-required-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-control-allow-system-required-wpr-qga-no-hit-20260412.json`
- Suggested command: `winopt research show-blocked power.control.allow-system-required-power-requests --json`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.session-watchdog-timeouts`

- Lane: `ghidra`
- Actionability: `active`
- Priority score: `33`
- Feature area: `Directed Power Watchdog Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `WatchdogResumeTimeout / WatchdogSleepTimeout`
- Blockers: `power-session-watchdog-timeouts-exact-runtime-read-unresolved`, `power-session-watchdog-timeouts-specific-caller-unresolved`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research show-blocked power.session-watchdog-timeouts --json`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.allow-audio-to-enable-execution-required-power-requests`

- Lane: `ghidra`
- Actionability: `active`
- Priority score: `32`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Blockers: `audio-execution-required-init-walker-not-symbol-resolved`, `audio-execution-required-megatrigger-etw-no-hit-current-build`, `audio-execution-required-no-primary-current-build-doc`
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-control-allow-system-required-wpr-qga-no-hit-20260412.json`
- Suggested command: `winopt research show-blocked power.control.allow-audio-to-enable-execution-required-power-requests --json`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.power-watchdog-timeout-cluster`

- Lane: `ghidra`
- Actionability: `active`
- Priority score: `32`
- Feature area: `Control Power Watchdog Defaults`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `PowerWatchdogDrvSetMonitorTimeoutMsec`
- Blockers: `powerwatchdog-timeout-family-no-current-build-string-or-symbol-hit`, `powerwatchdog-timeout-family-no-primary-current-build-doc`, `powerwatchdog-timeout-family-runtime-read-unresolved`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research show-blocked power.control.power-watchdog-timeout-cluster --json`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `system.kernel-dpc-watchdog-profile-cluster`

- Lane: `ghidra`
- Actionability: `active`
- Priority score: `31`
- Feature area: `Session Manager Kernel DPC Watchdog Profile`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DpcWatchdogProfileBufferSizeBytes`
- Blockers: `dpc-watchdog-profile-conditional-initialization-unproven`, `dpc-watchdog-profile-live-mixed-state-conflicts-with-repo-docs`, `dpc-watchdog-profile-no-primary-current-build-doc`, `dpc-watchdog-profile-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel-dpc-watchdog-profile-cluster --json`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `system.kernel.global-timer-resolution-requests`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `28`
- Feature area: `Session Manager Kernel Timer Resolution`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Blockers: `global-timer-resolution-no-primary-current-build-doc`, `global-timer-resolution-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel.global-timer-resolution-requests --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.control.win32k-callout-watchdog-timeout-seconds`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Control Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `Win32kCalloutWatchdogTimeoutSeconds`
- Blockers: `win32k-callout-watchdog-bounded-s1-registry-etw-no-hit-current-build`, `win32k-callout-watchdog-no-primary-current-build-doc`, `win32k-callout-watchdog-override-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research show-blocked power.control.win32k-callout-watchdog-timeout-seconds --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.session-win32-callout-watchdog-bugcheck-enabled`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Session Manager Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `Win32CalloutWatchdogBugcheckEnabled`
- Blockers: `win32-callout-watchdog-bugcheck-no-primary-current-build-doc`, `win32-callout-watchdog-bugcheck-procmon-saveas-timeout-on-bounded-callout-lane`, `win32-callout-watchdog-bugcheck-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked power.session-win32-callout-watchdog-bugcheck-enabled --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-dpc-watchdog-control-cluster`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Session Manager Kernel DPC Watchdog Control Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DPCTimeout`
- Blockers: `dpc-watchdog-control-live-zero-state-conflicts-with-repo-docs`, `dpc-watchdog-control-no-primary-current-build-doc`, `dpc-watchdog-control-runtime-read-unresolved`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel-dpc-watchdog-control-cluster --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-long-dpc-threshold-cluster`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Session Manager Kernel DPC Scheduling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `LongDpcQueueThreshold`
- Blockers: `long-dpc-threshold-no-primary-current-build-doc`, `long-dpc-threshold-procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane`, `long-dpc-threshold-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel-long-dpc-threshold-cluster --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.force-bugcheck-for-dpc-watchdog`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Session Manager Kernel DPC Watchdog`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `ForceBugcheckForDpcWatchdog`
- Blockers: `force-bugcheck-dpc-watchdog-no-primary-current-build-doc`, `force-bugcheck-dpc-watchdog-semantics-unproven`, `force-bugcheck-dpc-watchdog-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel.force-bugcheck-for-dpc-watchdog --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.timer-check-flags`

- Lane: `runtime-trace`
- Actionability: `active`
- Priority score: `27`
- Feature area: `Session Manager Kernel Timer Diagnostics`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `TimerCheckFlags`
- Blockers: `timer-check-flags-modern-bit-semantics-unproven`, `timer-check-flags-no-primary-current-build-doc`, `timer-check-flags-wpr-boot-no-hit-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-timer-check-flags-wpr-qga-no-hit-20260413.json`
- Suggested command: `winopt research show-blocked system.kernel.timer-check-flags --json`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

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
