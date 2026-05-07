# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T17:18:45Z
- Catalog candidates: 242
- Selected candidates: 2
- Planned apply-allowed candidates: 2
- Live successes: 2
- Live failures: 0

## Selected Candidates

- `developer.windows-dev-mode` | Windows Developer Mode | Developer
  docs: `research/records/developer.windows-dev-mode.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer
  docs: `research/records/developer.wsl2-memory.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `developer.windows-dev-mode` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `developer.wsl2-memory` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
