# Blocked Worklist

Generated: `2026-04-13T03:45:01.939631Z`

Blocked candidates: `18`

## Lane Summary

- `ghidra`: 5
- `intentional-hold`: 5
- `restore-story`: 1
- `runtime-trace`: 7

## Candidates

### `policy.system.enable-virtualization`

- Lane: `intentional-hold`
- Feature area: `Policy System Registry`
- Key path: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`
- Value name: `EnableVirtualization`
- Blockers: `enable-virtualization-research-only-raw-policy-system-value`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.allow-audio-to-enable-execution-required-power-requests`

- Lane: `ghidra`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Blockers: `audio-execution-required-init-walker-not-symbol-resolved`, `audio-execution-required-megatrigger-etw-no-hit-current-build`, `audio-execution-required-no-primary-current-build-doc`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.allow-system-required-power-requests`

- Lane: `ghidra`
- Feature area: `Control Power Requests`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowSystemRequiredPowerRequests`
- Blockers: `system-execution-required-init-walker-not-symbol-resolved`, `system-execution-required-wpr-boot-no-hit-current-build`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.hiber-file-size-percent`

- Lane: `intentional-hold`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `HiberFileSizePercent`
- Blockers: `hiber-file-size-percent-research-only-raw-power-manager-value`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.hibernate-enabled-default`

- Lane: `intentional-hold`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `HibernateEnabledDefault`
- Blockers: `hibernate-enabled-default-hibernate-trigger-not-available-on-current-vm`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.power-request-override-subtree`

- Lane: `restore-story`
- Feature area: `Power Request Override Routing`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Value name: `(subtree root, Driver, Process, Service)`
- Blockers: `powerrequestoverride-restore-story-unproven-subtree-presence-only`, `powerrequestoverride-static-context-adjacent-not-leaf-specific`, `powerrequestoverride-subtree-leaf-semantics-unresolved`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `power.control.power-watchdog-timeout-cluster`

- Lane: `ghidra`
- Feature area: `Control Power Watchdog Defaults`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `PowerWatchdogDrvSetMonitorTimeoutMsec`
- Blockers: `powerwatchdog-timeout-family-no-current-build-string-or-symbol-hit`, `powerwatchdog-timeout-family-no-primary-current-build-doc`, `powerwatchdog-timeout-family-runtime-read-unresolved`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.control.timer-rebase-threshold-on-drips-exit`

- Lane: `intentional-hold`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `TimerRebaseThresholdOnDripsExit`
- Blockers: `timer-rebase-threshold-drips-trigger-not-available-on-current-vm`
- Next action hint: Treat as environment-limited or intentional hold unless a safer lane becomes available.

### `power.control.ttm-enabled`

- Lane: `intentional-hold`
- Feature area: `Raw Power Manager Registry`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `TtmEnabled`
- Blockers: `ttmenabled-boot-unsafe-dedicated-boot-lane-required`, `ttmenabled-boot-unsafe-on-isolated-pilot-profile`, `ttmenabled-no-primary-current-build-doc`, `ttmenabled-reader-unresolved`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.control.win32k-callout-watchdog-timeout-seconds`

- Lane: `runtime-trace`
- Feature area: `Control Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `Win32kCalloutWatchdogTimeoutSeconds`
- Blockers: `win32k-callout-watchdog-bounded-s1-registry-etw-no-hit-current-build`, `win32k-callout-watchdog-no-primary-current-build-doc`, `win32k-callout-watchdog-override-semantics-unproven`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `power.session-watchdog-timeouts`

- Lane: `ghidra`
- Feature area: `Directed Power Watchdog Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `WatchdogResumeTimeout / WatchdogSleepTimeout`
- Blockers: `power-session-watchdog-timeouts-exact-runtime-read-unresolved`, `power-session-watchdog-timeouts-specific-caller-unresolved`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.session-win32-callout-watchdog-bugcheck-enabled`

- Lane: `runtime-trace`
- Feature area: `Session Manager Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `Win32CalloutWatchdogBugcheckEnabled`
- Blockers: `win32-callout-watchdog-bugcheck-no-primary-current-build-doc`, `win32-callout-watchdog-bugcheck-procmon-saveas-timeout-on-bounded-callout-lane`, `win32-callout-watchdog-bugcheck-semantics-unproven`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-dpc-watchdog-control-cluster`

- Lane: `runtime-trace`
- Feature area: `Session Manager Kernel DPC Watchdog Control Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DPCTimeout`
- Blockers: `dpc-watchdog-control-live-zero-state-conflicts-with-repo-docs`, `dpc-watchdog-control-no-primary-current-build-doc`, `dpc-watchdog-control-runtime-read-unresolved`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-dpc-watchdog-profile-cluster`

- Lane: `ghidra`
- Feature area: `Session Manager Kernel DPC Watchdog Profile`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DpcWatchdogProfileBufferSizeBytes`
- Blockers: `dpc-watchdog-profile-conditional-initialization-unproven`, `dpc-watchdog-profile-live-mixed-state-conflicts-with-repo-docs`, `dpc-watchdog-profile-no-primary-current-build-doc`, `dpc-watchdog-profile-wpr-boot-no-hit-current-build`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `system.kernel-long-dpc-threshold-cluster`

- Lane: `runtime-trace`
- Feature area: `Session Manager Kernel DPC Scheduling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `LongDpcQueueThreshold`
- Blockers: `long-dpc-threshold-no-primary-current-build-doc`, `long-dpc-threshold-procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane`, `long-dpc-threshold-wpr-boot-no-hit-current-build`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.force-bugcheck-for-dpc-watchdog`

- Lane: `runtime-trace`
- Feature area: `Session Manager Kernel DPC Watchdog`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `ForceBugcheckForDpcWatchdog`
- Blockers: `force-bugcheck-dpc-watchdog-no-primary-current-build-doc`, `force-bugcheck-dpc-watchdog-semantics-unproven`, `force-bugcheck-dpc-watchdog-wpr-boot-no-hit-current-build`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.global-timer-resolution-requests`

- Lane: `runtime-trace`
- Feature area: `Session Manager Kernel Timer Resolution`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `GlobalTimerResolutionRequests`
- Blockers: `global-timer-resolution-no-primary-current-build-doc`, `global-timer-resolution-wpr-boot-no-hit-current-build`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.timer-check-flags`

- Lane: `runtime-trace`
- Feature area: `Session Manager Kernel Timer Diagnostics`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `TimerCheckFlags`
- Blockers: `timer-check-flags-modern-bit-semantics-unproven`, `timer-check-flags-no-primary-current-build-doc`, `timer-check-flags-wpr-boot-no-hit-current-build`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.
