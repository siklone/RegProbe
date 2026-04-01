# Windows 11 Settings And Privacy Leads

These notes are incoming research leads, not auto-validated facts.

Use them as starting points for future proof-first validation passes. Each related record still needs its own `validation_proof` before it can move to `validated`.

## Windows 11 Settings Reference Leads

Source to review:

- https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-windows-11

Important caveat:

- Some Windows 11 settings are documented as subkey-based surfaces, not as simple values under the parent key.
- If the app writes `Explorer//Advanced -> SomeValue` as `REG_DWORD`, but Microsoft documents `Explorer//Advanced//SomeSubkey -> SystemSettings_*` as `REG_SZ`, treat that as a surface mismatch.

Reported mappings to verify:

- `TaskbarAl`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//TaskbarAl`
  - Value: `SystemSettings_DesktopTaskbar_Al`
  - Type: `REG_SZ`
  - `0 = Left`
  - `1 = Center`

- `ShowTaskViewButton`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//ShowTaskViewButton`
  - Value: `SystemSettings_DesktopTaskbar_TaskView`
  - Type: `REG_SZ`
  - `0 = Hidden`
  - `1 = Visible`

- `TaskbarGlomLevel`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//TaskbarGlomLevel`
  - Value: `SystemSettings_DesktopTaskbar_GroupingMode`
  - Type: `REG_SZ`
  - `0 = Always`
  - `1 = When full`
  - `2 = Never`

- `TaskbarSd`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//TaskbarSd`
  - Value: `SystemSettings_DesktopTaskbar_Sd`
  - Type: `REG_SZ`
  - `0 = Disabled`
  - `1 = Enabled`

- `TaskbarSn`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//TaskbarSn`
  - Value: `SystemSettings_DesktopTaskbar_Sn`
  - Type: `REG_SZ`
  - `0 = Disabled`
  - `1 = Enabled`

- `TaskbarDa`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced//TaskbarDa`
  - Value: `SystemSettings_DesktopTaskbar_Da`
  - Type: `REG_SZ`
  - `0 = Hidden`
  - `1 = Visible`

- `Start_Layout`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `Start_Layout`
  - Type: `REG_DWORD`
  - `0 = Default`
  - `1 = More Pins`
  - `2 = More Recommendations`

- `Start_IrisRecommendations`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `Start_IrisRecommendations`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `ShowAllPinsList`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Start`
  - Value: `ShowAllPinsList`
  - Type: `REG_DWORD`
  - `0 = Off`
  - `1 = On`

- `AutoGameModeEnabled`
  - Path: `HKCU//Software//Microsoft//GameBar`
  - Value: `AutoGameModeEnabled`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `UseNexusForGameBarEnabled`
  - Path: `HKCU//Software//Microsoft//GameBar`
  - Value: `UseNexusForGameBarEnabled`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `DisableAutoplay`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//AutoplayHandlers`
  - Value: `DisableAutoplay`
  - Type: `REG_DWORD`
  - `0 = Enabled`
  - `1 = Disabled`

- `AllowFailover`
  - Path: `HKLM//SOFTWARE//Microsoft//WcmSvc//CellularFailover`
  - Value: `AllowFailover`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

Follow-up rule:

- If the settings page documents a subkey-based `REG_SZ` surface and the app writes a parent-key `REG_DWORD`, keep the record in `review-required` and treat it as either an implementation mismatch or a Procmon follow-up.

## Privacy Policy Leads

Primary source to review:

- https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization

Reported mappings to verify:

- `AllowTelemetry`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//DataCollection`
  - Value: `AllowTelemetry`
  - Type: `REG_DWORD`
  - `0 = Diagnostic data off` (Enterprise, Education, Server only)
  - `1 = Required / Basic`
  - `2 = Enhanced` (Windows 10 only, removed in Windows 11)
  - `3 = Optional / Full`
  - `missing = user/UI controlled`

Important path note:

- Group Policy path:
  - `HKLM//SOFTWARE//Policies//Microsoft//Windows//DataCollection`
- Runtime or UI-related path:
  - `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//DataCollection`
- Do not treat those as interchangeable without proof.

Additional leads:

- `DoNotShowFeedbackNotifications`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//DataCollection`
  - Value: `DoNotShowFeedbackNotifications`
  - Type: `REG_DWORD`
  - `0 = Show feedback notifications`
  - `1 = Do not show feedback notifications`
  - Claimed source to verify: `DataCollection.admx`

- `DisabledByGroupPolicy` for Advertising ID
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//AdvertisingInfo`
  - Value: `DisabledByGroupPolicy`
  - Type: `REG_DWORD`
  - `0 = Advertising ID enabled`
  - `1 = Advertising ID disabled`
  - Claimed source to verify:
    - https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services

- `DisableWindowsConsumerFeatures`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//CloudContent`
  - Value: `DisableWindowsConsumerFeatures`
  - Type: `REG_DWORD`
  - `0 = Enabled`
  - `1 = Disabled`

- `DisableTailoredExperiencesWithDiagnosticData`
  - Path: `HKCU//SOFTWARE//Policies//Microsoft//Windows//CloudContent`
  - Value: `DisableTailoredExperiencesWithDiagnosticData`
  - Type: `REG_DWORD`
  - `0 = Enabled`
  - `1 = Disabled`

- `AllowCortana`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//Windows Search`
  - Value: `AllowCortana`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `EnableActivityFeed`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//System`
  - Value: `EnableActivityFeed`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `LetAppsRunInBackground`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//AppPrivacy`
  - Value: `LetAppsRunInBackground`
  - Type: `REG_DWORD`
  - `0 = User controlled`
  - `1 = Force allow`
  - `2 = Force deny`

## Security MDM Leads

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy

Reported mappings to verify:

- `Defender/AllowCloudProtection`
  - `0 = Disabled`

- `Defender/SubmitSamplesConsent`
  - `2 = Never send`

- `Defender/EnableSmartScreenInShell`
  - `0 = Disabled`

Important caveat:

- These are MDM Policy CSP surfaces.
- If the app writes a direct registry path instead of the MDM or ADMX-backed control surface, treat that as a surface mismatch until `WindowsDefender.admx`, `MicrosoftEdge.admx`, or an equivalent primary source proves the exact registry contract.

## Security Registry Leads

Primary source to review:

- https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services

