# Registry Value Experiments

This folder is for one-value-at-a-time VM experiments. It is intentionally stricter than a parser or existence check: the goal is to prove whether a value can be read, applied, survived through reboot, rolled back, and kept compatible with basic Windows app smoke.

## Contract

- Do not raw-apply a pasted batch.
- Parse pasted `reg add` text into an experiment plan first:

```bash
python3 scripts/registry/parse_reg_add_batch.py \
  --input pasted-reg-adds.txt \
  --json-output registry-research-framework/audit/registry-value-experiments/operator-batch.json \
  --markdown-output registry-research-framework/audit/registry-value-experiments/operator-batch.md
```

- Run only one value per VM snapshot or disposable overlay.
- For missing or opaque values, do not close as "not found" until ETW, Procmon, or static-string evidence has been attempted.
- For present values, test sensible alternate DWORD values one at a time.
- Smoke before and after reboot: shell process presence, command launch, PowerShell, x64 app launch, x86 app launch, Settings/Store URI best-effort, QGA health, rollback, reboot, final smoke.

## Reboot-Critical Runs

Boot-sensitive keys under `Session Manager`, `Control\Power`, scheduler, watchdog, hibernation, and mitigation paths must use either a libvirt snapshot or a disposable qcow2 overlay before apply/reboot testing.

Example guarded command:

```bash
python3 scripts/vm-kvm/run-guest-registry-value-experiment.py \
  --domain regprobe-win11-25h2-session \
  --connect qemu:///session \
  --registry-path 'HKLM\SYSTEM\CurrentControlSet\Control\Power' \
  --value-name PerfCalculateActualUtilization \
  --value-data 0 \
  --output-name perf-calculate-actual-utilization-0 \
  --require-domain-snapshot
```

## Pilot Lesson

`pilot-perf-calculate-actual-utilization-0` observed a reboot regression after setting `PerfCalculateActualUtilization=0` on the available VM profile. Offline registry restore, NTFS repair, SFC, DISM, System Restore, and update uninstall did not recover that image. Treat this as a hard safety signal for future boot-critical experiments: no snapshot or overlay means no apply/reboot run.
