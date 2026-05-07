# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T15:32:30Z
- Catalog candidates: 242
- Selected candidates: 1
- Planned apply-allowed candidates: 1
- Live successes: 1
- Live failures: 0

## Selected Candidates

- `power.disable-network-power-saving.policy` | Network Power and Multimedia Responsiveness | Power
  docs: `research/records/power.disable-network-power-saving.policy.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `power.disable-network-power-saving.policy` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
