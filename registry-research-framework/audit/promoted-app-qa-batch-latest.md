# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T20:30:28Z
- Catalog candidates: 242
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `cleanup.disable-reserved-storage` | Disable Reserved Storage | Cleanup
  docs: `research/records/cleanup.disable-reserved-storage.review.json`
  rollback: default=true | previous=true
- `notifications.disable-feedback-frequency` | Windows Feedback Request Frequency | Notifications
  docs: `research/records/notifications.disable-feedback-frequency.review.json`
  rollback: default=true | previous=true
- `performance.disable-animations` | Disable Window Animations | Performance
  docs: `research/records/performance.disable-animations.review.json`
  rollback: default=false | previous=true
- `peripheral.autoplay-take-no-action` | AutoPlay Event Default Action | Peripheral
  docs: `research/records/peripheral.autoplay-take-no-action.review.json`
  rollback: default=true | previous=true
- `system.aero-shake` | Aero Shake Window Minimizing Gesture | System
  docs: `research/records/system.aero-shake.json`
  rollback: default=true | previous=true

## Live Results

- `cleanup.disable-reserved-storage` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `notifications.disable-feedback-frequency` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `performance.disable-animations` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `peripheral.autoplay-take-no-action` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `system.aero-shake` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
