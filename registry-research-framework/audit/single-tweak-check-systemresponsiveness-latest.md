Query: SystemResponsiveness
Expected values: 10, 30000
Status: ok
Matches: 2

[1] power.disable-network-power-saving.policy
  promotion: promoted | record: validated | apply_allowed: true
  rollback: restore_default=true | restore_previous=true
  record_file: research/records/power.disable-network-power-saving.policy.review.json
  app_card: Network Power and Multimedia Responsiveness [Power/Registry]
  card_description: Writes the documented DisableTaskOffload and MMCSS SystemResponsiveness values while excluding the archived opaque NetworkThrottlingIndex write.
  research_surface: present (Power)
  research_doc: research/records/power.disable-network-power-saving.policy.review.json
  summary: This child record keeps only the documented DisableTaskOffload and SystemResponsiveness values. SystemResponsiveness is supported here for path plus rounding/clamping behavior; the opaque NetworkThrottlingIndex write remains outside this child in the deprecated parent audit trail.
  tracked_targets:
    - HKLM\System\CurrentControlSet\Services\TCPIP\Parameters :: DisableTaskOffload [REG_DWORD]
      * 0 -> Task offload enabled
      * 1 -> Task offload disabled
    - HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile :: SystemResponsiveness [REG_DWORD]
      * 10 -> Current app child value
  app_writes:
    - HKLM\System\CurrentControlSet\Services\TCPIP\Parameters :: DisableTaskOffload = 0
    - HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile :: SystemResponsiveness = 10
  validation_proof:
    - source_url: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
    - exact_quote_or_path: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\SystemResponsiveness; values not divisible by 10 are rounded down to the nearest multiple of 10.
    - key_found_on_page: true
  runtime_read_signals:
    - etw-trace: QGA-first ETW stackwalk probe receipt (evidence/captures/power-disable-network-power-saving-policy-etw-qga-unblock-20260507.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507.etl and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507.etl)
  expected_value 10: matched
    - allowed_values:
      * SystemResponsiveness = 10 (Current app child value)
    - app_writes:
      * SystemResponsiveness = 10
    - profiles:
      * Windows default -> system-responsiveness = 10
    - validation_proof_text: matched
  expected_value 30000: not found
  code_hits:
    - app/Services/TweakProviders/PowerTweakProvider.cs
    - app/Services/TweakProviders/PowerTweakProvider.cs:42 -> yield return CreateRegistryValueBatchTweak(
    - app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs
    - app/Services/TweakProviders/PowerTweakProvider.cs:46 -> "Writes the documented DisableTaskOffload and MMCSS SystemResponsiveness values while excluding the archived opaque NetworkThrottlingIndex write.",
    - app/Services/TweakProviders/PowerTweakProvider.cs:51 -> new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)
    - engine/Tweaks/Power/NetworkAdapterPowerTweaks.cs:18 -> new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)

[2] power.disable-network-power-saving
  promotion: rejected | record: deprecated | apply_allowed: false
  rollback: restore_default=false | restore_previous=true
  record_file: research/records/power.disable-network-power-saving.review.json
  research_surface: missing
  summary: Deprecated audit trail for the mixed network power-saving bundle. The official TCP/IP offload and MMCSS values are split into a child record; the opaque NetworkThrottlingIndex value remains tracked only here.
  tracked_targets:
    - HKLM\System\CurrentControlSet\Services\TCPIP\Parameters :: DisableTaskOffload [REG_DWORD]
      * 0 -> Task offload enabled
      * 1 -> Task offload disabled
    - HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile :: SystemResponsiveness [REG_DWORD]
      * 10 -> App responsiveness value
    - HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile :: NetworkThrottlingIndex [REG_DWORD]
      * None -> Primary-source mapping not yet confirmed
  validation_proof:
    - source_url: research/_source-mirrors/win-config/network/desc.md
    - exact_quote_or_path: RegSetValue HKLM\System\CurrentControlSet\Services\LanmanWorkstation\Parameters\DisableBandwidthThrottling Type: REG_DWORD, Length: 4, Data: 1. Task offloading has to be enabled, or RSS won't work (DisableTaskOffload). *RssOrVmqPreference = 0; // range 0-1; 0 Report RSS capabilities - 1 Report VMQ capabilities.
    - key_found_on_page: true
  runtime_read_signals:
    - etw-trace: QGA-first ETW stackwalk probe receipt (evidence/captures/power-disable-network-power-saving-etw-qga-unblock-20260507.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507.etl and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507.etl and 3 additional raw artifact refs listed in the receipt)
  expected_value 10: matched
    - allowed_values:
      * SystemResponsiveness = 10 (App responsiveness value)
  expected_value 30000: not found
  code_hits:
    - app/Services/TweakProviders/PowerTweakProvider.cs
    - app/Services/TweakProviders/PowerTweakProvider.cs:46 -> "Writes the documented DisableTaskOffload and MMCSS SystemResponsiveness values while excluding the archived opaque NetworkThrottlingIndex write.",
    - app/Services/TweakProviders/PowerTweakProvider.cs:51 -> new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)
    - engine/Tweaks/Power/NetworkAdapterPowerTweaks.cs:18 -> new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", RegistryValueKind.DWord, 10, RegistryView.Default)
    - engine/Tweaks/Power/NetworkAdapterPowerTweaks.cs:24 -> description: "Writes the documented DisableTaskOffload and MMCSS SystemResponsiveness values while excluding the archived opaque NetworkThrottlingIndex write.",
    - infrastructure/Commands/CommandAllowlist.cs:234 -> AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100");
