# system.kernel-dpc-watchdog-control-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-dpc-watchdog-control-cluster` blocked.

The current package still has three material gaps: live defaults conflict with the retained docs, no primary current-build documentation source exists outside the repo, and the runtime package does not capture an exact read. For watchdog-control values, those gaps are too large to promote.

This is evidence-missing, not an intentional hold. The active blockers remain `live-defaults-conflict-docs`, `no-doc-source-outside-repo`, and `runtime_no_read`.