Reported mappings to verify:

- `EnableSmartScreen`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//System`
  - Value: `EnableSmartScreen`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `ConfigureAppInstallControlEnabled`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender//SmartScreen`
  - Value: `ConfigureAppInstallControlEnabled`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `ConfigureAppInstallControl`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender//SmartScreen`
  - Value: `ConfigureAppInstallControl`
  - Type: `REG_SZ`
  - `"Anywhere" = Allow from anywhere`
  - `"Recommendations" = Show recommendations`
  - `"PreferStore" = Prefer Store`
  - `"StoreOnly" = Store only`

- `EnableWebContentEvaluation`
  - Path: `HKCU//SOFTWARE//Microsoft//Windows//CurrentVersion//AppHost`
  - Value: `EnableWebContentEvaluation`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `SpyNetReporting`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender//Spynet`
  - Value: `SpyNetReporting`
  - Type: `REG_DWORD`
  - `0 = MAPS disabled`
  - `1 = Basic membership`
  - `2 = Advanced membership`

- `SubmitSamplesConsent`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender//Spynet`
  - Value: `SubmitSamplesConsent`
  - Type: `REG_DWORD`
  - `0 = Always prompt`
  - `1 = Send safe samples automatically`
  - `2 = Never send`
  - `3 = Send all automatically`

- `DisableEnhancedNotifications`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender//Reporting`
  - Value: `DisableEnhancedNotifications`
  - Type: `REG_DWORD`
  - `0 = Notifications enabled`
  - `1 = Notifications disabled`

- `DisableAntiSpyware`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows Defender`
  - Value: `DisableAntiSpyware`
  - Type: `REG_DWORD`
  - `0 = Defender enabled`
  - `1 = Defender disabled`

Critical caveat:

- On Windows 11 and modern Windows 10 builds with Tamper Protection, `DisableAntiSpyware` is not a reliable control surface.
- Microsoft can ignore, remove, or override it.
- Treat this as a version-sensitive and potentially deprecated control, not a normal safe toggle.

Follow-up rule:

- For Defender and SmartScreen records, confirm whether the app uses the documented policy path, an MDM surface, or a runtime preference path.
- If the app writes a different registry location than the documented security policy surface, keep the record in `review-required` and mark it as a surface mismatch.

## Power And Performance Leads

Primary source to review:

- https://learn.microsoft.com/en-us/windows-hardware/customize/enterprise/hibernate-once-resume-many-horm-

Reported mappings to verify:

- `HiberbootEnabled`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Session Manager//Power`
  - Value: `HiberbootEnabled`
  - Type: `REG_DWORD`
  - `0 = Fast Startup disabled`
  - `1 = Fast Startup enabled`
  - Note: Fast Startup effectively depends on hibernation being enabled.

- `HibernateEnabled`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Session Manager//Power`
  - Value: `HibernateEnabled`
  - Type: `REG_DWORD`
  - `0 = Hibernate disabled`
  - `1 = Hibernate enabled`
  - Note: If hibernation is disabled, Fast Startup is also effectively disabled.

- `HttpAcceptLanguageOptOut`
  - Path: `HKCU//Control Panel//International//User Profile`
  - Value: `HttpAcceptLanguageOptOut`
  - Type: `REG_DWORD`
  - `0 = Allow websites to access language list`
  - `1 = Do not share language list`

Interpretation note:

- `HiberbootEnabled` and `HibernateEnabled` can look like straightforward registry toggles, but user-facing behavior depends on feature prerequisites and OS power-state support.
- Keep that dependency chain visible in the record instead of presenting them as isolated one-bit switches.

## Explorer Advanced User Preference Leads

Expected source strength:

- `medium`

Reason:

- These are commonly observed Explorer user-preference keys.
- They do not appear as normal ADMX-backed policy controls.
- Current confidence comes from Microsoft Q&A answers, Windows settings references, and widespread behavior consistency rather than a single first-party registry contract page.

Reported mappings to verify:

- `HideFileExt`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `HideFileExt`
  - Type: `REG_DWORD`
  - `0 = Show extensions`
  - `1 = Hide extensions`

- `Hidden`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `Hidden`
  - Type: `REG_DWORD`
  - `1 = Show hidden files`
  - `2 = Do not show hidden files`
  - Note: `2` is the documented default; `0` should not be treated as the canonical hidden-files default.

- `ShowSuperHidden`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `ShowSuperHidden`
  - Type: `REG_DWORD`
  - `0 = Do not show protected operating system files`
  - `1 = Show protected operating system files`

- `LaunchTo`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `LaunchTo`
  - Type: `REG_DWORD`
  - `1 = This PC`
  - `2 = Quick Access`
  - `3 = OneDrive` (when applicable)

- `UseCompactMode`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer//Advanced`
  - Value: `UseCompactMode`
  - Type: `REG_DWORD`
  - `0 = Normal spacing`
  - `1 = Compact mode`

- `ShowRecent`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer`
  - Value: `ShowRecent`
  - Type: `REG_DWORD`
  - `0 = Do not show`
  - `1 = Show`

- `ShowFrequent`
  - Path: `HKCU//Software//Microsoft//Windows//CurrentVersion//Explorer`
  - Value: `ShowFrequent`
  - Type: `REG_DWORD`
  - `0 = Do not show`
  - `1 = Show`

Interpretation note:

- These are good candidates for `medium` confidence records with explicit notes about source quality.
- They should not be upgraded to `validated` from a Q&A answer alone if a stronger official source later disagrees.

## System And Update Leads

Expected source strength:

- `high`

Reason:

- These are tied to Microsoft Learn pages, ADMX-backed policies, or long-standing Windows management surfaces.

Reported mappings to verify:

- `Disabled` for Windows Error Reporting
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//Windows Error Reporting`
  - Value: `Disabled`
  - Type: `REG_DWORD`
  - `0 = Error reporting enabled`
  - `1 = Error reporting disabled`

- `fAllowToGetHelp` for Remote Assistance
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Remote Assistance`
  - Value: `fAllowToGetHelp`
  - Type: `REG_DWORD`
  - `0 = Remote Assistance disabled`
  - `1 = Remote Assistance enabled`

- `DODownloadMode` for Delivery Optimization
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//DeliveryOptimization`
  - Value: `DODownloadMode`
  - Type: `REG_DWORD`
  - `0 = HTTP only / no P2P`
  - `1 = LAN`
  - `2 = Group / LAN + Internet`
  - `3 = Internet / limited`
  - `99 = Bypass`
  - `100 = Simple download mode`

