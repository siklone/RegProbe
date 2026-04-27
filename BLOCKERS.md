# Research Blockers

## Open Blockers

### 2026-04-27T11:39:50Z - QGA guest-exec failure and ensure-admin-shell timeouts for batch-20260427-134500

- status: open

- Scope: `privacy.disable-steps-recorder`, `visibility.disable-acrylic-logon`, `system.ntfs-disable-last-access`, `privacy.limit-dump-collection`, and `privacy.disable-find-my-device` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: after the earlier 2026-04-27 network sweep, this follow-up batch could no longer get stable QGA `guest-exec` responses. Every ETW retry failed during guest-dir setup with `qga-powershell-launch-error`, fell back to send-key transport, and then still timed out in `ensure-admin-shell` before any ETW artifact reached the host. Fast same-day guest Ghidra retries for the same values also failed in `ensure-admin-shell` with bounded timeouts, so no xref bundles reached the host.
- Mitigation: retain the ETW host summaries under `evidence/raw/etw-stackwalk/*-20260427a/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260427a/summary.json`, and treat this batch as a VM-control failure rather than runtime/static proof.
- Action: reboot or manually heal the guest agent before the next VM-backed sweep batch; until then, continue only with host-side/source-doc lanes for these records.

### 2026-04-27T10:28:11Z - ETW ingest-missing-etl receipts and guest Ghidra ensure-admin-shell timeouts for batch-20260427-115400 follow-up

- status: open

- Scope: `network.disable-smart-name-resolution`, `network.disable-plaintext-smb-passwords`, `network.disable-smb1`, and `network.disable-wifi-sense` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-27 ETW retries for `HKLM\Software\Policies\Microsoft\Windows NT\DNSClient` / `DisableSmartNameResolution`, `HKLM\System\CurrentControlSet\Services\LanmanWorkstation\Parameters` / `EnablePlainTextPassword`, `HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters` / `SMB1`, and `HKLM\Software\Microsoft\wcmsvc\wifinetworkmanager\config` / `AutoConnectAllowedOEM` all published host-visible summary and stage receipts, but ETL upload never completed so ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. Fast same-day guest Ghidra retries for the same values all failed in `ensure-admin-shell` with bounded 90 second wrapper timeouts, so no xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260427a/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260427b/summary.json`, and treat the attempts as transport or launcher failures rather than runtime/static proof.
- Action: continue with retained docs/source-mirror review for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-27T09:38:26Z - ETW tracerpt timeout and Ghidra launcher-exception for network.disable-netbios-resolution

- status: open

- Scope: `network.disable-netbios-resolution` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: after the 2026-04-27 VM reboot, a fresh ETW retry for `HKLM\Software\Policies\Microsoft\Windows NT\DNSClient` / `EnableNetbios` progressed farther than the older `ingest-missing-etl` failures and published host-visible summary/stage receipts showing `stage = tracerpt`, but it still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. A same-day guest Ghidra retry also progressed past the earlier `ensure-admin-shell` breakage, but still ended in `launcher-exception` because no `evidence.json` bundle was produced under the guest output root.
- Mitigation: keep the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/network-disable-netbios-resolution-etw-20260427a/`, keep the compact attempt summary in `evidence/captures/network-disable-netbios-resolution-etw-stackwalk-attempt-20260427.json`, retain the guest Ghidra launcher receipt under `evidence/raw/ghidra/ghidra-network-disable-netbios-resolution-20260427a/summary.json`, and treat both attempts as transport or launcher failures rather than runtime/static proof.
- Action: continue with retained docs/source-mirror lanes for `network.disable-netbios-resolution` while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T19:42:22Z - ETW ingest-missing-etl receipts and guest Ghidra launch failures for batch-20260424-192723

- status: open

- Scope: `developer.dotnet-telemetry-disable`, `developer.nodejs-performance`, `explorer.show-recent-items`, `explorer.show-status-bar`, and `network.disable-ipv6` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKCU\Environment` / `DOTNET_CLI_TELEMETRY_OPTOUT`, `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment` / `NODE_OPTIONS`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer` / `ShowRecent`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `ShowStatusBar`, and `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters` / `DisabledComponents` all published host-visible summary and stage receipts, but ETL upload never completed and ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. The paired guest Ghidra retries for the same values all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260424-batch6/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260424-batch6/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained official docs, Procmon/runtime captures, source-mirror review, and non-guest runtime lanes for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T19:12:25Z - ETW ingest-missing-etl receipts and guest Ghidra launch failures for batch-20260424-194855

