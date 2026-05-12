# Operator96 Low-Noise Rerun Aggregate

- Generated UTC: `2026-05-12T18:24:58Z`
- Status: `ok`
- Source campaigns: `3`
- Plan entries: `30`
- Results: `30`
- Non-ok: `0`
- Hard smoke all: `True`
- Noisy results: `2`

## Counts

- Verdicts: `{'cpu_gain': 1, 'harmful': 22, 'low_confidence': 5, 'noisy': 2}`
- Host noise: `{'noisy': 2, 'ok': 28}`
- Confidence: `{'low': 29, 'medium': 1}`

## Source Campaigns

| Campaign | Status | Plan | Results | Non-ok |
|---|---|---:|---:|---:|
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-02.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-03.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-04.json` | `ok` | 10 | 10 | 0 |

## Noisy Results

| Experiment | Value | Verdict | Host noise | Artifact |
|---|---|---|---|---|
| `operator96-020-forceidlegraceperiod-0` | `ForceIdleGracePeriod` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `ForceIdleGracePeriod` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-020-forceidlegraceperiod-1.json` |
