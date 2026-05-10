# Registry Value Experiment - win11-25h2-mf-buffering-threshold-0-registry-only-after-helper-fix

- Status: **ok**
- Generated UTC: `2026-05-08T23:43:49Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold`
- Test value: `0`

## Result

- `apply_smoke_hard_success`: `True`
- `post_reboot_smoke_hard_success`: `True`
- `post_rollback_smoke_hard_success`: `True`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-present`, value_exists=`True`, value=`0`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `0`
- `process-smoke-skipped`: `True` - smoke_profile=none

## Stage: post_reboot_rollback

- `status`: `ok`
- `error`: `None`
- `restore_action`: `restored-original-value`
- `after_reboot`: status=`value-present`, value_exists=`True`, value=`0`
- `after_restore`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `0`
- `process-smoke-skipped`: `True` - smoke_profile=none

## Stage: post_rollback

- `status`: `ok`
- `error`: `None`
- `final`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `0`
- `process-smoke-skipped`: `True` - smoke_profile=none
