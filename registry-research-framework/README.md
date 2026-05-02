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

Current v3.6 publishing, manifest, and metrics helpers live in the top-level `scripts/` and `Docs/research/` lanes. This folder remains because older evidence notes, audit packs, and replay scripts still reference the retained v3.1/v3.2 machinery directly.

`faz1` and `faz3` stay bootstrap-only by default. Pass `-ExecuteTools` when you want the phase wrapper to call the mapped VM runner for that tweak. `faz1` can now emit both ETW and Procmon lane manifests.

<!-- BEGIN:RESEARCH_HEALTH -->
## Research Health

| Metric | Value |
|--------|-------|
| Promoted | 226 |
| Blocked | 18 |
| Revalidation Pending | 24 |
| Gate Health | 🟢 green |
| Schema Complete | 100% |
| Missing Docs | 0 |
| Blocked Actionability | 18 hold |
| Blocked Worklist Gate | PASS |
| Blocked Worklist | `audit/blocked-worklist.md` |
<!-- END:RESEARCH_HEALTH -->

If you want the blocked queue in a hurry, start with `audit/blocked-worklist.md`. For terminal use, `winopt research list-blocked --worklist --actionability active --top 5` shows the highest-priority active items, `winopt research list-blocked --worklist --actionability hold` shows intentional holds, `winopt research show-blocked <candidate-id>` opens one blocked candidate in detail, and `winopt research list-blocked --summary` prints the current lane split without dumping the whole list. The operator flow is documented in `docs/blocked-worklist-operator-guide.md`.
