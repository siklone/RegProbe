# Cleanup Quarantine Ledger

Generated: `2026-05-15T04:37:11Z`

Quarantine ledger for cleanup review inventory. Only delete-candidate rows are cleanup candidates; retained rows are not deletion candidates.

## Deletion Policy

- No file is deleted by this generator.
- Delete only when live_reference_count is 0.
- Delete only when a replacement artifact or explicit obsolete reason is recorded.
- Manual review is required for raw ETL/PML and vm-tooling-staging bundles.
- Rows with cleanup_status other than delete-candidate are retained inventory, not deletion candidates.

## Summary

| Metric | Value |
|---|---:|
| Review inventory items | 89 |
| Delete candidates | 0 |
| Retained inventory items | 89 |
| Referenced items | 89 |
| Blocking referenced items | 66 |
| Audit-only referenced items | 23 |
| Delete eligible after review | 0 |
| Total sampled size bytes | 678702358 |

## Categories

| Category | Count |
|---|---:|
| `audit-archive-named-sample` | 10 |
| `large-raw-trace-sample` | 25 |
| `old-dated-audit-output-sample` | 25 |
| `operator96-superseded-pilot` | 4 |
| `vm-tooling-staging-oldest-sample` | 25 |

## Cleanup Statuses

| Status | Count | Meaning |
|---|---:|---|
| `retained-audit-trail-reference` | 23 | Not a deletion candidate yet; only audit/history references point at it. |
| `retained-live-reference` | 66 | Not a deletion candidate; real blocking references still point at it. |

## Delete Candidates

Only rows in this section are deletion candidates.

_No delete candidates in this ledger._

## Retained Inventory

Rows here were inspected by the cleanup scanner but are not deletion candidates.

| Path | Status | Category | Live refs | Blocking refs | Audit refs | Action | Reason |
|---|---|---|---:|---:|---:|---|---|
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.json` | `retained-live-reference` | `operator96-superseded-pilot` | 9 | 5 | 4 | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.md` | `retained-live-reference` | `operator96-superseded-pilot` | 9 | 5 | 4 | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.json` | `retained-live-reference` | `operator96-superseded-pilot` | 11 | 7 | 4 | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.md` | `retained-live-reference` | `operator96-superseded-pilot` | 11 | 7 | 4 | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `evidence/files/vm-tooling-staging/defender-cloud-demo-extracted` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/ghidra-probes` | `retained-live-reference` | `vm-tooling-staging-oldest-sample` | 6 | 2 | 4 | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/beep_start_toggle_out.txt` | `retained-live-reference` | `vm-tooling-staging-oldest-sample` | 5 | 1 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/crossdevice_resume_probe.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-securitycenter-1-20260324-213118` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-20260325-011845` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-20260325-012333` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_longpaths_probe.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_probe2.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_probe2.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/feedback_notifications_probe.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/gamemode_admin_probe.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/gamemode_admin_zero_probe.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/ghidra_explorer_serialize.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hags_toggle_out.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hideemptydrives-0-hits.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hideemptydrives-1-hits.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hideemptydrives-result.txt` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/iconsonly-0-hits.csv` | `retained-audit-trail-reference` | `vm-tooling-staging-oldest-sample` | 4 | 0 | 4 | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/raw/procmon/privacy.disable-appcompat-engine.policy/appcompat-policy-bundle-procmon.pml` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/procmon/privacy.disable-appdeviceinventory.policy/appdeviceinventory-policy-procmon.pml` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/procmon/privacy.disable-program-compatibility-assistant/disable-pca-policy-procmon.pml` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.reliability-timestamp-enabled-etw-20260424e/system.reliability-timestamp-enabled-etw-20260424e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system-io-allow-remote-dasd-etw-20260424/system-io-allow-remote-dasd-etw-20260424.etl` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-quoted-20260423/timer-check-flags-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 55 | 51 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.executive-uuid-sequence-number-etw-20260424e/system.executive-uuid-sequence-number-etw-20260424e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/global-timer-resolution-requests-etw-stackwalk-quoted-20260423/global-timer-resolution-requests-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-timercheckflags-e2e/wave4-timercheckflags-e2e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 7 | 3 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 17 | 13 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power.control.ttm-enabled-etw-20260424e/power.control.ttm-enabled-etw-20260424e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-request-override-callstack-20260423/power-request-override-callstack-20260423.etl` | `retained-live-reference` | `large-raw-trace-sample` | 12 | 8 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-disable-cpu-idle-states-etw-20260424-main/power-disable-cpu-idle-states-etw-20260424-main.etl` | `retained-live-reference` | `large-raw-trace-sample` | 13 | 9 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/audio.show-hidden-devices-etw-20260424e/audio.show-hidden-devices-etw-20260424e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl` | `retained-live-reference` | `large-raw-trace-sample` | 57 | 53 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/timer-check-flags-etw-stackwalk-20260418.etl` | `retained-live-reference` | `large-raw-trace-sample` | 14 | 10 | 4 | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.json` | `retained-live-reference` | `audit-archive-named-sample` | 5 | 1 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.md` | `retained-live-reference` | `audit-archive-named-sample` | 5 | 1 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.json` | `retained-live-reference` | `audit-archive-named-sample` | 22 | 18 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.md` | `retained-live-reference` | `audit-archive-named-sample` | 22 | 18 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.zip` | `retained-live-reference` | `audit-archive-named-sample` | 22 | 18 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.json` | `retained-live-reference` | `audit-archive-named-sample` | 5 | 1 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.md` | `retained-live-reference` | `audit-archive-named-sample` | 5 | 1 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json` | `retained-live-reference` | `audit-archive-named-sample` | 14 | 10 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.md` | `retained-live-reference` | `audit-archive-named-sample` | 14 | 10 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.zip` | `retained-live-reference` | `audit-archive-named-sample` | 14 | 10 | 4 | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/kernel-power-net-new-candidates-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 6 | 2 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/kernel-power-net-new-follow-up-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 7 | 3 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/kernel-power-existing-static-probe-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 14 | 10 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-boot-trace-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 16 | 12 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-candidate-package-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 5 | 1 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-etl-registry-review-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-candidate-package-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 6 | 2 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 11 | 7 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-procmon-bootlog-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-dcomlaunch-power-trigger-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-procmon-follow-up-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 13 | 9 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 14 | 10 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-etl-registry-review-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 17 | 13 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-procmon-bootlog-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 15 | 11 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-follow-up-package-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-reactos-hypothesis-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 16 | 12 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-stress-trigger-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-write-diagnostics-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 10 | 6 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-tooling-chain-review-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 10 | 6 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-stepwise-orchestration-20260328.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 6 | 2 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 5 | 1 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-runtime-capture-20260329.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 5 | 1 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-stepwise-runtime-capture-20260329.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 43 | 39 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-follow-up-20260329.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 15 | 11 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-guestvar-follow-up-20260329.json` | `retained-live-reference` | `old-dated-audit-output-sample` | 12 | 8 | 4 | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
