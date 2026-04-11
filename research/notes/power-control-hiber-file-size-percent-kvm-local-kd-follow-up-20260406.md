# Power Control HiberFileSizePercent KVM Local KD Follow-up

Date: 2026-04-06
Candidate: `power.control.hiber-file-size-percent`
Guest: `regprobe-win11-25h2-session`

## Objective
- extend the KVM inspect-only lane outside Session Manager and Policy families into the raw power-manager lane
- check whether the running current-build guest exposes a coherent hibernation symbol family, the expected `Control\Power` registry path, and a live `PopHiberFileSizePercent` state that matches the observed guest baseline

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and resolved a coherent hibernation symbol family including `PopHiberFileSizePercent`, `PopSetHiberFileSize`, `PopAdjustHiberFile`, `PopOpenHiberPersistedKey`, and `PopQueryHiberPersistedRegValue`
- `dd nt!PopHiberFileSizePercent L1` returned `0x00000000` on the running guest
- `dd nt!PopHiberFileType L1` returned `0x00000002`, while `dd nt!PopHiberEnabled L1` returned the live bitfield `0x00200501`
- `du poi(nt!PopHibernatePersistedRegLocation)` resolved the live path string to `\REGISTRY\MACHINE\SYSTEM\CURRENTCONTROLSET\CONTROL\POWER`
- `u nt!PopOpenHiberPersistedKey L0x120` showed the current-build helper loading `PopHibernatePersistedRegLocation`, initializing a Unicode string, and then opening or creating the target key
- `u nt!PopQueryHiberPersistedRegValue L0x140` showed the current-build path opening the persisted key, querying a value with `ZwQueryValueKey`, and on `STATUS_OBJECT_NAME_NOT_FOUND` falling back to `PopReadUlongPowerKey`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-hiber-symbols-20260406a/local-kd-hiber-symbols-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-hiber-symbols-20260406a/local-kd-hiber-symbols-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-hiber-disasm-20260406a/local-kd-hiber-disasm-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-hiber-disasm-20260406a/local-kd-hiber-disasm-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-hiber-strings-20260406a/local-kd-hiber-strings-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-hiber-strings-20260406a/local-kd-hiber-strings-20260406a.log`

## Short Take
- KVM local KD now confirms that the current-build hibernation lane really routes through `\Registry\Machine\System\CurrentControlSet\Control\Power`
- the running guest also exposes `PopHiberFileSizePercent = 0`, which lines up with the earlier guest baseline and repo docs
- this still does not count as a direct live registry-read proof, so the lane remains gated by `runtime_no_read`
