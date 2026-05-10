# Operator96 Low-Noise Rerun Plan

- Generated UTC: `2026-05-10T19:44:27Z`
- Review: `registry-research-framework/audit/operator96-app-surface-review-20260510.json`
- Candidate records: `85`
- First tranche records: `5`
- First tranche expected experiments: `10`
- First tranche indexes: `1, 2, 6, 9, 10`

## Commands

- Plan only:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --only-index 1 --only-index 2 --only-index 6 --only-index 9 --only-index 10`
- Run:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --only-index 1 --only-index 2 --only-index 6 --only-index 9 --only-index 10 --run`

## Policy

- `output_dir`: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510`
- `max_values_per_record`: `2`
- `host_noise_max_retries`: `18`
- `host_noise_retry_interval_seconds`: `10.0`
- `host_noise_busy_threshold_pct`: `12.5`
- `host_noise_load1_per_cpu_threshold`: `0.5`
- `host_noise_sample_interval_seconds`: `1.0`
- `post_reboot_delay_seconds`: `90`
- `claim_rule`: `Rerun results may support app-card copy only if host_noise=ok and confidence is not low.`

## First Tranche

| # | Value | Reason | Action |
|---:|---|---|---|
| 1 | `EnableLocalLogonSid` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 2 | `EnableVirtualization` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 6 | `TickcountRolloverDelay` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 9 | `ForceEnableMutantAutoboost` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 10 | `AllowRemoteDASD` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
