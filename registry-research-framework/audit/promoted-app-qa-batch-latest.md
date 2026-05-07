# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T17:56:48Z
- Catalog candidates: 242
- Selected candidates: 4
- Planned apply-allowed candidates: 4
- Live successes: 4
- Live failures: 0

## Selected Candidates

- `security.powershell-unrestricted` | Windows PowerShell Script Execution Policy | Security
  docs: `research/records/security.powershell-unrestricted.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `security.threat-file-hash-logging` | Microsoft Defender Threat File Hash Logging | Security
  docs: `research/records/security.threat-file-hash-logging.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `security.trusted-path-credential-prompting` | Trusted Path for Credential Entry | Security
  docs: `research/records/security.trusted-path-credential-prompting.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `security.uac-never-notify` | User Account Control Prompting Profile | Security
  docs: `research/records/security.uac-never-notify.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `security.powershell-unrestricted` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `security.threat-file-hash-logging` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `security.trusted-path-credential-prompting` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `security.uac-never-notify` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
