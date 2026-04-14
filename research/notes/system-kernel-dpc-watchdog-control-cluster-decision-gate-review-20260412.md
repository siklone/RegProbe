# system.kernel-dpc-watchdog-control-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-dpc-watchdog-control-cluster` blocked.

The current package still has three material gaps: live zero-state readings conflict with the retained repo defaults, no primary current-build documentation source exists outside the repo, and the last structural explanation above `KeUpdateDpcWatchdogConfiguration` is still unresolved. What remains is no longer a generic runtime-read story; it is the persisted seeding caller or exact inner query arm that would reconcile the all-zero live state with the retained `Session Manager\\Kernel` rows.

This stays active blocked work, not an intentional hold, because the family still has a concrete current-build writer/query path and a plausible debugger/decompiler pivot for the last missing explanation. The active blockers are now the live-state conflict, the missing non-repo documentation source, and the unresolved persisted seeding caller / exact query arm.

As of 2026-04-14, a dedicated guest-side WPR filter wrapper also exists for the control cluster, but the historical `kernel-timing-wpr-boot-registry-20260412.manual.csv` bundle was no longer retained on the working guest. The next runtime-trace step is therefore explicit: rerun or recover the boot-registry source bundle before expecting an exact-value filter result.
