# Power Control Docs-First Runtime Capture

Date: 2026-03-29

Source manifest: `registry-research-framework/audit/power-control-docs-first-value-exists-static-triage-20260329.json`
Snapshot: `RegProbe-Baseline-Clean-20260329`

## Outcome

- Shared Procmon-safe runtime batch ran against all 7 docs-first power-control values.
- VM shell stayed healthy before and after the run.
- Result status: `copy-incomplete`
- `exact_hit_candidates = 0`
- `path_only_candidates = 0`
- `copy_incomplete_candidates = 7`
- No guest-side wrapper-error file was copied back for any candidate.

## Interpretation

The batch is useful as a runtime follow-up, but it did not produce usable per-candidate guest capture artifacts on the clean baseline. That means it narrows the runtime lane less than a successful Procmon bundle would have, and it should be treated as a negative or incomplete runtime attempt rather than a promotion-grade exact-read proof.

## Artifacts

- `evidence/files/vm-tooling-staging/power-control-docs-first-runtime-20260329-134010/summary.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-runtime-20260329-134010/results.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-runtime-20260329-134010/driver-run.log`
