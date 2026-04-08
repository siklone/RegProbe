# system.kernel-long-dpc-threshold-cluster trigger follow-up - 2026-04-08

## Summary

- The `LongDpcQueueThreshold` / `LongDpcRuntimeThreshold` cluster no longer lacks a concrete repo-native next trigger surface.
- The nearest reusable primitive already exists in the guest runtime tooling:
  - `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
  - `Invoke-TimerResolutionTrigger`
- That helper directly exercises:
  - `NtSetTimerResolution(5000, true/false, ...)`
  - `timeBeginPeriod(1)` / `timeEndPeriod(1)`
  - a short CPU loop with repeated `Start-Sleep -Milliseconds 25`
- This aligns with the source-enrichment recommendation for the cluster:
  - `trigger_family = timer-dpc-stress`
  - `suggested_trigger = ["high-resolution timer request", "multiple concurrent timers", "DPC-heavy workload"]`
  - `suggested_runtime_priority = low`
  - `suggested_queue_bucket = windbg`

## Source artifacts

- `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
- `scripts/source_enrichment_scan.py`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-queue-threshold.json`
- `registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-runtime-threshold.json`

## Interpretation

- new proof gained:
  - the repo already contains a reusable high-resolution timer primitive instead of a purely abstract trigger recommendation
  - the enrichment guidance and the guest trigger surface now point in the same direction
- narrowed conclusion:
  - the next runtime lane for `LongDpc*Threshold` does not need a net-new trigger family design
  - it does still need a dedicated harness that adds the missing two pieces beyond `Invoke-TimerResolutionTrigger`:
    - multiple concurrent timers
    - a more obviously DPC-heavy workload
- next proof path:
  - build a small dedicated `timer-dpc-stress` guest trigger by reusing `Invoke-TimerResolutionTrigger` as the base primitive
  - then replay the `Session Manager\Kernel` long-DPC cluster under that narrower trigger instead of the broader kernel batch
