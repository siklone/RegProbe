# Research Blockers

## Open Blockers

### 2026-04-23T19:02:39Z - ETW stackwalk bridge artifact timeout for DPC watchdog control cluster representative

- status: open

- Scope: `system.kernel-dpc-watchdog-control-cluster` bounded ETW stackwalk retry through `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` using representative value `DPCTimeout`.
- Blocker: a QGA-launched ETW stackwalk retry for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel` / `DPCTimeout` was run with `--timeout-seconds 90`, `--first-artifact-timeout-seconds 90`, and an outer host `timeout 90s`, but no bridge artifact (`*-summary.json`, `*-stage.json`, ETL, XML, or normalized bundle) reached the host before the hard timeout exited.
- Mitigation: treat this as a transport/bridge failure instead of runtime evidence, keep the cluster on its existing exact-read gap, and avoid repeating the same bounded ETW attempt from this Linux host until the QGA upload chain is debugged with a manual VM session or a narrower host-visible artifact path.
- Action: continue with retained KD/Ghidra/init-descriptor evidence for the family and defer further exact-read ETW retries for this representative.

### 2026-04-23T18:59:22Z - Guest Ghidra launcher stall for PowerWatchdog timeout cluster representative

- status: open

- Scope: `power.control.power-watchdog-timeout-cluster` bounded guest-side string-xref retry through `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py` using representative value `PowerWatchdogPoCalloutTimeoutMsec`.
- Blocker: the 2026-04-23 host run returned `status = timeout`, `error_kind = guest-launcher-stall`, and a retained launcher-stage snapshot that never advanced beyond `stage = invoke-wrapper` / `status = starting` before the configured 180 second launcher stall budget expired. No Ghidra evidence bundle reached the host, so the attempt is transport-only and not static proof for the family.
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
