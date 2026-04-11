# Source Enrichment Record Mapping Follow-up

- Generated: `2026-04-12T01:43:00+03:00`
- Inputs:
  - `registry-research-framework/audit/source-enrichment-local-admx-follow-up-20260412.json`
  - `registry-research-framework/audit/source-enrichment-systeminformer-follow-up-20260412.json`

## Existing record mappings

- `power.throttling.power-throttling-off` -> `power.disable-power-throttling`
  - exact ADMX-backed path/value match
- `power.session.hiberboot-enabled` -> `power.disable-fast-startup`
  - same authoritative `Session Manager\Power\HiberbootEnabled` surface
- `power.control.hiberboot-enabled` -> `power.disable-fast-startup`
  - sibling/control-path candidate; existing record already documents why this path is not authoritative
- `system.kernel.disable-exception-chain-validation` -> `system.kernel.disable-exception-chain-validation`
  - existing validated record; source-enrichment only adds corroboration

## Orphan candidate

- `power.control.ttm-enabled`
  - manifest route bucket: `docs-first-new-candidate`
  - source-enrichment:
    - score `2`
    - queue bucket `runtime`
    - runtime priority `high`
    - supporting hit: `phnt/include/ntpoapi.h` -> `BOOLEAN TtmEnabled;`

This candidate is still missing both a `research/records` entry and a queue entry. It is now the cleanest source-enrichment-backed orphan from the kernel-power 96 batch.

## Execution-required pair

- `power.control.allow-audio-to-enable-execution-required-power-requests`: no source-enrichment support
- `power.control.allow-system-required-power-requests`: no source-enrichment support

The execution-required blocker remains outside the source/header lane.
