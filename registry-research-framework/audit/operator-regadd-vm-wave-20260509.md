# Operator Reg Add VM Wave - 2026-05-09

- Status: **ok**
- Tested records: `5`
- All experiments OK: `True`
- All hard smoke OK: `True`
- Baseline before: `registry-research-framework/audit/operator-regadd-vm-baseline-20260509T081911Z.json`
- Baseline after: `registry-research-framework/audit/operator-regadd-vm-baseline-20260509T083542Z.json`

## Baseline Counts

| Metric | Before | After |
|---|---:|---:|
| `total_entries` | `96` | `96` |
| `key_present_count` | `94` | `94` |
| `key_missing_count` | `2` | `2` |
| `value_present_count` | `21` | `21` |
| `value_missing_count` | `73` | `73` |
| `error_count` | `0` | `0` |

## Experiment Results

| Label | Target | Value | Original | After reboot | Restore | Final | Hard smoke |
|---|---|---:|---|---|---|---|---|
| power.disable-power-throttling/key-missing-create | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff` | `1` | `key-missing:None` | `value-present:1` | `removed-created-key` | `key-missing:None` | `apply_smoke_hard_success=True, post_reboot_smoke_hard_success=True, post_rollback_smoke_hard_success=True` |
| session-manager-power.hiberbootenabled/existing-toggle | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled` | `0` | `value-present:1` | `value-present:0` | `restored-original-value` | `value-present:1` | `apply_smoke_hard_success=True, post_reboot_smoke_hard_success=True, post_rollback_smoke_hard_success=True` |
| power.energy-estimation-enabled/existing-toggle | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled` | `0` | `value-present:1` | `value-present:0` | `restored-original-value` | `value-present:1` | `apply_smoke_hard_success=True, post_reboot_smoke_hard_success=True, post_rollback_smoke_hard_success=True` |
| power.perf-calculate-actual-utilization/existing-toggle | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization` | `0` | `value-present:1` | `value-present:0` | `restored-original-value` | `value-present:1` | `apply_smoke_hard_success=True, post_reboot_smoke_hard_success=True, post_rollback_smoke_hard_success=True` |
| power.force-hibernate-disabled-policy/key-missing-create | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy` | `1` | `key-missing:None` | `value-present:1` | `removed-created-key` | `key-missing:None` | `apply_smoke_hard_success=True, post_reboot_smoke_hard_success=True, post_rollback_smoke_hard_success=True` |

## Notes

- Store URI launch fails from QGA/SYSTEM with operation-not-supported in all stages; treated as soft/user-session smoke gap, not value-specific regression.
- Post-wave baseline matches pre-wave counts: rollback returned inventory to original key/value presence profile.
- Next waves should split missing existing-key values into ETW/Procmon/WPR lanes and high-risk kernel values into smaller snapshot-protected batches.

## Superseded Artifact

- `registry-research-framework/audit/registry-value-experiments/win11-25h2-powerthrottlingoff-1-gui-smoke.json`: first run exposed the empty-key rollback bug; clean run supersedes it.

## Lane Probe Attempts

| Record | Lane | Status | Finding | Artifact |
|---|---|---|---|---|
| `AllowAudioToEnableExecutionRequiredPowerRequests` | `etw-stackwalk` | `error` | `xperf.exe` missing in clean 25H2 VM | `registry-research-framework/audit/operator-allowaudio-execpowerrequests-etw-20260509-summary.json` |
| `AllowAudioToEnableExecutionRequiredPowerRequests` | `procmon-bootlog` | `error` | Procmon runner still uses `ensure-admin-shell`/send-key arming and timed out; needs QGA-first hardening | `registry-research-framework/audit/operator-allowaudio-execpowerrequests-procmon-20260509-summary.json` |
