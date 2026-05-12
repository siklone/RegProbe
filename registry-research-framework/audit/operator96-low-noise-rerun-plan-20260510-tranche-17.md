# Operator96 Low-Noise Rerun Plan

- Generated UTC: `2026-05-11T23:23:21Z`
- Review: `registry-research-framework/audit/operator96-app-surface-review-20260510.json`
- Candidate records: `85`
- Start offset: `80`
- Tranche records: `5`
- Tranche expected experiments: `10`
- Tranche indexes: `90, 91, 92, 94, 95`

## Commands

- Plan only:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-17-20260510.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-17-20260510.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --only-index 90 --only-index 91 --only-index 92 --only-index 94 --only-index 95`
- Run:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-17-20260510.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-17-20260510.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --only-index 90 --only-index 91 --only-index 92 --only-index 94 --only-index 95 --run`

## Policy

- `output_dir`: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17`
- `max_values_per_record`: `2`
- `host_noise_max_retries`: `18`
- `host_noise_retry_interval_seconds`: `10.0`
- `host_noise_busy_threshold_pct`: `12.5`
- `host_noise_load1_per_cpu_threshold`: `0.5`
- `host_noise_sample_interval_seconds`: `1.0`
- `post_reboot_delay_seconds`: `90`
- `claim_rule`: `Rerun results may support app-card copy only if host_noise=ok and confidence is not low.`

## Tranche

| # | Value | Reason | Action |
|---:|---|---|---|
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 94 | `EnableDsNetRefresh` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 95 | `EnabledActions` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
