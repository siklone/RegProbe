# Research Blockers

## Open Blockers

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

- Scope: 10-commit validation gate after the `research: capture exact LongDpcQueueThreshold query` milestone.
- Resolution: commit `51aefbe8` added repo-root `dotnetw` / `dotnetw.ps1` wrappers plus build-doc updates, so host-side validation no longer depends on mutating the shell `PATH` first.
- Residual note: raw `dotnet` may still be absent from the default host `PATH`, but the repository now carries a first-class supported wrapper for build/test flows.
