# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T21:55:03Z
- Catalog candidates: 242
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `privacy.disable-app-launch-tracking` | App Launch Tracking | Privacy
  docs: `research/records/privacy.disable-app-launch-tracking.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `privacy.disable-search-history` | Search History Storage and Display | Privacy
  docs: `research/records/privacy.disable-search-history.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.force-classic-control-panel` | Control Panel Default View | Visibility
  docs: `research/records/visibility.force-classic-control-panel.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `visibility.restore-classic-context-menu` | Classic Context Menu on Windows 11 | Visibility
  docs: `research/records/visibility.restore-classic-context-menu.review.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback
- `system.disable-app-archiving` | Automatic App Archiving | System
  docs: `research/records/system.disable-app-archiving.json`
  rollback: default=true | previous=true
  card snapshot: claim_boundary=true | fields=TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes | lanes=docs, runtime, source, rollback

## Live Results

- `privacy.disable-app-launch-tracking` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `privacy.disable-search-history` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `visibility.force-classic-control-panel` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `visibility.restore-classic-context-menu` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `system.disable-app-archiving` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
