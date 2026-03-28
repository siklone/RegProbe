# system.executive-additional-worker-threads ETL registry review - 2026-03-28

## Summary

- A bounded host-side `xperf -a dumper` extract from the successful boot ETL now proves boot-time access to `Session Manager\Executive`.
- The strongest early reader in this extract is `System (PID 4)`.
- The extract surfaced adjacent `UuidSequenceNumber` activity, including both a query and a set.
- The exact worker-thread pair names still did not appear in the bounded event payloads.

## Source artifacts

- ETL placeholder: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/watchdog-timeouts-boot.etl.md`
- Filtered review: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/registry-dump-session-manager-executive.txt`

## Counts

- `Session Manager\Executive`: `6`
- `UuidSequenceNumber`: `2`
- `AdditionalCriticalWorkerThreads`: `0`
- `AdditionalDelayedWorkerThreads`: `0`

## Why this matters

This closes the old gap around whether the Executive lane had any runtime signal at all. The bounded extract now shows the kernel touching `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive` during early boot, and it shows that the adjacent `UuidSequenceNumber` value is active in the same path.

That is stronger than a plain baseline export, but it is still not a direct live read of `AdditionalCriticalWorkerThreads` or `AdditionalDelayedWorkerThreads`. The lane therefore stays `Class C` and should not be promoted to a shipped app mapping yet.

## Representative findings

- `System (PID 4)` opened `Session Manager\Executive` during early boot.
- `System (PID 4)` queried `UuidSequenceNumber`.
- `System (PID 4)` later set `UuidSequenceNumber`.
- The bounded extract did not surface exact runtime reads for `AdditionalCriticalWorkerThreads` or `AdditionalDelayedWorkerThreads`.
