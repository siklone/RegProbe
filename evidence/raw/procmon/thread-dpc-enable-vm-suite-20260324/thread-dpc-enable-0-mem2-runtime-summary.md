# ThreadDpcEnable Memory Runtime Summary

Record: `system.kernel-thread-dpc-enable`

Test ID: `thread-dpc-enable-0-mem2`

This is the canonical checked-in summary for the bounded memory run. It replaces the older `.etl.md` placeholder as the live evidence reference. The original raw ETL is not stored in the repository.

## Flow

- Baseline: `ThreadDpcEnable` missing
- Apply: set `ThreadDpcEnable = 0`
- Reboot: yes
- Benchmark: WinSAT memory bounded run
- Restore: remove value and reboot back to missing baseline

## Result

| Metric | Value |
|---|---:|
| Status | complete |
| Restore complete | true |
| Measured duration | 30.50 s |
| Idle CPU before benchmark | 2.39% |
| Idle disk transfers/sec before benchmark | 0.07 |
| CPU average | 5.26% |
| CPU max | 15.54% |
| Disk transfers/sec average | 113.74 |
| Disk transfers/sec max | 628.56 |

## Canonical Artifacts

- Perf CSV: `evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/thread-dpc-enable-0-mem2.perf.csv`
- Suite note: `research/notes/thread-dpc-enable-vm-suite-20260324.md`

## Boundary

This proves the value was exercisable and reversible in the VM. It does not claim performance improvement and does not replace a byte-for-byte raw ETL payload.
