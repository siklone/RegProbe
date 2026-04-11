# Runtime-Trace Runner Readiness Audit

Date: 2026-04-08

## Outcome

- Runtime-trace records in current audit: `2`
- Records with mapped runtime runner: `2`
- All runtime-trace records mapped: `True`

## Records

- `power.control.allow-audio-to-enable-execution-required-power-requests` -> mapped=`True` script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-audio-to-enable-execution-required-power-requests']`
- `power.control.allow-system-required-power-requests` -> mapped=`True` script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-system-required-power-requests']`

## Interpretation

- The remaining runtime-trace queue is now operationally wired, not just theoretically open.
- The execution-required pair no longer depends on the broad mega-trigger pilot as its only runtime surface; both tweaks now have a dedicated mapped narrow path-aware ETW lane.
- Any remaining gap for these records is now live guest execution or evidence capture, not missing repo-native runner plumbing.
