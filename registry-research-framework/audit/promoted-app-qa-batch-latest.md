# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T15:44:37Z
- Catalog candidates: 242
- Selected candidates: 3
- Planned apply-allowed candidates: 3
- Live successes: 3
- Live failures: 0

## Selected Candidates

- `security.disable-system-restore` | Turn Off System Restore | Security
  docs: `research/records/security.disable-system-restore.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `security.disable-windows-firewall` | Windows Defender Firewall Policy | Security
  docs: `research/records/security.disable-windows-firewall.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `security.disable-windows-update.policy` | Windows Update Policy Control | Security
  docs: `research/records/security.disable-windows-update.policy.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `security.disable-system-restore` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `security.disable-windows-firewall` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `security.disable-windows-update.policy` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
