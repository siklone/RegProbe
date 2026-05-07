# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T17:14:00Z
- Catalog candidates: 242
- Selected candidates: 3
- Planned apply-allowed candidates: 3
- Live successes: 3
- Live failures: 0

## Selected Candidates

- `power.hide-sleep-option` | Show Sleep Option | Power
  docs: `research/records/power.hide-sleep-option.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.optimize-cpu-boost` | Optimize CPU Performance Boost | Power
  docs: `research/records/power.optimize-cpu-boost.json`
  rollback: default=false | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.optimize-gaming-network` | Games MMCSS Task Profile | Power
  docs: `research/records/power.optimize-gaming-network.json`
  rollback: default=false | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `power.hide-sleep-option` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.optimize-cpu-boost` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.optimize-gaming-network` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