- status: open

- Scope: `explorer.show-compressed-and-encrypted-files-in-color`, `explorer.show-drive-letters-first`, `explorer.show-file-extensions`, `explorer.show-full-path`, and `explorer.show-hidden-files` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `ShowCompColor`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer` / `ShowDriveLettersFirst`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `HideFileExt`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState` / `FullPath`, and `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `Hidden` all published host-visible summary and stage receipts, but ETL upload never completed and ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. The paired guest Ghidra retries for the same values all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260424-batch5/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260424-batch5/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained Explorer docs, Procmon runtime captures, source-mirror review, and non-guest runtime lanes for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T16:22:00Z - ETW ingest-missing-etl receipts and guest Ghidra launch failures for batch-20260424-191015

- status: open

- Scope: `explorer.always-show-icons-never-thumbnails`, `explorer.hide-empty-drives`, `explorer.launch-folder-windows-in-a-separate-process`, `explorer.show-info-tips`, and `explorer.show-protected-operating-system-files` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `IconsOnly`, `HideDrivesWithNoMedia`, `SeparateProcess`, `ShowInfoTip`, and `ShowSuperHidden` all published host-visible summary and stage receipts, but ETL upload never completed and ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. The paired guest Ghidra retries for the same values all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260424-batch4/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260424-batch4/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained Explorer docs, Procmon runtime captures, source-mirror review, and non-guest runtime lanes for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T14:59:09Z - ETW ingest-missing-etl receipts and guest Ghidra launch failures for batch-20260424-182120

- status: open

- Scope: `developer.powershell-execution`, `developer.python-path-fix`, `developer.ssh-agent-autostart`, `network.disable-llmnr`, and `network.disable-mdns` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKLM\SOFTWARE\Policies\Microsoft\Windows\PowerShell` / `ExecutionPolicy`, `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem` / `LongPathsEnabled`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` / `SSH Agent`, `HKLM\Software\Policies\Microsoft\Windows NT\DNSClient` / `EnableMulticast`, and `HKLM\Software\Policies\Microsoft\Windows NT\DNSClient` / `EnableMDNS` all published host-visible summary and stage receipts, but ETL upload never completed and ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. The paired guest Ghidra retries for `ExecutionPolicy`, `LongPathsEnabled`, `SSH Agent`, `EnableMulticast`, and `EnableMDNS` all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260424-batch3/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260424-batch3/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained docs, policy metadata, source-mirror review, and non-guest runtime lanes for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T14:30:26Z - ETW ingest-missing-etl receipts and guest Ghidra launch failures for batch-20260424-171615

- status: open

- Scope: `audio.disable-beep`, `developer.enable-windows-long-paths`, `developer.vscode-git-autofetch`, `explorer.disable-taskbar-chat`, and `network.disable-active-probing` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKLM\SYSTEM\CurrentControlSet\Services\Beep` / `Start`, `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem` / `LongPathsEnabled`, `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced` / `DisableGitAutofetch`, `HKLM\Software\Policies\Microsoft\Windows\Windows Chat` / `ChatIcon`, and `HKLM\Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator` / `NoActiveProbe` all published host-visible summary and stage receipts, but ETL upload never completed and ingest stopped with `error_kind = ingest-missing-etl` before any normalized bundle reached the repo. The paired guest Ghidra retries for `Beep`, `LongPathsEnabled`, `DisableGitAutofetch`, `ChatIcon`, and `NoActiveProbe` all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the ETW summary/stage receipts under `evidence/raw/etw-stackwalk/*-20260424-batch2/`, keep the compact attempt summaries in `evidence/captures/`, retain the guest Ghidra launcher summaries under `evidence/raw/ghidra/ghidra-*-20260424-batch2/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained docs, policy metadata, source-mirror review, and non-guest runtime lanes for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T13:07:44Z - ETW stage-only receipts and guest Ghidra launch failures for batch-20260424-154913

- status: open

- Scope: `power.disable-windows-search`, `power.control.hiber-file-size-percent`, `audio.disable-spatial-audio`, `developer.windows-dev-mode`, and `explorer.disable-low-disk-space-warning` bounded retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the 2026-04-24 bounded ETW retries for `HKLM\SYSTEM\CurrentControlSet\Services\WSearch` / `Start`, `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Audio` / `DisableSpatialOnLowLatency`, `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock` / `AllowDevelopmentWithoutDevLicense`, and `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer` / `NoLowDiskSpaceChecks` each reached `stage = artifact-upload` and only published host-visible stage receipts before the hard 90 second timeout fired. The paired guest Ghidra retries for `WSearch`, `HiberFileSizePercent`, `DisableSpatialOnLowLatency`, `AllowDevelopmentWithoutDevLicense`, and `NoLowDiskSpaceChecks` all failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error` before any xref bundle reached the host.
- Mitigation: retain the raw receipts under `evidence/raw/etw-stackwalk/*-20260424-batch1/` and `evidence/raw/ghidra/ghidra-*-20260424-batch1/`, keep the compact ETW attempt summaries in `evidence/captures/`, and treat these bounded retries as transport or guest-launch failures rather than runtime/static proof.
- Action: continue with retained docs, Procmon, KD, and source-mirror evidence for these records while leaving exact ETW and guest Ghidra closure to a later manual VM pass.

