# system.kernel-default-dynamic-hetero-cpu-policy

- Class: `E`
- Record status: `deprecated`
- Tested build: `26100`
- Reason: `class-e`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `official-doc` Microsoft Learn: SchedulingPolicy -> https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configuration-for-hetero-power-scheduling-schedulingpolicy
- `repo-doc` Repo system research notes for kernel registry values -> Docs/system/system.md
- `decompiled-pseudocode` nohuto mirror: dynamic heterogeneous CPU policy notes and kernel pseudocode -> research/_source-mirrors/win-config/system/desc.md; research/_source-mirrors/decompiled-pseudocode/ntoskrnl/KeConfigureHeteroProcessors.c
- `registry-observation` nohuto Session Manager quota-system trace -> research/_source-mirrors/win-registry/records/Session-Manager.txt
- `registry-observation` nohuto trace for DefaultDynamicHeteroCpuPolicy -> research/_source-mirrors/regkit/assets/traces/23H2.txt; research/_source-mirrors/regkit/assets/traces/24H2.txt; research/_source-mirrors/regkit/assets/traces/25H2.txt
- `runtime-benchmark` Strict VM sweep of DefaultDynamicHeteroCpuPolicy values 0..7 -> research/notes/hetero-dynamic-policy-strict-sweep-20260322.md; evidence/files/vm-tooling-staging/hetero-sweep-strict/hetero-sweep-strict-summary.csv; evidence/files/vm-tooling-staging/hetero-sweep-strict/hetero-sweep-strict-detail.json
