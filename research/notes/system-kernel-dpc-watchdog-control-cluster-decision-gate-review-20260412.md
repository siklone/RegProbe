# system.kernel-dpc-watchdog-control-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-dpc-watchdog-control-cluster` as an intentional hold.

The current package still has the same structural contradictions, but the runtime lane has now converged. A fresh QGA-launched WPR boot-registry replay on 2026-04-14 produced a retained `1.76 GB` ETL and a retained `5.23 GB` CSV for the exact current-build boot lane, and a dedicated guest-side exact-value filter still returned zero hits for `DPCTimeout`, `DpcSoftTimeout`, and `DpcCumulativeSoftTimeout`.

That changes the status of the record. This is no longer blocked because setup is incomplete or because the old source bundle disappeared. It is now repeating bounded no-hit runtime outcomes while the live zero-state conflict, the missing primary Microsoft documentation, and the unresolved persisted seeding caller / exact inner query arm all remain.

The hold is explicit: wait for a stronger current-build query-arm/decompiler pivot or authoritative documentation before re-opening active chase. Promotion still requires either a decisive exact read or a named boot/init path that explains why the live zero state diverges from the retained repo defaults.
