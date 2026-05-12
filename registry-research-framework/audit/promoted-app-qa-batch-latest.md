# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-12T19:55:15Z
- Catalog candidates: 258
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `audio.disable-beep` | System Beep Driver | Audio
  docs: `research/records/audio.disable-beep.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `cleanup.disable-reserved-storage` | Disable Reserved Storage | Cleanup
  docs: `research/records/cleanup.disable-reserved-storage.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `developer.docker-performance` | Docker Desktop WSL 2 Backend | Developer
  docs: `research/records/developer.docker-performance.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `explorer.always-show-icons-never-thumbnails` | Always Show Icons, Never Thumbnails | Explorer
  docs: `research/records/explorer.always-show-icons-never-thumbnails.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `misc.disable-edge-features` | Disable Microsoft Edge Features | Misc
  docs: `Docs/misc/misc.md`
  rollback: default=false | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `audio.disable-beep` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `cleanup.disable-reserved-storage` | success=true | status=not-applicable
  summary: Reserved Storage is currently in use by Windows servicing. Wait for servicing operations to complete and try again later.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `developer.docker-performance` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `explorer.always-show-icons-never-thumbnails` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `misc.disable-edge-features` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
