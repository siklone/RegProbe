# DefaultDynamicHeteroCpuPolicy Strict VM Sweep

This note records the strict Win25H2Clean validation pass for `HKLM/SYSTEM/CurrentControlSet/Control/Session Manager/Kernel/DefaultDynamicHeteroCpuPolicy`.

## Method

For each requested value from `0` through `7`:

1. Write the requested value into the guest registry.
2. Perform a clean guest OS reboot.
3. Wait for VMware Tools to report `running`, then settle for about 90 seconds.
4. Read back the registry value after boot 1.
5. Perform a second clean guest OS reboot.
6. Wait again, then read back the registry value after boot 2.
7. Run the synthetic benchmark:
   - 30 SHA256 rounds over a 64 MB buffer
   - 128 MB sequential disk write
8. Capture the benchmarked registry value and timings.

This pass deliberately used two reboot cycles per sample because the earlier quick sweep showed lagged or ambiguous readback behavior.

## Results

| Requested | Boot 1 | Boot 2 | Benchmark | CPU ms | Disk MB/s | Disk s |
| --- | --- | --- | --- | ---: | ---: | ---: |
| 0 | 0 | 0 | 0 | 1093.38 | 37.90 | 3.377 |
| 1 | 1 | 1 | 1 | 1065.86 | 49.99 | 2.560 |
| 2 | 2 | 2 | 2 | 1091.07 | 40.67 | 3.147 |
| 3 | 3 | 3 | 3 | 1080.80 | 51.54 | 2.483 |
| 4 | 4 | 4 | 4 | 1114.74 | 72.89 | 1.756 |
| 5 | 5 | 5 | 5 | 1069.42 | 56.98 | 2.247 |
| 6 | 6 | 6 | 6 | 1105.71 | 79.24 | 1.615 |
| 7 | 7 | 7 | 7 | 1102.88 | 80.28 | 1.594 |

## Notes

- Boot 1, boot 2, and the benchmark all matched the requested value for every sample in this strict pass.
- CPU timings stayed in a relatively tight band with no meaningful monotonic trend across values.
- Disk throughput varied more widely, but still did not produce a clean monotonic trend that would justify a performance claim.
- The guest was restored to `DefaultDynamicHeteroCpuPolicy = 3` after the sweep.

## Artifacts

- `research/evidence-files/vm-tooling-staging/hetero-sweep-strict/hetero-sweep-strict-summary.csv`
- `research/evidence-files/vm-tooling-staging/hetero-sweep-strict/hetero-sweep-strict-detail.json`
- `research/evidence-files/vm-tooling-staging/hetero-restore-3.json`
