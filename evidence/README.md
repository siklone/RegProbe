# Evidence

This is the canonical evidence root.

- `files/`
  normalized repo-tracked artifacts such as Procmon exports, Ghidra markdown, imported text captures, and placeholders
- `records/`
  v3.1 per-record machine bundles such as `metadata.json`, `runtime.json`, `static.json`, `behavior.json`, `classification.json`, `timeline.json`, `verdict.md`, `full-evidence.json`, and `re-audit.json`

`research/` stays generated and human-facing. It links into this tree but does not own the canonical files.

Large raw artifacts stay out of git and must be referenced through `artifact_refs.release_url`.
