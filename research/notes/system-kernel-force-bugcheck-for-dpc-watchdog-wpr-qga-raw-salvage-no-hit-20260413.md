# system.kernel.force-bugcheck-for-dpc-watchdog WPR/QGA raw collector no-hit - 2026-04-13

## Result

The targeted QGA-launched WPR boot-registry lane for `ForceBugcheckForDpcWatchdog` observed a real reboot and kept the guest stable, but the wrapper did not receive the requested target ETL/CSV/normalized bundle. The guest still retained the raw WPR System Collector ETL, so the run was salvageable as negative evidence rather than a pure transport failure.

- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\ForceBugcheckForDpcWatchdog`
- Run id: `force-bugcheck-dpc-watchdog-wpr-qga-20260413a`
- Transport: `qga`
- Wrapper outcome: `status = error`, `error_kind = normalized-bundle-missing`
- Reboot observed: `true`
- Guest health: `stable`
- Raw WPR System Collector ETL size: `28,311,552` bytes
- Raw tracerpt CSV size: `87,468,462` bytes
- Exact value-name hits: `0`

## Interpretation

This does not close `runtime_no_read`. The normal WPR boot-registry output path failed because `wpr -stopboot` reported that no trace profiles were running and did not save the requested target ETL.

It still improves the evidence story. The raw System Collector ETL left by WPR converted successfully through `tracerpt`, and exact filtering found no `ForceBugcheckForDpcWatchdog` rows in the resulting CSV. That means this run is not just "QGA failed"; it is a raw-collector no-hit on a real rebooted VM session.

The next useful step is a narrower debugger-assisted caller trace or a fixed WPR boot profile that reliably saves the requested target ETL while preserving the same exact value-name filter.

## Artifact Set

- [force-bugcheck-dpc-watchdog-wpr-qga-20260413a-summary-arm.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/force-bugcheck-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413/force-bugcheck-dpc-watchdog-wpr-qga-20260413a-summary-arm.json)
- [force-bugcheck-dpc-watchdog-wpr-qga-20260413a-summary.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/force-bugcheck-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413/force-bugcheck-dpc-watchdog-wpr-qga-20260413a-summary.json)
- [force-bugcheck-dpc-watchdog-wpr-qga-20260413a-stage.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/force-bugcheck-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413/force-bugcheck-dpc-watchdog-wpr-qga-20260413a-stage.json)
- [force-bugcheck-dpc-watchdog-wpr-qga-20260413a.raw-system-tracerpt.txt](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/force-bugcheck-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413/force-bugcheck-dpc-watchdog-wpr-qga-20260413a.raw-system-tracerpt.txt)
- [force-bugcheck-dpc-watchdog-wpr-qga-20260413a.raw-system-summary.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/force-bugcheck-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413/force-bugcheck-dpc-watchdog-wpr-qga-20260413a.raw-system-summary.json)
- [system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/system-kernel-force-bugcheck-for-dpc-watchdog-wpr-qga-raw-salvage-no-hit-20260413.json)
