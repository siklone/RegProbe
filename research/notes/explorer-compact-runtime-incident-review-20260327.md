# Explorer Compact Mode Incident Review - 2026-03-27

The older `baseline-20260327-shell-stable` snapshot produced several explorer compact runtime failures that triggered the incident-review hold for this record. Those failures are preserved in `research/vm-incidents.json` as historical evidence.

The runtime lane was rerun on `baseline-20260327-regprobe-visible-shell-stable` with:

- script: `scripts/vm/run-explorer-compact-mode-runtime-probe.ps1`
- summary: `evidence/files/vm-tooling-staging/explorer-compact-runtime-20260327-223536/summary.json`
- status: `ok`

Observed result:

- candidate restart completed
- restore restart completed
- `explorer`, `sihost`, `ShellHost`, and `ctfmon` remained healthy
- WPR started and stopped cleanly
- no recovery step was needed
- no snapshot revert was needed

Conclusion: the earlier failures were baseline-specific. The incident-review concern for `explorer.enable-explorer-compact-mode` is closed on the current visible-shell baseline.
