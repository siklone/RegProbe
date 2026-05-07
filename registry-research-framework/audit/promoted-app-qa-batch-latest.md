# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T19:38:13Z
- Catalog candidates: 243
- Selected candidates: 1
- Planned apply-allowed candidates: 1
- Live successes: 1
- Live failures: 0

## Selected Candidates

- `peripheral.audio-disable-ducking` | Disable Audio Ducking | Peripheral
  docs: `research/records/peripheral.audio-disable-ducking.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `peripheral.audio-disable-ducking` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
