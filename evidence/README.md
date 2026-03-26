# Evidence

This folder is the v3.1 machine evidence store.

Each audited record gets its own folder:

- `metadata.json`
- `runtime.json`
- `static.json`
- `behavior.json`
- `classification.json`
- `timeline.json`
- `verdict.md`
- `full-evidence.json`
- `re-audit.json` when the record comes from the retroactive audit queue

Large raw artifacts stay out of git and must be referenced through `artifact_refs.release_url`.
