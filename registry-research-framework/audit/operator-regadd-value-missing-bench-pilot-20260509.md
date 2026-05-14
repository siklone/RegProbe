# Operator Reg Add Value-Missing Bench Pilot

- Status: **ok**
- Records: `2`

| Record | Target | Value | Reboot value | Restore | Final | CPU single post-reboot | CPU multi post-reboot | IO MiB/s post-reboot |
|---|---|---:|---|---|---|---:|---:|---:|
| allow-system-required-power-requests explicit 0 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `0` | `0` | `removed-created-value` | `value-missing` | `0.0071` | `0.005` | `174.72` |
| allow-audio-execution-required-power-requests explicit 0 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `0` | `0` | `removed-created-value` | `value-missing` | `0.4716` | `0.2347` | `471.75` |

## Notes

- Both records were value-missing on clean 25H2; explicit REG_DWORD 0 survived reboot and rollback removed the created value.
- Benchmarks are small in-guest smoke metrics, not statistically rigorous performance claims; future waves should repeat and compare medians.

## Post-pilot baseline

- `registry-research-framework/audit/operator-regadd-vm-baseline-20260509T090105Z.json`
- Counts: `total=96`, `key_present=94`, `key_missing=2`, `value_present=21`, `value_missing=73`, `error=0`
