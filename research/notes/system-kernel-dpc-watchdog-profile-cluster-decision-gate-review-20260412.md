# system.kernel-dpc-watchdog-profile-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-dpc-watchdog-profile-cluster` blocked.

The WPR no-hit filter and reboot observation narrowed the lane, but the record still has live-default conflicts, no primary current-build documentation source outside the repo, no exact runtime read, and unresolved conditional initialization semantics.

This is evidence-missing, not an intentional hold. Promotion requires either a decisive exact read or a cleaner current-build initialization path that explains when the profile values are consumed.