- `EnableDynamicContentInWSB` for Search Highlights
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//Windows Search`
  - Value: `EnableDynamicContentInWSB`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

Follow-up rule:

- For `Search Highlights`, distinguish the documented policy surface from any separate user-side or runtime Search UI value.
- For Delivery Optimization, do not collapse multiple `DODownloadMode` states into a fake on/off toggle when the official surface is multi-valued.

## Network TCP And Multimedia Leads

Expected source strength:

- `high` for TCP registry behavior that Microsoft documents in KB or Troubleshoot articles
- `medium-high` for MMCSS values that are Microsoft-confirmed but not always described in a single modern primary registry contract page

Primary sources to review:

- https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/registry-entry-control-tcp-acknowledgment-behavior
- https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/description-tcp-features
- https://learn.microsoft.com/en-us/archive/blogs/nettracer/things-that-you-may-want-to-know-about-tcp-keepalives
- https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services

Reported mappings to verify:

- `TcpAckFrequency`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters//Interfaces//<Interface GUID>`
  - Value: `TcpAckFrequency`
  - Type: `REG_DWORD`
  - Valid range: `0-255`
  - Default: `2`
  - `1 = ACK every segment`
  - `2 = ACK every two segments`
  - `0 = Invalid / behaves like default`
  - Caveat: per-interface; Microsoft does not generally recommend changing it casually

- `TcpNoDelay`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters//Interfaces//<Interface GUID>`
  - Value: `TcpNoDelay`
  - Type: `REG_DWORD`
  - `0 = Nagle enabled`
  - `1 = Nagle disabled`
  - Caveat: per-interface

- `Tcp1323Opts`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters`
  - Value: `Tcp1323Opts`
  - Type: `REG_DWORD`
  - `0 = Scaling and timestamps disabled`
  - `1 = Window scaling only`
  - `2 = Timestamps only`
  - `3 = Both enabled`

- `SackOpts`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters`
  - Value: `SackOpts`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `KeepAliveTime`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters`
  - Value: `KeepAliveTime`
  - Type: `REG_DWORD`
  - Unit: milliseconds
  - Valid range: `1-0xFFFFFFFF`
  - Default: `7200000`

- `KeepAliveInterval`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters`
  - Value: `KeepAliveInterval`
  - Type: `REG_DWORD`
  - Unit: milliseconds
  - Valid range: `1-0xFFFFFFFF`
  - Default: `1000`

- `TcpMaxDataRetransmissions`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Tcpip//Parameters`
  - Value: `TcpMaxDataRetransmissions`
  - Type: `REG_DWORD`
  - Valid range: `0-0xFFFFFFFF`
  - Default: `5`

- `Teredo_State`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//TCPIP//v6Transition`
  - Value: `Teredo_State`
  - Type: `REG_SZ`
  - `"Default" = Windows managed`
  - `"Disabled" = Teredo off`
  - `"Client" = Client mode`
  - `"Enterprise Client" = Enterprise mode`
  - Caveat: can affect Xbox and Delivery Optimization behavior

- `NetworkThrottlingIndex`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows NT//CurrentVersion//Multimedia//SystemProfile`
  - Value: `NetworkThrottlingIndex`
  - Type: `REG_DWORD`
  - Default: `10`
  - `0xFFFFFFFF = Disable throttling`
  - Meaningful values: roughly `1-70`
  - Caveat: value missing should be treated as default behavior, not as a distinct tuned preset
  - Source quality note: Microsoft-confirmed but not as clean as an ADMX-backed policy; keep source strength at `medium-high` unless a stronger primary page is captured

- `SystemResponsiveness`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows NT//CurrentVersion//Multimedia//SystemProfile`
  - Value: `SystemResponsiveness`
  - Type: `REG_DWORD`
  - Default: `20`
  - `0 = All CPU to foreground`
  - `100 = MMCSS effectively disabled`
  - Note: values not divisible by 10 round down; values below 10 and above 100 are clamped to 20

Interpretation notes:

- TCP interface keys must be modeled as per-interface settings, not as a single global toggle.
- These are performance-sensitive settings; "documented" does not mean "recommended for general users."

## Developer Leads

Expected source strength:

- `high`

Primary sources to review:

- https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
- https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development
- https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-8dot3name
- https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
- https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies

Reported mappings to verify:

- `LongPathsEnabled`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//FileSystem`
  - Value: `LongPathsEnabled`
  - Type: `REG_DWORD`
  - `0 = MAX_PATH limit active`
  - `1 = Long paths enabled`
  - Caveat: the app itself must also be `longPathAware`; process behavior can be cached and Explorer may still ignore the setting

- `AllowDevelopmentWithoutDevLicense`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//AppModelUnlock`
  - Value: `AllowDevelopmentWithoutDevLicense`
  - Type: `REG_DWORD`
  - `0 = Developer Mode off`
  - `1 = Developer Mode on`
  - Note: this record is already validated in the current dataset

- `NtfsDisable8dot3NameCreation`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//FileSystem`
  - Value: `NtfsDisable8dot3NameCreation`
  - Type: `REG_DWORD`
  - `0 = 8.3 short names enabled`
  - `1 = 8.3 short names disabled`
  - Caveat: some legacy software still depends on 8.3 naming

- `NtfsDisableLastAccessUpdate`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//FileSystem`
  - Value: `NtfsDisableLastAccessUpdate`
  - Type: `REG_DWORD`
  - `0 = Update access timestamps`
  - `1 = Disable updates`
  - `2 = Disable user-mode updates, keep kernel-mode updates`
  - `3 = Disable both`
  - Claimed modern default: `0x80000001` (system managed)
  - Caveat: do not collapse this into a fake boolean if the official surface is multi-state or system-managed

- `ExecutionPolicy`
  - Path: `HKLM//SOFTWARE//Microsoft//PowerShell//1//ShellIds//Microsoft.PowerShell`
  - Value: `ExecutionPolicy`
  - Type: `REG_SZ`
  - `"Restricted"`
  - `"AllSigned"`
  - `"RemoteSigned"`
  - `"Unrestricted"`
  - `"Bypass"`
  - Caveat: user-scope `HKCU` policy can override machine-scope expectations

