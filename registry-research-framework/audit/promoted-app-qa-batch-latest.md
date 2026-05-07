# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T16:03:08Z
- Catalog candidates: 242
- Selected candidates: 2
- Planned apply-allowed candidates: 2
- Live successes: 2
- Live failures: 0

## Selected Candidates

- `system.enable-indexing-encrypted-items` | Indexing of Encrypted Files | System
  docs: `research/records/system.enable-indexing-encrypted-items.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `system.graphics-tdr-ddi-delay` | Graphics TDR DDI Delay | System
  docs: `research/records/system.graphics-tdr-ddi-delay.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `system.enable-indexing-encrypted-items` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `system.graphics-tdr-ddi-delay` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
