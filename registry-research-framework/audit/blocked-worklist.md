# Blocked Worklist

Generated: `2026-05-05T13:16:31.734596Z`

Blocked candidates: `51`

## Actionability

- `hold`: 51

## Lane Summary

- `intentional-hold`: 18 | first: `policy.system.enable-virtualization` | `winopt research list-blocked --worklist --lane intentional-hold` | Treat as environment-limited or intentional hold unless a safer lane becomes available.
- `validation-proof`: 33 | first: `misc.disable-edge-features` | `winopt research list-blocked --worklist --lane validation-proof` | Review blockers manually and choose the next evidence lane.

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
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-control-allow-system-required-wpr-qga-zero-exact-target-hits-20260412.json`
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
- Recent audit artifacts: `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`, `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`, `registry-research-framework/audit/power-request-override-reader-binding-execution-manifest-20260419.md`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.control.power-request-override-subtree`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `7`
- Feature area: `Power Request Override Routing`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Value name: `(subtree root, Driver, Process, Service)`
- Blockers: `intentional-hold`, `powerrequestoverride-static-context-adjacent-not-leaf-specific`, `powerrequestoverride-subtree-live-reader-binding-open-question`
- Recent audit artifacts: `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`, `registry-research-framework/audit/power-request-override-runtime-proof-20260418.json`, `registry-research-framework/audit/power-request-override-runtime-audit-20260408.md`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Continue static RE or Ghidra work until the exact reader or initializer is named.

### `power.session-watchdog-timeouts`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `7`
- Feature area: `Directed Power Watchdog Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `WatchdogResumeTimeout / WatchdogSleepTimeout`
- Blockers: `power-session-watchdog-timeouts-intentional-hold-validation-environment-limitation`, `power-session-watchdog-timeouts-no-current-build-registry-seeding-caller`, `power-session-watchdog-timeouts-sleep-side-exact-registry-read-missing`
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
- Blockers: `global-timer-resolution-clean-wpr-subtree-only-no-value-hit-current-build`, `global-timer-resolution-intentional-hold-no-current-build-pivot`, `global-timer-resolution-no-primary-current-build-doc`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-wpr-qga-timeout-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-global-timer-resolution-requests-runtime-sprint-20260418.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.control.power-watchdog-timeout-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Control Power Watchdog Defaults`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `PowerWatchdogDrvSetMonitorTimeoutMsec`
- Blockers: `powerwatchdog-timeout-family-helper-query-only-single-member`, `powerwatchdog-timeout-family-intentional-hold-no-current-build-pivot`, `powerwatchdog-timeout-family-no-current-build-string-or-symbol-hit`, `powerwatchdog-timeout-family-no-primary-current-build-doc`
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
- Blockers: `ttmenabled-boot-unsafe-dedicated-boot-lane-required`, `ttmenabled-boot-unsafe-on-isolated-pilot-profile`, `ttmenabled-no-primary-current-build-doc`, `ttmenabled-reader-open-question`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.control.win32k-callout-watchdog-timeout-seconds`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Control Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `Win32kCalloutWatchdogTimeoutSeconds`
- Blockers: `win32k-callout-watchdog-helper-query-only-not-boot-consumer`, `win32k-callout-watchdog-intentional-hold-no-current-build-pivot`, `win32k-callout-watchdog-no-primary-current-build-doc`, `win32k-callout-watchdog-override-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json`, `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `power.session-win32-callout-watchdog-bugcheck-enabled`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Power Watchdog Sibling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
- Value name: `Win32CalloutWatchdogBugcheckEnabled`
- Blockers: `win32-callout-watchdog-bugcheck-intentional-hold-adjacent-sibling-without-current-build-pivot`, `win32-callout-watchdog-bugcheck-no-primary-current-build-doc`, `win32-callout-watchdog-bugcheck-procmon-saveas-timeout-on-bounded-callout-lane`, `win32-callout-watchdog-bugcheck-semantics-unproven`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel-long-dpc-threshold-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Kernel DPC Scheduling`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `LongDpcQueueThreshold`
- Blockers: `long-dpc-threshold-intentional-hold-no-current-build-pivot`, `long-dpc-threshold-no-primary-current-build-doc`, `long-dpc-threshold-procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane`, `long-dpc-threshold-wpr-boot-zero-exact-target-hits-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Retry runtime capture with a narrower trigger or a more reliable trace lane.

