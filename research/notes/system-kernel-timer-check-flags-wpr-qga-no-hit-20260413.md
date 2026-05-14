# system.kernel.timer-check-flags WPR/QGA no-hit follow-up - 2026-04-13

## Result

The targeted current-build WPR boot-registry lane now runs cleanly through QGA on the KVM guest, but it still does not capture an exact runtime registry read for `TimerCheckFlags`.

- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\TimerCheckFlags`
- Run id: `timer-check-flags-wpr-qga-20260413a`
- Transport: `qga`
- Outcome: `status = ok`, `reboot_observed = true`, `etl_exists = true`, `csv_exists = true`, `normalization_status = ok`, `hit_line_count = 0`

## What This Adds

This is stronger negative runtime evidence than the earlier broad WPR and ETW lanes because it is a direct boot-registry capture aimed at the exact current-build key path and value name, completed through the hardened QGA transport, and finished with a normalized bundle rather than a partial artifact set.

The retained summary shows:

- saved ETL: `1,758,461,952` bytes
- saved tracerpt CSV: `6,063,340,406` bytes
- exact fragment hits for both the target key-path fragment and `TimerCheckFlags`: `0`
- normalized events retained: `0`

`tracerpt` reported dropped events during conversion, so this is not proof that the registry seed never happens. It is still strong enough to replace a generic missing-runtime story with a targeted current-build no-hit on the working VM lane.

## Artifact Set

- `evidence/files/vm-tooling-staging/timer-check-flags-wpr-qga-no-hit-20260413/timer-check-flags-wpr-qga-20260413a-summary-arm.json`
- `evidence/files/vm-tooling-staging/timer-check-flags-wpr-qga-no-hit-20260413/timer-check-flags-wpr-qga-20260413a-summary.json`
- `evidence/files/vm-tooling-staging/timer-check-flags-wpr-qga-no-hit-20260413/timer-check-flags-wpr-qga-20260413a.normalized.json`
- [system-kernel-timer-check-flags-wpr-qga-no-hit-20260413.json](../../registry-research-framework/audit/system-kernel-timer-check-flags-wpr-qga-no-hit-20260413.json)
