# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-07T17:12:35Z
- Catalog candidates: 242
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `visibility.disable-spotlight-settings` | Windows Spotlight on Settings | Visibility
  docs: `research/records/visibility.disable-spotlight-settings.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.disable-spotlight-third-party` | Third-Party Suggestions in Windows Spotlight | Visibility
  docs: `research/records/visibility.disable-spotlight-third-party.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.disable-spotlight-welcome` | Windows Spotlight Welcome Experience | Visibility
  docs: `research/records/visibility.disable-spotlight-welcome.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.disable-wcn-wizards` | Windows Connect Now Wizards | Visibility
  docs: `research/records/visibility.disable-wcn-wizards.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.disable-widgets` | Widgets Feature | Visibility
  docs: `research/records/visibility.disable-widgets.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `visibility.disable-spotlight-settings` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `visibility.disable-spotlight-third-party` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `visibility.disable-spotlight-welcome` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `visibility.disable-wcn-wizards` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
- `visibility.disable-widgets` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
  card contract: ok | claim_boundary=true | lanes=docs, runtime, source, rollback
