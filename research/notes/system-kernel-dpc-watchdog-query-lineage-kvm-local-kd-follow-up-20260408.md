# system.kernel-dpc-watchdog query lineage KVM local-KD follow-up - 2026-04-08

## Summary

- A dedicated live KVM local-KD pass now confirms the exact outer query wrapper for the DPC watchdog family on the current build:
  - `NtQuerySystemInformation+0x8c -> ExpQuerySystemInformation`
- The same pass resolved both outer symbols directly in the live guest:
  - `nt!NtQuerySystemInformation`
  - `nt!ExpQuerySystemInformation`
- A larger `u nt!ExpQuerySystemInformation L400` dispatch slice was captured in the same run.
- That captured slice did **not** yet expose a symbolized `KeQueryDpcWatchdogConfiguration` callsite.
- This narrows the query-side blocker:
  - it is no longer "no live query lineage"
  - it is now "inner `ExpQuerySystemInformation` watchdog query arm unresolved"

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-query-config-kd-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-query-config-kd-20260408a/dpc-watchdog-query-config-kd-20260408a.log`
- `evidence/files/vm-tooling-staging/dpc-watchdog-query-config-kd-20260408a/dpc-watchdog-query-config-kd-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-query-config-kd-20260408a/host-review.json`

## Interpretation

- new proof gained:
  - the outer query wrapper is now live-KD confirmed rather than inferred from older DPC scheduler review records
  - the current-build query lineage is explicitly `NtQuerySystemInformation -> ExpQuerySystemInformation`
- narrowed conclusion:
  - the remaining missing edge is not the syscall wrapper, but the exact `ExpQuerySystemInformation` arm that reaches `KeQueryDpcWatchdogConfiguration`
  - no persisted registry seeding caller is implied by this pass
- next proof path:
  - isolate the exact `ExpQuerySystemInformation` dispatch arm for the watchdog query family
  - keep that query-side arm separate from the already-confirmed writer-side `NtSetSystemInformation -> KeUpdateDpcWatchdogConfiguration` lane
