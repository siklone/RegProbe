# system.kernel.global-timer-resolution-requests decision gate review - 2026-04-12

## Decision

Keep `system.kernel.global-timer-resolution-requests` blocked.

The current evidence keeps this as a kernel timer research lead, but it still lacks a primary current-build Microsoft document for the exact value and no retained runtime lane has captured an exact registry read.

This is evidence-missing, not an intentional hold. The active blockers remain `no-primary-current-build-doc` and `runtime_no_read`.
