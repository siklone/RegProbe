# Notifications & Suggestions Tweaks
> Update (2025-12-31): Dedicated notifications doc added.

> Doc note: Reference material. The app may not implement every item; use the catalog for the actual tweak list.

Requires elevation: Mixed (per tweak).

## Scope
This category focuses on reducing suggestions, tips, and content delivery prompts while keeping system notifications functional where possible.

## Common areas
- HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager
- HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
- HKLM\Software\Policies\Microsoft\Windows\System

## Verification
Use `Docs/tweaks/tweak-catalog.md` for the exact source and per-tweak Detect/Apply/Verify/Rollback checks.

## App Coverage Notes (Notification Policies)

Push notifications policy values used by the app:

Path: `HKCU\Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications`
- `NoToastApplicationNotification` (REG_DWORD) - disables toast notifications.
- `NoTileApplicationNotification` (REG_DWORD) - disables live tile updates.
- `NoToastApplicationNotificationOnLockScreen` (REG_DWORD) - disables lock screen toasts.
- `DisallowNotificationMirroring` (REG_DWORD) - prevents notification mirroring.

Feedback frequency policy:

Path: `HKCU\Software\Microsoft\Siuf\Rules`
- `NumberOfSIUFInPeriod` (REG_DWORD) - controls feedback request frequency.

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="notifications.disable-feedback-frequency"></a> `notifications.disable-feedback-frequency` | Disable Feedback Requests | Stops Windows from asking for feedback or ratings. | Safe | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L505` |
| <a id="notifications.disable-lock-screen"></a> `notifications.disable-lock-screen` | Disable Lock Screen Notifications | Prevents app notifications from showing on the lock screen. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L492` |
| <a id="notifications.disable-mirroring"></a> `notifications.disable-mirroring` | Disable Notification Mirroring | Stops notifications from being mirrored to other devices. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L543` |
| <a id="notifications.disable-tile"></a> `notifications.disable-tile` | Disable Tile Notifications | Prevents apps from updating tiles and tile badges. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L530` |
| <a id="notifications.disable-toast"></a> `notifications.disable-toast` | Disable Toast Notifications | Blocks balloon and toast notifications for all applications for the current user. | Advanced | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L479` |
<!-- TWEAK INDEX END -->
