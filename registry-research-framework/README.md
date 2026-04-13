# Registry Research Framework

This folder holds the v3.1 machine pipeline for undocumented registry research.

- `pipeline/` runs the phase-based workflow and writes canonical per-record outputs under `evidence/records/`.
- `routing/` chooses the tool lane and applies Frida kernel guard.
- `tools/` contains thin wrappers for runtime, static, and behavior probes.
- `schemas/` defines the v3.1 machine evidence formats.
- `audit/` generates the retroactive re-audit queue and report.
- `config/` stores batch, routing, decision-tree defaults, and tweak-to-VM runner mappings.
- `docs/` explains the v3.1 rules without changing the existing human-facing research record schema.

Canonical imported artifacts live under `evidence/files/`. The published research surface stays under `research/`.

`faz1` and `faz3` stay bootstrap-only by default. Pass `-ExecuteTools` when you want the phase wrapper to call the mapped VM runner for that tweak. `faz1` can now emit both ETW and Procmon lane manifests.

<!-- BEGIN:RESEARCH_HEALTH -->
## Research Health

| Metric | Value |
|--------|-------|
| Promoted | 250 |
| Blocked | 18 |
| Revalidation Pending | 0 |
| Gate Health | 🟢 green |
| Schema Complete | 100% |
| Missing Docs | 0 |
| Blocked Actionability | 13 active, 5 hold |
| Blocked Worklist | `audit/blocked-worklist.md` |
<!-- END:RESEARCH_HEALTH -->

If you want the blocked queue in a hurry, start with `audit/blocked-worklist.md`. For terminal use, `winopt research list-blocked --worklist --top 5` shows the highest-priority active items, and `winopt research list-blocked --summary` prints the current lane split without dumping the whole list.
