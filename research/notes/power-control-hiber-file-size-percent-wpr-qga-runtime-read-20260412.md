# HiberFileSizePercent WPR/QGA Runtime Read

Date: 2026-04-12
Candidate: `power.control.hiber-file-size-percent`

The new QGA-launched WPR boot-registry lane produced the missing runtime-read proof for `HiberFileSizePercent`.

Command:

```bash
python3 scripts/vm-kvm/run-guest-wpr-boot-registry.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' \
  --value-name HiberFileSizePercent \
  --output-name hiberfilesizepercent-wpr-qga-smoke-20260412f \
  --timeout-seconds 900 \
  --prepare-timeout-seconds 180 \
  --reboot-settle-seconds 45 \
  --tracerpt-timeout-seconds 420
```

Result:

- `status = ok`
- `reboot_observed = true`
- `etl_exists = true`
- `csv_exists = true`
- `hits_csv_exists = true`
- `hit_line_count = 5`
- `normalized_bundle_exists = true`
- `normalization_status = ok`

The normalized bundle contains one exact registry event:

```text
operation: QueryValue
hive: HKLM
key_path: SYSTEM\CurrentControlSet\Control\Power
value_name: HiberFileSizePercent
normalization_note: raw-tracerpt-registry-hit-line
```

This closes the old `runtime_no_read` gap for the record. Earlier Procmon and lightweight ETW lanes had repeatedly shown adjacent `Control\Power` activity or exact value-name text, but not a normalized runtime read. This WPR boot trace gives the missing exact `Registry / QueryValue / HiberFileSizePercent` event.

One normalizer fix came out of this run. The tracerpt CSV can place the registry value name beyond the declared header columns, so `Import-Csv` can lose the actual value-name field even though the raw line contains it. [scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1](../../scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1) now falls back to raw registry hit lines when the structured CSV pass produces no events.

## Retained audit artifact

- [power-control-hiber-file-size-percent-wpr-qga-runtime-read-20260412.json](../../registry-research-framework/audit/power-control-hiber-file-size-percent-wpr-qga-runtime-read-20260412.json)
