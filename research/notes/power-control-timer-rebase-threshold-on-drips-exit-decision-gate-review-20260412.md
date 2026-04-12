# power.control.timer-rebase-threshold-on-drips-exit decision gate review - 2026-04-12

## Decision

Keep `power.control.timer-rebase-threshold-on-drips-exit` blocked as an intentional environment hold.

The record has baseline existence, repo-doc evidence, current-build string corroboration, Ghidra artifacts, shell-safe Procmon context, a DRIPS capability gate, and a KVM reboot observation proving the value persists at `60`.

The blocker is specific: the current VM baselines do not support S0 Low Power Idle / Modern Standby, so they cannot exercise a real DRIPS-exit trigger. This is not a generic documentation review and not a dead-flag result.