### 2026-04-24T21:40:00Z - ETW artifact-upload timeout and guest Ghidra launch failure for Windows Search service control

- status: open

- Scope: `system.services.disable-windows-search` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Services\\WSearch` reached `stage = artifact-upload` and published host-visible summary and stage receipts, but no ETL, XML, or normalized bundle reached the repo before the hard 90 second timeout fired. The paired guest Ghidra string-xref retry for `WSearch` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no bounded xref artifact reached the host.
- Mitigation: keep the ETW summary/stage receipts in `evidence/raw/etw-stackwalk/system-services-disable-windows-search-etw-20260424-main/` plus `evidence/captures/system-services-disable-windows-search-etw-stackwalk-attempt-20260424.json`, retain the guest Ghidra launcher summary in `evidence/raw/ghidra/ghidra-system-services-disable-windows-search-20260424-main/summary.json`, and avoid repeating the same bounded guest probes from this Linux host until the admin-shell/bootstrap path is repaired in a manual VM session.
- Action: continue with official service guidance and retained SCM snapshot evidence for `WSearch` while leaving exact ETW and guest Ghidra closure for a later manual VM pass.
### 2026-04-24T20:55:00Z - ETW artifact-upload timeout for policy.system.enable-virtualization

- status: open

- Scope: `policy.system.enable-virtualization` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System` / `EnableVirtualization` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/policy.system.enable-virtualization-etw-20260424i/` plus `evidence/captures/policy-system-enable-virtualization-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `policy.system.enable-virtualization` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:55:00Z - ETW artifact-upload timeout for power.control.lid-reliability-state

- status: open

- Scope: `power.control.lid-reliability-state` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power` / `LidReliabilityState` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/power.control.lid-reliability-state-etw-20260424i/` plus `evidence/captures/power-control-lid-reliability-state-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `power.control.lid-reliability-state` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:55:00Z - ETW artifact-upload timeout for power.control.mf-buffering-threshold

- status: open

- Scope: `power.control.mf-buffering-threshold` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power` / `MfBufferingThreshold` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/power.control.mf-buffering-threshold-etw-20260424i/` plus `evidence/captures/power-control-mf-buffering-threshold-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `power.control.mf-buffering-threshold` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:55:00Z - ETW artifact-upload timeout for power.control.perf-calculate-actual-utilization

- status: open

