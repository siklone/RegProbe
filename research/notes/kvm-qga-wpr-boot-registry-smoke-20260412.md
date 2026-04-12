# KVM QGA WPR Boot Registry Smoke

Date: 2026-04-12
Domain: `regprobe-win11-25h2-session`

We migrated [scripts/vm-kvm/run-guest-wpr-boot-registry.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/run-guest-wpr-boot-registry.py) onto the same QGA no-wait launch path that already works for the registry policy, local KD, and reboot-observation wrappers. The host wrapper now uploads generated guest launchers through [scripts/vm-kvm/qga-run-powershell.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/qga-run-powershell.py), returns immediately, and then polls the host bridge for arm and collect summaries.

Smoke command:

```bash
python3 scripts/vm-kvm/run-guest-wpr-boot-registry.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' \
  --value-name HiberFileSizePercent \
  --output-name hiberfilesizepercent-wpr-qga-smoke-20260412d \
  --timeout-seconds 900 \
  --prepare-timeout-seconds 180 \
  --reboot-settle-seconds 45 \
  --tracerpt-timeout-seconds 420
```

What the smoke proved:

- `arm_launch_transport = qga`
- `collect_launch_transport = qga`
- `status = ok`
- `reboot_observed = true`
- `etl_exists = true`
- `csv_exists = true`
- `hits_csv_exists = true`
- `hit_line_count = 25`
- `normalized_bundle_exists = true`
- `normalization_status = ok`
- `tracerpt_exit_code_indeterminate = true`

Two concrete fixes came out of this pass. First, [scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1) now treats `tracerpt` runs with `exit_code = null` as indeterminate instead of automatically fatal when the CSV file exists and stdout reports success. That matters because this guest build does sometimes complete `tracerpt` successfully without a concrete exit code surfacing through the current process wrapper.

Second, the guest normalizer no longer tries to build its root `[ordered]` bundle with inline `@($events)` and `@($events).Count` expressions. In this PowerShell environment those expressions can throw `Argument types do not match` when used directly inside the ordered hashtable literal. Precomputing the event array and count fixes that cleanly and made the normalizer stable on the retained hit-only CSV.

The collect stage also now writes a dedicated `*.hits.csv` that contains only the header plus matching lines before normalization. That keeps the normalizer focused on the relevant registry rows instead of forcing it to ingest the full `tracerpt` CSV payload.

Why this matters:

The WPR boot-registry lane is now on the same durable transport surface as the other long-running KVM wrappers. We can use it for real boot-time registry evidence without depending on fragile foreground typing, and we have a clean normalized bundle coming out the other side instead of a partially successful trace that still needs manual rescue.
