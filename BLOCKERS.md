# Research Blockers

## 2026-04-23T09:48:32Z - Guest Ghidra string-xref probe stalls after admin shell ready

- Scope: `power.session-win32-callout-watchdog-bugcheck-enabled` guest-side string-xref retry through `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`.
- Blocker: the KVM/QGA launcher reached `admin-shell-ready`, but the retained launcher stage stayed at `invoke-wrapper` with status `starting` and no evidence bundle was uploaded back to the host. This matches the broader guest-control hang pattern rather than a record-specific static-analysis result.
- Action: skip further bounded guest Ghidra retries from this Linux host, keep the sibling on local/static hold, and wait for either a manual VM session or a stronger host-side static pivot.

## 2026-04-23T04:49:51Z - Phase 3 WinDbg boot registry trace on KVM

- Scope: remaining `no-hit` power/kernel records after the PowerRequestOverride ETW call-stack capture.
- Blocker: the repo contains a WinDbg boot registry trace lane for VMware serial-pipe debugging (`registry-research-framework/tools/run-windbg-boot-registry-trace.ps1` plus `execute-windbg-boot-registry-trace.ps1`), but no KVM/vmrun wrapper that can attach WinDbg/KD during guest boot and arm `CmQueryValueKey`, `NtQueryValueKey`, or `nt!CmpCallCallBacks` breakpoints.
- Available KVM substitute checked: `scripts/vm-kvm/run-guest-local-kd-smoke.py` launches guest local-KD (`kd.exe -kl`) through QGA, but that lane is post-boot and is not a boot breakpoint capture path.
- Action: skip Phase 3 for this pass and continue with Phase 4 Procmon validation through the existing KVM scripts.

## 2026-04-23T06:53:00Z - Host build validation tool missing

- Scope: 10-commit validation gate after the `research: capture exact LongDpcQueueThreshold query` milestone.
- Blocker: `dotnet build RegProbe.sln -c Release` cannot run in this host session because `dotnet` is not installed or not on `PATH`; the shell returned `/usr/bin/bash: line 1: dotnet: command not found`.
- Action: treat host-side .NET build validation as blocked in this environment and continue with host-verifiable Python/test and research lanes until a .NET-capable host or VM validation path is available.
