# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T17:17:12Z
- Catalog candidates: 242
- Selected candidates: 4
- Planned apply-allowed candidates: 4
- Live successes: 4
- Live failures: 0

## Selected Candidates

- `explorer.show-info-tips` | Show Explorer Info Tips | Explorer
  docs: `research/records/explorer.show-info-tips.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `explorer.show-protected-operating-system-files` | Show Protected Operating System Files | Explorer
  docs: `research/records/explorer.show-protected-operating-system-files.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `explorer.show-recent-items` | Show Recent Items In Home | Explorer
  docs: `research/records/explorer.show-recent-items.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `explorer.show-type-overlay` | Display File Icons On Thumbnails | Explorer
  docs: `research/records/explorer.show-type-overlay.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `explorer.show-info-tips` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `explorer.show-protected-operating-system-files` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `explorer.show-recent-items` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `explorer.show-type-overlay` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
