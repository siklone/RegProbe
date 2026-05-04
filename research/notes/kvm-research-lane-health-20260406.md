# KVM Research Lane Health - 2026-04-06

## Summary

The Linux/KVM runtime lane is healthy enough at this audit point to keep advancing on branch `codex/kvm-research-lane-20260406`.

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

## Host-Driven Registry Replay

The next KVM friction point was operator error around guest-side quoting for registry paths with spaces.

What changed:

- `scripts/vm-kvm/run-guest-registry-policy-probe.py` now stages a generated guest script through the bridge instead of typing a long inline PowerShell command into the VM
- the host runner reuses the existing guest helper pair and waits for the summary artifact to return to `/tmp/regprobe-bridge`
- this removes the need to hand-type quoted `RegistryPath` values inside the guest for routine KVM Procmon replays

Supporting evidence:

- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-helper-smoke3-20260406/allowremotedasd-procmon-kvm-helper-smoke3-20260406.txt`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-helper-smoke3-20260406/allowremotedasd-procmon-kvm-helper-smoke3-20260406-summary.json`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-helper-smoke3-20260406/host-review.json`

Observed result:

- the host-driven replay completed end to end on the live KVM guest without manual guest interaction
- the replay produced a real Procmon CSV with `462070` lines
- the host-side review still found `0` lines containing `Session Manager\I/O System`, `AllowRemoteDASD`, or `RemovableStorageDevices`
- the research conclusion did not change, but the transport is now safer to repeat from the host

## Merge Direction

Keep the KVM lane on branch until we complete a few more real research follow-ups with the current transport shape.

Current merge gate recommendation:

- keep running symbolized and runtime probes on KVM
- preserve the host-side `validate-research-lane.py` audit as the fast readiness check
- merge to `main` only after the lane stays green across repeated guest sessions without needing ad hoc rescue steps
