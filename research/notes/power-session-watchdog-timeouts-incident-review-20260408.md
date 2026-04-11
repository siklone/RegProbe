# Session Watchdog Timeouts Incident Review - 2026-04-08

The older `baseline-20260327-shell-stable` and early `Win25H2Clean` watchdog runtime attempts produced repeated failures that triggered the incident-review hold for this record. Those failures remain preserved in `research/vm-incidents.json` as historical evidence.

The watchdog family was later rerun on the current visible-shell baseline with successful retained artifacts:

- boot trace summary: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/summary.json`
- Procmon boot-log summary: `evidence/files/vm-tooling-staging/watchdog-procmon-bootlog-20260328-131306/summary.json`
- post-boot trigger summary: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/summary.json`

Observed result:

- the host-driven boot trace completed with status `ok`
- the Procmon boot-log lane completed with status `procmon-bootlog-captured`
- the shell-safe post-boot trigger completed with status `no-hits`
- these retained reruns preserved real ETL, PML, and CSV artifacts instead of collapsing into the older guest-control failures

Conclusion: the older incidents are now reviewed as historical baseline or guest-control failures rather than as evidence that the watchdog lane is unstable. The incident-review hold for `power.session-watchdog-timeouts` is closed. The remaining blocker is narrower: the current VMware baseline is still S1-only and still does not produce the decisive exact live-read bundle for the watchdog timeout pair.