- Scope: `power.control.perf-calculate-actual-utilization` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power` / `PerfCalculateActualUtilization` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/power.control.perf-calculate-actual-utilization-etw-20260424i/` plus `evidence/captures/power-control-perf-calculate-actual-utilization-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `power.control.perf-calculate-actual-utilization` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:55:00Z - ETW artifact-upload timeout for power.control.class1-initial-unpark-count

- status: open

- Scope: `power.control.class1-initial-unpark-count` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power` / `Class1InitialUnparkCount` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/power.control.class1-initial-unpark-count-etw-20260424i/` plus `evidence/captures/power-control-class1-initial-unpark-count-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `power.control.class1-initial-unpark-count` while leaving exact-read ETW closure for a later manual VM pass.

### 2026-04-24T20:35:00Z - ETW artifact-upload timeout for power.control.timer-rebase-threshold-on-drips-exit

- status: open

- Scope: `power.control.timer-rebase-threshold-on-drips-exit` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power` / `TimerRebaseThresholdOnDripsExit` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/power.control.timer-rebase-threshold-on-drips-exit-etw-20260424h/` plus `evidence/captures/power-control-timer-rebase-threshold-on-drips-exit-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `power.control.timer-rebase-threshold-on-drips-exit` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:35:00Z - ETW artifact-upload timeout for audio.show-disconnected-devices

- status: open

- Scope: `audio.show-disconnected-devices` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Microsoft\\Multimedia\\Audio\\DeviceCpl` / `ShowDisconnectedDevices` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/audio.show-disconnected-devices-etw-20260424h/` plus `evidence/captures/audio-show-disconnected-devices-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `audio.show-disconnected-devices` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:35:00Z - ETW artifact-upload timeout for developer.terminal-dev-mode

- status: open

- Scope: `developer.terminal-dev-mode` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Microsoft\\Windows Terminal` / `DeveloperMode` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/developer.terminal-dev-mode-etw-20260424h/` plus `evidence/captures/developer-terminal-dev-mode-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `developer.terminal-dev-mode` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:35:00Z - ETW artifact-upload timeout for explorer.enable-explorer-compact-mode

- status: open

- Scope: `explorer.enable-explorer-compact-mode` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced` / `UseCompactMode` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/explorer.enable-explorer-compact-mode-etw-20260424h/` plus `evidence/captures/explorer-enable-explorer-compact-mode-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `explorer.enable-explorer-compact-mode` while leaving exact-read ETW closure for a later manual VM pass.
### 2026-04-24T20:35:00Z - ETW artifact-upload timeout for performance.disable-taskbar-animations

- status: open

- Scope: `performance.disable-taskbar-animations` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced` / `TaskbarAnimations` reached `stage = artifact-upload` and published host-visible summary/stage receipts, but still hit the hard 90 second timeout before any ETL, XML, or normalized bundle reached the repo. The retained receipt shows `error_kind = runner-timeout` and `transport_blocker = timeout`, so this remains a transport failure rather than runtime proof.
- Mitigation: keep the summary/stage receipt in `evidence/raw/etw-stackwalk/performance.disable-taskbar-animations-etw-20260424h/` plus `evidence/captures/performance-disable-taskbar-animations-etw-stackwalk-attempt-20260424.json`, treat the lane as reviewed but transport-blocked, and avoid repeating the same bounded Linux-host ETW retry until the QGA upload chain is debugged in a manual VM session.
- Action: continue with retained static/docs/procmon evidence for `performance.disable-taskbar-animations` while leaving exact-read ETW closure for a later manual VM pass.

### 2026-04-24T06:20:50Z - Guest Ghidra launch failure after retained ETW review for deprecated AllowRemoteDASD path

- status: open

- Scope: `system.io-allow-remote-dasd` bounded guest Ghidra retry through `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py` plus the same-day bounded ETW stackwalk review through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: the fresh 2026-04-24 ETW rerun for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\I/O System` / `AllowRemoteDASD` did retain ETL, XML, summary, and normalized-bundle artifacts, and the helper `reg.exe` probe confirmed `AllowRemoteDASD = 0` at the intended key. The retained bundle still exposed no exact `AllowRemoteDASD` registry-touch event for the deprecated Session Manager I/O lane. The paired guest Ghidra retry then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no new wrapper stage or bounded xref artifact landed from the guest.
- Mitigation: treat the ETW rerun as a retained no-hit/runtime-review artifact instead of exact helper-query proof, keep the record anchored in its existing path-aware Ghidra, local-KD, Procmon, and Microsoft policy collision evidence, and avoid repeating the same bounded guest Ghidra probe from this Linux host until the admin-shell bootstrap path is repaired in a manual VM session.
- Action: continue to carry this record as a historical collision trail and only revisit guest-side xref work after the VM bootstrap/admin-shell failure is cleared.

