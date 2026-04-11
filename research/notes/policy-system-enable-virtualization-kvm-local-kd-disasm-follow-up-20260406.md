# Policy System EnableVirtualization KVM Local KD Disassembly Follow-up

Date: 2026-04-06
Candidate: `policy.system.enable-virtualization`
Guest: `regprobe-win11-25h2-session`

## Objective
- move the KVM local-KD lane beyond symbol resolution and inspect the live current-build `PsBootPhaseComplete` path directly
- check whether the running guest still clusters `EnableVirtualization` under `\Registry\Machine\Software\Microsoft\Windows\CurrentVersion\Policies\System` instead of leaving the family path-ambiguous

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `u nt!PsBootPhaseComplete L0x180` showed a compact live current-build cluster that calls `ZwOpenKey` and then issues repeated `ZwQueryValueKey` lookups from the same stack-built policy family
- the same disassembly slice loaded live string literals for the adjacent value-name family, including `EnableLUA`, `EnableVirtualization`, `EnableInstallerDetection`, `TypeOfAdminApprovalMode`, `UACInstalled`, and `DevOverrideEnable`
- `du 0xfffff800\`976f21b0` resolved the shared key path to `\Registry\Machine\Software\Microsoft\Windows\CurrentVersion\Policies\System`
- `du` against the nearby value-name literals resolved `EnableLUA`, `EnableVirtualization`, and `EnableInstallerDetection` exactly on the running guest

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-enablevirtualization-slice-20260406a/local-kd-enablevirtualization-slice-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-enablevirtualization-slice-20260406a/local-kd-enablevirtualization-slice-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-enablevirtualization-strings-20260406a/local-kd-enablevirtualization-strings-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-enablevirtualization-strings-20260406a/local-kd-enablevirtualization-strings-20260406a.log`

## Short Take
- KVM local KD now confirms that the current-build `EnableVirtualization` family lives inside the expected `Policies\System` path on the running guest
- that removes the remaining path-context ambiguity from the current-build route and sharply lowers the old `EnableVirtualizationBasedSecurity` collision concern for this lane
- the remaining blocker is still direct runtime observation of the live read, not uncertainty about which registry path the inspected boot-phase code targets
