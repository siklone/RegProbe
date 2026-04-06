# KVM Research Lane Health - 2026-04-06

## Summary

The Linux/KVM runtime lane is currently healthy enough to keep advancing on branch `codex/kvm-research-lane-20260406`.

Host-side validation now reports `status: ok` in:

- `registry-research-framework/audit/kvm-research-lane-health-latest.json`

That audit confirms:

- host prerequisites are present (`python3`, `curl`, `virsh`, `qemu-img`, `xorriso`, `bash`)
- the guest bridge is healthy
- `regprobe-win11-25h2-session` is defined and running
- the bootstrap ISO rebuild succeeds
- required KVM lane files and core evidence artifacts are present

## Bootstrap And Tool Health

Current bootstrap summary:

- `evidence/files/vm-tooling-staging/kvm-tool-health-rerun-20260406b/bootstrap-summary.json`
- `status: ok`
- `failed_steps: []`

Current tool-health rerun:

- `evidence/files/vm-tooling-staging/kvm-tool-health-rerun-20260406b/tool-health.json`

Required smoke outcomes in the rerun:

- `procmon`: success
- `wpr`: success
- `winsat_cpu`: success
- `winsat_mem`: success
- `diskspd`: success
- `symchk_choice`: success
- `dotnet_info`: success

## Procmon Wrapper Finding

The prior KVM fragility was a Procmon wrapper issue, not a missing-tool issue.

What changed:

- fallback launch now uses `Start-Process` instead of a blocking direct invocation
- the tool-health smoke now checks the Procmon wrapper exit code instead of only checking whether a `.pml` file exists
- the bounded smoke window was tightened to `1 second / 64 MiB`

Supporting evidence:

- `evidence/files/vm-tooling-staging/kvm-tool-health-rerun-20260406b/procmon-direct-1s-summary.json`
- `evidence/files/vm-tooling-staging/kvm-tool-health-rerun-20260406b/procmon-direct-5s-summary.json`

Observed result:

- `1s / 64 MiB` completes with exit code `0` and produces a bounded `.pml`
- the older `5s / 32 MiB` shape can overshoot the size budget on the current Windows 11 guest

## Merge Direction

Keep the KVM lane on branch until we complete a few more real research follow-ups with the current transport shape.

Current merge gate recommendation:

- keep running symbolized and runtime probes on KVM
- preserve the host-side `validate-research-lane.py` audit as the fast readiness check
- merge to `main` only after the lane stays green across repeated guest sessions without needing ad hoc rescue steps
