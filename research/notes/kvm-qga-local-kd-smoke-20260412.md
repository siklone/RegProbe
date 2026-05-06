# KVM QGA Local KD Smoke

Date: 2026-04-12
Domain: `regprobe-win11-25h2-session`

We migrated [scripts/vm-kvm/run-guest-local-kd-smoke.py](../../scripts/vm-kvm/run-guest-local-kd-smoke.py) onto the same QGA no-wait launch path that now powers the registry policy probe wrapper. The host wrapper uploads a generated guest launcher through [scripts/vm-kvm/qga-run-powershell.py](../../scripts/vm-kvm/qga-run-powershell.py), returns immediately, and then polls the bridge for the local KD summary.

Smoke command:

```bash
python3 scripts/vm-kvm/run-guest-local-kd-smoke.py \
  --launch-transport qga \
  --output-name local-kd-qga-smoke-20260412b \
  --trigger-profile uuid-rpc-com-burst \
  --query-symbol nt!CmQueryValueKey \
  --timeout-seconds 300 \
  --smoke-timeout-seconds 180
```

What the smoke proved:

- `launch_transport = qga`
- `status = ok`
- `attached = true`
- `completed = true`
- `query_symbol_seen = true`
- `symchk_exit_code = 0`
- `trigger_executed = true`

The debugger log shows a clean local attach and symbol lookup:

```text
Connected to Windows 10 26100 x64 target
x nt!CmQueryValueKey
fffff805`83274900 nt!CmQueryValueKey (CmQueryValueKey)
```

One small quality fix came out of this run. The guest-side helper used to omit a `status` field, which made the host wrapper report successful runs as `unknown`. [scripts/vm/guest-tools/run-local-kd-smoke.ps1](../../scripts/vm/guest-tools/run-local-kd-smoke.ps1) now writes an explicit `status`, and the wrapper also has a safe fallback synthesizer for older summaries.

Why this matters:

This is the second long-running KVM wrapper that now launches cleanly through QGA instead of depending on visible shell state. The transport layer is getting simpler and more deterministic, which means future debugger and reboot lanes are more likely to fail for real research reasons rather than guest-input fragility.

## Audit artifact

- [kvm-qga-local-kd-smoke-20260412.json](../../registry-research-framework/audit/kvm-qga-local-kd-smoke-20260412.json)