### `system.kernel.force-bugcheck-for-dpc-watchdog`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `6`
- Feature area: `Session Manager Kernel DPC Watchdog`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `ForceBugcheckForDpcWatchdog`
- Blockers: `force-bugcheck-dpc-watchdog-intentional-hold-safety-sensitive-without-current-build-pivot`, `force-bugcheck-dpc-watchdog-no-primary-current-build-doc`, `force-bugcheck-dpc-watchdog-semantics-unproven`, `force-bugcheck-dpc-watchdog-wpr-boot-zero-exact-target-hits-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `system.kernel-dpc-watchdog-control-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Session Manager Kernel DPC Watchdog Control Timeouts`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DPCTimeout`
- Blockers: `dpc-watchdog-control-intentional-hold-no-current-build-pivot`, `dpc-watchdog-control-live-zero-state-conflicts-with-repo-docs`, `dpc-watchdog-control-no-current-build-persisted-seeding-caller-or-exact-query-arm`, `dpc-watchdog-control-no-primary-current-build-doc`, `dpc-watchdog-control-wpr-boot-zero-exact-target-hits-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `system.kernel-dpc-watchdog-profile-cluster`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Session Manager Kernel DPC Watchdog Profile`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `DpcWatchdogProfileBufferSizeBytes`
- Blockers: `dpc-watchdog-profile-conditional-init-semantics-unproven`, `dpc-watchdog-profile-intentional-hold-mixed-live-state-without-current-build-pivot`, `dpc-watchdog-profile-mixed-current-build-state-conflicts-with-repo-docs`, `dpc-watchdog-profile-no-persisted-boot-consumer-proof`, `dpc-watchdog-profile-no-primary-current-build-doc`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `system.kernel.timer-check-flags`

- Lane: `intentional-hold`
- Actionability: `hold`
- Priority score: `5`
- Feature area: `Session Manager Kernel Timer Diagnostics`
- Key path: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value name: `TimerCheckFlags`
- Blockers: `timer-check-flags-etw-stackwalk-helper-only-no-target-read`, `timer-check-flags-intentional-hold-no-current-build-pivot`, `timer-check-flags-modern-bit-semantics-unproven`, `timer-check-flags-no-primary-current-build-doc`, `timer-check-flags-wpr-boot-zero-exact-target-hits-current-build`
- Recent audit artifacts: `registry-research-framework/audit/system-kernel-timer-check-flags-wpr-qga-zero-exact-target-hits-20260413.json`, `registry-research-framework/audit/system-kernel-timer-check-flags-wpr-qga-no-hit-20260413.json`, `registry-research-framework/audit/system-kernel-timer-check-flags-etw-stackwalk-20260418.json`
- Suggested command: `winopt research list-blocked --worklist --lane intentional-hold`
- Next action hint: Find a primary current-build Microsoft source or explicitly accept research-only status.

