# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-17T15:01:31Z
- Catalog candidates: 258
- Selected candidates: 3
- Planned apply-allowed candidates: 3
- Live successes: 3
- Live failures: 0

## Selected Candidates

- `system.aero-shake` | Aero Shake Window Minimizing Gesture | System
  docs: `research/records/system.aero-shake.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `network.disable-active-probing` | NCSI Active Probing Policy | Network
  docs: `research/records/network.disable-active-probing.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.control.class1-initial-unpark-count` | Class1 Initial Unpark Count | Power
  docs: `research/records/power.control.class1-initial-unpark-count.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `system.aero-shake` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `network.disable-active-probing` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.control.class1-initial-unpark-count` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
