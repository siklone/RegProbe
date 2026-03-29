# Power Control Docs-First Value-Exists Static Triage

Date: 2026-03-29

Source queue: `docs-first-new-candidate-power-control-value-exists` from `kernel-power-96-phase0-follow-up-20260329.json`.

## Outcome

- Total candidates: `7`
- Exact docs hits: `7/7`
- Exact string hits: `7/7`
- Primary binary for all hits: `C:\Windows\System32\ntoskrnl.exe`
- Recommended next lane: `candidate-package`

## Candidates

- `power.control.class1-initial-unpark-count` -> docs `Docs/power/power.md:104`, `Docs/tweaks/tweak-provenance-overrides.json:539`, `Docs/tweaks/tweak-provenance-overrides.json:541`, `Docs/tweaks/tweak-provenance.json:4352`, `Docs/tweaks/tweak-provenance.json:4360`, `Docs/tweaks/tweak-provenance.md:124`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.hibernate-enabled` -> docs `Docs/power/power.md:154`, `Docs/power/power.md:367`, `Docs/power/power.md:378`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.hibernate-enabled-default` -> docs `Docs/power/power.md:154`, `Docs/power/power.md:367`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.lid-reliability-state` -> docs `Docs/power/power.md:170`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.mf-buffering-threshold` -> docs `Docs/power/power.md:173`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.perf-calculate-actual-utilization` -> docs `Docs/power/power.md:181`, `Docs/tweaks/tweak-provenance-overrides.json:633`, `Docs/tweaks/tweak-provenance.json:4879`, string hit in `C:\Windows\System32\ntoskrnl.exe`
- `power.control.timer-rebase-threshold-on-drips-exit` -> docs `Docs/power/power.md:206`, string hit in `C:\Windows\System32\ntoskrnl.exe`

## Notes

- This batch is stronger than the first net-new executive/session-manager-power follow-up because none of these seven values depend on adjacent-context interpretation.
- The current repo already contains exact `Docs/power/power.md` references for all seven values, so the next useful work is candidate packaging rather than another existence pass.
