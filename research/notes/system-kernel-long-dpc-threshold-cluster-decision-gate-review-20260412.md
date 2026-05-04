# system.kernel-long-dpc-threshold-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-long-dpc-threshold-cluster` as an intentional hold.

The current package still matters, but the chase is now looping through the same bounded outcomes: retained WPR no-hit evidence, two dedicated timer/DPC stress replays that both died at `Procmon SaveAs`, and no primary current-build documentation source outside the repo for either threshold value.

The hold is explicit: wait for a stronger binary/debugger pivot or a reliable non-Procmon trace path before re-opening active chase. Promotion still requires a decisive exact read or another current-build lane that captures the threshold values without the export failure.
