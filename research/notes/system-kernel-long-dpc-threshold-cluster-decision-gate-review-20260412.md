# system.kernel-long-dpc-threshold-cluster decision gate review - 2026-04-12

## Decision

Keep `system.kernel-long-dpc-threshold-cluster` blocked.

The current runtime lane did not produce a usable exact read, and the dedicated timer/DPC stress lane hit a Procmon SaveAs timeout. The record also still lacks a primary current-build documentation source outside the repo.

This is evidence-missing, not an intentional hold. Promotion requires a successful trace export or another runtime lane that captures the threshold values decisively.
