# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T17:10:19Z
- Catalog candidates: 240
- Selected candidates: 4
- Planned apply-allowed candidates: 4
- Live successes: 4
- Live failures: 0

## Selected Candidates

- `power.disable-fast-startup` | Fast Startup (Hiberboot) | Power
  docs: `research/records/power.disable-fast-startup.review.json`
  rollback: default=true | previous=true
- `power.disable-windows-search` | Windows Search Service | Power
  docs: `research/records/power.disable-windows-search.json`
  rollback: default=true | previous=true
- `explorer.hide-empty-drives` | Hide Empty Drives | Explorer
  docs: `research/records/explorer.hide-empty-drives.review.json`
  rollback: default=true | previous=true
- `privacy.disable-find-my-device` | Find My Device | Privacy
  docs: `research/records/privacy.disable-find-my-device.json`
  rollback: default=true | previous=true

## Live Results

- `power.disable-fast-startup` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `power.disable-windows-search` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `explorer.hide-empty-drives` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `privacy.disable-find-my-device` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
