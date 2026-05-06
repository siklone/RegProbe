## Scope

Lock the remaining execution-required research queue to a concrete repo-native runtime lane instead of leaving it as a generic `runtime-trace` placeholder.

## Findings

1. The current audit now leaves only two records at `next_missing_layer = runtime-trace`:
   - `power.control.allow-system-required-power-requests`
   - `power.control.allow-audio-to-enable-execution-required-power-requests`
2. Both records are now mapped in `registry-research-framework/config/tweak-vm-runners.json` to the same dedicated narrow runner:
   - `registry-research-framework/tools/run-path-aware-runtime-probe.ps1`
3. The path-aware runner now declares both execution-required candidates directly and uses a dedicated trigger family:
   - `execution-required-power-requests-short`
   - `execution-required-power-requests-only`
4. That trigger family is narrower than the retained broad mega-trigger pilot:
   - `powercfg /requests`
   - `powercfg /requestsoverride`
   - power-plan toggles
   - `PowerCreateRequest` / `PowerSetRequest` / `PowerClearRequest`
5. The broad mega-trigger lane therefore remains a fallback and historical evidence source, not the only repo-native runtime path for this pair.

## Interpretation

The remaining open gap for the execution-required pair is no longer runner design. The repo now has explicit runtime-trace plumbing for both records, and the trigger surface is aligned with the already-retained override and request evidence. What remains unresolved is live guest execution and exact capture, not missing lane scaffolding.

## Retained readiness audit

- [runtime-trace-runner-readiness-20260408.md](../../registry-research-framework/audit/runtime-trace-runner-readiness-20260408.md)
- [runtime-trace-runner-readiness-20260408.json](../../registry-research-framework/audit/runtime-trace-runner-readiness-20260408.json)
