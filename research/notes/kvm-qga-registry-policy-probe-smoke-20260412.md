# KVM QGA Registry Policy Probe Smoke

Date: 2026-04-12
Domain: `regprobe-win11-25h2-session`

We migrated [scripts/vm-kvm/run-guest-registry-policy-probe.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/run-guest-registry-policy-probe.py) to use the new [scripts/vm-kvm/qga-run-powershell.py](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/scripts/vm-kvm/qga-run-powershell.py) helper in `--no-wait` mode. That matters because these host wrappers were never meant to wait on the guest PowerShell process directly; they launch a guest workflow and then poll stage and summary files from the host bridge.

Smoke command:

```bash
python3 scripts/vm-kvm/run-guest-registry-policy-probe.py \
  --launch-transport qga \
  --registry-path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System' \
  --value-name 'AllowRemoteDASD' \
  --output-name 'allowremotedasd-procmon-kvm-qga-async-20260412' \
  --trigger-profile 'session-manager-io-raw-burst' \
  --saveas-timeout-seconds 20 \
  --timeout-seconds 120
```

What the smoke proved:

- The wrapper launched through QGA without calling `ensure-guest-admin-shell.py`.
- Host-side polling resumed immediately after launch.
- The wrapper produced the expected probe-stage fallback summary on failure instead of hanging on guest process lifetime.

Observed failure:

- `probe_stage = exception`
- `status = error`
- `message = Procmon SaveAs timed out after 20 second(s).`

Why this result still matters:

The failure was inside the existing Procmon export lane, not the transport layer. We now have cleaner separation: QGA launch is green, Procmon `SaveAs` remains its own bottleneck. That separation is narrower than the earlier state where both concerns were mixed together behind `send-key` automation.

## Audit artifact

- [kvm-qga-registry-policy-probe-smoke-20260412.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/kvm-qga-registry-policy-probe-smoke-20260412.json)
