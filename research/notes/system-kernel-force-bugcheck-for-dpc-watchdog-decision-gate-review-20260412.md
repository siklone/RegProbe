# system.kernel.force-bugcheck-for-dpc-watchdog decision gate review - 2026-04-12

## Decision

Keep `system.kernel.force-bugcheck-for-dpc-watchdog` blocked.

This value is too safety-sensitive to promote without a stronger package. The current blockers are specific: no primary current-build documentation source outside the repo, non-default semantics remain unproven, and no exact runtime read has been captured.

This is evidence-missing, not an intentional hold. A future lane needs either authoritative semantics or a controlled runtime proof before this can move.
