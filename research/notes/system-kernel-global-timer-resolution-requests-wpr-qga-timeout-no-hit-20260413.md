# system.kernel.global-timer-resolution-requests WPR/QGA timeout no-hit - 2026-04-13

## Result

The targeted QGA-launched WPR boot-registry lane reached post-reboot collection for `GlobalTimerResolutionRequests`, but the host wrapper timed out before the guest emitted a complete summary. Timeout salvage still recovered enough to classify this as a targeted no-hit, not a transport-only failure.

- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\GlobalTimerResolutionRequests`
- Run id: `global-timer-resolution-requests-wpr-qga-20260413a`
- Transport: `qga`
- Wrapper outcome: `status = timeout`, `error_kind = runner-timeout`
- Guest stage at salvage: `collect-tracerpt`
- Guest boot time after run: `2026-04-12T22:07:22.5000000Z`
- ETL size: `1,751,121,920` bytes
- tracerpt CSV size: `2,445,849,654` bytes
- salvaged hits CSV: header-only, `hit_line_count = 0`
- host-normalized salvage bundle: `event_count = 0`

## Interpretation

This does not close `runtime_no_read`. The run timed out at the host wrapper level, and the guest left `summary.json` plus the original guest `normalized.json` as zero-byte files.

It still improves the evidence story because the trace reached the expensive part of the lane, generated a large ETL/CSV pair, and retained a header-only target hit file. The host salvage path now converts that state into an explicit empty normalized bundle through `HostTimeoutSalvageNormalizer`, so future reviews do not have to infer no-hit status from a zero-byte artifact.

The next useful step is either a longer timeout budget for the same WPR boot-registry lane or a narrower runtime trace focused on the exact INIT descriptor consumer.

## Artifact Set

- [global-timer-resolution-requests-wpr-qga-20260413a-summary-arm.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413/global-timer-resolution-requests-wpr-qga-20260413a-summary-arm.json)
- [global-timer-resolution-requests-wpr-qga-20260413a-summary.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413/global-timer-resolution-requests-wpr-qga-20260413a-summary.json)
- [global-timer-resolution-requests-wpr-qga-20260413a.hits.csv](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413/global-timer-resolution-requests-wpr-qga-20260413a.hits.csv)
- [global-timer-resolution-requests-wpr-qga-20260413a.normalized.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413/global-timer-resolution-requests-wpr-qga-20260413a.normalized.json)
- [system-kernel-global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/system-kernel-global-timer-resolution-requests-wpr-qga-timeout-no-hit-20260413.json)
