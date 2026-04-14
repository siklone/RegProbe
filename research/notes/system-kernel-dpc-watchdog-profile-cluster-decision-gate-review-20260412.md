# system.kernel-dpc-watchdog-profile-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-dpc-watchdog-profile-cluster` as an intentional hold.

The WPR no-hit filter and reboot observation narrowed the lane, but the current-build story is now mixed rather than merely incomplete: one live profile field matches the retained repo-doc default while the other live fields stay at zero, and the exact query arm or init path that explains that split is still unresolved.

The hold is explicit: wait for a stronger current-build query-arm/decompiler pivot or authoritative documentation before re-opening active chase. Promotion still requires either a decisive exact read or a cleaner initialization path that explains why the mixed live state diverges from the retained defaults.