### `misc.disable-edge-features`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Microsoft Edge Policy Bundle`
- Key path: `HKLM\Software\Policies\Microsoft\Edge + HKLM/HKCU\Software\Policies\Microsoft\Windows\EdgeUI`
- Value name: `PolicyBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `misc.disable-office-telemetry`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Microsoft Office Telemetry Policy Bundle`
- Key path: `HKCU\Software\Policies\Microsoft\Office\16.0\OSM + related Office policy paths`
- Value name: `PolicyBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `misc.disable-onedrive`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `OneDrive Policy and Explorer Bundle`
- Key path: `HKLM\Software\Policies\Microsoft\Windows\OneDrive + HKLM\SOFTWARE\Microsoft\OneDrive + HKCU\Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}`
- Value name: `PolicyBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `misc.disable-visual-studio-telemetry`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Visual Studio Telemetry Policy Bundle`
- Key path: `HKLM\SOFTWARE\Policies\Microsoft\VisualStudio + HKLM\SOFTWARE\Microsoft\VSCommon`
- Value name: `PolicyBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `misc.disable-vscode-telemetry`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `VS Code User Settings Profile`
- Key path: `%APPDATA%\Code\User\settings.json`
- Value name: `ManagedKeysProfile`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `misc.optimize-7zip-settings`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `7-Zip User Options`
- Key path: `HKCU\Software\7-Zip\Options`
- Value name: `OptionBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.audio-disable-ducking`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Audio Communications Preference`
- Key path: `HKCU\Software\Microsoft\Multimedia\Audio`
- Value name: `UserDuckingPreference`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.audio-disable-enhancements`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Audio Enhancement Flags`
- Key path: `HKCU\Software\Microsoft\Windows\CurrentVersion\Audio + HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render`
- Value name: `EnhancementBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.keyboard-disable-language-hotkey`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Keyboard Layout Toggle Bundle`
- Key path: `HKCU\Keyboard Layout\Toggle`
- Value name: `ToggleBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.keyboard-optimize-repeat`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Keyboard Repeat Profile`
- Key path: `HKCU\Control Panel\Keyboard + HKCU\Control Panel\Desktop`
- Value name: `ProfileBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.mouse-disable-acceleration`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Mouse Acceleration Profile`
- Key path: `HKCU\Control Panel\Mouse`
- Value name: `ProfileBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `peripheral.mouse-disable-throttle`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `Raw Mouse Throttle Bundle`
- Key path: `HKCU\Control Panel\Mouse`
- Value name: `ProfileBundle`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `power.disable-superfetch`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-2`
- Feature area: `SysMain Service Stop Command`
- Key path: `sc.exe SysMain`
- Value name: `ServiceState`
- Blockers: `The card still lives only in the first-party provider and has not been promoted into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `cleanup.component-store`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `DISM Component Store Cleanup`
- Key path: `DISM.exe /Online /Cleanup-Image`
- Value name: `CleanupAction`
- Blockers: `The card still lives only in the first-party cleanup provider and has not been promoted into the research-provider surface.`, `This is an on-demand maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.directx-shader-cache`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Shader Cache Cleanup`
- Key path: `%LOCALAPPDATA%\D3DSCache; %LOCALAPPDATA%\NVIDIA\DXCache; %LOCALAPPDATA%\NVIDIA\GLCache; %LOCALAPPDATA%\NVIDIA Corporation\NV_Cache; %LOCALAPPDATA%\AMD\DXCache; %LOCALAPPDATA%\Intel\DXCache`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.eventlog-system`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `System Event Log Cleanup`
- Key path: `%SystemRoot%\System32\wevtutil.exe`
- Value name: `SystemLogAction`
- Blockers: `Clearing the event log has no rollback path.`, `The card has not yet been promoted from the first-party system provider into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.font-cache`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Font Cache Cleanup`
- Key path: `%WINDIR%\ServiceProfiles\LocalService\AppData\Local\FontCache; %WINDIR%\System32\FNTCACHE.DAT`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.memory-dumps`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Crash Dump Cleanup`
- Key path: `%WINDIR%\MEMORY.DMP; %WINDIR%\Minidump`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.prefetch-files`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Prefetch Cleanup`
- Key path: `%WINDIR%\Prefetch`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.product-key`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `slmgr Product Key Cleanup`
- Key path: `cscript.exe %SystemRoot%\System32\slmgr.vbs`
- Value name: `Action`
- Blockers: `The card has not yet been promoted into the research-provider surface.`, `This is a one-shot cleanup action without automatic rollback.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.recycle-bin`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Recycle Bin Cleanup`
- Key path: `powershell.exe`
- Value name: `Action`
- Blockers: `The card has not yet been promoted into the research-provider surface.`, `This is a one-shot destructive cleanup action without rollback.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.shadow-copies`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Volume Shadow Copy Cleanup`
- Key path: `vssadmin.exe shadows`
- Value name: `Action`
- Blockers: `The card has not yet been promoted into the research-provider surface.`, `This is a destructive cleanup action without rollback.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.temp-files`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Temporary File Cleanup`
- Key path: `%TEMP%; %WINDIR%\Temp`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.thumbnail-cache`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Explorer Thumbnail Cache Cleanup`
- Key path: `%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.wer-files`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Windows Error Reporting Cleanup`
- Key path: `%PROGRAMDATA%\Microsoft\Windows\WER; %LOCALAPPDATA%\Microsoft\Windows\WER; %TEMP%\WER`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.windows-old`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Windows.old Cleanup`
- Key path: `%SystemDrive%\Windows.old`
- Value name: `CleanupAction`
- Blockers: `Deleting Windows.old has no rollback path through the app.`, `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `cleanup.windows-update-cache`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Windows Update Cache Cleanup`
- Key path: `%WINDIR%\SoftwareDistribution; %WINDIR%\System32\catroot2`
- Value name: `CleanupAction`
- Blockers: `The card has not yet been promoted from the first-party cleanup provider into the research-provider surface.`, `This is a maintenance action with no rollback support.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `network.flush-dns-cache`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `DNS Resolver Cache`
- Key path: `ipconfig.exe`
- Value name: `Action`
- Blockers: `The card still ships only as a first-party network utility and has not been promoted into the research-provider surface.`, `This is a one-shot troubleshooting action without rollback.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `network.reset-winsock`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `Winsock Catalog Reset`
- Key path: `netsh.exe winsock`
- Value name: `Action`
- Blockers: `The command is a repair action that usually expects reboot and does not support rollback.`, `Tweak provenance still marks network.reset-winsock as category-fallback and review-only.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Prove restore or rollback behavior for the exact subtree or value.

