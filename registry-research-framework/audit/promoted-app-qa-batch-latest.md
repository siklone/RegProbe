# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T18:16:56Z
- Catalog candidates: 242
- Selected candidates: 1
- Planned apply-allowed candidates: 1
- Live successes: 1
- Live failures: 0

## Selected Candidates

- `system.wait-to-kill-service-timeout` | Service Shutdown Timeout | System
  docs: `research/records/system.wait-to-kill-service-timeout.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `system.wait-to-kill-service-timeout` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
