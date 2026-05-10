# Registry Value Experiment Recovery - win11-25h2-mf-buffering-threshold-0-smoke

- Status: **recovered-via-snapshot**
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold`
- Test value: `0`
- VM: `regprobe-win11-25h2-session`
- Snapshot: `clean-25h2-qga`

## Observation

After the apply stage reboot, the guest did not return to QGA. The VM displayed Windows Automatic Repair with "Your PC did not start correctly".

Screenshot: `registry-research-framework/audit/registry-value-experiments/screenshots/win11-25h2-mf-buffering-threshold-0-automatic-repair.png`

## Recovery

- Captured the repair screen as evidence.
- Stopped the experiment runner.
- Destroyed the broken VM runtime.
- Reverted the `clean-25h2-qga` libvirt snapshot.
- Started the VM again.
- Verified `vm-health-check.py` returned `status=ok`, `guest_health=stable`, and `transport_blocker=none`.

## Control

A plain post-recovery reboot from the clean snapshot returned to QGA successfully. That means the failure is not explained by the fresh VM being generally unable to reboot.

## Repeat

Repeat artifact: `registry-research-framework/audit/registry-value-experiments/win11-25h2-mf-buffering-threshold-0-repeat.json`

The repeat run observed `MfBufferingThreshold=0` before apply and `MfBufferingThreshold=0` after apply, so no value change happened. The guest still failed to return after the apply reboot, then the hardened runner auto-reverted `clean-25h2-qga` and QGA returned successfully.

## Isolation

- Manual QGA `New-ItemProperty ... MfBufferingThreshold=0` followed by reboot returned to QGA successfully.
- Registry-only runner before the helper fix still hit Automatic Repair.
- Registry-only runner after the helper fix passed: `registry-research-framework/audit/registry-value-experiments/win11-25h2-mf-buffering-threshold-0-registry-only-after-helper-fix.json`

## Interpretation

This is a real boot-return failure observation, but it should not be classified as a registry-value failure. The issue was the old experiment helper using `New-Item -Force` on an existing critical registry key. The helper now only creates missing keys and uses `Set-ItemProperty` for existing values; the fixed registry-only run passes.
