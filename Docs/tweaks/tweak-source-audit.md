# Tweak Source Audit (Generated)

Total tweaks: 307

Missing documentation: 8

| ID | Name | Call | Missing Tokens | Source |
| --- | --- | --- | --- | --- |
| `explorer.disable-taskbar-chat` | Hide Taskbar Chat Icon | CreateRegistryTweak | Software\Policies\Microsoft\Windows\Windows Chat, ChatIcon | `WindowsOptimizer.App\Services\TweakProviders\VisibilityTweakProvider.cs#L214` |
| `peripheral.disable-autoplay` | Disable AutoPlay | CreateRegistryValueBatchTweak | Software\Microsoft\Windows\CurrentVersion\Policies\Explorer, NoDriveTypeAutoRun, Software\Policies\Microsoft\Windows\Explorer, NoAutoplayfornonVolume | `WindowsOptimizer.App\Services\TweakProviders\PeripheralTweakProvider.cs#L40` |
| `power.disable-windows-search` | Disable Windows Search | CreateServiceStartModeBatchTweak | WSearch | `WindowsOptimizer.App\Services\TweakProviders\PerformanceTweakProvider.cs#L68` |
| `privacy.disable-advertising-id` | Disable Advertising ID | CreateRegistryTweak | Software\Policies\Microsoft\Windows\AdvertisingInfo, DisabledByGroupPolicy | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L34` |
| `privacy.disable-find-my-device` | Disable Find My Device | CreateRegistryTweak | SOFTWARE\Policies\Microsoft\FindMyDevice, AllowFindMyDevice | `WindowsOptimizer.App\Services\TweakProviders\PrivacyTweakProvider.cs#L663` |
| `security.disable-enhanced-defender-notifications` | Hide Non-Critical Windows Security Notifications | CreateRegistryTweak | Software\Policies\Microsoft\Windows Defender Security Center\Notifications, DisableEnhancedNotifications | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L94` |
| `security.disable-system-restore` | Disable System Restore | CreateRegistryTweak | SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore, DisableSR | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L277` |
| `security.enable-defender-maps-advanced-membership` | Join Microsoft MAPS (Advanced) | CreateRegistryTweak | Software\Policies\Microsoft\Windows Defender\Spynet, SpyNetReporting | `WindowsOptimizer.App\Services\TweakProviders\SecurityTweakProvider.cs#L106` |
