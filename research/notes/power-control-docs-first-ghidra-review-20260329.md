# Power Control Docs-First Ghidra Review

Date: 2026-03-29
Target binary: `C:\Windows\System32\ntoskrnl.exe`
Probe: `power-control-docs-first-ntoskrnl`

## Outcome

- Shared current-build Ghidra batch completed successfully for all 7 docs-first power-control values.
- Exact string references were preserved for all 7 patterns.
- Two values (`LidReliabilityState`) resolved into naturally identified functions.
- The remaining values still relied on forced-boundary fallback blocks, but the artifacts are reviewable and repo-tracked.
- This is strong static corroboration for candidate packaging, not a direct runtime-read proof.

## Artifacts

- `evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/ghidra-matches.md`
- `evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/evidence.json`
- `evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/ghidra-run.log`
