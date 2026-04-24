# Research Blockers

## Open Blockers

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
