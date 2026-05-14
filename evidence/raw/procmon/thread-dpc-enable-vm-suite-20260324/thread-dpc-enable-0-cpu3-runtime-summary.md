# ThreadDpcEnable CPU Runtime Summary

Record: `system.kernel-thread-dpc-enable`

Test ID: `thread-dpc-enable-0-cpu3`

This is the canonical checked-in summary for the bounded CPU run. It replaces the older `.etl.md` placeholder as the live evidence reference. The original raw ETL is not stored in the repository.

## Flow

- Baseline: `ThreadDpcEnable` missing
- Apply: set `ThreadDpcEnable = 0`
- Reboot: yes
- Benchmark: WinSAT CPU bounded run
- Restore: remove value and reboot back to missing baseline

## Result

| Metric | Value |
|---|---:|
| Status | complete |
| Restore complete | true |
| Measured duration | 30.31 s |
| Idle CPU before benchmark | 2.72% |
| Idle disk transfers/sec before benchmark | 0.00 |
| CPU average | 7.29% |
| CPU max | 22.65% |
| Disk transfers/sec average | 135.07 |
| Disk transfers/sec max | 610.11 |

## Canonical Artifacts

- Perf CSV: `evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-cpu3.perf.csv`
- Suite note: `research/notes/thread-dpc-enable-vm-suite-20260324.md`

## Boundary

This proves the value was exercisable and reversible in the VM. It does not claim performance improvement and does not replace a byte-for-byte raw ETL payload.
