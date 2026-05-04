# system.kernel-long-dpc-threshold-cluster trigger follow-up - 2026-04-08

## Summary

- The `LongDpcQueueThreshold` / `LongDpcRuntimeThreshold` cluster now has a dedicated repo-native `timer-dpc-stress` harness and a reusable Procmon replay profile, not just an abstract trigger plan.
- The guest runtime tooling now exposes:
  - `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
  - `Invoke-TimerDpcStressTrigger`
  - `scripts/vm/guest-tools/run-registry-policy-probe.ps1`
  - `TriggerProfile = timer-dpc-stress`
- The new dedicated trigger layers:
  - `NtSetTimerResolution(5000, true/false, ...)`
  - `timeBeginPeriod(1)` / `timeEndPeriod(1)`
  - eight concurrent `System.Threading.Timer` instances with short periods
  - bounded multi-core CPU jobs with short sleeps to keep a timer/DPC-heavy cadence
- The older `Invoke-TimerResolutionTrigger` remains as the base primitive.
- This aligns with the source-enrichment guidance for the cluster:
  - `trigger_family = timer-dpc-stress`
  - `suggested_trigger = ["high-resolution timer request", "multiple concurrent timers", "DPC-heavy workload"]`
  - `suggested_runtime_priority = low`
  - `suggested_queue_bucket = windbg`

## Source artifacts

- `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
- `scripts/vm/guest-tools/run-registry-policy-probe.ps1`
- `scripts/source_enrichment_scan.py`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-queue-threshold.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-runtime-threshold.json`

## Interpretation

- new proof gained:
  - the repo now contains a dedicated `timer-dpc-stress` trigger instead of only a reusable timer-resolution primitive
  - the generic Procmon guest tool can invoke the same trigger family without requiring a one-off custom command
  - the enrichment guidance and the guest trigger surface now point in the same direction
- narrowed conclusion:
  - the next runtime lane for `LongDpc*Threshold` does not need a net-new trigger family design or a new guest harness
  - the remaining gap is no longer harness design; it is execution and evidence capture
- next proof path:
  - replay the `Session Manager\Kernel` long-DPC cluster under `TriggerProfile = timer-dpc-stress` instead of the broader kernel batch
  - keep the lane narrow and preserve the existing broad batch as supporting evidence only