### `power.disable-cpu-parking`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `PowerCfg Core Parking`
- Key path: `powercfg.exe /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100 /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100 /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMAXCORES 100 /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMAXCORES 100 /setactive SCHEME_CURRENT`
- Value name: `CoreParkingPercent`
- Blockers: `The card has not yet been promoted from the first-party power provider into the research-provider surface.`, `This review pass documents the behavior but does not yet publish it as a validated research card.`, `validation-proof`
- Recent audit artifacts: `registry-research-framework/audit/power-disable-cpu-idle-states-write-diagnostics-20260328.json`, `registry-research-framework/audit/power-disable-cpu-idle-states-tooling-chain-review-20260328.json`, `registry-research-framework/audit/power-disable-cpu-idle-states-stepwise-orchestration-20260328.json`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `power.disable-hibernation`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `PowerCfg Hibernation`
- Key path: `powercfg.exe /hibernate`
- Value name: `Mode`
- Blockers: `The card has not yet been promoted from the first-party power provider into the research-provider surface.`, `This review pass documents the behavior but does not yet publish it as a validated research card.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `power.disable-usb-selective-suspend`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `PowerCfg USB Selective Suspend`
- Key path: `powercfg.exe /setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 /setdcvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 /setactive SCHEME_CURRENT`
- Value name: `UsbSelectiveSuspend`
- Blockers: `The card has not yet been promoted from the first-party power provider into the research-provider surface.`, `This review pass documents the behavior but does not yet publish it as a validated research card.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.

### `security.disable-uac`

- Lane: `validation-proof`
- Actionability: `hold`
- Priority score: `-3`
- Feature area: `User Account Control / EnableLUA`
- Key path: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`
- Value name: `EnableLUA`
- Blockers: `The action is high risk and reboot-sensitive, so it should not be promoted without stronger upstream provenance and runtime confidence.`, `Tweak provenance still marks security.disable-uac as category-fallback and review-only.`, `validation-proof`
- Suggested command: `winopt research list-blocked --worklist --lane validation-proof`
- Next action hint: Review blockers manually and choose the next evidence lane.
