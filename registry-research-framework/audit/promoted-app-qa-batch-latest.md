# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T17:37:33Z
- Catalog candidates: 242
- Selected candidates: 2
- Planned apply-allowed candidates: 2
- Live successes: 2
- Live failures: 0

## Selected Candidates

- `privacy.disable-diagnostic-data` -> `privacy.set-diagnostic-data-to-minimum-supported-level` | Set Diagnostic Data to Minimum Supported Level | Privacy
  docs: `research/records/privacy.set-diagnostic-data-to-minimum-supported-level.review.json`
  rollback: default=true | previous=true
- `privacy.disable-sync-settings` -> `privacy.turn-off-sync-by-default-allow-user-override` | Turn Off Settings Sync by Default | Privacy
  docs: `research/records/privacy.turn-off-sync-by-default-allow-user-override.review.json`
  rollback: default=true | previous=true

## Live Results

- `privacy.disable-diagnostic-data` -> `privacy.set-diagnostic-data-to-minimum-supported-level` | success=true | status=not-applicable
  summary: This tweak only applies on Enterprise, Education, or Server-class editions where AllowTelemetry=0 is documented as supported. Current edition: Professional.
- `privacy.disable-sync-settings` -> `privacy.turn-off-sync-by-default-allow-user-override` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
