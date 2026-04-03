# Source Enrichment Wave

This directory is the canonical output surface for the source-enrichment lane.

## What gets written

- `outputs/source-enrichment-<stamp>/master-enrichment.json`
- `outputs/source-enrichment-<stamp>/master-enrichment.md`
- `outputs/source-enrichment-<stamp>/source-index.json`
- `outputs/source-enrichment-<stamp>/priority-queue.json`
- `outputs/source-enrichment-<stamp>/per-key/*.json`
- `outputs/source-enrichment-<stamp>/per-source/*.json`

The wrapper also writes two repo-facing summaries:

- `registry-research-framework/audit/source-enrichment-<stamp>.json`
- `research/notes/source-enrichment-<stamp>.md`

## Scoring

Each candidate is scored from configured source weights and local hit counts.

- ReactOS, WRK: source-code heavyweights
- System Informer, ADMX, WDK headers, Geoff cache: supporting sources
- Sandboxie, Wine: optional secondary context

The queue is intentionally conservative:

- runtime-suitable candidates are grouped for follow-up
- WinDbg-suitable candidates are grouped separately
- everything else falls into hold

## Usage

Run the wrapper from the repo root or the checked-out workspace:

```powershell
.\registry-research-framework\tools\run-source-enrichment-scan.ps1
```

If source caches are missing, the run still emits a valid v1 bundle and marks the missing sources honestly.
