# Cleanup Quarantine Ledger

Generated: `2026-05-12T01:41:09Z`

Quarantine ledger for cleanup candidates. This ledger does not delete files.

## Deletion Policy

- No file is deleted by this generator.
- Delete only when live_reference_count is 0.
- Delete only when a replacement artifact or explicit obsolete reason is recorded.
- Manual review is required for raw ETL/PML and vm-tooling-staging bundles.

## Summary

| Metric | Value |
|---|---:|
| Total items | 55 |
| Referenced items | 52 |
| Delete eligible after review | 1 |
| Total sampled size bytes | 678633457 |

## Categories

| Category | Count |
|---|---:|
| `large-raw-trace-sample` | 25 |
| `operator96-superseded-pilot` | 5 |
| `vm-tooling-staging-oldest-sample` | 25 |

## Candidates

| Path | Category | Live refs | Delete eligible | Action | Reason |
|---|---|---:|---:|---|---|
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.json` | `operator96-superseded-pilot` | 2 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.md` | `operator96-superseded-pilot` | 2 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.json` | `operator96-superseded-pilot` | 5 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.md` | `operator96-superseded-pilot` | 5 | `False` | `keep-referenced` | pilot artifact superseded by full operator96 baseline, but referenced as safety example |
| `registry-research-framework/audit/operator-regadd-value-missing-bench-pilot-20260509.json` | `operator96-superseded-pilot` | 0 | `True` | `delete-after-review` | early operator value pilot superseded by full 179/179 matrix and enriched matrix |
| `evidence/files/vm-tooling-staging/defender-cloud-demo-extracted` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/ghidra-probes` | `vm-tooling-staging-oldest-sample` | 2 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/beep_start_toggle_out.txt` | `vm-tooling-staging-oldest-sample` | 7 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/crossdevice_resume_probe.csv` | `vm-tooling-staging-oldest-sample` | 11 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-baseline-1-20260324-214343` | `vm-tooling-staging-oldest-sample` | 0 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-reporting-1-20260324-213700` | `vm-tooling-staging-oldest-sample` | 0 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-securitycenter-1-20260324-213118` | `vm-tooling-staging-oldest-sample` | 7 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-20260325-011845` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-20260325-012333` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/devmode_longpaths_probe.csv` | `vm-tooling-staging-oldest-sample` | 9 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/devmode_probe2.csv` | `vm-tooling-staging-oldest-sample` | 10 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/devmode_probe2.txt` | `vm-tooling-staging-oldest-sample` | 10 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/feedback_notifications_probe.txt` | `vm-tooling-staging-oldest-sample` | 6 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/gamemode_admin_probe.txt` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/gamemode_admin_zero_probe.txt` | `vm-tooling-staging-oldest-sample` | 7 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/ghidra_explorer_serialize.txt` | `vm-tooling-staging-oldest-sample` | 11 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/hags_toggle_out.txt` | `vm-tooling-staging-oldest-sample` | 8 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/hideemptydrives-0-hits.csv` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
| `evidence/files/vm-tooling-staging/hideemptydrives-1-hits.csv` | `vm-tooling-staging-oldest-sample` | 1 | `False` | `keep-pending-review` | staging diagnostic bundle; verify no record/evidence-index dependency before deletion |
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