### 2026-04-24T06:20:50Z - DpcWatchdogPeriod bounded ETW retry retained no payload and guest Ghidra launch failed

- status: open

- Scope: `system.kernel-dpc-watchdog-period` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel` / `DpcWatchdogPeriod` reached the helper `reg.exe` probe and again showed the value absent, but xperf stop and tracerpt did not retain ETL or XML output. The repo now has only summary and stage receipts for that attempt, not a repo-ingestable runtime bundle or exact registry-touch capture. The paired guest Ghidra retry then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no new wrapper stage or bounded xref artifact reached the host.
- Mitigation: treat both attempts as transport/runtime-lane failures rather than exact proof, keep the record on its existing Microsoft-doc plus current-build KD/Ghidra live-zero and writer-path evidence, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with retained documentation, KD, and prior Ghidra evidence for DpcWatchdogPeriod while deferring further bounded guest ETW/Ghidra retries to a later manual VM pass.

### 2026-04-24T06:08:48Z - ETW artifact-upload timeout and guest Ghidra launch failure for Threat File Hash Logging

- status: open

- Scope: `security.threat-file-hash-logging` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender` / `ThreatFileHashLogging` progressed past capture and tracerpt, but still hit the hard 90 second host timeout at `stage = artifact-upload` before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `ThreatFileHashLogging` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the child record on its Microsoft-doc and MsMpEng-runtime lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with documentation-backed and retained runtime evidence for this child while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T06:06:13Z - ETW artifact-upload timeout and guest Ghidra launch failure for Enable Sudo

- status: open

- Scope: `security.enable-sudo` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\Software\\Policies\\Microsoft\\Windows\\Sudo` / `Enabled` progressed past capture and tracerpt, but still hit the hard 90 second host timeout at `stage = artifact-upload` before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `Enabled` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the record on its local ADMX/ADML-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official policy and app-path evidence for this record while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T06:03:51Z - ETW artifact-upload timeout and guest Ghidra launch failure for Defender MAPS advanced membership

- status: open

- Scope: `security.enable-defender-maps-advanced-membership` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet` / `SpyNetReporting` progressed past capture and tracerpt, but still hit the hard 90 second host timeout at `stage = artifact-upload` before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `SpyNetReporting` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the child record on its documentation-plus-Procmon lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with Microsoft policy, app-path, and Procmon evidence for this child while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:34:43Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Suggestions policy child

- status: open

- Scope: `privacy.disable-suggestions.policy` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent` / `DisableThirdPartySuggestions` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `DisableThirdPartySuggestions` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the named policy child on its ADMX/ADML-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with documentation-backed evidence for this child while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:32:19Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Suggestions CDM bundle

- status: open

- Scope: `privacy.disable-suggestions-cdm` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent` / `DisableThirdPartySuggestions` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `DisableThirdPartySuggestions` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the mixed CDM record on its repo-doc and official-policy lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with documentation-backed evidence for this mixed CDM lane while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:29:39Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Cross-Device Experiences mixed parent

- status: open

- Scope: `privacy.disable-cross-device-experiences` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\Software\\Policies\\Microsoft\\Windows\\System` / `EnableCdp` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `EnableCdp` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the mixed parent record on its documentation-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official policy/decompiled Settings/Procmon evidence for this parent while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:27:14Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Cross-Device Experiences policy child

- status: open

- Scope: `privacy.disable-cross-device-experiences.policy` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\Software\\Policies\\Microsoft\\Windows\\System` / `EnableCdp` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `EnableCdp` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the child record on its documentation-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official CSP/ADMX and source-mirror evidence for this child while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:24:45Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Network Power Saving parent lane

- status: open