## Power And Standby Leads

Expected source strength:

- `high` for hibernate and hiberfile sizing sources
- `medium` or `medium-high` for Modern Standby override keys that are hardware-sensitive or inconsistently honored

Primary sources to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-power
- https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options

Reported mappings to verify:

- `HibernateEnabled`
  - Claimed Path A: `HKLM//SYSTEM//CurrentControlSet//Control//Power`
  - Claimed Path B: `HKLM//SYSTEM//CurrentControlSet//Control//Session Manager//Power`
  - Value: `HibernateEnabled`
  - Type: `REG_DWORD`
  - `0 = Hibernate disabled`
  - `1 = Hibernate enabled`
  - Critical caveat: this note currently carries conflicting path claims from different sources. Do not promote a record until the exact path is proven from a primary Microsoft source.

- `HiberbootEnabled`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Session Manager//Power`
  - Value: `HiberbootEnabled`
  - Type: `REG_DWORD`
  - `0 = Fast Startup disabled`
  - `1 = Fast Startup enabled`
  - Caveat: if hibernation is disabled, this value may exist but have no practical effect

- `HiberFileSizePercent`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Power`
  - Value: `HiberFileSizePercent`
  - Type: `REG_DWORD`
  - Default: `75`
  - Valid range: `40-100`
  - Caveat: reduced hiberfile behavior below 40% should be documented separately if exposed

- `PlatformAoAcOverride`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Power`
  - Value: `PlatformAoAcOverride`
  - Type: `REG_DWORD`
  - `0 = Force legacy S3-style path`
  - `missing = Windows default Modern Standby behavior`
  - Source quality note: treat as `medium` until a stronger primary Microsoft source is captured
  - Caveat: can break on unsupported hardware and may stop working on newer Windows builds

- `CsEnabled`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Power`
  - Value: `CsEnabled`
  - Type: `REG_DWORD`
  - `0 = Modern Standby disabled`
  - `1 = Modern Standby enabled`
  - Source quality note: treat as `medium-high`; firmware support still determines real behavior

Interpretation notes:

- Hibernate and Fast Startup settings are dependency-linked and should not be modeled as isolated independent toggles.
- Modern Standby override keys are exactly the kind of settings where source quality, firmware support, and runtime behavior all matter separately.

## UAC Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration

Reported mappings to verify:

- `EnableLUA`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `EnableLUA`
  - Type: `REG_DWORD`
  - `0 = UAC disabled`
  - `1 = UAC enabled`
  - Caveat: reboot required; modern Windows behavior can degrade badly when set to `0`

- `ConsentPromptBehaviorAdmin`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `ConsentPromptBehaviorAdmin`
  - Type: `REG_DWORD`
  - `0 = Elevate without prompting`
  - `1 = Prompt for credentials on secure desktop`
  - `2 = Prompt for consent on secure desktop`
  - `3 = Prompt for credentials on normal desktop`
  - `4 = Prompt for consent on normal desktop`
  - `5 = Prompt for consent for Windows binaries, credentials for others`
  - Caveat: modern Windows UI can mask some raw numeric differences

- `ConsentPromptBehaviorUser`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `ConsentPromptBehaviorUser`
  - Type: `REG_DWORD`
  - `0 = Automatically deny elevation requests`
  - `1 = Prompt for credentials on secure desktop`
  - `3 = Prompt for credentials on normal desktop`

- `EnableInstallerDetection`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `EnableInstallerDetection`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`
  - Caveat: defaults differ by edition and environment

- `PromptOnSecureDesktop`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `PromptOnSecureDesktop`
  - Type: `REG_DWORD`
  - `0 = Normal desktop`
  - `1 = Secure desktop`

- `EnableVirtualization`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `EnableVirtualization`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `FilterAdministratorToken`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `FilterAdministratorToken`
  - Type: `REG_DWORD`
  - `0 = Built-in Administrator runs with full token`
  - `1 = Built-in Administrator uses Admin Approval Mode`

Interpretation note:

- UAC records are high-confidence control surfaces, but they are not "safe casual tweak" candidates by default. UI behavior, sign-in experience, and compatibility can change sharply.

## LocalPoliciesSecurityOptions Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-localpoliciessecurityoptions
- Local security metadata such as `research/evidence-files/external/c/Windows/inf/sceregvl.inf.md`

Reported mappings to verify:

- `NoConnectedUser`
  - Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//Policies//System`
  - Value: `NoConnectedUser`
  - Type: `REG_DWORD`
  - `0 = Allow Microsoft accounts`
  - `1 = Block adding new Microsoft accounts`
  - `3 = Block sign-in with Microsoft accounts`
  - Note: already validated in the current dataset

- `Accounts_EnableAdministratorAccountStatus`
  - Surface is policy/CSP or account-management tooling, not a normal writable registry preference
  - Caveat: underlying storage is SAM-based, so direct registry modeling is misleading
  - Follow-up: use CSP, security policy, or account-management command surfaces rather than pretending this is a normal JSON-friendly registry value

- `AddPrinterDrivers`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Print//Providers//LanMan Print Services//Servers`
  - Value: `AddPrinterDrivers`
  - Type: `REG_DWORD`
  - `0 = Non-admin users can install printer drivers`
  - `1 = Only admins can install printer drivers`
  - Caveat: workstation/server defaults can differ

Interpretation note:

- If a Local Security Option maps to SAM or account databases instead of a normal registry policy value, keep the record explicit about that and avoid fake "simple registry toggle" modeling.

## Windows Update Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update

Reported mappings to verify:

- `AUOptions`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//WindowsUpdate//AU`
  - Value: `AUOptions`
  - Type: `REG_DWORD`
  - `1 = Notify before download`
  - `2 = Auto download, notify install`
  - `3 = Auto download, auto install at scheduled time`
  - `4 = Auto download, schedule install`
  - `5 = Local admin control`

- `NoAutoUpdate`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//WindowsUpdate//AU`
  - Value: `NoAutoUpdate`
  - Type: `REG_DWORD`
  - `1 = Automatic updates disabled`
  - Caveat: do not collapse this and `AUOptions` into a single fake boolean without documenting the multi-state policy interaction

