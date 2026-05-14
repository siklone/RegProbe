# Cleanup Retained Inventory Plan

Generated: `2026-05-14T14:14:17Z`
Ledger: `registry-research-framework/audit/cleanup-quarantine-ledger-20260514.json`

Action plan for cleanup retained inventory. It does not delete files and it does not redefine delete eligibility.

## Rules

- `delete_candidate_rule`: Only release_state=delete-ready rows may enter a deletion PR.
- `retained_rule`: retained rows are not stale-delete candidates; they need reference migration, replacement proof, or an explicit retention decision.
- `operator_rule`: Use this plan to reduce blocking references before regenerating cleanup-quarantine-ledger.

## Summary

| Metric | Value |
|---|---:|
| Retained inventory items | 89 |
| Delete-ready rows | 0 |
| Reference migration needed | 0 |
| Audit-only retained | 15 |
| Intentional reference keep | 4 |
| Needs replacement/retention decision | 70 |
| Retained pending review | 0 |

## Release States

| State | Count |
|---|---:|
| `audit-only-retained` | 15 |
| `intentional-reference-keep` | 4 |
| `needs-replacement-or-retention-decision` | 70 |

## Top Blocking Reference Paths

| Path | Count |
|---|---:|
| `research/evidence-index.json` | 47 |
| `research/evidence-manifest.json` | 44 |
| `research/evidence-atlas.md` | 39 |
| `research/evidence-manifest.md` | 38 |
| `registry-research-framework/audit/rejected-closure-ledger.json` | 30 |
| `research/evidence-classes.json` | 18 |
| `registry-research-framework/audit/operator-regadd-inventory-20260508-repo.json` | 13 |
| `registry-research-framework/audit/etw-stackwalk-reopen-rotation-ledger.json` | 6 |
| `registry-research-framework/scripts/check_etw_stackwalk_reopen_history_archive.py` | 5 |
| `research/records/power.control.perf-calculate-actual-utilization.json` | 4 |
| `research/records/power.session-watchdog-timeouts.json` | 4 |
| `registry-research-framework/enrichment/enrichment-cache.jsonl` | 3 |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/README.md` | 3 |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/commands/01-promote-previous-snapshot.txt` | 3 |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive/CHECKSUMS.json` | 3 |

## Reference Migration Queue

These rows are the only retained items that already have replacement artifacts and can plausibly become delete-candidates after references are migrated.

_No reference migration rows currently exist._

## Retained Inventory Worklist

| Path | Release state | Category | Blocking refs | Next action |
|---|---|---|---:|---|
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.json` | `intentional-reference-keep` | `operator96-superseded-pilot` | 2 | Keep as a historical example unless a maintainer explicitly rewrites the current docs/record to the replacement artifacts. |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0-recovery.md` | `intentional-reference-keep` | `operator96-superseded-pilot` | 2 | Keep as a historical example unless a maintainer explicitly rewrites the current docs/record to the replacement artifacts. |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.json` | `intentional-reference-keep` | `operator96-superseded-pilot` | 4 | Keep as a historical example unless a maintainer explicitly rewrites the current docs/record to the replacement artifacts. |
| `registry-research-framework/audit/registry-value-experiments/pilot-perf-calculate-actual-utilization-0.md` | `intentional-reference-keep` | `operator96-superseded-pilot` | 4 | Keep as a historical example unless a maintainer explicitly rewrites the current docs/record to the replacement artifacts. |
| `evidence/files/vm-tooling-staging/defender-cloud-demo-extracted` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 1 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/showinfotip-1-hits.csv..md` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 6 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/showsuperhidden-1-hits.csv..md` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 6 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 8 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 8 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 8 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/ghidra-probes` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 2 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/beep_start_toggle_out.txt` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 9 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/crossdevice_resume_probe.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/defender-enhanced-notifications-securitycenter-1-20260324-213118` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-20260325-011845` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 6 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-20260325-012333` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/devmode_longpaths_probe.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/devmode_probe2.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/devmode_probe2.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/feedback_notifications_probe.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/gamemode_admin_probe.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/gamemode_admin_zero_probe.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/ghidra_explorer_serialize.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/hags_toggle_out.txt` | `needs-replacement-or-retention-decision` | `vm-tooling-staging-oldest-sample` | 8 | Decide whether this staging bundle has a canonical evidence/raw replacement before attempting deletion. |
| `evidence/files/vm-tooling-staging/hideemptydrives-0-hits.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/hideemptydrives-1-hits.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/hideemptydrives-result.txt` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/files/vm-tooling-staging/iconsonly-0-hits.csv` | `audit-only-retained` | `vm-tooling-staging-oldest-sample` | 0 | No live blocking references remain; keep for audit trail or handle in a dedicated deletion PR that explicitly accepts audit-only history references. |
| `evidence/raw/procmon/privacy.disable-appcompat-engine.policy/appcompat-policy-bundle-procmon.pml` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/procmon/privacy.disable-appdeviceinventory.policy/appdeviceinventory-policy-procmon.pml` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/procmon/privacy.disable-program-compatibility-assistant/disable-pca-policy-procmon.pml` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/system.reliability-timestamp-enabled-etw-20260424e/system.reliability-timestamp-enabled-etw-20260424e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/system-io-allow-remote-dasd-etw-20260424/system-io-allow-remote-dasd-etw-20260424.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423/power-watchdog-po-callout-timeout-msec-etw-stackwalk-skiptracerpt-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-quoted-20260423/timer-check-flags-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 51 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/system.executive-uuid-sequence-number-etw-20260424e/system.executive-uuid-sequence-number-etw-20260424e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-single-dpc-threshold-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423/long-dpc-queue-threshold-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423/win32k-callout-watchdog-timeout-seconds-etw-stackwalk-skiptracerpt-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423/watchdog-resume-timeout-etw-stackwalk-skiptracerpt-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423/long-dpc-runtime-threshold-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/global-timer-resolution-requests-etw-stackwalk-quoted-20260423/global-timer-resolution-requests-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423/force-bugcheck-dpc-watchdog-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423/dpc-watchdog-profile-offset-etw-stackwalk-quoted-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/wave4-timercheckflags-e2e/wave4-timercheckflags-e2e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 3 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 13 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/power.control.ttm-enabled-etw-20260424e/power.control.ttm-enabled-etw-20260424e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/power-request-override-callstack-20260423/power-request-override-callstack-20260423.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 8 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/power-disable-cpu-idle-states-etw-20260424-main/power-disable-cpu-idle-states-etw-20260424-main.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 9 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/audio.show-hidden-devices-etw-20260424e/audio.show-hidden-devices-etw-20260424e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 53 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `evidence/raw/etw-stackwalk/timer-check-flags-etw-stackwalk-20260418/timer-check-flags-etw-stackwalk-20260418.etl` | `needs-replacement-or-retention-decision` | `large-raw-trace-sample` | 10 | Keep until a derived parse or current index replaces the raw ETL/PML reference. |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.json` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive-check.md` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.json` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 18 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.md` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 18 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.zip` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 18 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.json` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive-check.md` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 10 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.md` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 10 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/etw-stackwalk-reopen-history-archive.zip` | `needs-replacement-or-retention-decision` | `audit-archive-named-sample` | 10 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/kernel-power-net-new-candidates-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 2 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/kernel-power-net-new-follow-up-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 3 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/kernel-power-existing-static-probe-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 10 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-boot-trace-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 12 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-candidate-package-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-etl-registry-review-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-candidate-package-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 2 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-sleep-capability-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 7 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-procmon-bootlog-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-dcomlaunch-power-trigger-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-procmon-follow-up-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 9 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 10 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-etl-registry-review-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 13 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-procmon-bootlog-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 11 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-follow-up-package-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-reactos-hypothesis-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 12 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/system-executive-additional-worker-threads-stress-trigger-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-disable-cpu-idle-states-write-diagnostics-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 6 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-disable-cpu-idle-states-tooling-chain-review-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 6 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-disable-cpu-idle-states-stepwise-orchestration-20260328.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 2 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-session-watchdog-timeouts-stepwise-boot-trace-20260329.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-control-docs-first-runtime-capture-20260329.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 1 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-control-docs-first-stepwise-runtime-capture-20260329.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 39 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-follow-up-20260329.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 11 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
| `registry-research-framework/audit/power-control-docs-first-trigger-etw-guestvar-follow-up-20260329.json` | `needs-replacement-or-retention-decision` | `old-dated-audit-output-sample` | 8 | Keep as historical audit inventory until a current report explicitly supersedes it and live refs are removed. |