- Scope: `power.disable-network-power-saving` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Services\\TCPIP\\Parameters` / `DisableTaskOffload` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `DisableTaskOffload` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the parent record on its repo-doc and official-doc lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with documentation-backed evidence for the parent lane while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:12:15Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Power Throttling

- status: open

- Scope: `power.disable-power-throttling` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling` / `PowerThrottlingOff` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `PowerThrottlingOff` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the record on its policy-backed documentation lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official policy and source-mirror evidence for this record while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:08:40Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Network Power Saving policy child

- status: open

- Scope: `power.disable-network-power-saving.policy` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Services\\TCPIP\\Parameters` / `DisableTaskOffload` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `DisableTaskOffload` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the child record on its documentation-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official-doc and source-mirror evidence for this child record while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T05:08:05Z - ETW tracerpt timeout and guest Ghidra launch failure for Disable Default Shares

- status: open

- Scope: `network.disable-default-shares` bounded ETW and guest Ghidra retries through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` and `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the fresh 2026-04-24 ETW retry for `HKLM\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters` / `AutoShareServer` advanced far enough to publish `stage = tracerpt`, but still hit the hard 90 second host timeout before any summary, ETL, XML, or normalized bundle was ingested into the repo. The paired guest Ghidra retry for `AutoShareServer` then failed in `ensure-admin-shell` with `error_kind = ghidra-string-launch-error`, so no wrapper stage or evidence bundle reached the host.
- Mitigation: treat both attempts as guest-control / post-process transport failures rather than runtime or static proof, keep the record on its documentation-backed lane, and avoid repeating the same bounded guest probes from this Linux host until the VM bootstrap/admin-shell path is repaired in a manual session.
- Action: continue with official-doc and source-mirror evidence for this record while leaving ETW and guest Ghidra closure for a later manual VM pass.

### 2026-04-24T04:37:39Z - Repeat ETW stackwalk transport failure for Win32CalloutWatchdogBugcheckEnabled

- status: open

- Scope: `power.session-win32-callout-watchdog-bugcheck-enabled` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: a fresh 2026-04-24 retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power` / `Win32CalloutWatchdogBugcheckEnabled` again failed before any host-visible bridge artifact arrived. The runner first surfaced `error_kind = qga-powershell-launch-error` at `stage = ensure-guest-dir` with `Guest agent is not responding: QEMU guest agent is not connected`, then fell back to `send-key` transport and still timed out under the hard 90 second outer budget without uploading any `*-summary.json`, `*-stage.json`, ETL, XML, or normalized bundle.
- Mitigation: treat the retry as another VM transport/control failure rather than runtime evidence, keep the record unexecuted for ETW purposes, and avoid more bounded Linux-host ETW retries for this sibling until the guest-agent/bootstrap path is repaired in a manual VM session.
- Action: continue with retained KD/Ghidra/init-descriptor/source-mirror evidence for the sibling and leave exact-read ETW closure to a later manual VM pass.

### 2026-04-24T05:02:19Z - Repeat ETW stackwalk bridge timeout for DPC watchdog control cluster representative

- status: open

