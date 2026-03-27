# Re-audit Report

- Generated: 2026-03-27T11:08:33.6662483+03:00
- Total queued: 7

## Priority summary

| Priority | Count |
| --- | --- |
| 1 | 1 |
| 2 | 4 |
| 3+ | 2 |

## Queue

| Tweak | Class | Official | Lane | Layer | Priority | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| power.disable-cpu-idle-states | B | True | official-policy | kernel | 1 | current_blocker |
| explorer.enable-explorer-compact-mode | A | False | runtime | user-mode | 2 | non_official_v31_reaudit; etw_not_recorded; dead_flag_checks_incomplete |
| performance.disable-taskbar-animations | A | False | runtime | user-mode | 2 | non_official_v31_reaudit; etw_not_recorded; dead_flag_checks_incomplete |
| system.disable-jpeg-reduction | A | False | runtime | user-mode | 2 | non_official_v31_reaudit; etw_not_recorded; dead_flag_checks_incomplete |
| system.disable-startup-delay | A | False | system | user-mode | 2 | non_official_v31_reaudit; etw_not_recorded; dead_flag_checks_incomplete |
| audio.show-disconnected-devices | A | False | runtime | user-mode | 3 | non_official_v31_reaudit |
| audio.show-hidden-devices | A | False | runtime | user-mode | 3 | non_official_v31_reaudit |
