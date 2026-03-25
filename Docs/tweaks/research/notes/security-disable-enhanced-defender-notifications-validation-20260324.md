# Windows Security Enhanced Notifications

Record: `security.disable-enhanced-defender-notifications`

This record uses:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications`
- value: `DisableEnhancedNotifications`

## Source check

`Docs/system/system.md` shows two Microsoft policy surfaces with the same value name:

- `WindowsDefenderSecurityCenter.admx`
  - path: `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications`
  - value: `DisableEnhancedNotifications`
  - enabled: `1`
  - disabled: `0`
- `WindowsDefender.admx`
  - path: `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Reporting`
  - value: `DisableEnhancedNotifications`
  - enabled: `1`
  - disabled: `0`

That gives us a documented path, value name, and `0/1` model. The runtime probe below is why this record stays on the Security Center path.

## VM proof

Snapshot:

- `baseline-20260324-high-risk-lane`

Artifacts:

- baseline:
  - `H:\Temp\vm-tooling-staging\defender-enhanced-notifications-baseline-1-20260324-214343\defender-disable-enhanced-baseline-1.txt`
- Security Center `1`:
  - `H:\Temp\vm-tooling-staging\defender-enhanced-notifications-securitycenter-1-20260324-213118\defender-disable-enhanced-securitycenter-1.txt`
- Reporting `1` alias check:
  - `H:\Temp\vm-tooling-staging\defender-enhanced-notifications-reporting-1-20260324-213700\defender-disable-enhanced-reporting-1.txt`

Baseline showed the live policy read and the existing sibling policy on the same branch:

```text
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications\DisableNotifications | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications\DisableEnhancedNotifications | NAME NOT FOUND | Length: 16
```

After writing the Security Center path, the same service read the value back as `1`:

```text
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications\DisableEnhancedNotifications | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

When only the Reporting path was set, this launch probe still showed the Security Center path being queried. It did not show a matching read from the Reporting path.

## Class result

This record is now app-ready on the Security Center Notifications path.

- Microsoft documents a second path under `Windows Defender\Reporting`, but the VM alias check still read the Security Center path.
- The clean VM already had `DisableNotifications = 1` on the same branch, but that is a sibling policy, not a path conflict.
- The app writes the Security Center path that the runtime probe actually consumed.

So the current state is:

- validated
- app-mapped
- actionable
- `Class A`
