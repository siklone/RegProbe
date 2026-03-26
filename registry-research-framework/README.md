# Registry Research Framework

This folder holds the v3.1 machine pipeline for undocumented registry research.

- `pipeline/` runs the phase-based workflow and writes canonical per-record outputs under `evidence/`.
- `routing/` chooses the tool lane and applies Frida kernel guard.
- `tools/` contains thin wrappers for runtime, static, and behavior probes.
- `schemas/` defines the v3.1 machine evidence formats.
- `audit/` generates the retroactive re-audit queue and report.
- `config/` stores batch, routing, and decision-tree defaults.
- `docs/` explains the v3.1 rules without changing the existing human-facing research record schema.

The published research surface stays under `research/`.
