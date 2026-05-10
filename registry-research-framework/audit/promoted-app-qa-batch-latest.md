# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-10T17:18:01Z
- Catalog candidates: 258
- Selected candidates: 2
- Planned apply-allowed candidates: 2
- Live successes: 2
- Live failures: 0

## Selected Candidates

- `explorer.always-show-icons-never-thumbnails` | Always Show Icons, Never Thumbnails | Explorer
  docs: `research/records/explorer.always-show-icons-never-thumbnails.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.control.class1-initial-unpark-count` | Class1 Initial Unpark Count | Power
  docs: `research/records/power.control.class1-initial-unpark-count.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `explorer.always-show-icons-never-thumbnails` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.control.class1-initial-unpark-count` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
