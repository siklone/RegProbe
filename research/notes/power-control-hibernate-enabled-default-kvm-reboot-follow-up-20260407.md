# Power Control HibernateEnabledDefault KVM Reboot Follow-up

Date: 2026-04-07
Candidate: `power.control.hibernate-enabled-default`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify whether `HibernateEnabledDefault` stays stable across a real KVM reboot on the current Linux-hosted baseline
- check whether the current KVM guest still reports hibernation as unsupported before and after reboot
- replace the older VMware-specific environment-limitation wording with a cleaner current-baseline statement backed by live KVM evidence

## Result
- the host-driven KVM reboot observation completed end-to-end and confirmed a real boot-time advance from `2026-04-07T10:19:17.5000000Z` to `2026-04-07T10:30:32.5000000Z`
- `HibernateEnabledDefault` stayed `REG_DWORD 1` before and after reboot; the raw summary reports `value_changed = true` only because the JSON blob carries new timestamps and boot metadata, while `value_preserved = true` captures the actual registry value result
- `powercfg /a` was identical before and after reboot and continued to report that hibernation is unsupported, with Fast Startup also unavailable because hibernation is unavailable
- this does not promote the lane to exact runtime-read status, but it does show that the environment limitation is not just an older VMware quirk; the current KVM guest is hibernation-unsupported too

## Artifacts
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/hibernateenableddefault-reboot-kvm-20260407a-before.json`
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/hibernateenableddefault-reboot-kvm-20260407a-after.json`
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/hibernateenableddefault-reboot-kvm-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/hibernateenableddefault-reboot-kvm-20260407a-powercfg-a-before.txt`
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/hibernateenableddefault-reboot-kvm-20260407a-powercfg-a-after.txt`
- `evidence/files/vm-tooling-staging/hibernateenableddefault-reboot-kvm-20260407a/host-review.json`

## Short Take
- `HibernateEnabledDefault` persists at `1` across a real KVM reboot on the current guest
- the same guest still reports hibernation as unsupported before and after reboot
- the remaining gate is therefore an environment-limited exact runtime read, not uncertainty about whether the current Linux/KVM baseline behaves like the older VMware lane
