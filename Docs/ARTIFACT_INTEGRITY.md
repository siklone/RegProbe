# Artifact Integrity Report

- Generated UTC: 2026-04-23T18:02:55Z
- Records scanned: 323
- Evidence items with local path references: 1185
- Evidence items marked `missing`: 163
- Evidence items marked `sha256_mismatch`: 0
- Evidence items without recorded `sha256`: 1022
- Record files updated: 110

## Notes

- The current research records store artifact references inside `evidence[].location`; there is no standalone `artifact_refs` field in the checked-in record schema.
- HTTP URLs and narrative-only locations were ignored. Only repo-relative path-like references were evaluated.
- No evidence items carried a checked-in `sha256` field during this sweep, so no SHA256 mismatches were recorded.

## Missing Artifact Samples

### `explorer.always-show-icons-never-thumbnails.review.json`

- `dump-25h2-explorer-advanced-iconsonly`
  Title: 25H2 raw registry and default-hive corroboration for IconsOnly
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.disable-low-disk-space-warning.json`

- `dump-25h2-policies-explorer-nolowdiskspacechecks`
  Title: 25H2 raw registry corroboration for NoLowDiskSpaceChecks
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.enable-explorer-compact-mode.review.json`

- `dump-25h2-explorer-advanced-usecompactmode`
  Title: 25H2 raw registry corroboration for UseCompactMode
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.hide-empty-drives.review.json`

- `dump-25h2-explorer-advanced-hidedriveswithnomedia`
  Title: 25H2 raw registry corroboration for HideDrivesWithNoMedia
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.launch-folder-windows-in-a-separate-process.review.json`

- `dump-hkcu25h2-explorer-advanced-separateprocess`
  Title: 25H2 default hive corroboration for SeparateProcess
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-compressed-and-encrypted-files-in-color.review.json`

- `dump-hkcu25h2-explorer-advanced-showcompcolor`
  Title: 25H2 default hive corroboration for ShowCompColor
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-drive-letters-first.review.json`

- `dump-25h2-explorer-showdrivelettersfirst`
  Title: 25H2 raw registry corroboration for ShowDriveLettersFirst
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.show-file-extensions.review.json`

- `dump-hkcu25h2-explorer-advanced-hidefileext`
  Title: 25H2 default hive and raw dump corroboration for HideFileExt
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.show-full-path.review.json`

- `dump-hkcu25h2-explorer-cabinetstate-fullpath`
  Title: 25H2 default hive corroboration for FullPath
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-hidden-files.review.json`

- `dump-hkcu25h2-explorer-advanced-hidden`
  Title: 25H2 default hive and raw dump corroboration for Hidden
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.show-info-tips.review.json`

- `dump-hkcu25h2-explorer-advanced-showinfotip`
  Title: 25H2 default hive corroboration for ShowInfoTip
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-protected-operating-system-files.review.json`

- `dump-25h2-explorer-advanced-showsuperhidden`
  Title: 25H2 dump and default hive corroboration for ShowSuperHidden
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-recent-items.review.json`

- `dump-25h2-explorer-showrecent`
  Title: 25H2 raw registry corroboration for ShowRecent
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `explorer.show-status-bar.review.json`

- `dump-25h2-explorer-advanced-showstatusbar`
  Title: 25H2 raw registry and default-hive corroboration for ShowStatusBar
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.show-type-overlay.review.json`

- `dump-25h2-explorer-advanced-showtypeoverlay`
  Title: 25H2 raw registry and default-hive corroboration for ShowTypeOverlay
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`
  Missing: `research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg`

### `explorer.taskbar-alignment-left.review.json`

- `dump-25h2-explorer-advanced-taskbaral`
  Title: 25H2 raw registry corroboration for TaskbarAl
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `network.disable-active-probing.review.json`

- `nohuto-ncsi-mirror`
  Title: nohuto mirror - NCSI NoActiveProbe registry evidence
  Missing: `research/_source-mirrors/win-config/network/desc.md`
  Missing: `research/_source-mirrors/win-registry/records/25H2.txt`

