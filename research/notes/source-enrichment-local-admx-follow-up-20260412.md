# Source Enrichment Local ADMX Follow-up

- Generated: `2026-04-11T22:21:02.863124Z`
- Candidate manifest: `registry-research-framework/audit/kernel-power-96-phase0-candidates-20260329.json`
- Source config: `registry-research-framework/config/source-enrichment-sources.json`
- Source root override: `REGPROBE_SOURCE_ROOT_ADMX=evidence/files/external/c`

## Result

The Linux-host source-enrichment follow-up successfully scanned the repo-local ADMX mirror after the root-expansion and override fixes. The run covered `96` phase-0 candidates, scanned `121` ADMX/ADML files, and produced exactly `1` supported candidate after the generic-token and prefix-collision tightening.

## Supported candidate

- `power.throttling.power-throttling-off`
  - `Power.admx` line `667`
  - Exact hit: `key="System\CurrentControlSet\Control\Power\PowerThrottling" valueName="PowerThrottlingOff"`
  - Queue suggestion: `runtime`

## Explicit no-support

- `power.control.allow-audio-to-enable-execution-required-power-requests`
- `power.control.allow-system-required-power-requests`

The execution-required pair remains outside the ADMX lane; the checked-in repo-local policy-template surface does not contain exact support for those value names.

## Notes

- Generic value names now require the most specific registry leaf to appear in the file before hits count.
- Value-name matches now require token boundaries, so prefix collisions like `EnableVirtualization` vs `EnableVirtualizationBasedSecurity` no longer register as support.
- No local Windows SDK/WDK headers mirror was found on this Linux host, so the WDK half of the seed plan is still blocked on source availability.
