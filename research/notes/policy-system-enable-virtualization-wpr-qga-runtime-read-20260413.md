# EnableVirtualization WPR/QGA Runtime Read

Date: 2026-04-13
Candidate: `policy.system.enable-virtualization`

The QGA-launched WPR boot-registry lane now produces a clean exact-value runtime-read result for `EnableVirtualization`.

Command:

```bash
python3 scripts/vm-kvm/run-guest-wpr-boot-registry.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' \
  --value-name EnableVirtualization \
  --output-name enable-virtualization-wpr-qga-20260413b \
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

The retained raw hit set contains exactly five quoted `EnableVirtualization` lines:

```text
Registry, QueryValue, ... "EnableVirtualization"
```

The normalized bundle contains five target-bound registry events:

```text
operation: QueryValue
hive: HKLM
key_path: SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System
value_name: EnableVirtualization
normalization_note: raw-tracerpt-registry-hit-line
```

One important nuance is worth keeping explicit. The retained raw tracerpt hit lines carry the exact quoted value name, but they do not print the full registry path text. The normalizer binds those exact-value hits to the intended `Policies\System` target path supplied to the run. That path binding is not guesswork in isolation: earlier static triage and the KVM local-KD follow-up had already fixed the current-build read site to `\Registry\Machine\Software\Microsoft\Windows\CurrentVersion\Policies\System`.

This run therefore closes the old `runtime_no_read` blocker for the record. The record still stays intentionally held because `EnableVirtualization` is tracked here as a research-only legacy UAC virtualization control, not as a shipped end-user tweak.

This run also produced one tooling fix. The first 2026-04-13 pass overcaptured `EnableVirtualizationForInprocServer` and `EnableVirtualizationForInprocHandler` because the raw hit filter matched the target value name as a substring. [run-wpr-boot-registry-probe.ps1](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1) now requires an exact quoted value-name match before a raw tracerpt registry line is retained or normalized.