- `ActiveHoursStart`
  - Path: `HKLM//SOFTWARE//Microsoft//WindowsUpdate//UX//Settings`
  - Value: `ActiveHoursStart`
  - Type: `REG_DWORD`
  - Range: `0-23`

- `ActiveHoursEnd`
  - Path: `HKLM//SOFTWARE//Microsoft//WindowsUpdate//UX//Settings`
  - Value: `ActiveHoursEnd`
  - Type: `REG_DWORD`
  - Range: `0-23`
  - Caveat: this looks like a runtime or UX settings surface, not the same thing as a policy-backed control

- `DODownloadMode`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//DeliveryOptimization`
  - Value: `DODownloadMode`
  - Type: `REG_DWORD`
  - `0 = HTTP only`
  - `1 = LAN`
  - `2 = Group / LAN + Internet`
  - `3 = HTTP + Internet P2P`
  - `99 = Bypass`
  - `100 = Simple`

Interpretation notes:

- Windows Update has both policy surfaces and UX/runtime surfaces; do not treat them as interchangeable.
- `ActiveHoursStart/End` should be modeled as user-experience timing settings, not as the same class of evidence as policy-backed `AUOptions`.
- `DODownloadMode` is a true multi-state policy surface, not a binary on/off setting. If the app only writes `0`, treat that as a specific implementation choice rather than a complete representation of the policy.

## Windows Firewall Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/firewall-csp
- Local policy files if an ADMX mapping exists
- https://learn.microsoft.com/en-us/troubleshoot/azure/virtual-machines/windows/disable-guest-os-firewall-windows

Reported mappings to verify:

- `EnableFirewall`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//SharedAccess//Parameters//FirewallPolicy//{DomainProfile|StandardProfile|PublicProfile}`
  - Value: `EnableFirewall`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`
  - Caveat: this is a runtime profile surface, not automatically the same thing as a policy-key write

- `DefaultInboundAction`
  - Same profile path family as above
  - Type: `REG_DWORD`
  - `0 = Allow`
  - `1 = Block`

- `DefaultOutboundAction`
  - Same profile path family as above
  - Type: `REG_DWORD`
  - `0 = Allow`
  - `1 = Block`

- `DisableNotifications`
  - Same profile path family as above
  - Type: `REG_DWORD`
  - `0 = Notifications enabled`
  - `1 = Notifications disabled`

- `DisableUnicastResponsesToMulticastBroadcast`
  - Same profile path family as above
  - Type: `REG_DWORD`
  - `0 = Allow unicast responses`
  - `1 = Block unicast responses`

Interpretation notes:

- Firewall records need a strict split between policy-backed surfaces and `SharedAccess` runtime surfaces.
- If the app writes `SharedAccess//Parameters//FirewallPolicy//...` while Microsoft documents a CSP or policy path elsewhere, treat that as `implementation-mismatch` until the exact runtime surface is proven.
- Firewall CSP is the strongest source for the managed policy path and value semantics.
- The Azure VM recovery article confirms the runtime `SharedAccess//Parameters//FirewallPolicy//{DomainProfile|StandardProfile|PublicProfile}` paths are real, but it should be tagged as `troubleshoot_doc` rather than a full primary design reference.
- Firewall disable-style records are not casual-safe defaults.

## Remote Desktop Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/troubleshoot/windows-server/remote/rdp-error-general-troubleshooting
- Local Terminal Services ADMX / CSP docs where applicable

Reported mappings to verify:

- `fDenyTSConnections`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Terminal Server`
  - Value: `fDenyTSConnections`
  - Type: `REG_DWORD`
  - `0 = Allow RDP`
  - `1 = Deny RDP`
  - Caveat: policy path `HKLM//SOFTWARE//Policies//Microsoft//Windows NT//Terminal Services` can override this local runtime surface

- `UserAuthentication`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Terminal Server//WinStations//RDP-Tcp`
  - Value: `UserAuthentication`
  - Type: `REG_DWORD`
  - `0 = NLA not required`
  - `1 = NLA required`

- `PortNumber`
  - Same `RDP-Tcp` path
  - Type: `REG_DWORD`
  - Default: `3389`
  - Caveat: firewall rules must follow port changes

- `fEnableWinStation`
  - Same `RDP-Tcp` path
  - Type: `REG_DWORD`
  - `0 = Listener disabled`
  - `1 = Listener enabled`

Interpretation notes:

- RDP records can have both local runtime surfaces and policy override surfaces. Keep them separate in research records.
- `fDenyTSConnections` is not enough by itself to describe the full managed-state story if a policy path exists.

## BitLocker Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/configure
- Local `FVE.admx` and related ADML help text

Reported mappings to verify:

- `PreventDeviceEncryption`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//BitLocker`
  - Value: `PreventDeviceEncryption`
  - Type: `REG_DWORD`
  - `0 = Allow automatic encryption`
  - `1 = Prevent automatic encryption`
  - Caveat: hardware and Windows version requirements can change behavior, especially on newer Windows 11 releases

- `EncryptionMethodWithXtsOs`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//FVE`
  - Type: `REG_DWORD`
  - `3 = AES-CBC 128`
  - `4 = AES-CBC 256`
  - `6 = XTS-AES 128`
  - `7 = XTS-AES 256`

- `EncryptionMethodWithXtsFdv`
  - Same `HKLM//SOFTWARE//Policies//Microsoft//FVE` path
  - Same `3/4/6/7` range

Interpretation notes:

- BitLocker encryption-method records are true multi-value policy surfaces. Do not collapse them into a plain enable/disable toggle.
- `PreventDeviceEncryption` has safety and compatibility consequences and should stay clearly marked as non-casual.

## Search And Cortana Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
- Local `Search.admx` / `Search.adml`

Reported mappings to verify:

- `AllowCortana`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//Windows Search`
  - Value: `AllowCortana`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`
  - Caveat: Cortana is removed on newer Windows 11 builds, so this can become a legacy or partial-effect setting

- `EnableDynamicContentInWSB`
  - Same Windows Search policy path
  - Value: `EnableDynamicContentInWSB`
  - Type: `REG_DWORD`
  - `0 = Search highlights off`
  - `1 = Search highlights on`

- `BingSearchEnabled`
  - Path: `HKCU//SOFTWARE//Microsoft//Windows//CurrentVersion//Search`
  - Value: `BingSearchEnabled`
  - Type: `REG_DWORD`
  - `0 = Bing web search off`
  - `1 = Bing web search on`
  - Caveat: this is a user/runtime surface and may be ignored on newer builds

