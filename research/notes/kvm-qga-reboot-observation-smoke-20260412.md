# KVM QGA Reboot Observation Smoke

Date: 2026-04-12
Domain: `regprobe-win11-25h2-session`

We migrated [scripts/vm-kvm/run-guest-reboot-observation.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/run-guest-reboot-observation.py) to the QGA no-wait launch model used by the other KVM wrappers. This wrapper is slightly trickier because it launches one guest script before reboot and a second guest script after reboot, which means the post-reboot transport has to wait for qemu guest agent readiness instead of assuming it is immediately available.

Smoke command:

```bash
python3 scripts/vm-kvm/run-guest-reboot-observation.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' \
  --value-name HiberFileSizePercent \
  --output-name hiberfilesizepercent-qga-reboot-smoke-20260412d \
  --timeout-seconds 420 \
  --prepare-timeout-seconds 180 \
  --reboot-settle-seconds 45 \
  --post-reboot-delay-seconds 20
```

Final result:

- `prepare_launch_transport = qga`
- `post_reboot_launch_transport = qga`
- `status = ok`
- `reboot_observed = true`
- `value_changed = false`
- `value_preserved = true`

Observed value:

- before: `HiberFileSizePercent = 0`
- after: `HiberFileSizePercent = 0`
- boot time changed from `2026-04-12T17:54:58.5000000Z` to `2026-04-12T17:57:12.5000000Z`

Fixes that were needed along the way:

- the guest reboot helper was rejecting `HKLM:\...` registry paths because its provider-path normalization only handled `HKLM\...`
- post-reboot QGA launch needed a retry window because the guest agent does not become ready instantly after reboot
- `value_changed` was comparing full JSON payloads, which incorrectly treated timestamp drift as registry drift

Why this matters:

This closes another fragile operator assumption. Reboot-backed KVM observation no longer needs visible guest typing for either stage, and the resulting summary now describes the actual registry outcome more honestly.
