# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T18:07:07Z
- Catalog candidates: 242
- Selected candidates: 4
- Planned apply-allowed candidates: 4
- Live successes: 4
- Live failures: 0

## Selected Candidates

- `network.smb-require-dialect-3_1_1` | Require SMB Dialect 3.1.1 | Network
  docs: `research/records/network.smb-require-dialect-3_1_1.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `network.smb-require-signing-client` | SMB Client Signing Requirement | Network
  docs: `research/records/network.smb-require-signing-client.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `network.smb-require-signing-server` | SMB Server Signing Requirement | Network
  docs: `research/records/network.smb-require-signing-server.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `network.smb-set-cipher-suite-order` | SMB Cipher Suite Order | Network
  docs: `research/records/network.smb-set-cipher-suite-order.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `network.smb-require-dialect-3_1_1` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `network.smb-require-signing-client` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `network.smb-require-signing-server` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `network.smb-set-cipher-suite-order` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