- `SearchboxTaskbarMode`
  - Path: `HKCU//SOFTWARE//Microsoft//Windows//CurrentVersion//Search`
  - Value: `SearchboxTaskbarMode`
  - Type: `REG_DWORD`
  - `0 = Hidden`
  - `1 = Icon only`
  - `2 = Search box`
  - `3 = Search box plus richer widget treatment on some builds`

- `AllowCloudSearch`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//Windows Search`
  - Value: `AllowCloudSearch`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

Interpretation notes:

- Search records often mix policy surfaces and user/runtime surfaces; keep them explicitly separate.
- `BingSearchEnabled` is not equivalent to a policy-backed web-search setting and may need `docs-first` treatment on newer Windows 11 builds.

## Accessibility Leads

Expected source strength:

- `medium`

Primary source to review:

- Microsoft Win32 accessibility API docs
- Microsoft Q&A confirmations for default values

Reported mappings to verify:

- `StickyKeys -> Flags`
  - Path: `HKCU//Control Panel//Accessibility//StickyKeys`
  - Value: `Flags`
  - Type: `REG_SZ`
  - `506 = StickyKeys fully off`
  - `58 = Hotkey disabled`
  - `510 = Enabled/default-like`
  - Caveat: Microsoft prefers using `SystemParametersInfo` instead of direct registry writes

- `Keyboard Response -> Flags`
  - Path: `HKCU//Control Panel//Accessibility//Keyboard Response`
  - Value: `Flags`
  - Type: `REG_SZ`
  - `122 = FilterKeys dialog and hotkey disabled`
  - `126 = Default-like`

- `ToggleKeys -> Flags`
  - Path: `HKCU//Control Panel//Accessibility//ToggleKeys`
  - Value: `Flags`
  - Type: `REG_SZ`
  - `58 = ToggleKeys dialog and hotkey disabled`
  - `62 = Default-like`

- `Keyboard Response tuning values`
  - Path: `HKCU//Control Panel//Accessibility//Keyboard Response`
  - Values:
    - `AutoRepeatDelay = "1000"`
    - `AutoRepeatRate = "31"`
    - `DelayBeforeAcceptance = "1000"`
    - `BounceTime = "0"`
  - Caveat: source quality is weaker here and should stay below ADMX-grade confidence

- `Narrator auto-start`
  - Path: `HKCU//Software//Microsoft//Windows NT//CurrentVersion//Accessibility`
  - Value: `Configuration`
  - Type: `REG_SZ`
  - `"" = Narrator does not auto-start`
  - `"narrator" = Narrator auto-starts at sign-in`

- `HighContrast -> Flags`
  - Path: `HKCU//Control Panel//Accessibility//HighContrast`
  - Value: `Flags`
  - Type: `REG_SZ`
  - `122 = High contrast off`
  - `123 = High contrast on`

Interpretation notes:

- Accessibility records are not ADMX-backed in the normal way and should not be treated like policy-backed enterprise settings.
- Microsoft's guidance prefers `SystemParametersInfo` and related APIs over direct registry edits for many of these features.
- Use `strength: medium` by default unless a stronger API-backed source is found.
- Use `decision.needs_vm_validation = true` for accessibility records unless the runtime behavior has been observed directly.
- Keep `decision.apply_allowed` conservative because disabling accessibility helpers can create serious usability issues for users who rely on them.

## Printing Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/troubleshoot/windows-server/printing/troubleshoot-printing-scenarios
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-printers
- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-printing2
- Local printing-related ADMX files

Reported mappings to verify:

- `Spooler -> Start`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Services//Spooler`
  - Value: `Start`
  - Type: `REG_DWORD`
  - `2 = Automatic`
  - `3 = Manual`
  - `4 = Disabled`
  - Caveat: disabling this stops local printing entirely

- `NoWarningNoElevationOnInstall`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows NT//Printers//PointAndPrint`
  - Value: `NoWarningNoElevationOnInstall`
  - Type: `REG_DWORD`
  - `0 = Show warning / elevation prompt`
  - `1 = Do not show warning / elevation prompt`
  - Caveat: strong PrintNightmare-era security warning; default-safe posture is `0`

- `UpdatePromptSettings`
  - Same PointAndPrint policy path
  - Type: `REG_DWORD`
  - `0 = Show UAC prompt`
  - `1 = Elevated prompt`
  - `2 = No UAC prompt`
  - Caveat: same security risk area as above

- `RegisterSpoolerRemoteRpcEndPoint`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows NT//Printers`
  - Value: `RegisterSpoolerRemoteRpcEndPoint`
  - Type: `REG_DWORD`
  - `1 = Accept spooler client connections`
  - `2 = Refuse spooler client connections`
  - Caveat: `2` effectively kills network printing to that machine

- `DriverIsolationOverride`
  - Policy surface under Printers CSP / ADMX-backed printing policy
  - Caveat: do not confuse policy intent with unrelated runtime values such as `IPCTimeout` unless a primary source proves the mapping

Interpretation notes:

- Printing records often look simple but can have major security consequences. Keep PrintNightmare-era prompt suppression settings clearly marked as non-casual and high-risk.
- Driver isolation should only be validated against the exact policy or documented registry surface. Do not infer it from nearby print runtime keys.

## App Privacy Leads

Expected source strength:

- `high` for policy/CSP surfaces
- `medium-high` for runtime `CapabilityAccessManager` consent surfaces

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
- Local `AppPrivacy.admx` / `AppPrivacy.adml`
- `CapabilityAccessManager` runtime observations and Microsoft Q&A confirmations

Reported mappings to verify:

- `LetAppsAccessCamera`
- `LetAppsAccessMicrophone`
- `LetAppsAccessLocation`
- `LetAppsAccessContacts`
- `LetAppsAccessCalendar`
- `LetAppsAccessCallHistory`
- `LetAppsAccessEmail`
- `LetAppsAccessMessaging`
- `LetAppsAccessPhone`
- `LetAppsAccessRadios`
- `LetAppsAccessTasks`
- `LetAppsAccessNotifications`

Common policy surface:

- Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//AppPrivacy`
- Type: `REG_DWORD`
- Common values:
  - `0 = User in control`
  - `1 = Force Allow`
  - `2 = Force Deny`

