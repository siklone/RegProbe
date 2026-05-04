# system.kernel.force-bugcheck-for-dpc-watchdog decision gate review - 2026-04-12

## Decision

Keep `system.kernel.force-bugcheck-for-dpc-watchdog` as an intentional hold.

This value is too safety-sensitive to keep chasing through bounded runtime retries without a stronger pivot. The checked-in build still gives us the repo-doc default, clean baseline behavior, a live KD read of `KiForceBugcheckForDpcWatchdog = 0`, and a checked-in-build INIT descriptor binding from the registry value to the same kernel global, but the retained runtime lanes still stop at the same gap: no exact checked-in-build registry read, no primary Microsoft documentation source for the internal contract, and no proven non-default bugcheck semantics.

The hold is explicit: wait for a stronger debugger-assisted caller trace or authoritative documentation before re-opening active chase. The active blockers are now the safety-sensitive intentional hold, the non-repo documentation gap, the unresolved non-default semantics, and the repeated WPR boot no-hit result.
