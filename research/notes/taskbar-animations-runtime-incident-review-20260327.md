# Taskbar Animations Incident Review - 2026-03-27

The older `baseline-20260327-shell-stable` snapshot produced repeated taskbar animations runtime failures that triggered the incident-review hold for this record. Those failures remain preserved in `research/vm-incidents.json` as historical evidence.

The runtime lane was rerun on `baseline-20260327-regprobe-visible-shell-stable` with:

- script: `scripts/vm/run-explorer-shell-registry-runtime-probe.ps1`
- summary: `evidence/files/vm-tooling-staging/taskbar-animations-runtime-20260327-224704/summary.json`
- status: `ok`

Observed result:

- candidate restart completed
- restore restart completed
- `explorer`, `sihost`, `ShellHost`, and `ctfmon` remained healthy
- WPR started and stopped cleanly
- no recovery step was needed
- no snapshot revert was needed

Conclusion: the earlier failures were baseline-specific. The incident-review concern for `performance.disable-taskbar-animations` is closed on the current visible-shell baseline.
