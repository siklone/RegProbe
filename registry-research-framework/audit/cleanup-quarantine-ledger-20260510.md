# Cleanup Quarantine Ledger

Generated: `2026-05-13T23:48:21Z`

Quarantine ledger for cleanup candidates. This ledger does not delete files.

## Deletion Policy

- No file is deleted by this generator.
- Delete only when live_reference_count is 0.
- Delete only when a replacement artifact or explicit obsolete reason is recorded.
- Manual review is required for raw ETL/PML and vm-tooling-staging bundles.

## Summary

| Metric | Value |
|---|---:|
| Total items | 89 |
| Referenced items | 89 |
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

## Candidates

| Path | Category | Live refs | Delete eligible | Action | Reason |
|---|---|---:|---:|---|---|
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.json` | `operator96-superseded-pilot` | 2 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.md` | `operator96-superseded-pilot` | 2 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.json` | `operator96-superseded-pilot` | 5 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.md` | `operator96-superseded-pilot` | 5 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `evidence/files/vm-tooling-staging/defender-cloud-demo-extracted` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/ghidra-probes` | `vm-tooling-staging-oldest-sample` | 2 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/beep_start_toggle_out.txt` | `vm-tooling-staging-oldest-sample` | 9 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/crossdevice_resume_probe.csv` | `vm-tooling-staging-oldest-sample` | 11 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-securitycenter-1-20260324-213118` | `vm-tooling-staging-oldest-sample` | 7 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-20260325-011845` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-20260325-012333` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_longpaths_probe.csv` | `vm-tooling-staging-oldest-sample` | 9 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_probe2.csv` | `vm-tooling-staging-oldest-sample` | 10 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/devmode_probe2.txt` | `vm-tooling-staging-oldest-sample` | 10 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/feedback_notifications_probe.txt` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/gamemode_admin_probe.txt` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/gamemode_admin_zero_probe.txt` | `vm-tooling-staging-oldest-sample` | 7 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/ghidra_explorer_serialize.txt` | `vm-tooling-staging-oldest-sample` | 11 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hags_toggle_out.txt` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/hideemptydrives-0-hits.csv` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hideemptydrives-1-hits.csv` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/hideemptydrives-result.txt` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/files/vm-tooling-staging/iconsonly-0-hits.csv` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `delete-after-review` | staging diagnostic bundle duplicated by canonical evidence/raw artifact |
| `evidence/raw/procmon/privacy.disable-appcompat-engine.policy/appcompat-policy-bundle-procmon.pml` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/procmon/privacy.disable-appdeviceinventory.policy/appdeviceinventory-policy-procmon.pml` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/procmon/privacy.disable-program-compatibility-assistant/disable-pca-policy-procmon.pml` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.reliability-timestamp-enabled-etw-20260424e/system.reliability-timestamp-enabled-etw-20260424e.etl` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system-io-allow-remote-dasd-etw-20260424/system-io-allow-remote-dasd-etw-20260424.etl` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423.etl` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-quoted-20260423/timer-check-flags-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl` | `large-raw-trace-sample` | 51 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.executive-uuid-sequence-number-etw-20260424e/system.executive-uuid-sequence-number-etw-20260424e.etl` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423.etl` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/global-timer-resolution-requests-etw-stackwalk-quoted-20260423/global-timer-resolution-requests-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-timercheckflags-e2e/wave4-timercheckflags-e2e.etl` | `large-raw-trace-sample` | 3 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e.etl` | `large-raw-trace-sample` | 13 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power.control.ttm-enabled-etw-20260424e/power.control.ttm-enabled-etw-20260424e.etl` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-request-override-callstack-20260423/power-request-override-callstack-20260423.etl` | `large-raw-trace-sample` | 8 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/power-disable-cpu-idle-states-etw-20260424-main/power-disable-cpu-idle-states-etw-20260424-main.etl` | `large-raw-trace-sample` | 9 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/audio.show-hidden-devices-etw-20260424e/audio.show-hidden-devices-etw-20260424e.etl` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl` | `large-raw-trace-sample` | 53 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/timer-check-flags-etw-stackwalk-20260418.etl` | `large-raw-trace-sample` | 10 | `False` | `keep-pending-review` | large raw trace; keep until indexed replacement/derived parse is confirmed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.json` | `audit-archive-named-sample` | 1 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.md` | `audit-archive-named-sample` | 1 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.json` | `audit-archive-named-sample` | 18 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.md` | `audit-archive-named-sample` | 18 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.zip` | `audit-archive-named-sample` | 18 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.json` | `audit-archive-named-sample` | 1 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.md` | `audit-archive-named-sample` | 1 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json` | `audit-archive-named-sample` | 10 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.md` | `audit-archive-named-sample` | 10 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.zip` | `audit-archive-named-sample` | 10 | `False` | `keep-pending-review` | audit artifact name marks it as archived, superseded, obsolete, or stale; keep until live references and replacement surfaces are reviewed |
| `registry-research-framework/audit/kernel-power-net-new-candidates-20260328.json` | `old-dated-audit-output-sample` | 2 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/kernel-power-net-new-follow-up-20260328.json` | `old-dated-audit-output-sample` | 3 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/kernel-power-existing-static-probe-20260328.json` | `old-dated-audit-output-sample` | 10 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-boot-trace-20260328.json` | `old-dated-audit-output-sample` | 12 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-candidate-package-20260328.json` | `old-dated-audit-output-sample` | 1 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-etl-registry-review-20260328.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-candidate-package-20260328.json` | `old-dated-audit-output-sample` | 2 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json` | `old-dated-audit-output-sample` | 7 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-procmon-bootlog-20260328.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-dcomlaunch-power-trigger-20260328.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-procmon-follow-up-20260328.json` | `old-dated-audit-output-sample` | 9 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json` | `old-dated-audit-output-sample` | 10 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-etl-registry-review-20260328.json` | `old-dated-audit-output-sample` | 13 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-procmon-bootlog-20260328.json` | `old-dated-audit-output-sample` | 11 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-follow-up-package-20260328.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-reactos-hypothesis-20260328.json` | `old-dated-audit-output-sample` | 12 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/system-executive-additional-worker-threads-stress-trigger-20260328.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-write-diagnostics-20260328.json` | `old-dated-audit-output-sample` | 6 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-tooling-chain-review-20260328.json` | `old-dated-audit-output-sample` | 6 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-disable-cpu-idle-states-stepwise-orchestration-20260328.json` | `old-dated-audit-output-sample` | 2 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json` | `old-dated-audit-output-sample` | 1 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-runtime-capture-20260329.json` | `old-dated-audit-output-sample` | 1 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-stepwise-runtime-capture-20260329.json` | `old-dated-audit-output-sample` | 39 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-follow-up-20260329.json` | `old-dated-audit-output-sample` | 11 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-guestvar-follow-up-20260329.json` | `old-dated-audit-output-sample` | 8 | `False` | `keep-pending-review` | older dated audit output; keep until a current index, report, or historical replacement is confirmed |
