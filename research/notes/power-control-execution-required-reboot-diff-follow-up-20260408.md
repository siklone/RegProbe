# Execution-Required Reboot-Diff KVM Follow-Up

Date: 2026-04-08
Target path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
Probe: `power-control-batch-lightweight-runtime-primary-20260331-191300`

## Outcome

- A retained broad KVM lightweight runtime lane armed both execution-required candidates from baseline-missing to candidate value `1`.
- The retained lane completed a real soft reboot with healthy shell state before and after the reboot window.
- The broad post-boot capture still produced `no-hit` for both `AllowAudioToEnableExecutionRequiredPowerRequests` and `AllowSystemRequiredPowerRequests`.
- The retained batch therefore closes the missing reboot-diff layer at a broad level, but it does not provide an exact per-setting post-boot read.

## Artifacts

- `evidence/files/vm-tooling-staging/power-control-batch-lightweight-runtime-primary-20260331-191300/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-lightweight-runtime-primary-20260331-191300/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-lightweight-runtime-primary-20260331-191300/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-lightweight-runtime-primary-20260331-191300/manifest.json`

## Interpretation

- The execution-required pair no longer lacks reboot evidence altogether: a retained broad runtime lane wrote the pair to `1`, rebooted with `reboot_mode=soft`, and returned a stable post-boot `no-hit` batch result.
- This is still weaker than a dedicated exact-read lane. The broad batch only proves that a real rebooted observation window was attempted and that the pair remained unresolved in that window.
- The next missing layer is therefore narrower than before. The open problem is no longer "did we ever test a rebooted window?" but "can we get a dedicated exact runtime trace or a narrower post-boot diff for this pair?"
