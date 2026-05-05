# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T21:40:21Z
- Catalog candidates: 242
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `explorer.taskbar-alignment-left` | Taskbar Alignment | Explorer
  docs: `research/records/explorer.taskbar-alignment-left.review.json`
  rollback: default=false | previous=true
- `explorer.disable-taskbar-chat` | Taskbar Chat Icon | Explorer
  docs: `research/records/explorer.disable-taskbar-chat.json`
  rollback: default=true | previous=true
- `performance.disable-menu-show-delay` | Remove Menu Show Delay | Performance
  docs: `research/records/performance.disable-menu-show-delay.review.json`
  rollback: default=false | previous=true
- `performance.disable-taskbar-animations` | Taskbar Animations | Performance
  docs: `research/records/performance.disable-taskbar-animations.review.json`
  rollback: default=true | previous=true
- `notifications.disable-lock-screen` | Lock Screen Toast Notifications | Notifications
  docs: `research/records/notifications.disable-lock-screen.json`
  rollback: default=true | previous=true

## Live Results

- `explorer.taskbar-alignment-left` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `explorer.disable-taskbar-chat` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `performance.disable-menu-show-delay` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `performance.disable-taskbar-animations` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `notifications.disable-lock-screen` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
