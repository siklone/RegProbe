# UuidSequenceNumber WPR/QGA Runtime Read

Date: 2026-04-13
Candidate: `system.executive-uuid-sequence-number`

The QGA-launched WPR boot-registry lane now produces the missing direct runtime-read proof for `UuidSequenceNumber`.

Command:

```bash
python3 scripts/vm-kvm/run-guest-wpr-boot-registry.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Executive' \
  --value-name UuidSequenceNumber \
  --output-name uuid-sequence-number-wpr-qga-20260413a \
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
- `hit_line_count = 2`
- `normalized_bundle_exists = true`
- `normalization_status = ok`

The retained raw hit set contains two exact quoted lines:

```text
Registry, QueryValue, ... "UuidSequenceNumber"
Registry, SetValue, ... "UuidSequenceNumber"
```

The normalized bundle binds those exact-value hits to the intended Session Manager Executive target path:

```text
operation: QueryValue
hive: HKLM
key_path: SYSTEM\CurrentControlSet\Control\Session Manager\Executive
value_name: UuidSequenceNumber

operation: SetValue
hive: HKLM
key_path: SYSTEM\CurrentControlSet\Control\Session Manager\Executive
value_name: UuidSequenceNumber
```

One nuance stays explicit here too. The retained raw tracerpt hit lines carry the exact quoted value name, but they do not print the full registry path text. The normalizer binds those hits to the intended `Session Manager\Executive` target path supplied to the run. That path binding is supported by earlier evidence, not by the WPR trace alone: the bounded Executive ETL review had already shown adjacent boot activity in the same path, and the KVM local-KD disassembly had already tied the current-build load/save path to `\Registry\Machine\System\CurrentControlSet\Control\Session Manager\Executive`.

Within this repo, that is enough to close the old `runtime_no_read` blocker. The record still remains research-only and not app-mapped, but it no longer lacks direct runtime observation.

## Retained audit artifact

- [system-executive-uuid-sequence-number-wpr-qga-runtime-read-20260413.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/system-executive-uuid-sequence-number-wpr-qga-runtime-read-20260413.json)
