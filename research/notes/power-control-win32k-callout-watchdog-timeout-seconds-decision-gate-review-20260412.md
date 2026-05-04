# power.control.win32k-callout-watchdog-timeout-seconds decision gate review - 2026-04-12

## Decision

Keep `power.control.win32k-callout-watchdog-timeout-seconds` as an intentional hold.

The current package is still a watchdog research lead, but the bounded S1 chase is now repeating the same narrow outcome set: two dedicated Procmon runs both reached guest execution before falling over at export, the fallback S1 registry ETW lane still produced no exact target hit, and the value still lacks both a primary current-build documentation source and a proven non-default override model.

The hold is explicit: wait for a stronger boot/init pivot or a more reliable trace transport before re-opening active chase. The active blockers are now the no-pivot intentional hold, the bounded S1 ETW no-hit result, the non-repo documentation gap, and the unresolved override semantics.
