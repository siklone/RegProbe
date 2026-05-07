# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T16:59:48Z
- Catalog candidates: 242
- Selected candidates: 2
- Planned apply-allowed candidates: 2
- Live successes: 2
- Live failures: 0

## Selected Candidates

- `notifications.disable-mirroring` | Notification Mirroring | Notifications
  docs: `research/records/notifications.disable-mirroring.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `notifications.disable-tile` | Tile Notifications | Notifications
  docs: `research/records/notifications.disable-tile.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `notifications.disable-mirroring` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `notifications.disable-tile` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
