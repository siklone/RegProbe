# system.kernel-long-dpc-threshold-cluster KVM Procmon timer-dpc-stress follow-up - 2026-04-08

## Summary

- The dedicated `timer-dpc-stress` lane is no longer theoretical for the long-DPC cluster.
- `LongDpcRuntimeThreshold` was replayed through the KVM Procmon guest wrapper twice with:
- `LongDpcRuntimeThreshold` was replayed through the KVM Procmon guest wrapper twice with canonical retained runs `20260408a` and `20260408d`:
  - `TriggerProfile = timer-dpc-stress`
  - `RegistryPath = HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
  - `ValueName = LongDpcRuntimeThreshold`
- Replay `20260408a` reached live guest execution and advanced through Procmon capture stages, but failed during export:
  - `probe_stage = exception`
  - `error_kind = probe-stage-error`
  - `transport_blocker = probe-stage-error`
  - `guest_health = degraded`
  - exact error: `Procmon SaveAs timed out after 60 second(s).`
- Replay `20260408d` retried the same lane with `SaveAsTimeoutSeconds = 180` after widening the wrapper deadline beyond the SaveAs budget:
  - `probe_stage = exception`
  - `probe_stage_status = error`
  - `error_kind = probe-stage-error`
  - `transport_blocker = probe-stage-error`
  - `guest_health = degraded`
  - exact error: `Procmon SaveAs timed out after 180 second(s).`
- Combined interpretation:
  - the narrow lane is no longer merely unrun
  - increasing the SaveAs budget from `60` to `180` seconds did not unblock export
  - the blocker is now specifically a Procmon export timeout under the dedicated `timer-dpc-stress` lane

## Source artifacts

- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a-probe-stage.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a-launcher-stage.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408a/host-review.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d-summary.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d-probe-stage.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d-launcher-stage.json`
- `evidence/files/vm-tooling-staging/longdpcruntimethreshold-procmon-kvm-timerdpc-20260408d/host-review.json`

## Interpretation

- new proof gained:
  - the dedicated `timer-dpc-stress` replay path does launch successfully in the guest
  - the blocker is no longer missing harness or missing trigger profile
  - the failure point is specifically the Procmon export/save stage
  - widening the SaveAs timeout from `60` to `180` seconds did not change the failure class
- narrowed conclusion:
  - the long-DPC cluster has now crossed from `unrun` into `runtime-attempted but export-blocked`
  - there is still no CSV, hits CSV, or normalized bundle from this narrow replay
  - the strongest blocker wording is now `procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane`
- next proof path:
  - either harden the Procmon SaveAs/export lane further or replace Procmon export for this trigger profile
  - or pivot to a non-Procmon lane for the same `timer-dpc-stress` trigger family