Secondary runtime consent surface:

- Path: `HKLM//SOFTWARE//Microsoft//Windows//CurrentVersion//CapabilityAccessManager//ConsentStore//<capability>`
- Value: `Value`
- Type: `REG_SZ`
- Common values:
  - `"Allow"`
  - `"Deny"`

Interpretation notes:

- The `LetAppsAccess*` policy family only governs the supported app-privacy policy surface, largely for Store/UWP-style app capability control.
- `CapabilityAccessManager` is a separate runtime consent surface and should not be treated as the same thing as the policy-backed `LetAppsAccess*` family.
- Do not claim that these policy values fully govern Win32 desktop apps such as Chrome, Zoom, or Discord unless a stronger primary source proves that scope.
- If the app writes a `CapabilityAccessManager` path while the official source documents `LetAppsAccess*`, treat that as a surface mismatch until proven otherwise.

## VBS And Device Guard Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/virtualization-apps-not-work-with-hyper-v
- Local `DeviceGuard.admx` / `DeviceGuard.adml`
- Official Windows Hello / Credential Guard docs where applicable

Reported mappings to verify:

- `EnableVirtualizationBasedSecurity`
  - Runtime path: `HKLM//SYSTEM//CurrentControlSet//Control//DeviceGuard`
  - Policy path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//DeviceGuard`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `RequirePlatformSecurityFeatures`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//DeviceGuard`
  - Type: `REG_DWORD`
  - `0 = No platform-security requirement`
  - `1 = Secure Boot`
  - `3 = Secure Boot + DMA protection`

- `LsaCfgFlags`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Lsa`
  - Type: `REG_DWORD`
  - `0 = Credential Guard disabled`
  - `1 = Enabled with UEFI lock`
  - `2 = Enabled without lock`

Interpretation notes:

- VBS and Credential Guard records are security-sensitive and should default to `recommended_for_general_users = false` for disable-style profiles.
- A documented registry key does not guarantee that the setting can be turned off purely by writing that key. Firmware state, policy, Windows version, and security features such as Windows Hello or Tamper Protection can override the registry.
- Keep runtime `SYSTEM` paths and policy-backed `SOFTWARE//Policies` paths separate in the records.
- `LsaCfgFlags = 1` is especially important: registry-only rollback is not enough if UEFI lock is involved.

## Windows Hello Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/configure
- Local policy files for Hello or system policies when applicable

Reported mappings to verify:

- `Scenarios//WindowsHello -> Enable`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//DeviceGuard//Scenarios//WindowsHello`
  - Value: `Enable`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

- `AllowDomainPINLogon`
  - Path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//System`
  - Value: `AllowDomainPINLogon`
  - Type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Enabled`

Interpretation notes:

- Windows Hello and VBS can be intertwined on newer Windows 11 releases. A record that looks like a simple Hello toggle may actually depend on VBS, policy, or device enrollment state.
- Do not assume that disabling a Hello-related registry value fully detaches PIN or VBS behavior without VM validation.
- `AllowDomainPINLogon` is a policy surface. `Scenarios//WindowsHello//Enable` is a runtime/security feature surface; keep them distinct.

## Memory Management Leads

Expected source strength:

- `high` for most Win32 / KB-backed memory-management keys
- `medium` where the only surviving Microsoft source is old or version-sensitive

Primary source to review:

- Microsoft Learn Win32 docs
- KB and troubleshoot articles
- Security advisory documentation for mitigation overrides

Reported mappings to verify:

- `DisablePagingExecutive`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//Session Manager//Memory Management`
  - Value: `DisablePagingExecutive`
  - Type: `REG_DWORD`
  - `0 = Pageable`
  - `1 = Keep kernel and drivers resident`
  - Caveat: effect is limited on newer 64-bit Windows and can be overridden by runtime memory-management behavior

- `LargeSystemCache`
  - Same path
  - Type: `REG_DWORD`
  - `0 = Normal workstation cache`
  - `1 = Large system cache`
  - Caveat: old Microsoft documentation exists, but modern Windows client behavior is less cleanly aligned

- `ClearPageFileAtShutdown`
  - Same path
  - Type: `REG_DWORD`
  - `0 = Do not clear`
  - `1 = Clear at shutdown`
  - Caveat: shutdown time and storage wear can increase

- `PagedPoolSize`
  - Same path
  - Type: `REG_DWORD`
  - `0 = Automatic`
  - `0xFFFFFFFF = Maximum paged pool`
  - Caveat: modern 64-bit Windows dynamically manages this far more aggressively than older systems

- `NonPagedPoolSize`
  - Same path
  - Type: `REG_DWORD`
  - `0 = Automatic`
  - Caveat: manual tuning is much less meaningful on modern 64-bit systems

- `FeatureSettingsOverride`
- `FeatureSettingsOverrideMask`
  - Same path
  - Type: `REG_DWORD`
  - `3 + 3 = Disable Spectre/Meltdown mitigations`
  - Caveat: this is a major security decision and should never be treated as a casual-safe optimization

Interpretation notes:

- Memory-management keys can be documented without being good general-user recommendations.
- `FeatureSettingsOverride` and `FeatureSettingsOverrideMask` should default to `apply_allowed = false` and `recommended_for_general_users = false`.
- Old workstation/server tuning keys such as `LargeSystemCache`, `PagedPoolSize`, and `NonPagedPoolSize` need modern-behavior caveats even when a Microsoft source exists.

## Event Log Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-eventlogservice
- Local Event Log policy files where applicable

Reported mappings to verify:

- Runtime event-log channel path:
  - `HKLM//SYSTEM//CurrentControlSet//Services//EventLog//<LogName>`

- `MaxSize`
  - Runtime path type: `REG_DWORD`
  - Units: `bytes`
  - Common default: `20971520`
  - Caveat: minimum and alignment rules matter

- `Retention`
  - Runtime path type: `REG_DWORD`
  - `0 = Circular overwrite`
  - `0xFFFFFFFF = Never overwrite`
  - `<seconds> = Keep entries at least that long`

- `AutoBackupLogFiles`
  - Runtime path type: `REG_DWORD`
  - `0 = Disabled`
  - `1 = Backup automatically when full`

