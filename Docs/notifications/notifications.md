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
| <a id="notifications.disable-feedback-frequency"></a> `notifications.disable-feedback-frequency` | Windows Feedback Request Frequency | Windows can occasionally ask for feedback. This tweak sets a user preference intended to reduce or stop those requests. | Medium | `research/records/notifications.disable-feedback-frequency.review.json` |
| <a id="notifications.disable-lock-screen"></a> `notifications.disable-lock-screen` | Lock Screen Toast Notifications | Windows can show some app notifications directly on the lock screen. This policy decides whether apps can raise those lock-screen toast a... | Medium | `research/records/notifications.disable-lock-screen.json` |
| <a id="notifications.disable-mirroring"></a> `notifications.disable-mirroring` | Notification Mirroring | Windows can mirror some notifications to other devices. This policy decides whether that cross-device mirroring is allowed for the curren... | Medium | `research/records/notifications.disable-mirroring.json` |
| <a id="notifications.disable-tile"></a> `notifications.disable-tile` | Tile Notifications | Tile notifications are the live updates and badges some Windows tiles can show. This policy decides whether those tile updates are allowe... | Medium | `research/records/notifications.disable-tile.json` |
| <a id="notifications.disable-toast"></a> `notifications.disable-toast` | Toast Notifications | Toast notifications are the pop-up alerts apps show in Windows. This policy decides whether apps can raise those pop-ups for the current... | Medium | `research/records/notifications.disable-toast.json` |
<!-- TWEAK INDEX END -->
