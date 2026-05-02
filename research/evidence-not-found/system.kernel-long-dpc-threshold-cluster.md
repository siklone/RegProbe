# system.kernel-long-dpc-threshold-cluster

- Class: `B`
- Record status: `draft`
- Tested build: `26200.8246`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26200.8246: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `procmon, ghidra, wpr, reboot`

## Why it stays negative

Cross-layer evidence is strong, but an explicit policy or supportability gate still blocks promotion.

## Attached references

- `repo-doc` Repo system docs assign LongDpcQueueThreshold = 3 and LongDpcRuntimeThreshold = 100 -> Docs/system/system.md and research/notes/kernel-power-96-key-routing-20260327.md
- `registry-observation` Observed baseline existence for LongDpcQueueThreshold and LongDpcRuntimeThreshold -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `inference` Current-build string hits for LongDpcQueueThreshold and LongDpcRuntimeThreshold -> evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json
- `vm-test` Lightweight runtime batch kept both LongDpc thresholds in residual negative-result hold -> research/notes/session-manager-kernel-batch-lightweight-runtime-20260331.md and evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json
- `analysis-output` Source-enrichment keeps both LongDpc thresholds as docs-first new candidates -> registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-queue-threshold.json and registry-research-framework/enrichment/outputs/source-enrichment-20260403-044821/per-key/system.kernel.long-dpc-runtime-threshold.json
- `repo-code` Repo guest tooling now exposes a dedicated timer-dpc-stress harness -> scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1, scripts/vm/guest-tools/run-registry-policy-probe.ps1, and scripts/source_enrichment_scan.py