- Scope: `system.kernel-dpc-watchdog-control-cluster` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` using representative value `DPCTimeout`.
- Blocker: the fresh 2026-04-24 bounded retry again ran with the hard `--timeout-seconds 90` ceiling and still failed to deliver repo evidence. This time the host-visible timeout summary showed `stage = artifact-upload` / `status = starting`, with no ETL, XML, normalized bundle, or other bridge artifact uploaded before the deadline fired.
- Mitigation: treat this as another transport/bridge failure instead of runtime evidence, keep the cluster on its existing exact-read gap, and avoid repeating the same bounded ETW attempt from this Linux host until the QGA upload chain is debugged with a manual VM session or a narrower host-visible artifact path.
- Action: continue with retained KD/Ghidra/init-descriptor evidence for the family and defer further exact-read ETW retries for this representative.

### 2026-04-24T05:02:19Z - Repeat guest Ghidra launch failure for PowerWatchdog timeout cluster representative

- status: open

- Scope: `power.control.power-watchdog-timeout-cluster` bounded guest-side string-xref retry through `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py` using representative value `PowerWatchdogPoCalloutTimeoutMsec`.
- Blocker: the fresh 2026-04-24 bounded retry returned `status = error`, `error_kind = ghidra-string-launch-error`, and failed in `ensure-admin-shell` after the admin-shell recovery helper itself timed out. No guest wrapper stage advanced and no Ghidra evidence bundle reached the host, so the attempt is still transport-only and not static proof for the family.
- Mitigation: keep the family on docs-first / ETW-helper hold, treat the failed guest Ghidra attempt as a wrapper-control issue instead of a no-hit static result, and avoid re-running the same guest string-xref probe from this Linux host until the guest launcher path is debugged in a manual VM session.
- Action: continue with retained source-enrichment, ETW-adjacent review, or other non-guest-Ghidra lanes for the `PowerWatchdog*TimeoutMsec` family.

### 2026-04-23T18:40:11Z - ETW stackwalk bridge artifact timeout for Win32CalloutWatchdogBugcheckEnabled

- status: open

- Scope: `power.session-win32-callout-watchdog-bugcheck-enabled` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`.
- Blocker: a QGA-launched ETW stackwalk retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power` / `Win32CalloutWatchdogBugcheckEnabled` was run with `--timeout-seconds 90`, `--first-artifact-timeout-seconds 90`, and an outer host `timeout 90s`, but no bridge artifact (`*-summary.json`, `*-stage.json`, ETL, or XML) reached the host before the hard timeout fired.
- Mitigation: treat this as a transport/bridge failure instead of runtime evidence, do not mark the record executed, and avoid re-running this exact ETW lane from the Linux host until the QGA upload chain is debugged with a manual VM session or a narrower host-visible artifact path.
- Action: skip further bounded ETW retries for this sibling in the current sweep, keep the record on static/runtime-hold, and continue with local source-enrichment or other non-ETW lanes.

### 2026-04-23T09:48:32Z - Guest Ghidra string-xref probe stalls after admin shell ready

- status: open

- Scope: `power.session-win32-callout-watchdog-bugcheck-enabled` guest-side string-xref retry through `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the KVM/QGA launcher reached `admin-shell-ready`, but the retained launcher stage stayed at `invoke-wrapper` with status `starting` and no evidence bundle was uploaded back to the host. This matches the broader guest-control hang pattern rather than a record-specific static-analysis result.
- Mitigation: host-side Ghidra runners now clear stale stage files and fail fast with a `guest-launcher-stall` summary when `launcher-stage` stays in `starting` beyond the configured stall threshold, so the lane no longer burns the full outer timeout before surfacing the blocker.
- Action: skip further bounded guest Ghidra retries from this Linux host, keep the sibling on local/static hold, and wait for either a manual VM session or a stronger host-side static pivot.

### 2026-04-23T04:49:51Z - Phase 3 WinDbg boot registry trace on KVM

- status: open

- Scope: remaining `no-hit` power/kernel records after the PowerRequestOverride ETW call-stack capture.
- Blocker: the repo contains a WinDbg boot registry trace lane for VMware serial-pipe debugging (`registry-research-framework/tools/run-windbg-boot-registry-trace.ps1` plus `execute-windbg-boot-registry-trace.ps1`), but no KVM/vmrun wrapper that can attach WinDbg/KD during guest boot and arm `CmQueryValueKey`, `NtQueryValueKey`, or `nt!CmpCallCallBacks` breakpoints.
- Available KVM substitute checked: `scripts/vm-kvm/run-guest-local-kd-smoke.py` launches guest local-KD (`kd.exe -kl`) through QGA, but that lane is post-boot and is not a boot breakpoint capture path.
- Action: skip Phase 3 for this pass and continue with Phase 4 Procmon validation through the existing KVM scripts.

## Resolved Blockers

### [RESOLVED 51aefbe8] 2026-04-23T06:53:00Z - Host build validation tool missing on default PATH

- status: resolved

- Scope: 10-commit validation gate after the `research: capture exact LongDpcQueueThreshold query` milestone.
- Resolution: commit `51aefbe8` added repo-root `dotnetw` / `dotnetw.ps1` wrappers plus build-doc updates, so host-side validation no longer depends on mutating the shell `PATH` first.
- Residual note: raw `dotnet` may still be absent from the default host `PATH`, but the repository now carries a first-class supported wrapper for build/test flows.
