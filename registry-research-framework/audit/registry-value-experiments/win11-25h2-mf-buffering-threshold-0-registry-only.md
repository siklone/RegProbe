# Registry Value Experiment - win11-25h2-mf-buffering-threshold-0-registry-only

- Status: **error**
- Generated UTC: `2026-05-08T23:35:29Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold`
- Test value: `0`

## Result

- Error: `guest-did-not-return-after-apply-reboot`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-present`, value_exists=`True`, value=`0`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `0`
- `process-smoke-skipped`: `True` - smoke_profile=none
