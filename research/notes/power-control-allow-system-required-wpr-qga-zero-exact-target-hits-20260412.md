# AllowSystemRequiredPowerRequests WPR/QGA No-Hit

Date: 2026-04-12
Candidate: `power.control.allow-system-required-power-requests`

After the HiberFileSizePercent WPR lane proved viable, we tried the same QGA-launched boot-registry capture against `AllowSystemRequiredPowerRequests`.

Command:

```bash
python3 scripts/vm-kvm/run-guest-wpr-boot-registry.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' \
  --value-name AllowSystemRequiredPowerRequests \
  --output-name allow-system-required-wpr-qga-20260412a \
  --timeout-seconds 900 \
  --prepare-timeout-seconds 180 \
  --reboot-settle-seconds 45 \
  --tracerpt-timeout-seconds 420
```

What happened:

- The arm stage completed through QGA.
- The guest rebooted and entered `collect-tracerpt`.
- The guest produced a `1.55 GB` ETL and a `2.97 GB` tracerpt CSV.
- The target-specific `*.hits.csv` was only `346` bytes and contained just the header.
- The host wrapper eventually returned `runner-timeout`.
- Guest `summary.json` and `*.normalized.json` were left as zero-byte files, consistent with trace-size/disk-pressure failure after the hit filter.

The important research result is the salvaged hit CSV:

```text
Event Name, Type, Event ID, Version, Channel, Level, Opcode, Task, Keyword, PID, TID, Processor Number, Instance ID, Parent Instance ID, Activity ID, Related Activity ID, Clock-Time, Kernel(ms), User(ms), User Data
```

There are zero exact `AllowSystemRequiredPowerRequests` lines. This is a target-specific zero-exact-target-hits result for the WPR boot lane, not a transport failure. It keeps `runtime_no_read` open and indicates that the next step is either symbolizing the unlabeled INIT walker or building a smaller streamed trace that does not materialize multi-GB tracerpt CSV files.

Tooling note:

[scripts/vm-kvm/qga-get-file.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/qga-get-file.py) was added during this pass. It downloads guest files through QGA `guest-file-read`, which let us salvage the tiny hit CSV even when PowerShell output capture was unreliable under guest I/O pressure.

Cleanup:

The temporary WPR/QGA trace directories were removed from `C:\RegProbe-Diag\wpr-boot-registry` after host-side evidence capture. Guest `C:` free space was back to roughly `35.7 GB` after cleanup.

## Retained audit artifact

- [power-control-allow-system-required-wpr-qga-zero-exact-target-hits-20260412.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-control-allow-system-required-wpr-qga-zero-exact-target-hits-20260412.json)
