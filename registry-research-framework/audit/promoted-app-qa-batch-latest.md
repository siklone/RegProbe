# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-08T16:36:14Z
- Catalog candidates: 258
- Selected candidates: 4
- Planned apply-allowed candidates: 4
- Live successes: 4
- Live failures: 0

## Selected Candidates

- `power.control.class1-initial-unpark-count` | Class1 Initial Unpark Count | Power
  docs: `research/records/power.control.class1-initial-unpark-count.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.control.lid-reliability-state` | Lid Reliability State | Power
  docs: `research/records/power.control.lid-reliability-state.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.control.mf-buffering-threshold` | MF Buffering Threshold | Power
  docs: `research/records/power.control.mf-buffering-threshold.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `power.control.perf-calculate-actual-utilization` | Perf Calculate Actual Utilization | Power
  docs: `research/records/power.control.perf-calculate-actual-utilization.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `power.control.class1-initial-unpark-count` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.control.lid-reliability-state` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.control.mf-buffering-threshold` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `power.control.perf-calculate-actual-utilization` | success=true | status=already-applied
  summary: The tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
