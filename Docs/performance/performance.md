# Performance Tweaks
> Update (2025-12-31): Dedicated performance doc added.

> Doc note: Reference material. The app may not implement every item; use the catalog for the actual tweak list.

Requires elevation: Mixed (per tweak).

## Scope
This category targets UI responsiveness and background activity. Typical changes:
- Window and taskbar animation settings
- Menu show delay
- Background app execution policy

## Common areas
- HKCU\Control Panel\Desktop
- HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
- HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications

## Verification
Use `Docs/tweaks/tweak-catalog.md` for the exact source and per-tweak Detect/Apply/Verify/Rollback checks.

## App Coverage Notes (Performance Toggles)

Window animation toggle:

Path: `HKCU\Control Panel\Desktop\WindowMetrics`
- `MinAnimate` (REG_SZ) - disables minimize/restore animations when set to `0`.

Power throttling policy:

Path: `HKLM\System\CurrentControlSet\Control\Power`
- `PowerThrottlingOff` (REG_DWORD) - disables power throttling for background apps.

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="performance.disable-animations"></a> `performance.disable-animations` | Disable Window Animations | Disables window animations to make the UI feel snappier and improve responsiveness on lower-end hardware. | Safe | `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs#L19` |
| <a id="performance.disable-background-apps"></a> `performance.disable-background-apps` | Disable Power Throttling | Disables Windows Power Throttling to ensure apps get full CPU performance even when in background. | Advanced | `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs#L62` |
| <a id="performance.disable-menu-show-delay"></a> `performance.disable-menu-show-delay` | Remove Menu Show Delay | Removes the artificial delay when showing menus for a more responsive feel. | Safe | `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs#L32` |
| <a id="performance.disable-taskbar-animations"></a> `performance.disable-taskbar-animations` | Disable Taskbar Animations | Disables taskbar animations for a slight performance boost. | Safe | `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs#L45` |
<!-- TWEAK INDEX END -->
