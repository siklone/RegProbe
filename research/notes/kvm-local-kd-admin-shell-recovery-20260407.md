# KVM Local KD Admin-Shell Recovery Follow-up

Date: 2026-04-07
Guest: `regprobe-win11-25h2-session`
Transport: `scripts/vm-kvm/ensure-guest-admin-shell.py` + `scripts/vm-kvm/run-guest-local-kd-smoke.py`

## Objective
- prove that the KVM host-side runners can recover from a missing elevated guest PowerShell window instead of silently depending on a pre-opened admin shell
- validate that the hardened host path can reopen the shell, restage the guest helper, and still complete a live local-KD symbol smoke

## Result
- after explicitly closing the visible elevated guest PowerShell window, the host-side admin-shell helper reopened a fresh elevated PowerShell session and uploaded a ready marker through the guest bridge
- the same recovery flow then launched `run-guest-local-kd-smoke.ps1` from the recovered shell and completed a full local-KD smoke without manual guest interaction
- the recovered smoke stayed healthy: `attached = true`, `completed = true`, `symchk_exit_code = 0`, and `query_symbol_seen = true` for `nt!CmQueryValueKey`
- this does not add new candidate evidence by itself, but it materially improves the trustworthiness of the KVM runtime and local-KD lanes because guest runners no longer rely on a manually preserved admin shell

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-admin-recovery-20260407a/local-kd-admin-recovery-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-admin-recovery-20260407a/local-kd-admin-recovery-20260407a.log`
- `evidence/files/vm-tooling-staging/local-kd-admin-recovery-20260407a/local-kd-admin-recovery-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-admin-recovery-20260407a/local-kd-admin-recovery-20260407a.stderr.txt`
- `evidence/files/vm-tooling-staging/local-kd-admin-recovery-20260407a/local-kd-admin-recovery-20260407a.txt`
- `registry-research-framework/audit/kvm-research-lane-health-latest.json`

## Short Take
- the KVM lane now recovers one of its most fragile operator assumptions on its own: missing elevated shell state
- bridge auto-start plus admin-shell recovery now work together well enough to restage and complete a real local-KD smoke from the desktop state
- this is a lane-health improvement rather than a new setting-level finding, but it lowers the chance that future KVM follow-ups fail for transport reasons instead of research reasons