- Policy EventLog `MaxSize`
  - Policy path: `HKLM//SOFTWARE//Policies//Microsoft//Windows//EventLog//{Application|System|Security}`
  - Type: `REG_DWORD`
  - Units: `kilobytes`

Interpretation notes:

- Event Log records must clearly separate runtime channel keys from policy-backed keys.
- `MaxSize` is especially dangerous to mis-document because the runtime key uses bytes while the policy path uses kilobytes.
- Do not validate one unit system from documentation for the other surface.

## NTFS And FileSystem Leads

Expected source strength:

- `high`

Primary source to review:

- https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
- File-system and path-length Microsoft documentation

Reported mappings to verify:

- `NtfsDisableLastAccessUpdate`
  - Path: `HKLM//SYSTEM//CurrentControlSet//Control//FileSystem`
  - Type: `REG_DWORD`
  - Caveat: modern defaults can be system-managed rather than a simple `0/1`

- `NtfsDisable8dot3NameCreation`
  - Same path
  - Type: `REG_DWORD`
  - `0 = Create 8.3 names`
  - `1 = Disable creation`
  - `2 = Per-volume`
  - `3 = Disable creation without deleting existing short names`

- `LongPathsEnabled`
  - Same path
  - Type: `REG_DWORD`
  - `0 = MAX_PATH limit active`
  - `1 = Long paths enabled`
  - Caveat: application manifest support is still required

Interpretation notes:

- `NtfsDisableLastAccessUpdate` should be documented with modern system-managed defaults, not forced into a fake simple boolean.
- `LongPathsEnabled` is a real documented control surface, but it is not sufficient by itself unless the application is also long-path aware.

## Manually Verified Source URL Pool

These links were manually fetched or confirmed from search results and are safe to reuse as candidate primary sources in later proof passes. They are still lead-level inputs only; each record must capture its own `validation_proof.exact_quote_or_path` before promotion.

### TCP And Network

- `TcpAckFrequency`
  - https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/registry-entry-control-tcp-acknowledgment-behavior

- `Tcp1323Opts`, `SackOpts`, `TcpMaxDataRetransmissions`
  - https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/description-tcp-features

- `TcpMaxDataRetransmissions` detail
  - https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/modify-tcp-ip-maximum-retransmission-time-out

- General TCP/IP and NBT parameter reference
  - https://learn.microsoft.com/en-us/troubleshoot/windows-client/networking/tcpip-and-nbt-configuration-parameters

### UAC

- UAC settings and configuration
  - https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration

- How UAC works
  - https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/how-it-works

- `ConsentPromptBehaviorAdmin` protocol documentation
  - https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gpsb/341747f5-6b5d-4d30-85fc-fa1cc04038d4

- Elevation prompt behavior values
  - https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-10/security/threat-protection/security-policy-settings/user-account-control-behavior-of-the-elevation-prompt-for-administrators-in-admin-approval-mode

### Windows Settings Reference

- Windows 11 settings registry reference
  - https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-windows-11

- Windows 10 and Windows 11 shared settings registry reference
  - https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-common

### Accessibility

- StickyKeys, FilterKeys, ToggleKeys in the gaming context
  - https://learn.microsoft.com/en-us/windows/win32/dxtecharts/disabling-shortcut-keys-in-games

### Memory Management

- Memory-management registry keys
  - https://learn.microsoft.com/en-us/windows/win32/memory/memory-management-registry-keys

- `DisablePagingExecutive`
  - https://learn.microsoft.com/en-us/windows-hardware/test/wpt/kernel-trace-control-api-reference
  - Caveat: this is a real Microsoft primary source, but it is older and version-scoped and explicitly says systems with Windows 8 and higher do not need this registry change. Treat the key as documented legacy tracing guidance, not as an effective Windows 11 tweak.

- `ClearPageFileAtShutdown`
  - https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-10/security/threat-protection/security-policy-settings/shutdown-clear-virtual-memory-pagefile
  - https://learn.microsoft.com/en-us/previous-versions/windows/embedded/bb521398(v=winembedded.51)

### BitLocker

- BitLocker CSP
  - https://learn.microsoft.com/en-us/windows/client-management/mdm/bitlocker-csp

- Intune BitLocker troubleshooting
  - https://learn.microsoft.com/en-us/troubleshoot/mem/intune/device-protection/troubleshoot-bitlocker-policies

- AES-XTS value table background
  - https://learn.microsoft.com/en-us/archive/blogs/dubaisec/bitlocker-aes-xts-new-encryption-type

### LocalPoliciesSecurityOptions

- Policy CSP LocalPoliciesSecurityOptions
  - https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-localpoliciessecurityoptions

### Power Settings

- `PerfBoostMode` values
  - https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode

- Performance state engine settings
  - https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/static-configuration-options-for-the-performance-state-engine

### Remote Desktop

- `fDenyTSConnections` unattend documentation
  - https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-terminalservices-localsessionmanager-fdenytsconnections

- RDP troubleshooting
  - https://learn.microsoft.com/en-us/troubleshoot/windows-server/remote/rdp-error-general-troubleshooting

### MMCSS / Multimedia

- MMCSS reference
  - https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
  - Caveat: this is the strongest primary source currently captured for `SystemResponsiveness`; `NetworkThrottlingIndex` still lacks its own first-class Microsoft article and may need extra runtime caution

### App Privacy

- Policy CSP Privacy
  - https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy

- Manage connections from Windows components
  - https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
  - Caveat: good for policy path and value interpretation, but do not overstate scope for classic Win32 desktop apps

### Previously Confirmed In This Research Pass

- Developer Mode
  - https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development

- IPv6 `DisabledComponents`
  - https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows

## Known Unverified URL Leads

These links were called out as likely useful, but they were not yet personally fetched or confirmed in the current pass and should stay in the "candidate" bucket until opened.

- Windows Firewall CSP
  - Still useful to expand with profile-by-profile exact quotes, but the main source URL is now confirmed

- Newer Windows 11-specific primary runtime proof for `DisablePagingExecutive`
  - Candidate source still needs fetch confirmation beyond the older kernel-trace documentation

## Practical Next Step

When these leads are used in a validation pass:

1. Open the cited Microsoft page or local ADMX.
2. Capture `validation_proof.exact_quote_or_path`.
3. Decide whether the documented surface exactly matches the app write.
4. If it does not match, keep the record in `review-required` and describe the mismatch explicitly.
