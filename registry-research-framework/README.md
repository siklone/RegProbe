# Registry Research Framework

This folder retains the historical v3.1/v3.2 machine-pipeline docs and supporting tooling for undocumented registry research.

- `pipeline/` runs the phase-based workflow and writes canonical per-record outputs under `evidence/records/`.
- `routing/` chooses the tool lane and applies Frida kernel guard.
- `tools/` contains thin wrappers for runtime, static, and behavior probes.
- `schemas/` defines the historical machine evidence formats used by the retained pipeline notes.
- `audit/` generates the retroactive re-audit queue and report.
- `config/` stores batch, routing, decision-tree defaults, and tweak-to-VM runner mappings.
- `docs/` explains the retained v3.1 rules without changing the existing human-facing research record schema.

Canonical imported artifacts live under `evidence/files/`. The published research surface stays under `research/`.

The checked-in v3.6 publishing, manifest, and metrics helpers live in the top-level `scripts/` and `Docs/research/` lanes. This folder remains because older evidence notes, audit packs, and replay scripts still reference the retained v3.1/v3.2 machinery directly.

`faz1` and `faz3` stay bootstrap-only by default. Pass `-ExecuteTools` when you want the phase wrapper to call the mapped VM runner for that tweak. `faz1` can now emit both ETW and Procmon lane manifests.

<!-- BEGIN:RESEARCH_HEALTH -->
## Research Health

| Metric | Value |
|--------|-------|
| Promoted | 244 |
| Blocked | 0 |
| Revalidation Pending | 0 |
| Gate Health | 🟡 yellow |
| Schema Complete | 100% |
| Missing Docs | 32 |
| Blocked Actionability | n/a |
| Blocked Worklist Gate | PASS |
| Blocked Worklist | `audit/blocked-worklist.md` |
<!-- END:RESEARCH_HEALTH -->

For a compact blocked-queue entrypoint, start with `audit/blocked-worklist.md`. The current active blocked worklist is intentionally empty; the 2026-05-07 closure ledger in `audit/blocked-worklist-closure-20260507.md` explains which records were rejected from promotion and what evidence would be required to reopen them. For terminal use, `winopt research list-blocked --summary` prints the checked-in lane split without dumping the whole list, and `winopt research show-blocked <candidate-id>` remains available if future blocked records are added. The operator flow is documented in `docs/blocked-worklist-operator-guide.md`.
