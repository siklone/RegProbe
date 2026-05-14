# Operator96 Low-Noise Rerun Plan

- Generated UTC: `2026-05-13T05:50:14Z`
- Review: `registry-research-framework/audit/operator96-app-surface-review-20260510.json`
- Candidate records: `84`
- Start offset: `45`
- Tranche records: `5`
- Tranche expected experiments: `10`
- Tranche indexes: `53, 54, 55, 56, 57`

## Commands

- Plan only:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-10.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-10.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --abort-on-noisy-host --only-index 53 --only-index 54 --only-index 55 --only-index 56 --only-index 57`
- Run:
  `python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --output-dir registry-research-framework/audit/registry-value-experiments-low-noise-20260510 --campaign-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-10.json --markdown-output registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-10.md --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --host-noise-max-retries 18 --host-noise-retry-interval-seconds 10.0 --host-noise-busy-threshold-pct 12.5 --host-noise-load1-per-cpu-threshold 0.5 --host-noise-sample-interval-seconds 1.0 --rerun --abort-on-noisy-host --only-index 53 --only-index 54 --only-index 55 --only-index 56 --only-index 57 --run`

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

## Tranche

| # | Value | Reason | Action |
|---:|---|---|---|
| 53 | `EventProcessorEnabled` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 54 | `LidReliabilityState` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 55 | `HibernateEnabled` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 56 | `DisableInboxPepGeneratedConstraints` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 57 | `DisableDisplayBurstOnPowerSourceChange` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
