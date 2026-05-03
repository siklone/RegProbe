# KVM QGA PowerShell Runner Smoke

Date: 2026-04-12
Domain: `regprobe-win11-25h2-session`

We now have a reusable host-side helper at [scripts/vm-kvm/qga-run-powershell.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/qga-run-powershell.py) that uploads a local PowerShell script into the Windows guest through qemu guest agent `guest-file-*`, runs it through `guest-exec`, waits for completion, and optionally deletes the staged guest copy.

Smoke command:

```bash
python3 scripts/vm-kvm/qga-run-powershell.py \
  --domain regprobe-win11-25h2-session \
  --script scripts/vm/run-dpc-timer-etw-trace-guest.ps1 \
  --wait-timeout 900 \
  --ps-arg=-TraceSeconds \
  --ps-arg=5 \
  --ps-arg=-UploadBaseUrl \
  --ps-arg=http://10.0.2.2:8766 \
  --ps-arg=-UploadPrefix \
  --ps-arg=qga-runner-dpc-timer-smoke-20260412
```

What happened:

- The host script uploaded into `C:\RegProbe-Diag\staging\run-dpc-timer-etw-trace-guest.ps1`.
- Guest PowerShell executed it with exit code `0`.
- The staged guest copy was removed after execution.
- The run produced uploaded artifacts under `/tmp/regprobe-bridge/qga-runner-dpc-timer-smoke-20260412/`.

Artifact summary:

- `dpc-timer-registry.etl`: `1,040,384` bytes
- `dpc-timer-registry.xml`: `9,666,445` bytes
- `trace-summary.json`: `status=completed`, `target_hits_count=0`

Why this matters:

This closes the fragile part of the KVM lane. We no longer need ISO short-name gymnastics or long `send-kvm-text` sequences just to get a guest script started. The remaining work is mostly migration: moving existing runtime probes, bench runners, and collection scripts onto this QGA-backed execution path.

## Audit artifact

- [kvm-qga-powershell-runner-smoke-20260412.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/kvm-qga-powershell-runner-smoke-20260412.json)
