# Registry Research Framework

This folder holds the v3.1 machine pipeline for undocumented registry research.

- `pipeline/` runs the phase-based workflow and writes canonical per-record outputs under `evidence/records/`.
- `routing/` chooses the tool lane and applies Frida kernel guard.
- `tools/` contains thin wrappers for runtime, static, and behavior probes.
- `schemas/` defines the v3.1 machine evidence formats.
- `audit/` generates the retroactive re-audit queue and report.
- `config/` stores batch, routing, decision-tree defaults, and tweak-to-VM runner mappings.
- `docs/` explains the v3.1 rules without changing the existing human-facing research record schema.
- `schemas/normalized-registry-*.schema.json` defines the compact ETW/Procmon/imported registry event contract used by new runtime normalizers.
- `schemas/imported-candidate-backlog.schema.json` defines the canonical aggregated queue published at `research/imported-candidate-backlog.json`.
- `tools/import-external-evidence.py` converts supported external exports into an importer-specific bundle, a canonical `normalized-registry-bundle.json`, plus candidate queue, note stubs, and record seeds without touching the tweak catalog.
- `scripts/generate_imported_candidate_backlog.py` folds per-run imported candidate queues into `research/imported-candidate-backlog.json` so external leads are visible in one research queue.
- `winopt research validate-json-tweaks` emits a machine-readable invalid-definition report for JSON tweak batches without loading them into the app catalog.

Canonical imported artifacts live under `evidence/files/`. The published research surface stays under `research/`.
Published summary surfaces now carry imported backlog telemetry through `research/evidence-index.json`, `research/evidence-manifest.json`, and `research/evidence-audit.json`.

`faz1` and `faz3` stay bootstrap-only by default. Pass `-ExecuteTools` when you want the phase wrapper to call the mapped VM runner for that tweak. `faz1` can now emit both ETW and Procmon lane manifests.

Runtime lanes should now prefer `summary.json` + normalized bundle over raw ETL/PML/CSV. Raw capture files stay off-git or helper-only whenever possible.
Wrapper manifests should carry `normalized_result_ref`, `normalization_status`, `normalizer_name`, and `normalization_errors` whenever a mapped runner produces a normalized bundle.
