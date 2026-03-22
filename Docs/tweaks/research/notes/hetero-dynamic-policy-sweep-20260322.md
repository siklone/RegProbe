# DefaultDynamicHeteroCpuPolicy VM Sweep

This note records a VM-backed sweep of `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\DefaultDynamicHeteroCpuPolicy` in `Win25H2Clean`.

## Method

For each requested value from `0` through `7`:

1. Write the requested value into the guest registry.
2. Restart the guest with an OS-level reboot.
3. Run the synthetic benchmark:
   - 30 SHA256 rounds over a 64 MB buffer
   - 128 MB sequential disk write
4. Capture the measured registry value and benchmark timings.

The guest registry value observed by the benchmark lagged one iteration behind the requested value. The first sample still saw the prior boot's value, which suggests the setting is not applied immediately on the same reboot cycle used for the write.

## Results

| Requested | Observed | CPU ms | Disk MB/s | Disk s |
| --- | --- | ---: | ---: | ---: |
| 0 | 3 | 1427.38 | 64.26 | 1.992 |
| 1 | 0 | 1104.82 | 51.21 | 2.500 |
| 2 | 1 | 1086.06 | 49.73 | 2.574 |
| 3 | 2 | 1067.99 | 43.74 | 2.927 |
| 4 | 3 | 1072.75 | 46.21 | 2.770 |
| 5 | 4 | 1081.30 | 49.96 | 2.562 |
| 6 | 5 | 1070.13 | 55.53 | 2.305 |
| 7 | 6 | 1087.93 | 52.22 | 2.451 |

## Notes

- The sweep did not show a monotonic CPU or disk trend across the requested values.
- The value readback is likely delayed by one reboot cycle or one registry persistence step.
- The VM was restored to `DefaultDynamicHeteroCpuPolicy = 3` after the sweep.

## Artifacts

- `H:\Temp\vm-tooling-staging\hetero-sweep\hetero-sweep-summary.csv`
- `H:\Temp\vm-tooling-staging\hetero-sweep\hetero-sweep-detail.json`
- `H:\Temp\vm-tooling-staging\hetero-restore-3.json`