### `network.disable-llmnr.json`

- `local-dnsclient-admx`
  Title: Local Microsoft DnsClient.admx mapping
  Missing: `evidence/files/external/c/WINDOWS/PolicyDefinitions/DnsClient.admx`

### `network.disable-netbios-resolution.json`

- `local-dnsclient-netbios-admx`
  Title: Local Microsoft DnsClient.admx NetBIOS enum mapping
  Missing: `evidence/files/external/c/WINDOWS/PolicyDefinitions/DnsClient.admx`

### `network.disable-smart-name-resolution.json`

- `local-dnsclient-admx`
  Title: Local Microsoft DnsClient.admx mapping
  Missing: `evidence/files/external/c/WINDOWS/PolicyDefinitions/DnsClient.admx`

### `policy.system.enable-virtualization.json`

- `nohuto-uac-bootphase`
  Title: Boot-phase UAC policy cluster lead
  Missing: `research/_source-mirrors/decompiled-pseudocode/ntoskrnl/PsBootPhaseComplete.c`

### `power.control.allow-audio-to-enable-execution-required-power-requests.json`

- `vm-power-control-allow-audio-to-enable-execution-required-power-requests-kd-symbol-20260408`
  Title: Sibling KVM local-KD wildcard sweep surfaces PopPowerRequestActiveAudioEnablesExecutionRequired
  Missing: `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/local-kd-allowsystemrequired-20260408a.log`
- `vm-power-control-allow-audio-to-enable-execution-required-power-requests-kd-value-20260408`
  Title: KVM local-KD resolves live PopPowerRequestActiveAudioEnablesExecutionRequired = 1
  Missing: `evidence/files/vm-tooling-staging/local-kd-allowaudio-20260408a/local-kd-allowaudio-20260408a.log`
- `vm-power-control-allow-audio-to-enable-execution-required-power-requests-kd-reader-20260408`
  Title: KVM local-KD disassembly shows current-build consumer for PopPowerRequestActiveAudioEnablesExecutionRequired
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.log`
- `vm-power-control-execution-required-setting-lineage-kd-20260408`
  Title: KVM local-KD wildcard pass narrows execution-required setting lineage
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a.log`
- `vm-power-control-execution-required-init-lineage-kd-20260408`
  Title: KVM local-KD disassembly shows init and override path without a visible registry read
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a.log`

### `power.control.allow-system-required-power-requests.json`

- `vm-power-control-allow-system-required-power-requests-kd-20260408`
  Title: KVM local-KD resolves live PopPowerRequestConvertSystemToExecution = 1
  Missing: `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/local-kd-allowsystemrequired-20260408a.log`
- `vm-power-control-allow-system-required-power-requests-kd-reader-20260408`
  Title: KVM local-KD disassembly shows current-build consumers for PopPowerRequestConvertSystemToExecution
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.log`
- `vm-power-control-execution-required-setting-lineage-kd-20260408`
  Title: KVM local-KD wildcard pass narrows execution-required setting lineage
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-settinglineage-20260408a/local-kd-powerrequest-settinglineage-20260408a.log`
- `vm-power-control-execution-required-init-lineage-kd-20260408`
  Title: KVM local-KD disassembly shows init and override path without a visible registry read
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a.log`
- `vm-power-control-execution-required-umpo-lineage-kd-20260408`
  Title: KVM local-KD disassembly shows UMPO override-query lineage without a visible registry read
  Missing: `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.log`

### `power.control.power-watchdog-timeout-cluster.json`

- `enrichment-power-watchdog-timeout-cluster-20260403`
  Title: Enrichment output converges on the same runtime family for all five values
  Missing: `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/power.control.power-watchdog-*-timeout-msec.json`

### `power.disable-cpu-idle-states.json`

- `nohuto-power-disable-idle-states-trace`
  Title: nohuto power trace for DisableIdleStatesAtBoot
  Missing: `research/_source-mirrors/win-registry/records/Power.txt`
