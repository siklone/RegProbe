from __future__ import annotations

import importlib.util
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
import unittest.mock
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


research_v36_lib = load_module("research_v36_lib", SCRIPTS_ROOT / "research_v36_lib.py")
evidence_class_lib = load_module("evidence_class_lib", SCRIPTS_ROOT / "evidence_class_lib.py")
validate_contracts = load_module("validate_research_contracts", FRAMEWORK_SCRIPTS / "validate_research_contracts.py")
metrics_publish_v36_lib = load_module("metrics_publish_v36_lib", SCRIPTS_ROOT / "metrics_publish_v36_lib.py")
blocked_worklist_lib = load_module("generate_blocked_worklist", FRAMEWORK_SCRIPTS / "generate_blocked_worklist.py")
blocked_worklist_check = load_module("check_blocked_worklist", FRAMEWORK_SCRIPTS / "check_blocked_worklist.py")
power_request_override_audit = load_module("audit_power_request_override_runtime", FRAMEWORK_SCRIPTS / "audit_power_request_override_runtime.py")
ghidra_job_queue = load_module("generate_ghidra_job_queue", FRAMEWORK_SCRIPTS / "generate_ghidra_job_queue.py")
ghidra_dispatch_batch = load_module("generate_ghidra_dispatch_batch", FRAMEWORK_SCRIPTS / "generate_ghidra_dispatch_batch.py")
ghidra_dispatch_runner = load_module("run_ghidra_dispatch_batch", FRAMEWORK_SCRIPTS / "run_ghidra_dispatch_batch.py")
ghidra_autotrigger = load_module("generate_ghidra_autotrigger_seeds", FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_seeds.py")
ghidra_autotrigger_inputs = load_module("generate_ghidra_autotrigger_inputs", FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_inputs.py")
ghidra_autotrigger_smoke_check = load_module("check_ghidra_autotrigger_smoke", FRAMEWORK_SCRIPTS / "check_ghidra_autotrigger_smoke.py")
research_quality_gate = load_module("run_research_quality_gate", FRAMEWORK_SCRIPTS / "run_research_quality_gate.py")
etw_stackwalk_plan = load_module("generate_etw_stackwalk_capture_plan", FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_capture_plan.py")
etw_stackwalk_check = load_module("check_etw_stackwalk_capture_plan", FRAMEWORK_SCRIPTS / "check_etw_stackwalk_capture_plan.py")
etw_stackwalk_dispatch_batch = load_module(
    "generate_etw_stackwalk_dispatch_batch",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_dispatch_batch.py",
)
etw_stackwalk_dispatch_check = load_module(
    "check_etw_stackwalk_dispatch_batch",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_dispatch_batch.py",
)
etw_stackwalk_dispatch_runner = load_module(
    "run_etw_stackwalk_dispatch_batch",
    FRAMEWORK_SCRIPTS / "run_etw_stackwalk_dispatch_batch.py",
)
etw_stackwalk_dispatch_run_check = load_module(
    "check_etw_stackwalk_dispatch_run",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_dispatch_run.py",
)
etw_stackwalk_hold_reopen_plan = load_module(
    "generate_etw_stackwalk_hold_reopen_plan",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_hold_reopen_plan.py",
)
etw_stackwalk_hold_reopen_check = load_module(
    "check_etw_stackwalk_hold_reopen_plan",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_hold_reopen_plan.py",
)
etw_stackwalk_hold_reopen_pack = load_module(
    "materialize_etw_stackwalk_hold_reopen_pack",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_hold_reopen_pack.py",
)
etw_stackwalk_hold_reopen_pack_check = load_module(
    "check_etw_stackwalk_hold_reopen_pack",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_hold_reopen_pack.py",
)
etw_stackwalk_reopen_decision_ledger = load_module(
    "generate_etw_stackwalk_reopen_decision_ledger",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_decision_ledger.py",
)
etw_stackwalk_reopen_decision_ledger_check = load_module(
    "check_etw_stackwalk_reopen_decision_ledger",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_decision_ledger.py",
)
etw_stackwalk_reopen_readiness_scoreboard = load_module(
    "generate_etw_stackwalk_reopen_readiness_scoreboard",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_readiness_scoreboard.py",
)
etw_stackwalk_reopen_readiness_scoreboard_check = load_module(
    "check_etw_stackwalk_reopen_readiness_scoreboard",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_readiness_scoreboard.py",
)
etw_stackwalk_reopen_prerequisite_delta = load_module(
    "generate_etw_stackwalk_reopen_prerequisite_delta",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_prerequisite_delta.py",
)
etw_stackwalk_reopen_prerequisite_delta_check = load_module(
    "check_etw_stackwalk_reopen_prerequisite_delta",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_prerequisite_delta.py",
)
etw_stackwalk_reopen_operator_brief = load_module(
    "generate_etw_stackwalk_reopen_operator_brief",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_operator_brief.py",
)
etw_stackwalk_reopen_operator_brief_check = load_module(
    "check_etw_stackwalk_reopen_operator_brief",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_operator_brief.py",
)
etw_stackwalk_reopen_journal = load_module(
    "generate_etw_stackwalk_reopen_journal",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_journal.py",
)
etw_stackwalk_reopen_journal_check = load_module(
    "check_etw_stackwalk_reopen_journal",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_journal.py",
)
etw_stackwalk_reopen_snapshot = load_module(
    "generate_etw_stackwalk_reopen_snapshot",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_snapshot.py",
)
etw_stackwalk_reopen_snapshot_check = load_module(
    "check_etw_stackwalk_reopen_snapshot",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_snapshot.py",
)
etw_stackwalk_reopen_transition_summary = load_module(
    "generate_etw_stackwalk_reopen_transition_summary",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_transition_summary.py",
)
etw_stackwalk_reopen_transition_summary_check = load_module(
    "check_etw_stackwalk_reopen_transition_summary",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_transition_summary.py",
)
etw_stackwalk_reopen_baseline_archive = load_module(
    "materialize_etw_stackwalk_reopen_baseline_archive",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_reopen_baseline_archive.py",
)
etw_stackwalk_reopen_baseline_archive_check = load_module(
    "check_etw_stackwalk_reopen_baseline_archive",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_baseline_archive.py",
)
etw_stackwalk_reopen_history_archive = load_module(
    "materialize_etw_stackwalk_reopen_history_archive",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_reopen_history_archive.py",
)
etw_stackwalk_reopen_history_archive_check = load_module(
    "check_etw_stackwalk_reopen_history_archive",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_history_archive.py",
)
etw_stackwalk_reopen_rotation_ledger = load_module(
    "generate_etw_stackwalk_reopen_rotation_ledger",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_rotation_ledger.py",
)
etw_stackwalk_reopen_rotation_ledger_check = load_module(
    "check_etw_stackwalk_reopen_rotation_ledger",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_rotation_ledger.py",
)
etw_stackwalk_reopen_seed_receipt = load_module(
    "generate_etw_stackwalk_reopen_seed_receipt",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_seed_receipt.py",
)
etw_stackwalk_reopen_seed_receipt_check = load_module(
    "check_etw_stackwalk_reopen_seed_receipt",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_seed_receipt.py",
)
etw_stackwalk_reopen_seed_ack_journal = load_module(
    "generate_etw_stackwalk_reopen_seed_ack_journal",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_seed_ack_journal.py",
)
etw_stackwalk_reopen_seed_ack_journal_check = load_module(
    "check_etw_stackwalk_reopen_seed_ack_journal",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_reopen_seed_ack_journal.py",
)
etw_stackwalk_execution_manifest = load_module(
    "generate_etw_stackwalk_execution_manifest",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_execution_manifest.py",
)
etw_stackwalk_execution_manifest_check = load_module(
    "check_etw_stackwalk_execution_manifest",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_execution_manifest.py",
)
etw_stackwalk_execution_pack = load_module(
    "materialize_etw_stackwalk_execution_pack",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_execution_pack.py",
)
etw_stackwalk_execution_pack_check = load_module(
    "check_etw_stackwalk_execution_pack",
    FRAMEWORK_SCRIPTS / "check_etw_stackwalk_execution_pack.py",
)
etw_stackwalk_bundle = load_module("generate_etw_stackwalk_bundle", FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_bundle.py")
ghidra_symbol_queue = load_module("generate_ghidra_symbol_resolution_queue", FRAMEWORK_SCRIPTS / "generate_ghidra_symbol_resolution_queue.py")
ghidra_symbol_batch = load_module("generate_ghidra_symbol_resolution_batch", FRAMEWORK_SCRIPTS / "generate_ghidra_symbol_resolution_batch.py")
ghidra_symbol_runner = load_module("run_ghidra_symbol_resolution_batch", FRAMEWORK_SCRIPTS / "run_ghidra_symbol_resolution_batch.py")
ghidra_refresh_pipeline = load_module("refresh_ghidra_autotrigger_pipeline", FRAMEWORK_SCRIPTS / "refresh_ghidra_autotrigger_pipeline.py")
ghidra_autotrigger_health = load_module("generate_ghidra_autotrigger_health", FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_health.py")
ghidra_autotrigger_health_check = load_module("check_ghidra_autotrigger_health", FRAMEWORK_SCRIPTS / "check_ghidra_autotrigger_health.py")
ghidra_autotrigger_sync = load_module("sync_ghidra_autotrigger_lane", FRAMEWORK_SCRIPTS / "sync_ghidra_autotrigger_lane.py")
ghidra_autotrigger_smoke = load_module("run_ghidra_autotrigger_smoke", FRAMEWORK_SCRIPTS / "run_ghidra_autotrigger_smoke.py")
ghidra_symbol_handoff = load_module("generate_ghidra_symbol_resolution_handoff", FRAMEWORK_SCRIPTS / "generate_ghidra_symbol_resolution_handoff.py")
ghidra_symbol_transfer = load_module("generate_ghidra_symbol_resolution_transfer", FRAMEWORK_SCRIPTS / "generate_ghidra_symbol_resolution_transfer.py")
ghidra_symbol_transfer_pack = load_module("materialize_ghidra_symbol_resolution_transfer_pack", FRAMEWORK_SCRIPTS / "materialize_ghidra_symbol_resolution_transfer_pack.py")
ghidra_symbol_transfer_pack_check = load_module("check_ghidra_symbol_resolution_transfer_pack", FRAMEWORK_SCRIPTS / "check_ghidra_symbol_resolution_transfer_pack.py")
ghidra_symbol_transfer_pack_unpack = load_module("unpack_ghidra_symbol_resolution_transfer_pack", FRAMEWORK_SCRIPTS / "unpack_ghidra_symbol_resolution_transfer_pack.py")
ghidra_transfer_pack_execution = load_module("generate_ghidra_transfer_pack_execution_plan", FRAMEWORK_SCRIPTS / "generate_ghidra_transfer_pack_execution_plan.py")
ghidra_transfer_pack_execution_run = load_module("run_ghidra_transfer_pack_execution_plan", FRAMEWORK_SCRIPTS / "run_ghidra_transfer_pack_execution_plan.py")
ghidra_transfer_pack_execution_run_check = load_module("check_ghidra_transfer_pack_execution_run", FRAMEWORK_SCRIPTS / "check_ghidra_transfer_pack_execution_run.py")
etw_stackwalk_runner = load_module(
    "run_guest_etw_stackwalk_capture",
    REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-etw-stackwalk-capture.py",
)


class ContractValidationTests(unittest.TestCase):
    def test_contract_examples_validate(self) -> None:
        results = validate_contracts.validate_contract_examples()
        self.assertTrue(all(not errors for errors in results.values()), results)


class PromotionStateTests(unittest.TestCase):
    def test_legacy_curated_actionable_record_promotes(self) -> None:
        record = {
            "record_id": "example.promoted",
            "tweak_id": "example.promoted",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }
        audit = {"next_missing_layer": "none"}

        gate = research_v36_lib.derive_promotion_state(record, audit)

        self.assertEqual(gate["promotion_state"], "promoted")
        self.assertEqual(gate["tweak_origin"], "legacy-curated")
        self.assertTrue(gate["tweak_ingest_allowed"])

    def test_default_registry_value_name_is_schema_complete(self) -> None:
        record = {
            "record_id": "example.default-value",
            "tweak_id": "example.default-value",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Classes\\Example\\InprocServer32",
                        "value_name": "",
                        "value_type": "REG_SZ",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }
        audit = {"next_missing_layer": "none"}

        gate = research_v36_lib.derive_promotion_state(record, audit)

        self.assertEqual(gate["promotion_state"], "promoted")
        self.assertNotIn("schema-incomplete", gate["promotion_blockers"])
        self.assertTrue(gate["tweak_ingest_allowed"])

    def test_research_derived_record_stays_blocked_with_missing_layer(self) -> None:
        record = {
            "record_id": "example.blocked",
            "tweak_id": "example.blocked",
            "record_status": "draft",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
            },
            "app_current_implementation": {
                "status": "not-mapped",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }
        audit = {"next_missing_layer": "runtime-trace"}

        gate = research_v36_lib.derive_promotion_state(record, audit)

        self.assertEqual(gate["promotion_state"], "blocked")
        self.assertEqual(gate["promotion_blockers"], ["no-runtime-proof"])
        self.assertEqual(gate["tweak_origin"], "research-derived")
        self.assertTrue(gate["debug_override_allowed"])

    def test_research_derived_vm_safety_pass_promotes_record_without_ingest(self) -> None:
        record = {
            "record_id": "example.vm-promoted",
            "tweak_id": "example.vm-promoted",
            "record_status": "draft",
            "setting": {
                "area": "Power",
                "targets": [
                    {
                        "path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                        "value_name": "ExampleValue",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "not-mapped",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {
                "behavior": {
                    "benchmark": {
                        "executed": True,
                        "safety_passed": True,
                    }
                },
                "bench_results": {
                    "executed": True,
                    "safety_passed": True,
                    "rollback_verified": True,
                },
                "rollback_verification": {
                    "rollback_declared": True,
                    "rollback_executed": True,
                    "rollback_verified": True,
                    "rollback_verification_method": "vm-safety-bench-restore-baseline",
                    "rollback_failure_reason": None,
                },
                "negative_evidence": {},
                "reproducibility": {"vm_name": "windows-11-25h2-vm"},
            },
            evaluated_at="2026-04-11T00:00:00Z",
        )

        self.assertEqual(gate["promotion_state"], "promoted")
        self.assertFalse(gate["tweak_ingest_allowed"])
        self.assertTrue(gate["record_promotion_allowed"])

    def test_score_breakdown_is_deterministic_and_contains_overall_score(self) -> None:
        record = {
            "record_id": "example.scored",
            "tweak_id": "power.example-scored",
            "record_status": "validated",
            "setting": {
                "area": "Power",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        score = research_v36_lib.score_candidate(record, {"next_missing_layer": "none"})

        self.assertIn("overall_score", score)

    def test_score_etl_candidate_prefers_security_writes_with_value_context(self) -> None:
        candidate = {
            "discovery_source": "etl-registry-touch",
            "feature_area": "Security",
            "operation": "RegSetValue",
            "key_path": "HKLM\\Software\\Microsoft\\Windows Defender",
            "value_name": "DisableRealtimeMonitoring",
            "value_data": "1",
        }

        score = research_v36_lib.score_etl_candidate(candidate)

        self.assertEqual(score["profile"], "etl-runtime-v1")
        self.assertEqual(score["runtime_evidence_strength"], 0.8)
        self.assertEqual(score["rollback_clarity"], 0.3)
        self.assertEqual(score["tweak_suitability"], 0.9)
        self.assertEqual(score["bench_priority"], 0.4)
        self.assertTrue(score["has_value_context"])
        self.assertFalse(score["has_caller_stack"])
        self.assertEqual(score["caller_stack_frame_count"], 0)
        self.assertGreaterEqual(score["total"], 0.57)

    def test_score_etl_candidate_reports_caller_stack_context(self) -> None:
        candidate = {
            "discovery_source": "etl-registry-touch",
            "feature_area": "System",
            "operation": "RegQueryValue",
            "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
            "value_name": "TimerCheckFlags",
            "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopReadRegKeyValue"],
        }

        score = research_v36_lib.score_etl_candidate(candidate)

        self.assertTrue(score["has_caller_stack"])
        self.assertEqual(score["caller_stack_frame_count"], 2)

    def test_has_exact_runtime_read_ignores_negative_gap_language(self) -> None:
        record = {
            "evidence": [
                {
                    "kind": "vm-test",
                    "summary": "The exact runtime-read gap remains open after the Procmon SaveAs timeout.",
                }
            ]
        }

        self.assertFalse(evidence_class_lib.has_exact_runtime_read(record))

    def test_has_exact_runtime_read_accepts_positive_exact_read_language(self) -> None:
        record = {
            "evidence": [
                {
                    "kind": "procmon-trace",
                    "summary": "The guest-processed boot log captured an exact runtime read for ExampleValue via RegQueryValue.",
                }
            ]
        }

        self.assertTrue(evidence_class_lib.has_exact_runtime_read(record))

    def test_etw_trace_satisfies_runtime_trace_lane_before_static_followup(self) -> None:
        record = {
            "record_id": "example.etw-trace",
            "tweak_id": "example.etw-trace",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
            "evidence": [
                {
                    "kind": "etw-trace",
                    "location": "evidence/raw/etw-stackwalk/example/normalized-registry-bundle.json",
                    "summary": "Narrow ETW registry stackwalk captured RegQueryValue for Enabled.",
                }
            ],
        }

        self.assertTrue(evidence_class_lib.has_trace_evidence(record))
        self.assertTrue(evidence_class_lib.has_runtime_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "ghidra")
        class_entry = evidence_class_lib.build_class_entry(record)
        runtime = class_entry["runtime_proof"]
        self.assertTrue(runtime["has_runtime_evidence"])
        self.assertIn("Narrow ETW registry stackwalk", runtime["summary"])
        self.assertEqual(runtime["links"][0]["kind"], "etw-trace")

    def test_failed_procmon_or_wpr_mentions_do_not_count_as_trace_evidence(self) -> None:
        record = {
            "record_id": "example.failed-trace-mentions",
            "tweak_id": "example.failed-trace-mentions",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
                "notes": "Procmon SaveAs timed out and WPR no-hit remained unresolved.",
            },
            "evidence": [
                {
                    "kind": "ghidra-headless",
                    "summary": "Static xref exists, but runtime still needs a real trace artifact.",
                }
            ],
        }

        self.assertFalse(evidence_class_lib.has_procmon_evidence(record))
        self.assertFalse(evidence_class_lib.has_wpr_evidence(record))
        self.assertFalse(evidence_class_lib.has_trace_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "runtime-trace")

    def test_runtime_diff_alone_does_not_satisfy_trace_lane(self) -> None:
        record = {
            "record_id": "example.runtime-diff",
            "tweak_id": "example.runtime-diff",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
            "evidence": [
                {
                    "kind": "runtime-diff",
                    "summary": "A reversible write/read diff changed Enabled, but no trace was captured.",
                }
            ],
        }

        self.assertFalse(evidence_class_lib.has_trace_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "runtime-trace")

    def test_repo_code_only_static_evidence_scores_low(self) -> None:
        record = {
            "record_id": "example.repo-code",
            "tweak_id": "example.repo-code",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {"apply_allowed": True, "confidence": "medium"},
            "evidence": [
                {
                    "kind": "repo-code",
                    "supports": ["path", "value", "ui-mapping"],
                    "summary": "The provider writes Enabled = 1 for the current tweak.",
                }
            ],
        }

        score = research_v36_lib.score_candidate(record, {"next_missing_layer": "none"})

        self.assertEqual(score["static_evidence_strength"], 1)

    def test_string_reference_ghidra_claim_does_not_score_as_strong_static(self) -> None:
        record = {
            "record_id": "example.string-xref",
            "tweak_id": "example.string-xref",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {"apply_allowed": False, "confidence": "medium"},
            "evidence": [
                {
                    "kind": "ghidra-headless",
                    "supports": ["path", "string-reference", "version-scope"],
                    "summary": "A string/xref lead found the registry text but did not recover a caller chain or API semantics.",
                }
            ],
        }

        score = research_v36_lib.score_candidate(record, {"next_missing_layer": "none"})

        self.assertEqual(score["static_evidence_strength"], 2)

    def test_old_verified_record_enters_revalidation_pending(self) -> None:
        record = {
            "record_id": "example.revalidation",
            "tweak_id": "example.revalidation",
            "record_status": "validated",
            "last_reviewed_utc": "2026-03-01T00:00:00Z",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {"behavior": {}, "negative_evidence": {}, "reproducibility": {}},
            evaluated_at="2026-04-09T00:00:00Z",
        )

        self.assertEqual(gate["promotion_state"], "revalidation-pending")
        self.assertEqual(gate["promotion_blockers"], ["stale-evidence"])

    def test_stale_promoted_record_by_build_drift_enters_revalidation_pending(self) -> None:
        record = {
            "record_id": "example.stale-build",
            "tweak_id": "example.stale-build",
            "record_status": "validated",
            "tested_on": [
                {"environment": "vm", "os": "Windows 11", "build": "26097"},
            ],
            "last_reviewed_utc": "2026-04-08T00:00:00Z",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {"behavior": {}, "negative_evidence": {}, "reproducibility": {}},
            evaluated_at="2026-04-09T00:00:00Z",
        )

        self.assertEqual(gate["promotion_state"], "revalidation-pending")
        self.assertEqual(gate["freshness_status"]["stale_reason"], "build-drift-threshold")
        self.assertEqual(gate["promotion_blockers"], ["stale-evidence"])

    def test_apply_allowed_with_unverified_rollback_sets_rollback_unverified_blocker(self) -> None:
        record = {
            "record_id": "example.rollback-unverified",
            "tweak_id": "example.rollback-unverified",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {
                "behavior": {},
                "negative_evidence": {},
                "rollback_verification": {
                    "rollback_declared": True,
                    "rollback_executed": False,
                    "rollback_verified": False,
                    "rollback_verification_method": "state_diff",
                    "rollback_failure_reason": "rollback-not-executed",
                },
            },
        )

        self.assertEqual(gate["promotion_state"], "blocked")
        self.assertIn("rollback-unverified", gate["promotion_blockers"])

    def test_restore_mismatch_sets_rollback_failed_blocker(self) -> None:
        record = {
            "record_id": "example.rollback-failed",
            "tweak_id": "example.rollback-failed",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {
                "behavior": {},
                "negative_evidence": {},
                "rollback_verification": {
                    "rollback_declared": True,
                    "rollback_executed": True,
                    "rollback_verified": False,
                    "rollback_verification_method": "state_diff",
                    "rollback_failure_reason": "rollback-state-mismatch",
                },
            },
        )

        self.assertEqual(gate["promotion_state"], "blocked")
        self.assertIn("rollback-failed", gate["promotion_blockers"])

    def test_vm_safety_bench_projection_surfaces_passed_result(self) -> None:
        record = {
            "record_id": "example.vm-safety",
            "tweak_id": "example.vm-safety",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }
        full_evidence = {
            "bench_results": {
                "bench_tier": "vm",
                "bench_profile": "functional",
                "safety_passed": True,
                "boot_success": True,
                "shell_usable": True,
                "services_healthy": True,
                "event_log_clean": True,
                "rollback_executed": True,
                "rollback_verified": True,
                "bench_measurement_reliability": "functional",
                "executed_at": "2026-04-10T17:10:25.9167605-07:00",
            },
            "reproducibility": {"vm_name": "windows-11-25h2-vm"},
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "none"}, full_evidence)

        self.assertTrue(gate["bench_status"]["executed"])
        self.assertEqual(gate["bench_status"]["bench_tier"], "vm")
        self.assertTrue(gate["bench_status"]["safety_passed"])
        self.assertEqual(gate["bench_status"]["safety_status"], "passed")
        self.assertEqual(gate["bench_status"]["bench_measurement_reliability"], "functional")
        self.assertNotIn("bench-failed-safety", gate["promotion_blockers"])

    def test_record_level_bench_results_override_legacy_full_evidence(self) -> None:
        record = {
            "record_id": "example.vm-safety-record-override",
            "tweak_id": "example.vm-safety-record-override",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
            "bench_results": {
                "bench_tier": "vm",
                "executed": True,
                "safety_passed": True,
                "summary": "Canonical record-level bench passed.",
            },
        }
        full_evidence = {
            "bench_results": {
                "bench_tier": "unknown",
                "executed": True,
                "summary": "Legacy exploratory runner failed.",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "none"}, full_evidence)

        self.assertEqual(gate["bench_status"]["bench_tier"], "vm")
        self.assertEqual(gate["bench_status"]["summary"], "Canonical record-level bench passed.")
        self.assertTrue(gate["bench_status"]["safety_passed"])

    def test_specific_decision_gate_blocker_suppresses_generic_doc_review(self) -> None:
        record = {
            "record_id": "example.decision-specific-blocker",
            "tweak_id": "example.decision-specific-blocker",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": ["runtime_no_read"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertIn("runtime_no_read", gate["promotion_blockers"])
        self.assertNotIn("documentation-first-review", gate["promotion_blockers"])

    def test_failed_vm_safety_bench_blocks_candidate(self) -> None:
        record = {
            "record_id": "example.vm-safety-failed",
            "tweak_id": "example.vm-safety-failed",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }
        full_evidence = {
            "bench_results": {
                "bench_tier": "vm",
                "safety_passed": False,
                "rollback_executed": True,
                "rollback_verified": False,
                "rollback_failure_reason": "post-delete-state-mismatch",
            },
            "rollback_verification": {
                "rollback_declared": True,
                "rollback_executed": True,
                "rollback_verified": True,
                "rollback_verification_method": "state_diff",
            },
            "reproducibility": {"vm_name": "windows-11-25h2-vm"},
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "none"}, full_evidence)

        self.assertEqual(gate["bench_status"]["safety_status"], "failed")
        self.assertIn("bench-failed-safety", gate["promotion_blockers"])

    def test_wpr_no_hit_blocker_maps_to_runtime_trace_lane(self) -> None:
        record = {
            "record_id": "example.wpr-no-hit",
            "tweak_id": "example.wpr-no-hit",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": ["wpr-boot-registry-no-hit-current-build"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "runtime-trace")
        self.assertIn("wpr-boot-registry-no-hit-current-build", gate["promotion_blockers"])
        self.assertNotIn("documentation-first-review", gate["promotion_blockers"])

    def test_runtime_lane_specific_no_hit_blockers_stay_runtime_trace(self) -> None:
        for blocker in [
            "win32k-callout-watchdog-bounded-s1-registry-etw-no-hit-current-build",
            "win32-callout-watchdog-bugcheck-procmon-saveas-timeout-on-bounded-callout-lane",
            "long-dpc-threshold-procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane",
            "power-session-watchdog-timeouts-exact-runtime-read-unresolved",
            "powerwatchdog-timeout-family-runtime-read-unresolved",
            "dpc-watchdog-control-runtime-read-unresolved",
            "timer-check-flags-wpr-boot-no-hit-current-build",
        ]:
            record = {
                "record_id": f"example.{blocker}",
                "tweak_id": f"example.{blocker}",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": False,
                    "confidence": "medium",
                    "restore_default_supported": True,
                    "blocking_issues": [blocker],
                },
                "validation_proof": {
                    "source_url": "Docs/example.md",
                    "exact_quote_or_path": "Docs/example.md:1",
                },
            }

            gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

            self.assertEqual(gate["next_missing_layer"], "runtime-trace")
            self.assertIn(blocker, gate["promotion_blockers"])

    def test_specific_current_build_doc_blockers_keep_official_doc_and_metrics_mapping(self) -> None:
        blocker = "timer-check-flags-no-primary-current-build-doc"
        record = {
            "record_id": f"example.{blocker}",
            "tweak_id": f"example.{blocker}",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": [blocker],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "official-doc")
        self.assertIn(blocker, gate["promotion_blockers"])
        self.assertEqual(
            metrics_publish_v36_lib.normalize_blocker_name(blocker),
            "no-primary-current-build-doc",
        )

    def test_execution_required_init_walker_blocker_maps_to_ghidra_lane(self) -> None:
        record = {
            "record_id": "example.init-walker",
            "tweak_id": "example.init-walker",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": ["execution-required-init-walker-not-symbol-resolved"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "ghidra")
        self.assertIn("execution-required-init-walker-not-symbol-resolved", gate["promotion_blockers"])

    def test_specific_execution_required_seeding_path_blockers_stay_ghidra(self) -> None:
        for blocker in [
            "audio-execution-required-no-current-build-registry-seeding-path",
            "system-execution-required-no-current-build-registry-seeding-path",
        ]:
            record = {
                "record_id": f"example.{blocker}",
                "tweak_id": f"example.{blocker}",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": False,
                    "confidence": "medium",
                    "restore_default_supported": True,
                    "blocking_issues": [blocker],
                },
                "validation_proof": {
                    "source_url": "Docs/example.md",
                    "exact_quote_or_path": "Docs/example.md:1",
                },
            }

            gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

            self.assertEqual(gate["next_missing_layer"], "ghidra")
            self.assertIn(blocker, gate["promotion_blockers"])

    def test_string_or_symbol_no_hit_blocker_maps_to_ghidra_lane(self) -> None:
        record = {
            "record_id": "example.string-or-symbol-no-hit",
            "tweak_id": "example.string-or-symbol-no-hit",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": ["no-current-build-string-or-symbol-hit"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "ghidra")
        self.assertIn("no-current-build-string-or-symbol-hit", gate["promotion_blockers"])

    def test_specific_ghidra_blockers_stay_ghidra(self) -> None:
        for blocker in [
            "powerwatchdog-timeout-family-no-current-build-string-or-symbol-hit",
            "powerrequestoverride-static-context-adjacent-not-leaf-specific",
            "dpc-watchdog-profile-conditional-initialization-unproven",
            "power-session-watchdog-timeouts-specific-caller-unresolved",
        ]:
            record = {
                "record_id": f"example.{blocker}",
                "tweak_id": f"example.{blocker}",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": False,
                    "confidence": "medium",
                    "restore_default_supported": True,
                    "blocking_issues": [blocker],
                },
                "validation_proof": {
                    "source_url": "Docs/example.md",
                    "exact_quote_or_path": "Docs/example.md:1",
                },
            }

            gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

            self.assertEqual(gate["next_missing_layer"], "ghidra")
            self.assertIn(blocker, gate["promotion_blockers"])

    def test_specific_hold_blockers_map_to_intentional_hold_lane_without_generic_tag(self) -> None:
        for blocker in [
            "enable-virtualization-research-only-raw-policy-system-value",
            "hiber-file-size-percent-research-only-raw-power-manager-value",
            "hibernate-enabled-default-hibernate-trigger-not-available-on-current-vm",
            "timer-rebase-threshold-drips-trigger-not-available-on-current-vm",
            "ttmenabled-boot-unsafe-dedicated-boot-lane-required",
            "powerwatchdog-timeout-family-intentional-hold-no-current-build-pivot",
        ]:
            record = {
                "record_id": f"example.{blocker}",
                "tweak_id": f"example.{blocker}",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": False,
                    "confidence": "medium",
                    "restore_default_supported": True,
                    "blocking_issues": [blocker],
                },
                "validation_proof": {
                    "source_url": "Docs/example.md",
                    "exact_quote_or_path": "Docs/example.md:1",
                },
            }

            gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

            self.assertEqual(gate["next_missing_layer"], "intentional-hold")
            self.assertIn(blocker, gate["promotion_blockers"])

            ghidra_gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "ghidra"}, {})

            self.assertEqual(ghidra_gate["next_missing_layer"], "intentional-hold")

    def test_reboot_diff_boot_unsafe_blocker_upgrades_to_intentional_hold_lane(self) -> None:
        record = {
            "record_id": "example.boot-unsafe-reboot-diff",
            "tweak_id": "example.boot-unsafe-reboot-diff",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": [
                    "ttmenabled-boot-unsafe-dedicated-boot-lane-required",
                    "ttmenabled-boot-unsafe-on-isolated-pilot-profile",
                ],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "reboot-diff"}, {})

        self.assertEqual(gate["next_missing_layer"], "intentional-hold")
        self.assertIn("ttmenabled-boot-unsafe-dedicated-boot-lane-required", gate["promotion_blockers"])
        self.assertNotIn("reboot-diff", gate["promotion_blockers"])

    def test_restore_story_specific_blocker_maps_to_restore_story_lane(self) -> None:
        record = {
            "record_id": "example.restore-story",
            "tweak_id": "example.restore-story",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": False,
                "confidence": "medium",
                "restore_default_supported": True,
                "blocking_issues": ["powerrequestoverride-restore-story-leaf-model-unproven"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "restore-story")
        self.assertIn("powerrequestoverride-restore-story-leaf-model-unproven", gate["promotion_blockers"])

    def test_specific_execution_required_runtime_no_hit_blockers_map_to_runtime_trace_lane(self) -> None:
        for blocker in [
            "audio-execution-required-megatrigger-etw-no-hit-current-build",
            "system-execution-required-wpr-boot-no-hit-current-build",
        ]:
            record = {
                "record_id": f"example.{blocker}",
                "tweak_id": f"example.{blocker}",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": False,
                    "confidence": "medium",
                    "restore_default_supported": True,
                    "blocking_issues": [blocker],
                },
                "validation_proof": {
                    "source_url": "Docs/example.md",
                    "exact_quote_or_path": "Docs/example.md:1",
                },
            }

            gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

            self.assertEqual(gate["next_missing_layer"], "runtime-trace")
            self.assertIn(blocker, gate["promotion_blockers"])

    def test_negative_evidence_functional_no_effect_blocks_candidate(self) -> None:
        record = {
            "record_id": "example.functional-no-effect",
            "tweak_id": "example.functional-no-effect",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_default_supported": True,
            },
            "app_current_implementation": {
                "status": "not-mapped",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none", "tools_used": ["procmon"], "layers_used": ["runtime"]},
            {
                "behavior": {
                    "registry_sideeffects": {
                        "executed": True,
                        "sideeffect_count": 0,
                        "summary_counts": {
                            "added_keys": 0,
                            "removed_keys": 0,
                            "added_values": 0,
                            "removed_values": 0,
                            "modified_values": 0,
                            "unchanged_values": 1,
                        },
                    }
                },
                "negative_evidence": {
                    "eligible": True,
                    "reason": "runtime-or-source-no-hit",
                    "attempted_tools": ["procmon"],
                    "attempted_layers": ["runtime"],
                },
            },
        )

        self.assertEqual(gate["promotion_state"], "blocked")
        self.assertIn("functional-no-effect", gate["promotion_blockers"])
        self.assertLessEqual(gate["score_breakdown"]["runtime_evidence_strength"], 1)

    def test_rejected_promotion_disposition_closes_active_blocker_without_hiding_reason(self) -> None:
        record = {
            "record_id": "example.rejected-disposition",
            "tweak_id": "example.rejected-disposition",
            "record_status": "review-required",
            "setting": {
                "name": "Rejected Disposition",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Example",
                        "value_name": "ExampleValue",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "confidence": "low",
                "apply_allowed": False,
                "restore_default_supported": False,
                "restore_previous_supported": False,
                "promotion_disposition": "rejected",
                "promotion_disposition_reason": "One-shot action has no rollback story.",
                "blocking_issues": ["validation-proof", "one-shot-no-rollback"],
            },
        }

        gate = research_v36_lib.derive_promotion_state(record, {})

        self.assertEqual(gate["promotion_state"], "rejected")
        self.assertEqual(gate["promotion_disposition"], "rejected")
        self.assertEqual(gate["promotion_blockers"], ["promotion-disposition-non-reversible-action"])
        self.assertEqual(gate["closure_status"], "decision-backed-rejected")
        self.assertEqual(gate["closure_kind"], "non-reversible-action")
        self.assertIn("validation-proof", gate["rejection_closure"]["superseded_blockers"])
        self.assertIn("one-shot-no-rollback", gate["rejection_closure"]["superseded_blockers"])


class GapAnalysisTests(unittest.TestCase):
    def test_gap_analysis_emits_hkcu_and_policy_analogs(self) -> None:
        records = [
            {
                "record_id": "example.seed",
                "tweak_id": "example.seed",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Example",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {"confidence": "medium"},
                "validation_proof": {"exact_quote_or_path": "Docs/example.md:1"},
            }
        ]

        gaps = research_v36_lib.gap_analysis_candidates(records)
        reasons = {item["discovery_reason"] for item in gaps}

        self.assertIn("missing_hkcu_analog", reasons)
        self.assertIn("missing_policy_analog", reasons)

    def test_gap_analysis_summary_counts_triaged_and_discarded(self) -> None:
        entries = [
            {
                "candidate_id": "gap::one",
                "discovery_source": "ai-gap-analysis",
                "discovery_reason": "missing_hkcu_analog",
                "state": "triaged",
            },
            {
                "candidate_id": "gap::two",
                "discovery_source": "ai-gap-analysis",
                "discovery_reason": "missing_policy_analog",
                "state": "discarded",
                "discard_reason": ["triage:missing:feature_area"],
            },
        ]

        summary = research_v36_lib.summarize_gap_analysis(entries)

        self.assertEqual(summary["total_generated"], 2)
        self.assertEqual(summary["triaged"], 1)
        self.assertEqual(summary["discarded"], 1)
        self.assertEqual(summary["top_gap_types"][0]["gap_type"], "missing_hkcu_analog")
        self.assertEqual(summary["top_discard_reasons"][0]["reason"], "triage:missing:feature_area")

    def test_triage_candidate_rejects_non_registry_path(self) -> None:
        accepted, reasons = research_v36_lib.triage_candidate(
            {
                "discovery_source": "ai-gap-analysis",
                "feature_area": "Power",
                "required_followup": "triage",
                "key_path": "C:\\Temp\\NotRegistry",
            }
        )

        self.assertFalse(accepted)
        self.assertIn("invalid:key_path", reasons)

    def test_triage_candidate_rejects_etl_runtime_noise_families(self) -> None:
        accepted, reasons = research_v36_lib.triage_candidate(
            {
                "schema_version": research_v36_lib.CURRENT_SCHEMA_VERSION,
                "candidate_id": "etl::noise",
                "discovery_source": "etl-registry-touch",
                "discovery_reason": "registry_touch_extracted",
                "feature_area": "System",
                "key_path": "HKLM\\Software\\Microsoft\\WBEM\\Tracing\\Providers\\WMIPingProvider @ root\\CIMV2",
                "value_name": "LastDownloadTime",
                "registry_clue": "RegSetValue via pid:100",
                "initial_confidence": "medium",
                "seed_reference": "tests/runtime",
                "required_followup": "triage",
                "execution_context": research_v36_lib.default_execution_context(),
            }
        )

        self.assertFalse(accepted)
        self.assertIn("etl:wbem-tracing-noise", reasons)
        self.assertIn("etl:timestamp-value-noise", reasons)

    def test_sibling_discovery_only_triggers_for_promotable_states(self) -> None:
        self.assertTrue(research_v36_lib.should_trigger_sibling_discovery({"promotion_state": "promotion-eligible"}))
        self.assertTrue(research_v36_lib.should_trigger_sibling_discovery({"promotion_state": "promoted"}))
        self.assertFalse(research_v36_lib.should_trigger_sibling_discovery({"promotion_state": "blocked"}))
        self.assertFalse(research_v36_lib.should_trigger_sibling_discovery({"state": "scored"}))

    def test_sibling_expansion_emits_controlled_candidates(self) -> None:
        records = [
            {
                "record_id": "example.promoted",
                "tweak_id": "example.promoted",
                "record_status": "validated",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {
                    "apply_allowed": True,
                    "confidence": "high",
                    "restore_default_supported": True,
                },
                "app_current_implementation": {"status": "matches-research"},
                "validation_proof": {"exact_quote_or_path": "Docs/example.md:1"},
            },
            {
                "record_id": "example.blocked",
                "tweak_id": "example.blocked",
                "record_status": "draft",
                "setting": {
                    "area": "Example",
                    "targets": [
                        {
                            "path": "HKLM\\Software\\Blocked",
                            "value_name": "Enabled",
                            "value_type": "REG_DWORD",
                        }
                    ],
                },
                "decision": {"apply_allowed": False, "confidence": "medium"},
                "app_current_implementation": {"status": "not-mapped"},
                "validation_proof": {"exact_quote_or_path": "Docs/example.md:1"},
            },
        ]

        candidates = research_v36_lib.sibling_expansion_candidates(
            records,
            {"example.blocked": {"next_missing_layer": "runtime-trace"}},
            {"example.promoted": {"promotion_state": "promoted"}},
        )
        reasons = {item["discovery_reason"] for item in candidates}
        sources = {item["discovery_source"] for item in candidates}

        self.assertIn("sibling_expansion", sources)
        self.assertIn("missing_hkcu_analog", reasons)
        self.assertIn("missing_policy_analog", reasons)


class EtlDiscoveryTests(unittest.TestCase):
    def test_extract_registry_touches_from_tracerpt_xml_normalizes_key_path(self) -> None:
        xml_payload = """<?xml version="1.0" encoding="utf-8"?>
<Events>
  <Event>
    <System>
      <Provider Guid="{AE53722E-C863-11D2-8659-00C04FA321A1}" />
      <EventID>10</EventID>
      <Execution ProcessID="4242" />
    </System>
    <EventData>
      <Data Name="KeyName">\\REGISTRY\\MACHINE\\System\\CurrentControlSet\\Control\\Power</Data>
      <Data Name="ValueName">AllowSystemRequiredPowerRequests</Data>
      <Data Name="ProcessName">svchost.exe</Data>
      <Data Name="Operation">QueryValueKey</Data>
      <Data Name="Stack">nt!PopReadRegKeyValue; nt!PopPowerRequestInitialize</Data>
    </EventData>
  </Event>
</Events>
"""
        with tempfile.TemporaryDirectory() as temp_dir:
            xml_path = Path(temp_dir) / "sample.etl.xml"
            xml_path.write_text(xml_payload, encoding="utf-8")

            touches = research_v36_lib.extract_registry_touches_from_tracerpt_xml(
                xml_path,
                provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
            )

        self.assertEqual(len(touches), 1)
        self.assertEqual(touches[0]["key_path"], "HKLM\\System\\CurrentControlSet\\Control\\Power")
        self.assertEqual(touches[0]["value_name"], "AllowSystemRequiredPowerRequests")
        self.assertEqual(touches[0]["operation"], "RegQueryValue")
        self.assertEqual(touches[0]["caller_stack"], ["nt!PopReadRegKeyValue", "nt!PopPowerRequestInitialize"])

    def test_extract_registry_touches_from_tracerpt_xml_attaches_separate_stackwalk_events(self) -> None:
        xml_payload = """<?xml version="1.0" encoding="utf-8"?>
<Events>
  <Event>
    <System>
      <Provider Guid="{AE53722E-C863-11D2-8659-00C04FA321A1}" />
      <EventID>10</EventID>
      <Execution ProcessID="7228" ThreadID="3976" />
    </System>
    <EventData>
      <Data Name="InitialTime">10401529963</Data>
      <Data Name="KeyName">\\REGISTRY\\MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\Kernel</Data>
      <Data Name="ValueName">TimerCheckFlags</Data>
      <Data Name="ProcessName">wbemsvc.dll</Data>
      <Data Name="Operation">QueryValueKey</Data>
    </EventData>
    <RenderingInfo>
      <EventName xmlns="http://schemas.microsoft.com/win/2004/08/events/trace">Registry</EventName>
    </RenderingInfo>
  </Event>
  <Event>
    <System>
      <Provider Guid="{9E814AAD-3204-11D2-9A82-006008A86939}" />
      <EventID>0</EventID>
      <Execution ProcessID="4294967295" ThreadID="4294967295" />
    </System>
    <EventData>
      <Data Name="EventTimeStamp">10401529966</Data>
      <Data Name="StackProcess">0x1C3C</Data>
      <Data Name="StackThread">3976</Data>
      <Data Name="Stack1">0xFFFFF803C3FEDD84</Data>
      <Data Name="Stack2">0xFFFFF803C3FED794</Data>
      <Data Name="Stack3">0xFFFFF803C3F27B4D</Data>
    </EventData>
    <RenderingInfo>
      <EventName xmlns="http://schemas.microsoft.com/win/2004/08/events/trace">StackWalk</EventName>
    </RenderingInfo>
  </Event>
</Events>
"""
        with tempfile.TemporaryDirectory() as temp_dir:
            xml_path = Path(temp_dir) / "sample.etl.xml"
            xml_path.write_text(xml_payload, encoding="utf-8")

            touches = research_v36_lib.extract_registry_touches_from_tracerpt_xml(
                xml_path,
                provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
            )

        self.assertEqual(len(touches), 1)
        self.assertEqual(touches[0]["key_path"], "HKLM\\System\\CurrentControlSet\\Control\\Session Manager\\Kernel")
        self.assertEqual(touches[0]["value_name"], "TimerCheckFlags")
        self.assertEqual(
            touches[0]["caller_stack"],
            ["0xFFFFF803C3FEDD84", "0xFFFFF803C3FED794", "0xFFFFF803C3F27B4D"],
        )
        self.assertEqual(touches[0]["caller_stack_frame_count"], 3)

    def test_extract_registry_touches_from_tracerpt_xml_uses_rendering_opcode_and_thread_context(self) -> None:
        xml_payload = """<?xml version="1.0" encoding="utf-8"?>
<Events>
  <Event>
    <System>
      <Provider Guid="{9E814AAD-3204-11D2-9A82-006008A86939}" />
      <EventID>0</EventID>
      <Execution ProcessID="6272" ThreadID="6140" />
    </System>
    <EventData>
      <Data Name="InitialTime">22599122242</Data>
      <Data Name="Status">0</Data>
      <Data Name="Index">0</Data>
      <Data Name="KeyHandle">0xFFFF81081B30A990</Data>
      <Data Name="KeyName">SYSTEM\\CurrentControlSet\\Control\\Power</Data>
    </EventData>
    <RenderingInfo>
      <Opcode>Open</Opcode>
      <EventName xmlns="http://schemas.microsoft.com/win/2004/08/events/trace">Registry</EventName>
    </RenderingInfo>
    <ExtendedTracingInfo>
      <EventGuid>{AE53722E-C863-11D2-8659-00C04FA321A1}</EventGuid>
    </ExtendedTracingInfo>
  </Event>
  <Event>
    <System>
      <Provider Guid="{9E814AAD-3204-11D2-9A82-006008A86939}" />
      <EventID>0</EventID>
      <Execution ProcessID="6272" ThreadID="6140" />
    </System>
    <EventData>
      <Data Name="InitialTime">22599169464</Data>
      <Data Name="Status">0</Data>
      <Data Name="Index">2</Data>
      <Data Name="KeyHandle">0xFFFF81081B3D4D40</Data>
      <Data Name="KeyName">AllowSystemRequiredPowerRequests</Data>
    </EventData>
    <RenderingInfo>
      <Opcode>QueryValue</Opcode>
      <EventName xmlns="http://schemas.microsoft.com/win/2004/08/events/trace">Registry</EventName>
    </RenderingInfo>
    <ExtendedTracingInfo>
      <EventGuid>{AE53722E-C863-11D2-8659-00C04FA321A1}</EventGuid>
    </ExtendedTracingInfo>
  </Event>
  <Event>
    <System>
      <Provider Guid="{9E814AAD-3204-11D2-9A82-006008A86939}" />
      <EventID>0</EventID>
      <Execution ProcessID="4294967295" ThreadID="4294967295" />
    </System>
    <EventData>
      <Data Name="EventTimeStamp">22599169540</Data>
      <Data Name="StackProcess">0x1880</Data>
      <Data Name="StackThread">6140</Data>
      <Data Name="Stack1">0xFFFFF803C3FEDD84</Data>
      <Data Name="Stack2">0xFFFFF803C3FED794</Data>
      <Data Name="Stack3">0xFFFFF803C3F27B4D</Data>
    </EventData>
    <RenderingInfo>
      <Opcode>Stack</Opcode>
      <EventName xmlns="http://schemas.microsoft.com/win/2004/08/events/trace">StackWalk</EventName>
    </RenderingInfo>
  </Event>
</Events>
"""
        with tempfile.TemporaryDirectory() as temp_dir:
            xml_path = Path(temp_dir) / "sample.etl.xml"
            xml_path.write_text(xml_payload, encoding="utf-8")

            touches = research_v36_lib.extract_registry_touches_from_tracerpt_xml(
                xml_path,
                provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
            )

        matching = [
            touch
            for touch in touches
            if touch.get("key_path") == "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power"
            and touch.get("value_name") == "AllowSystemRequiredPowerRequests"
        ]
        self.assertEqual(len(matching), 1)
        self.assertEqual(matching[0]["operation"], "RegQueryValue")
        self.assertEqual(
            matching[0]["caller_stack"],
            ["0xFFFFF803C3FEDD84", "0xFFFFF803C3FED794", "0xFFFFF803C3F27B4D"],
        )

    def test_etl_touch_candidates_preserve_caller_stack_context(self) -> None:
        candidates = research_v36_lib.etl_touch_candidates(
            {
                "etl_path": "evidence/raw/etw-stackwalk/sample.etl",
                "registry_touches": [
                    {
                        "provider_guid_matched": True,
                        "process_name": "System",
                        "operation": "RegQueryValue",
                        "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
                        "value_name": "TimerCheckFlags",
                        "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopReadRegKeyValue"],
                    }
                ],
            }
        )

        self.assertEqual(len(candidates), 1)
        self.assertEqual(candidates[0]["caller_stack"], ["ntoskrnl.exe+0x1F234", "nt!PopReadRegKeyValue"])
        self.assertEqual(candidates[0]["caller_stack_frame_count"], 2)

    def test_normalized_registry_schema_allows_caller_stack(self) -> None:
        event_schema = json.loads(
            (REPO_ROOT / "registry-research-framework" / "schemas" / "normalized-registry-event.schema.json").read_text(
                encoding="utf-8"
            )
        )
        bundle_schema = json.loads(
            (REPO_ROOT / "registry-research-framework" / "schemas" / "normalized-registry-bundle.schema.json").read_text(
                encoding="utf-8"
            )
        )

        self.assertIn("caller_stack", event_schema["properties"])
        self.assertIn("stack_capture", bundle_schema["properties"])

    def test_etw_stackwalk_capture_plan_enables_registry_stackwalk(self) -> None:
        profile = {
            "profile_id": "kernel-registry-stackwalk-v1",
            "tool": "xperf",
            "capture_phase": "runtime",
            "kernel_flags": ["PROC_THREAD", "LOADER", "REGISTRY"],
            "stackwalk_events": ["RegQueryValue", "RegSetValue"],
            "buffer": {"size_kb": 1024, "min_buffers": 64, "max_buffers": 256},
            "default_duration_seconds": 45,
            "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
            "stack_capture": {"expected": True, "source_fields": ["Stack", "CallStack"]},
            "postprocess": {"normalized_bundle_field": "caller_stack"},
        }

        plan = etw_stackwalk_plan.build_capture_plan(
            profile,
            run_id="Timer Check Flags!",
            registry_path=r"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            value_name="TimerCheckFlags",
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(plan["plan_status"], "ready")
        self.assertEqual(plan["run"]["run_id"], "Timer-Check-Flags")
        self.assertTrue(plan["stack_capture"]["expected"])
        self.assertEqual(plan["stack_capture"]["normalized_bundle_field"], "caller_stack")
        self.assertIn("-stackwalk", plan["commands"]["start"])
        self.assertIn("PROC_THREAD+LOADER+REGISTRY", plan["commands"]["start"])
        self.assertIn("RegQueryValue+RegSetValue", plan["commands"]["start"])
        self.assertEqual(
            plan["commands"]["repo_parse"][-1],
            "evidence/raw/etw-stackwalk/Timer-Check-Flags/Timer-Check-Flags.etl",
        )

    def test_etw_stackwalk_capture_plan_blocks_missing_stackwalk_events(self) -> None:
        profile = {
            "profile_id": "broken",
            "tool": "xperf",
            "capture_phase": "runtime",
            "kernel_flags": ["REGISTRY"],
            "stackwalk_events": [],
            "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
        }

        plan = etw_stackwalk_plan.build_capture_plan(
            profile,
            run_id="broken",
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(plan["plan_status"], "blocked")
        self.assertIn("stackwalk_events must include at least one registry event.", plan["errors"])

    def test_etw_stackwalk_capture_plan_check_accepts_ready_plan(self) -> None:
        profile = {
            "profile_id": "kernel-registry-stackwalk-v1",
            "tool": "xperf",
            "capture_phase": "runtime",
            "kernel_flags": ["PROC_THREAD", "LOADER", "REGISTRY"],
            "stackwalk_events": ["RegQueryValue", "RegSetValue"],
            "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
            "stack_capture": {"expected": True, "source_fields": ["Stack"]},
            "postprocess": {"normalized_bundle_field": "caller_stack"},
        }
        plan = etw_stackwalk_plan.build_capture_plan(
            profile,
            run_id="ready",
            generated_utc="2026-04-13T00:00:00Z",
        )

        payload = etw_stackwalk_check.check_plan(plan, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_capture_plan_check_rejects_missing_stack_handoff(self) -> None:
        profile = {
            "profile_id": "kernel-registry-stackwalk-v1",
            "tool": "xperf",
            "capture_phase": "runtime",
            "kernel_flags": ["REGISTRY"],
            "stackwalk_events": ["RegQueryValue"],
            "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
            "stack_capture": {"expected": False, "source_fields": []},
            "postprocess": {"normalized_bundle_field": "raw_text"},
        }
        plan = etw_stackwalk_plan.build_capture_plan(
            profile,
            run_id="bad",
            generated_utc="2026-04-13T00:00:00Z",
        )

        payload = etw_stackwalk_check.check_plan(plan, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(payload["check_status"], "error")
        self.assertIn("missing required stackwalk events: RegSetValue", payload["errors"])
        self.assertIn("stack_capture.expected must be true.", payload["errors"])
        self.assertIn("stack_capture.normalized_bundle_field must be caller_stack.", payload["errors"])

    def test_build_etl_corpus_inventory_marks_placeholder_reason(self) -> None:
        inventory = research_v36_lib.build_etl_corpus_inventory(
            [
                {
                    "path": "evidence/files/example/sample.etl.md",
                    "size": 123,
                    "is_placeholder": True,
                    "estimated_source": "manual-trace",
                    "actual_etl_path": None,
                }
            ],
            parse_results=[],
            parser_name="tracerpt",
            provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
        )

        self.assertEqual(inventory["summary"]["total_artifacts"], 1)
        self.assertEqual(inventory["summary"]["placeholder_only_count"], 1)
        self.assertFalse(inventory["entries"][0]["parsed"])
        self.assertEqual(inventory["entries"][0]["parse_reason"], "placeholder-markdown-only")

    def test_kvm_etw_stackwalk_launcher_help_loads(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-etw-stackwalk-capture.py"),
                "--help",
            ],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("ETW registry stackwalk capture helper", result.stdout)
        self.assertIn("--ingest-to-repo", result.stdout)
        self.assertIn("--profile-id", result.stdout)
        self.assertIn("--list-profiles", result.stdout)
        self.assertIn("--candidate-id", result.stdout)
        self.assertIn("--print-effective-config", result.stdout)

    def test_kvm_etw_stackwalk_launcher_lists_profiles(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-etw-stackwalk-capture.py"),
                "--list-profiles",
            ],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        payload = json.loads(result.stdout)
        self.assertEqual(payload["default_profile"], "kernel-registry-stackwalk-v1")
        profile_ids = {item["profile_id"] for item in payload["profiles"]}
        self.assertIn("execution-required-system-stackwalk-v1", profile_ids)
        self.assertIn("execution-required-audio-stackwalk-v1", profile_ids)

    def test_etw_stackwalk_runner_profile_resolution_uses_profile_defaults(self) -> None:
        config = json.loads(
            (REPO_ROOT / "registry-research-framework" / "config" / "etw-stackwalk-profiles.json").read_text(
                encoding="utf-8"
            )
        )

        resolved = etw_stackwalk_runner.resolve_effective_capture_settings(
            config=config,
            profile_id="execution-required-system-stackwalk-v1",
            run_id=None,
            duration_seconds=None,
            registry_path=None,
            value_name=None,
            guest_output_root=None,
            kernel_flags=None,
            stackwalk_events=None,
            buffer_size_kb=None,
            min_buffers=None,
            max_buffers=None,
        )

        self.assertEqual(resolved["profile_id"], "execution-required-system-stackwalk-v1")
        self.assertEqual(resolved["run_id"], "wave4-allow-system-required-e2e")
        self.assertEqual(resolved["registry_path"], r"HKLM\SYSTEM\CurrentControlSet\Control\Power")
        self.assertEqual(resolved["value_name"], "AllowSystemRequiredPowerRequests")
        self.assertIn("REGISTRY", resolved["kernel_flags"])
        self.assertIn("RegQueryValue", resolved["stackwalk_events"])

    def test_kvm_etw_stackwalk_launcher_prints_effective_config_for_candidate(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-etw-stackwalk-capture.py"),
                "--candidate-id",
                "power.control.allow-system-required-power-requests",
                "--print-effective-config",
            ],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        payload = json.loads(result.stdout)
        effective = payload["effective"]
        self.assertEqual(payload["candidate_id"], "power.control.allow-system-required-power-requests")
        self.assertEqual(effective["profile_id"], "execution-required-system-stackwalk-v1")
        self.assertEqual(effective["run_id"], "wave4-allow-system-required-e2e")
        self.assertEqual(effective["registry_path"], r"HKLM\SYSTEM\CurrentControlSet\Control\Power")
        self.assertEqual(effective["value_name"], "AllowSystemRequiredPowerRequests")

    def test_kvm_etw_stackwalk_launcher_rejects_unmapped_candidate(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(REPO_ROOT / "scripts" / "vm-kvm" / "run-guest-etw-stackwalk-capture.py"),
                "--candidate-id",
                "does.not.exist",
                "--print-effective-config",
            ],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("no ETW stackwalk profile mapping", result.stderr)

    def test_etw_stackwalk_dispatch_batch_builds_mapped_candidate_commands(self) -> None:
        profile_config = {
            "default_profile": "kernel-registry-stackwalk-v1",
            "profiles": [
                {
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "description": "Focused system profile.",
                    "tool": "xperf",
                    "capture_phase": "runtime",
                    "kernel_flags": ["PROC_THREAD", "REGISTRY"],
                    "stackwalk_events": ["RegQueryValue", "RegSetValue"],
                    "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
                    "default_duration_seconds": 45,
                    "default_run_id": "wave4-system",
                    "target_defaults": {
                        "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                        "value_name": "AllowSystemRequiredPowerRequests",
                    },
                    "buffer": {
                        "size_kb": 512,
                        "min_buffers": 32,
                        "max_buffers": 128,
                    },
                    "stack_capture": {
                        "expected": True,
                        "source_fields": ["Stack"],
                    },
                    "postprocess": {
                        "normalized_bundle_field": "caller_stack",
                    },
                }
            ],
        }
        runner_config = {
            "runtime": {
                "power.control.allow-system-required-power-requests": {
                    "script": "registry-research-framework/tools/run-path-aware-runtime-probe.ps1",
                    "etw_stackwalk_profile_id": "execution-required-system-stackwalk-v1",
                    "required_capabilities": ["registry_read", "etw_capture"],
                    "supported_backend_types": ["vm"],
                    "args": ["-CandidateIds", "power.control.allow-system-required-power-requests"],
                }
            }
        }
        queue_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "state": "blocked",
                    "feature_area": "Control Power Requests",
                    "key_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                }
            ]
        }
        gates_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "promotion_state": "blocked",
                    "next_missing_layer": "runtime-trace",
                    "promotion_blockers": ["needs-focused-etw"],
                }
            ]
        }

        payload = etw_stackwalk_dispatch_batch.build_dispatch_batch(
            profile_config=profile_config,
            runner_config=runner_config,
            queue_payload=queue_payload,
            gates_payload=gates_payload,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["batch_status"], "ready")
        self.assertEqual(payload["mapped_candidate_count"], 1)
        self.assertEqual(payload["dispatch_recommended_count"], 1)
        self.assertEqual(payload["profiles_used"], ["execution-required-system-stackwalk-v1"])
        item = payload["items"][0]
        self.assertEqual(item["candidate_id"], "power.control.allow-system-required-power-requests")
        self.assertEqual(item["actionability"], "active")
        self.assertTrue(item["capture_ready"])
        self.assertTrue(item["dispatch_recommended"])
        self.assertIn("--candidate-id power.control.allow-system-required-power-requests", item["dispatch_command"])
        self.assertEqual(item["capture_plan"]["run"]["run_id"], "wave4-system")

    def test_etw_stackwalk_dispatch_batch_marks_intentional_hold_candidates(self) -> None:
        profile_config = {
            "default_profile": "kernel-registry-stackwalk-v1",
            "profiles": [
                {
                    "profile_id": "execution-required-audio-stackwalk-v1",
                    "description": "Focused audio profile.",
                    "tool": "xperf",
                    "capture_phase": "runtime",
                    "kernel_flags": ["PROC_THREAD", "REGISTRY"],
                    "stackwalk_events": ["RegQueryValue", "RegSetValue"],
                    "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
                    "default_duration_seconds": 60,
                    "default_run_id": "wave4-audio",
                    "target_defaults": {
                        "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                        "value_name": "AllowAudioToEnableExecutionRequiredPowerRequests",
                    },
                    "buffer": {
                        "size_kb": 512,
                        "min_buffers": 32,
                        "max_buffers": 128,
                    },
                    "stack_capture": {
                        "expected": True,
                        "source_fields": ["Stack"],
                    },
                    "postprocess": {
                        "normalized_bundle_field": "caller_stack",
                    },
                }
            ],
        }
        runner_config = {
            "runtime": {
                "power.control.allow-audio-to-enable-execution-required-power-requests": {
                    "script": "registry-research-framework/tools/run-path-aware-runtime-probe.ps1",
                    "etw_stackwalk_profile_id": "execution-required-audio-stackwalk-v1",
                }
            }
        }
        queue_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "state": "blocked",
                    "feature_area": "Control Power Requests",
                    "key_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowAudioToEnableExecutionRequiredPowerRequests",
                }
            ]
        }
        gates_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "promotion_state": "blocked",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": [
                        "audio-execution-required-no-current-build-registry-seeding-path",
                        "intentional-hold",
                    ],
                }
            ]
        }

        payload = etw_stackwalk_dispatch_batch.build_dispatch_batch(
            profile_config=profile_config,
            runner_config=runner_config,
            queue_payload=queue_payload,
            gates_payload=gates_payload,
            candidate_ids={"power.control.allow-audio-to-enable-execution-required-power-requests"},
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["mapped_candidate_count"], 1)
        self.assertEqual(payload["dispatch_recommended_count"], 0)
        self.assertEqual(payload["hold_candidate_count"], 1)
        item = payload["items"][0]
        self.assertEqual(item["actionability"], "hold")
        self.assertFalse(item["dispatch_recommended"])
        self.assertIn("Reopen only when a boot/init reader or registry seeding caller pivot becomes available.", item["next_action_hint"])

    def test_etw_stackwalk_dispatch_batch_check_accepts_matching_surface(self) -> None:
        profile_config = {
            "default_profile": "kernel-registry-stackwalk-v1",
            "profiles": [
                {
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "description": "Focused system profile.",
                    "tool": "xperf",
                    "capture_phase": "runtime",
                    "kernel_flags": ["PROC_THREAD", "REGISTRY"],
                    "stackwalk_events": ["RegQueryValue", "RegSetValue"],
                    "default_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
                    "default_duration_seconds": 45,
                    "default_run_id": "wave4-system",
                    "target_defaults": {
                        "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                        "value_name": "AllowSystemRequiredPowerRequests",
                    },
                    "buffer": {"size_kb": 512, "min_buffers": 32, "max_buffers": 128},
                    "stack_capture": {"expected": True, "source_fields": ["Stack"]},
                    "postprocess": {"normalized_bundle_field": "caller_stack"},
                }
            ],
        }
        runner_config = {
            "runtime": {
                "power.control.allow-system-required-power-requests": {
                    "script": "registry-research-framework/tools/run-path-aware-runtime-probe.ps1",
                    "etw_stackwalk_profile_id": "execution-required-system-stackwalk-v1",
                }
            }
        }
        queue_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "state": "blocked",
                    "feature_area": "Control Power Requests",
                    "key_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                }
            ]
        }
        gates_payload = {
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "promotion_state": "blocked",
                    "next_missing_layer": "runtime-trace",
                    "promotion_blockers": ["needs-focused-etw"],
                }
            ]
        }
        surface = etw_stackwalk_dispatch_batch.build_dispatch_batch(
            profile_config=profile_config,
            runner_config=runner_config,
            queue_payload=queue_payload,
            gates_payload=gates_payload,
            generated_utc="2026-04-14T18:00:00Z",
        )

        payload = etw_stackwalk_dispatch_check.compare_batch(
            surface,
            surface,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_dispatch_batch_check_rejects_mismatched_counts(self) -> None:
        good_surface = {
            "schema_version": "1.0",
            "batch_status": "ready",
            "mapped_candidate_count": 1,
            "ready_capture_count": 1,
            "dispatch_recommended_count": 1,
            "active_candidate_count": 1,
            "hold_candidate_count": 0,
            "profiles_used": ["execution-required-system-stackwalk-v1"],
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "queue_state": "blocked",
                    "promotion_state": "blocked",
                    "next_missing_layer": "runtime-trace",
                    "actionability": "active",
                    "capture_ready": True,
                    "dispatch_recommended": True,
                    "promotion_blockers": ["needs-focused-etw"],
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "next_action_hint": "Ready to dispatch when we want another focused ETW caller-stack capture.",
                    "capture_plan": {
                        "run": {
                            "run_id": "wave4-system",
                            "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                        },
                        "stack_capture": {
                            "expected": True,
                            "stackwalk_events": ["RegQueryValue", "RegSetValue"],
                        },
                    },
                }
            ],
        }
        bad_surface = dict(good_surface)
        bad_surface["mapped_candidate_count"] = 2

        payload = etw_stackwalk_dispatch_check.compare_batch(
            bad_surface,
            good_surface,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("mapped_candidate_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_dispatch_run_plan_skips_hold_candidates_by_default(self) -> None:
        payload = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "actionability": "hold",
                    "capture_ready": True,
                    "dispatch_recommended": False,
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "dispatch_command_argv": [
                        "python3",
                        "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
                        "--candidate-id",
                        "power.control.allow-system-required-power-requests",
                    ],
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests",
                    "next_action_hint": "Reopen only when a boot/init reader pivot exists.",
                }
            ]
        }

        plan = etw_stackwalk_dispatch_runner.build_run_plan(payload, generated_utc="2026-04-14T18:00:00Z")

        self.assertEqual(plan["mode"], "dry-run")
        self.assertEqual(plan["selected_job_count"], 0)
        self.assertEqual(plan["skipped_hold_count"], 1)

    def test_etw_stackwalk_dispatch_run_plan_can_include_holds(self) -> None:
        payload = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "actionability": "hold",
                    "capture_ready": True,
                    "dispatch_recommended": False,
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "dispatch_command_argv": [
                        "python3",
                        "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
                        "--candidate-id",
                        "power.control.allow-system-required-power-requests",
                    ],
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests",
                    "next_action_hint": "Reopen only when a boot/init reader pivot exists.",
                }
            ]
        }

        plan = etw_stackwalk_dispatch_runner.build_run_plan(
            payload,
            include_holds=True,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(plan["selected_job_count"], 1)
        self.assertEqual(plan["skipped_hold_count"], 0)
        self.assertEqual(plan["jobs"][0]["candidate_id"], "power.control.allow-system-required-power-requests")

    def test_etw_stackwalk_dispatch_run_executes_selected_jobs(self) -> None:
        payload = {
            "items": [
                {
                    "candidate_id": "example.dispatch",
                    "actionability": "active",
                    "capture_ready": True,
                    "dispatch_recommended": True,
                    "profile_id": "kernel-registry-stackwalk-v1",
                    "dispatch_command_argv": [sys.executable, "-c", "print('dispatch-ok')"],
                }
            ]
        }

        result = etw_stackwalk_dispatch_runner.run_jobs(
            payload,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(result["mode"], "run")
        self.assertEqual(result["selected_job_count"], 1)
        self.assertEqual(result["executed_job_count"], 1)
        self.assertEqual(result["jobs"][0]["exit_code"], 0)
        self.assertIn("dispatch-ok", result["jobs"][0]["stdout"])

    def test_etw_stackwalk_dispatch_run_check_accepts_matching_dry_run(self) -> None:
        batch = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "actionability": "hold",
                    "capture_ready": True,
                    "dispatch_recommended": False,
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "dispatch_command_argv": [
                        "python3",
                        "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
                        "--candidate-id",
                        "power.control.allow-system-required-power-requests",
                    ],
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests",
                    "next_action_hint": "Reopen only when a boot/init reader pivot exists.",
                }
            ]
        }
        run_surface = etw_stackwalk_dispatch_runner.build_run_plan(
            batch,
            generated_utc="2026-04-14T18:00:00Z",
        )

        payload = etw_stackwalk_dispatch_run_check.compare_run_plan(
            run_surface,
            run_surface,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_dispatch_run_check_rejects_mismatched_skipped_hold_count(self) -> None:
        expected = {
            "schema_version": "1.0",
            "mode": "dry-run",
            "source_batch": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "include_holds": False,
            "runner_available": True,
            "selected_job_count": 0,
            "skipped_hold_count": 2,
            "jobs": [],
        }
        surface = dict(expected)
        surface["skipped_hold_count"] = 1

        payload = etw_stackwalk_dispatch_run_check.compare_run_plan(
            surface,
            expected,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("skipped_hold_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_hold_reopen_plan_collects_hold_candidates(self) -> None:
        batch = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "actionability": "hold",
                    "capture_ready": True,
                    "dispatch_recommended": False,
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "capture_plan": {
                        "run": {
                            "run_id": "wave4-allow-system-required-e2e",
                            "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                        }
                    },
                },
                {
                    "candidate_id": "example.active",
                    "actionability": "active",
                    "capture_ready": True,
                    "dispatch_recommended": True,
                },
            ]
        }
        run_payload = {
            "mode": "dry-run",
            "selected_job_count": 0,
            "skipped_hold_count": 1,
        }

        payload = etw_stackwalk_hold_reopen_plan.build_hold_reopen_plan(
            batch,
            run_payload,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["reopen_candidate_count"], 1)
        self.assertEqual(payload["default_selected_job_count"], 0)
        self.assertEqual(payload["default_skipped_hold_count"], 1)
        item = payload["items"][0]
        self.assertEqual(item["candidate_id"], "power.control.allow-system-required-power-requests")
        self.assertTrue(item["default_dispatch_excluded"])
        self.assertIn("registry seeding caller proof", " ".join(item["reopen_prerequisites"]))
        self.assertIn("--include-holds --candidate-id power.control.allow-system-required-power-requests", item["include_holds_plan_command"])

    def test_etw_stackwalk_hold_reopen_plan_ignores_non_hold_candidates(self) -> None:
        payload = etw_stackwalk_hold_reopen_plan.build_hold_reopen_plan(
            {
                "items": [
                    {
                        "candidate_id": "example.active",
                        "actionability": "active",
                        "capture_ready": True,
                    }
                ]
            },
            {"mode": "dry-run", "selected_job_count": 1, "skipped_hold_count": 0},
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["reopen_candidate_count"], 0)
        self.assertEqual(payload["items"], [])

    def test_etw_stackwalk_hold_reopen_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 2,
            "reopen_candidate_count": 1,
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "default_dispatch_excluded": True,
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "run_id": "wave4-allow-system-required-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                }
            ],
        }

        payload = etw_stackwalk_hold_reopen_check.compare_hold_reopen_plan(
            surface,
            surface,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_hold_reopen_check_rejects_mismatched_reopen_count(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 2,
            "reopen_candidate_count": 2,
            "items": [],
        }
        surface = dict(expected)
        surface["reopen_candidate_count"] = 1

        payload = etw_stackwalk_hold_reopen_check.compare_hold_reopen_plan(
            surface,
            expected,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("reopen_candidate_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_hold_reopen_pack_materializes_ready_bundle(self) -> None:
        plan = {
            "schema_version": "1.0",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 2,
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "default_dispatch_excluded": True,
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "run_id": "wave4-allow-system-required-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            plan_path = base / "etw-stackwalk-hold-reopen-plan.json"
            plan_path.write_text(json.dumps(plan), encoding="utf-8")
            plan_path.with_suffix(".md").write_text("# hold reopen plan\n", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.json").write_text("{}", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.md").write_text("# execution manifest\n", encoding="utf-8")

            payload = etw_stackwalk_hold_reopen_pack.materialize_hold_reopen_pack(
                plan,
                plan_path=plan_path,
                output_root=base / "pack",
                summary_path=base / "pack.json",
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-15T10:00:00Z",
            )

            self.assertEqual(payload["pack_status"], "ready")
            self.assertEqual(payload["counts"]["reopen_candidates"], 1)
            self.assertEqual(payload["counts"]["command_files_written"], 1)
            command_path = base / "pack" / "commands" / payload["command_files"][0]
            self.assertTrue(command_path.exists())
            self.assertIn("--include-holds --candidate-id", command_path.read_text(encoding="utf-8"))

    def test_etw_stackwalk_hold_reopen_pack_defaults_to_idle_without_candidates(self) -> None:
        plan = {
            "schema_version": "1.0",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 0,
            "items": [],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            plan_path = base / "etw-stackwalk-hold-reopen-plan.json"
            plan_path.write_text(json.dumps(plan), encoding="utf-8")
            plan_path.with_suffix(".md").write_text("# hold reopen plan\n", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.json").write_text("{}", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.md").write_text("# execution manifest\n", encoding="utf-8")

            payload = etw_stackwalk_hold_reopen_pack.materialize_hold_reopen_pack(
                plan,
                plan_path=plan_path,
                output_root=base / "pack",
                summary_path=base / "pack.json",
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-15T10:00:00Z",
            )

            self.assertEqual(payload["pack_status"], "idle")
            self.assertEqual(payload["counts"]["reopen_candidates"], 0)
            self.assertEqual(payload["counts"]["command_files_written"], 0)

    def test_etw_stackwalk_hold_reopen_pack_check_accepts_matching_surface(self) -> None:
        plan = {
            "schema_version": "1.0",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 1,
            "items": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": ["intentional-hold", "audio-execution-required-no-primary-current-build-doc"],
                    "reopen_prerequisites": [
                        "Land a primary current-build Microsoft document for the exact value semantics.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "default_dispatch_excluded": True,
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run",
                    "run_id": "wave4-allow-audio-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            plan_path = base / "etw-stackwalk-hold-reopen-plan.json"
            summary_path = base / "etw-stackwalk-hold-reopen-pack.json"
            plan_path.write_text(json.dumps(plan), encoding="utf-8")
            plan_path.with_suffix(".md").write_text("# hold reopen plan\n", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.json").write_text("{}", encoding="utf-8")
            (base / "etw-stackwalk-execution-manifest.md").write_text("# execution manifest\n", encoding="utf-8")

            etw_stackwalk_hold_reopen_pack.materialize_hold_reopen_pack(
                plan,
                plan_path=plan_path,
                output_root=base / "pack",
                summary_path=summary_path,
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-15T10:00:00Z",
            )
            summary = json.loads(summary_path.read_text(encoding="utf-8"))

            payload = etw_stackwalk_hold_reopen_pack_check.compare_pack_summary(
                summary,
                etw_stackwalk_hold_reopen_pack.build_pack_plan(plan, plan_path=plan_path),
                generated_utc="2026-04-15T10:00:00Z",
            )
            asset_errors, counts = etw_stackwalk_hold_reopen_pack_check.validate_pack_assets(summary)

            self.assertEqual(payload["check_status"], "ok")
            self.assertEqual(asset_errors, [])
            self.assertGreaterEqual(counts["checked_pack_files"], 1)

    def test_etw_stackwalk_hold_reopen_pack_check_rejects_reopen_candidate_mismatch(self) -> None:
        expected = {
            "source_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "source_plan_markdown_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.md",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_execution_manifest_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.json",
            "source_execution_manifest_markdown_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.md",
            "pack_status": "ready",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 1,
            "operator": {
                "next_action": "Review prerequisites, dry-run the include-holds plan command, then run the include-holds reopen command intentionally.",
                "intentional_reopen_required": True,
            },
            "reopen_candidate_ids": ["example.candidate"],
            "required_repo_paths": ["scripts/vm-kvm/run-guest-etw-stackwalk-capture.py"],
            "items": [
                {
                    "candidate_id": "example.candidate",
                    "feature_area": "Example",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": ["intentional-hold"],
                    "reopen_prerequisites": ["Explicitly reopen the lane before dispatching runtime capture."],
                    "default_dispatch_excluded": True,
                    "effective_config_command": "python3 example.py --print-effective-config",
                    "dispatch_command": "python3 example.py --run",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                    "next_action_hint": "Reopen intentionally.",
                }
            ],
        }
        surface = {
            "schema_version": "1.0",
            "source_plan_path": expected["source_plan_path"],
            "source_plan_markdown_path": expected["source_plan_markdown_path"],
            "source_batch_path": expected["source_batch_path"],
            "source_run_path": expected["source_run_path"],
            "source_execution_manifest_path": expected["source_execution_manifest_path"],
            "source_execution_manifest_markdown_path": expected["source_execution_manifest_markdown_path"],
            "pack_status": "idle",
            "default_run_mode": "dry-run",
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 1,
            "operator": {
                "next_action": expected["operator"]["next_action"],
                "intentional_reopen_required": True,
            },
            "counts": {
                "reopen_candidates": 0,
                "repo_files_copied": 0,
                "command_files_written": 0,
                "manifest_files_written": 0,
                "pack_files_checksummed": 0,
            },
            "reopen_candidate_ids": [],
            "required_repo_paths": expected["required_repo_paths"],
            "copied_repo_paths": [],
            "command_files": [],
            "manifest_files": [],
            "items": [],
            "pack_files": [],
        }

        payload = etw_stackwalk_hold_reopen_pack_check.compare_pack_summary(
            surface,
            expected,
            generated_utc="2026-04-15T10:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("reopen_candidate_ids mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_decision_ledger_defers_candidates_with_unsatisfied_prereqs(self) -> None:
        pack_payload = {
            "pack_status": "ready",
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "effective_config_command": "python3 reopen.py --print-effective-config",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                    "next_action_hint": "Reopen only when a pivot appears.",
                }
            ],
        }

        payload = etw_stackwalk_reopen_decision_ledger.build_reopen_decision_ledger(
            pack_payload,
            generated_utc="2026-04-15T12:00:00Z",
        )

        self.assertEqual(payload["ledger_status"], "deferred")
        self.assertEqual(payload["deferred_candidate_count"], 1)
        entry = payload["entries"][0]
        self.assertEqual(entry["decision_state"], "defer")
        self.assertIn("await-seeding-pivot", entry["decision_reason_codes"])
        self.assertEqual(entry["prerequisite_status"], "unsatisfied")

    def test_etw_stackwalk_reopen_decision_ledger_marks_ready_candidates_for_review(self) -> None:
        pack_payload = {
            "pack_status": "ready",
            "items": [
                {
                    "candidate_id": "example.ready",
                    "feature_area": "Example",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": ["intentional-hold"],
                    "reopen_prerequisites": [],
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "effective_config_command": "python3 reopen.py --print-effective-config",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                    "next_action_hint": "Review manually.",
                }
            ],
        }

        payload = etw_stackwalk_reopen_decision_ledger.build_reopen_decision_ledger(
            pack_payload,
            generated_utc="2026-04-15T12:00:00Z",
        )

        self.assertEqual(payload["ledger_status"], "review-ready")
        self.assertEqual(payload["review_ready_candidate_count"], 1)
        self.assertEqual(payload["entries"][0]["decision_state"], "review-ready")

    def test_etw_stackwalk_reopen_decision_ledger_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_hold_reopen_pack_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-pack.json",
            "pack_status": "ready",
            "ledger_status": "deferred",
            "operator": {
                "next_action": "Keep the ETW lane closed until one of the listed prerequisites lands.",
                "intentional_reopen_required": True,
            },
            "reopen_candidate_count": 1,
            "deferred_candidate_count": 1,
            "review_ready_candidate_count": 0,
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": [
                        "audio-execution-required-no-current-build-registry-seeding-path",
                        "audio-execution-required-no-primary-current-build-doc",
                        "intentional-hold",
                    ],
                    "blocker_count": 3,
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Land a primary current-build Microsoft document for the exact value semantics.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "prerequisite_count": 3,
                    "prerequisite_status": "unsatisfied",
                    "decision_state": "defer",
                    "decision_reason_codes": [
                        "await-seeding-pivot",
                        "await-primary-doc",
                        "explicit-reopen-required",
                    ],
                    "decision_summary": "Keep the lane deferred until the listed prerequisites are satisfied.",
                    "next_review_trigger": "Revisit after a current-build seeding-path pivot and a primary Microsoft doc both land.",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config",
                    "run_id": "wave4-allow-audio-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                }
            ],
        }

        payload = etw_stackwalk_reopen_decision_ledger_check.compare_reopen_decision_ledger(
            surface,
            surface,
            generated_utc="2026-04-15T12:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_decision_ledger_check_rejects_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_hold_reopen_pack_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-pack.json",
            "pack_status": "ready",
            "ledger_status": "deferred",
            "operator": {
                "next_action": "Keep the ETW lane closed until one of the listed prerequisites lands.",
                "intentional_reopen_required": True,
            },
            "reopen_candidate_count": 2,
            "deferred_candidate_count": 2,
            "review_ready_candidate_count": 0,
            "entries": [],
        }
        surface = dict(expected)
        surface["deferred_candidate_count"] = 1

        payload = etw_stackwalk_reopen_decision_ledger_check.compare_reopen_decision_ledger(
            surface,
            expected,
            generated_utc="2026-04-15T12:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("deferred_candidate_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_readiness_scoreboard_marks_blocked_candidates(self) -> None:
        ledger_payload = {
            "ledger_status": "deferred",
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "decision_state": "defer",
                    "decision_reason_codes": [
                        "await-seeding-pivot",
                        "await-primary-doc",
                        "explicit-reopen-required",
                    ],
                    "blocker_count": 3,
                    "prerequisite_count": 3,
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Land a primary current-build Microsoft document for the exact value semantics.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "next_review_trigger": "Revisit after a current-build seeding-path pivot and a primary Microsoft doc both land.",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "wave4-audio",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-audio/wave4-audio.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_readiness_scoreboard.build_reopen_readiness_scoreboard(
            ledger_payload,
            generated_utc="2026-04-15T13:00:00Z",
        )

        self.assertEqual(payload["scoreboard_status"], "blocked")
        self.assertEqual(payload["counts"]["blocked_count"], 1)
        self.assertEqual(payload["entries"][0]["dominant_reason_code"], "await-seeding-pivot")
        self.assertEqual(payload["entries"][0]["unblocker_class"], "evidence-gap")

    def test_etw_stackwalk_reopen_readiness_scoreboard_marks_ready_candidates(self) -> None:
        ledger_payload = {
            "ledger_status": "review-ready",
            "entries": [
                {
                    "candidate_id": "example.ready",
                    "feature_area": "Example",
                    "decision_state": "review-ready",
                    "decision_reason_codes": ["explicit-reopen-required"],
                    "blocker_count": 1,
                    "prerequisite_count": 0,
                    "reopen_prerequisites": [],
                    "next_review_trigger": "Revisit only after we intentionally reopen this ETW lane.",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_readiness_scoreboard.build_reopen_readiness_scoreboard(
            ledger_payload,
            generated_utc="2026-04-15T13:00:00Z",
        )

        self.assertEqual(payload["scoreboard_status"], "review-ready")
        self.assertEqual(payload["counts"]["ready_count"], 1)
        self.assertEqual(payload["entries"][0]["readiness_bucket"], "ready")

    def test_etw_stackwalk_reopen_readiness_scoreboard_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_reopen_decision_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-decision-ledger.json",
            "ledger_status": "deferred",
            "scoreboard_status": "blocked",
            "operator": {
                "next_action": "Track the next unlock prerequisite for the top blocked reopen candidate.",
                "intentional_reopen_required": True,
            },
            "counts": {
                "candidate_count": 1,
                "ready_count": 0,
                "blocked_count": 1,
                "dominant_reason_counts": {"await-seeding-pivot": 1},
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "decision_state": "defer",
                    "readiness_bucket": "blocked",
                    "dominant_reason_code": "await-seeding-pivot",
                    "reason_code_priority": 1,
                    "unblocker_class": "evidence-gap",
                    "blocker_count": 2,
                    "prerequisite_count": 2,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after a current-build boot/init reader or registry seeding caller pivot lands.",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_readiness_scoreboard_check.compare_reopen_readiness_scoreboard(
            surface,
            surface,
            generated_utc="2026-04-15T13:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_readiness_scoreboard_check_rejects_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_reopen_decision_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-decision-ledger.json",
            "ledger_status": "deferred",
            "scoreboard_status": "blocked",
            "operator": {
                "next_action": "Track the next unlock prerequisite for the top blocked reopen candidate.",
                "intentional_reopen_required": True,
            },
            "counts": {
                "candidate_count": 2,
                "ready_count": 0,
                "blocked_count": 2,
                "dominant_reason_counts": {"await-seeding-pivot": 2},
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["counts"] = dict(expected["counts"])
        surface["counts"]["blocked_count"] = 1

        payload = etw_stackwalk_reopen_readiness_scoreboard_check.compare_reopen_readiness_scoreboard(
            surface,
            expected,
            generated_utc="2026-04-15T13:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("counts.blocked_count mismatch" in error for error in payload["errors"]))

    def test_validate_candidate_urls_dedupes_duplicate_url_checks_per_candidate(self) -> None:
        record = {"record_id": "system.memory-disable-paging-executive"}
        full_evidence = {
            "source_enrichment": [
                {
                    "evidence_id": "ms-kernel-trace-control-api",
                    "kind": "official-doc",
                    "title": "Microsoft Learn: Kernel Trace Control API Reference",
                    "url": "https://learn.microsoft.com/en-us/windows-hardware/test/wpt/kernel-trace-control-api-reference",
                },
                {
                    "evidence_id": "validation-proof",
                    "kind": "validation-proof",
                    "title": "Validation proof",
                    "url": "https://learn.microsoft.com/en-us/windows-hardware/test/wpt/kernel-trace-control-api-reference",
                },
            ]
        }

        calls: list[str] = []

        def checker(url: str, timeout: float = 5.0) -> tuple[bool, int | None, str | None]:
            del timeout
            calls.append(url)
            return True, 200, None

        payload = research_v36_lib.validate_candidate_urls(record, full_evidence, checker=checker)

        self.assertEqual(calls, ["https://learn.microsoft.com/en-us/windows-hardware/test/wpt/kernel-trace-control-api-reference"])
        self.assertEqual(payload["checked_url_count"], 2)
        self.assertEqual(payload["reachable_url_count"], 2)
        self.assertEqual(payload["dead_link_count"], 0)
        self.assertEqual(payload["status"], "ok")

    def test_is_url_reachable_sends_browser_compatible_user_agent(self) -> None:
        requests: list[object] = []

        class _Response:
            status = 200

            def __enter__(self) -> "_Response":
                return self

            def __exit__(self, exc_type, exc, tb) -> None:
                del exc_type, exc, tb
                return None

        def fake_urlopen(request: object, timeout: float = 5.0) -> _Response:
            del timeout
            requests.append(request)
            return _Response()

        with unittest.mock.patch.object(research_v36_lib.urllib.request, "urlopen", side_effect=fake_urlopen):
            reachable, status_code, error = research_v36_lib.is_url_reachable("https://example.test/hags")

        self.assertTrue(reachable)
        self.assertEqual(status_code, 200)
        self.assertIsNone(error)
        self.assertEqual(len(requests), 1)
        request = requests[0]
        self.assertEqual(request.get_header("User-agent"), "Mozilla/5.0 (compatible; RegProbeURLValidator/1.0)")
        self.assertEqual(request.get_method(), "HEAD")

    def test_is_url_reachable_falls_back_to_get_after_403(self) -> None:
        requests: list[object] = []
        head_error: _TrackingHttpError | None = None

        class _Response:
            status = 200

            def __enter__(self) -> "_Response":
                return self

            def __exit__(self, exc_type, exc, tb) -> None:
                del exc_type, exc, tb
                return None

        class _TrackingHttpError(research_v36_lib.urllib.error.HTTPError):
            def __init__(self, url: str) -> None:
                super().__init__(url, 403, "Forbidden", hdrs=None, fp=None)
                self.was_closed = False

            def close(self) -> None:
                self.was_closed = True
                super().close()

        def fake_urlopen(request: object, timeout: float = 5.0) -> _Response:
            nonlocal head_error
            del timeout
            requests.append(request)
            if len(requests) == 1:
                head_error = _TrackingHttpError(request.full_url)
                raise head_error
            return _Response()

        with unittest.mock.patch.object(research_v36_lib.urllib.request, "urlopen", side_effect=fake_urlopen):
            reachable, status_code, error = research_v36_lib.is_url_reachable("https://example.test/hags")

        self.assertTrue(reachable)
        self.assertEqual(status_code, 200)
        self.assertIsNone(error)
        self.assertEqual(len(requests), 2)
        self.assertEqual(requests[0].get_method(), "HEAD")
        self.assertEqual(requests[1].get_method(), "GET")
        self.assertIsNotNone(head_error)
        assert head_error is not None
        self.assertTrue(head_error.was_closed)

    def test_etw_stackwalk_reopen_prerequisite_delta_counts_outstanding_reasons(self) -> None:
        ledger_payload = {
            "ledger_status": "deferred",
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "decision_state": "defer",
                    "decision_reason_codes": [
                        "await-seeding-pivot",
                        "await-primary-doc",
                        "explicit-reopen-required",
                    ],
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Land a primary current-build Microsoft document for the exact value semantics.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "next_review_trigger": "Revisit after both evidence lanes land.",
                    "run_id": "wave4-audio",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-audio/wave4-audio.etl",
                },
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "decision_state": "defer",
                    "decision_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "next_review_trigger": "Revisit after the seeding-path pivot lands.",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                },
            ],
        }

        payload = etw_stackwalk_reopen_prerequisite_delta.build_reopen_prerequisite_delta(
            ledger_payload,
            generated_utc="2026-04-15T14:00:00Z",
        )

        self.assertEqual(payload["delta_status"], "blocked")
        self.assertEqual(payload["counts"]["candidate_count"], 2)
        self.assertEqual(payload["counts"]["outstanding_reason_counts"]["await-seeding-pivot"], 2)
        self.assertEqual(payload["counts"]["outstanding_reason_counts"]["await-primary-doc"], 1)
        self.assertEqual(payload["counts"]["outstanding_reason_counts"]["explicit-reopen-required"], 2)
        self.assertEqual(payload["entries"][0]["candidate_id"], "power.control.allow-audio-to-enable-execution-required-power-requests")

    def test_etw_stackwalk_reopen_prerequisite_delta_marks_clear_candidates(self) -> None:
        ledger_payload = {
            "ledger_status": "review-ready",
            "entries": [
                {
                    "candidate_id": "example.ready",
                    "feature_area": "Example",
                    "decision_state": "review-ready",
                    "decision_reason_codes": ["explicit-reopen-required"],
                    "reopen_prerequisites": [],
                    "next_review_trigger": "Revisit only after we intentionally reopen this ETW lane.",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_prerequisite_delta.build_reopen_prerequisite_delta(
            ledger_payload,
            generated_utc="2026-04-15T14:00:00Z",
        )

        self.assertEqual(payload["delta_status"], "clear")
        self.assertEqual(payload["counts"]["blocked_candidate_count"], 0)
        self.assertEqual(payload["counts"]["clear_candidate_count"], 1)
        self.assertEqual(payload["entries"][0]["remaining_to_ready_count"], 0)

    def test_etw_stackwalk_reopen_prerequisite_delta_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_reopen_decision_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-decision-ledger.json",
            "ledger_status": "deferred",
            "delta_status": "blocked",
            "operator": {
                "next_action": "Use the delta entries to land the next outstanding prerequisite before reopening the ETW lane.",
                "intentional_reopen_required": True,
            },
            "counts": {
                "candidate_count": 1,
                "blocked_candidate_count": 1,
                "clear_candidate_count": 0,
                "outstanding_reason_counts": {
                    "await-seeding-pivot": 1,
                    "explicit-reopen-required": 1,
                },
                "unique_prerequisite_count": 2,
            },
            "unique_prerequisites": [
                {
                    "prerequisite": "Explicitly reopen the lane before dispatching runtime capture.",
                    "candidate_ids": ["power.control.allow-system-required-power-requests"],
                    "candidate_count": 1,
                },
                {
                    "prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "candidate_ids": ["power.control.allow-system-required-power-requests"],
                    "candidate_count": 1,
                },
            ],
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "decision_state": "defer",
                    "delta_status": "blocked",
                    "remaining_to_ready_count": 2,
                    "outstanding_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "outstanding_reason_classes": ["evidence-gap", "operator-decision"],
                    "outstanding_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Explicitly reopen the lane before dispatching runtime capture.",
                    ],
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after a current-build boot/init reader or registry seeding caller pivot lands.",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_prerequisite_delta_check.compare_reopen_prerequisite_delta(
            surface,
            surface,
            generated_utc="2026-04-15T14:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_prerequisite_delta_check_rejects_reason_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_reopen_decision_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-decision-ledger.json",
            "ledger_status": "deferred",
            "delta_status": "blocked",
            "operator": {
                "next_action": "Use the delta entries to land the next outstanding prerequisite before reopening the ETW lane.",
                "intentional_reopen_required": True,
            },
            "counts": {
                "candidate_count": 1,
                "blocked_candidate_count": 1,
                "clear_candidate_count": 0,
                "outstanding_reason_counts": {"await-seeding-pivot": 1},
                "unique_prerequisite_count": 1,
            },
            "unique_prerequisites": [],
            "entries": [],
        }
        surface = dict(expected)
        surface["counts"] = dict(expected["counts"])
        surface["counts"]["outstanding_reason_counts"] = {"await-seeding-pivot": 2}

        payload = etw_stackwalk_reopen_prerequisite_delta_check.compare_reopen_prerequisite_delta(
            surface,
            expected,
            generated_utc="2026-04-15T14:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("counts.outstanding_reason_counts mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_operator_brief_marks_blocked_candidates_do_not_run(self) -> None:
        delta_payload = {
            "delta_status": "blocked",
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "delta_status": "blocked",
                    "remaining_to_ready_count": 2,
                    "outstanding_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after the seeding-path pivot lands.",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }
        pack_payload = {
            "pack_status": "ready",
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                }
            ],
        }

        payload = etw_stackwalk_reopen_operator_brief.build_reopen_operator_brief(
            delta_payload,
            pack_payload,
            generated_utc="2026-04-15T15:00:00Z",
        )

        self.assertEqual(payload["brief_status"], "blocked")
        self.assertEqual(payload["operator"]["blocker"], "reopen-prerequisites-blocked")
        self.assertEqual(payload["entries"][0]["operator_posture"], "do-not-run")

    def test_etw_stackwalk_reopen_operator_brief_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_reopen_prerequisite_delta_path": "registry-research-framework/audit/etw-stackwalk-reopen-prerequisite-delta.json",
            "source_hold_reopen_pack_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-pack.json",
            "delta_status": "blocked",
            "pack_status": "ready",
            "brief_status": "blocked",
            "operator": {
                "blocker": "reopen-prerequisites-blocked",
                "next_action": "Do not run the include-holds commands yet; land the next unlock prerequisite first.",
            },
            "counts": {
                "candidate_count": 1,
                "blocked_candidates": 1,
                "review_ready_candidates": 0,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "brief_status": "blocked",
                    "operator_posture": "do-not-run",
                    "operator_blocker": "outstanding-prerequisites",
                    "remaining_to_ready_count": 2,
                    "outstanding_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after the seeding-path pivot lands.",
                    "promotion_blockers": [
                        "intentional-hold",
                        "system-execution-required-no-current-build-registry-seeding-path",
                    ],
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_operator_brief_check.compare_reopen_operator_brief(
            surface,
            surface,
            generated_utc="2026-04-15T15:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_operator_brief_check_rejects_brief_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_reopen_prerequisite_delta_path": "registry-research-framework/audit/etw-stackwalk-reopen-prerequisite-delta.json",
            "source_hold_reopen_pack_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-pack.json",
            "delta_status": "blocked",
            "pack_status": "ready",
            "brief_status": "blocked",
            "operator": {
                "blocker": "reopen-prerequisites-blocked",
                "next_action": "Do not run the include-holds commands yet; land the next unlock prerequisite first.",
            },
            "counts": {
                "candidate_count": 1,
                "blocked_candidates": 1,
                "review_ready_candidates": 0,
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["counts"] = dict(expected["counts"])
        surface["counts"]["blocked_candidates"] = 2

        payload = etw_stackwalk_reopen_operator_brief_check.compare_reopen_operator_brief(
            surface,
            expected,
            generated_utc="2026-04-15T15:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("counts.blocked_candidates mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_journal_marks_blocked_candidates_deferred(self) -> None:
        brief_payload = {
            "brief_status": "blocked",
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "brief_status": "blocked",
                    "operator_posture": "do-not-run",
                    "operator_blocker": "outstanding-prerequisites",
                    "remaining_to_ready_count": 2,
                    "outstanding_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after the seeding-path pivot lands.",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_journal.build_reopen_journal(
            brief_payload,
            generated_utc="2026-04-15T16:00:00Z",
        )

        self.assertEqual(payload["journal_status"], "deferred")
        self.assertEqual(payload["operator"]["blocker"], "acknowledge-deferred-holds")
        self.assertEqual(payload["entries"][0]["journal_state"], "deferred")
        self.assertEqual(payload["entries"][0]["recommended_disposition"], "keep-closed")

    def test_etw_stackwalk_reopen_journal_marks_review_ready_candidates(self) -> None:
        brief_payload = {
            "brief_status": "review-ready",
            "entries": [
                {
                    "candidate_id": "example.ready",
                    "feature_area": "Example",
                    "brief_status": "review-ready",
                    "operator_posture": "review-before-run",
                    "operator_blocker": "explicit-review-required",
                    "remaining_to_ready_count": 0,
                    "outstanding_reason_codes": ["explicit-reopen-required"],
                    "next_unlock_prerequisite": None,
                    "next_review_trigger": "Review before reopening.",
                    "next_action_hint": "Review first.",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_journal.build_reopen_journal(
            brief_payload,
            generated_utc="2026-04-15T16:00:00Z",
        )

        self.assertEqual(payload["journal_status"], "review-pending")
        self.assertEqual(payload["counts"]["review_pending_count"], 1)
        self.assertEqual(payload["entries"][0]["journal_state"], "review-pending")

    def test_etw_stackwalk_reopen_journal_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_reopen_operator_brief_path": "registry-research-framework/audit/etw-stackwalk-reopen-operator-brief.json",
            "brief_status": "blocked",
            "journal_status": "deferred",
            "operator": {
                "blocker": "acknowledge-deferred-holds",
                "next_action": "Keep the blocked lanes deferred until their prerequisites land.",
            },
            "counts": {
                "candidate_count": 1,
                "deferred_count": 1,
                "review_pending_count": 0,
                "ack_required_count": 1,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "journal_state": "deferred",
                    "recommended_disposition": "keep-closed",
                    "operator_ack_required": True,
                    "operator_blocker": "outstanding-prerequisites",
                    "operator_posture": "do-not-run",
                    "remaining_to_ready_count": 2,
                    "outstanding_reason_codes": [
                        "await-seeding-pivot",
                        "explicit-reopen-required",
                    ],
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_review_trigger": "Revisit after the seeding-path pivot lands.",
                    "next_action": "Do not run the include-holds commands yet.",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "include_holds_plan_command": "python3 reopen.py --plan",
                    "include_holds_run_command": "python3 reopen.py --run",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }

        payload = etw_stackwalk_reopen_journal_check.compare_reopen_journal(
            surface,
            surface,
            generated_utc="2026-04-15T16:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_journal_check_rejects_ack_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_reopen_operator_brief_path": "registry-research-framework/audit/etw-stackwalk-reopen-operator-brief.json",
            "brief_status": "blocked",
            "journal_status": "deferred",
            "operator": {
                "blocker": "acknowledge-deferred-holds",
                "next_action": "Keep the blocked lanes deferred until their prerequisites land.",
            },
            "counts": {
                "candidate_count": 1,
                "deferred_count": 1,
                "review_pending_count": 0,
                "ack_required_count": 1,
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["counts"] = dict(expected["counts"])
        surface["counts"]["ack_required_count"] = 2

        payload = etw_stackwalk_reopen_journal_check.compare_reopen_journal(
            surface,
            expected,
            generated_utc="2026-04-15T16:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("counts.ack_required_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_snapshot_tracks_top_deferred_candidate(self) -> None:
        journal_payload = {
            "journal_status": "deferred",
            "operator": {
                "blocker": "acknowledge-deferred-holds",
                "next_action": "Keep the blocked lanes deferred until their prerequisites land.",
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "recommended_disposition": "keep-closed",
                    "remaining_to_ready_count": 3,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_action": "Do not run the include-holds commands yet.",
                    "run_id": "wave4-audio",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-audio/wave4-audio.etl",
                },
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "recommended_disposition": "keep-closed",
                    "remaining_to_ready_count": 2,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_action": "Do not run the include-holds commands yet.",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                },
            ],
        }

        payload = etw_stackwalk_reopen_snapshot.build_reopen_snapshot(
            journal_payload,
            generated_utc="2026-04-15T17:00:00Z",
        )

        self.assertEqual(payload["snapshot_status"], "deferred")
        self.assertEqual(payload["counts"]["candidate_count"], 2)
        self.assertEqual(payload["focus"]["top_deferred_candidate"], "power.control.allow-audio-to-enable-execution-required-power-requests")
        self.assertTrue(payload["snapshot_id"])

    def test_etw_stackwalk_reopen_snapshot_handles_idle_state(self) -> None:
        journal_payload = {
            "journal_status": "idle",
            "operator": {
                "blocker": "no-reopen-candidates",
                "next_action": "No ETW reopen journal entries are currently tracked.",
            },
            "entries": [],
        }

        payload = etw_stackwalk_reopen_snapshot.build_reopen_snapshot(
            journal_payload,
            generated_utc="2026-04-15T17:00:00Z",
        )

        self.assertEqual(payload["snapshot_status"], "idle")
        self.assertEqual(payload["counts"]["candidate_count"], 0)
        self.assertEqual(payload["focus"]["top_deferred_candidate"], None)

    def test_etw_stackwalk_reopen_snapshot_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_reopen_journal_path": "registry-research-framework/audit/etw-stackwalk-reopen-journal.json",
            "snapshot_scope": "current-reopen-state",
            "snapshot_status": "deferred",
            "snapshot_id": "abc123def456",
            "operator": {
                "blocker": "acknowledge-deferred-holds",
                "next_action": "Keep the blocked lanes deferred until their prerequisites land.",
            },
            "counts": {
                "candidate_count": 1,
                "deferred_count": 1,
                "review_pending_count": 0,
                "ack_required_count": 1,
            },
            "focus": {
                "top_deferred_candidate": "power.control.allow-system-required-power-requests",
                "top_review_pending_candidate": None,
                "top_next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
            },
            "history_markers": {
                "state_signature": [
                    "power.control.allow-system-required-power-requests:deferred:2",
                ],
                "blocker_signature": [
                    "power.control.allow-system-required-power-requests:outstanding-prerequisites",
                ],
                "run_id_signature": ["wave4-system"],
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "recommended_disposition": "keep-closed",
                    "remaining_to_ready_count": 2,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                    "next_action": "Do not run the include-holds commands yet.",
                    "run_id": "wave4-system",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-system/wave4-system.etl",
                }
            ],
        }
        expected = dict(surface)

        payload = etw_stackwalk_reopen_snapshot_check.compare_reopen_snapshot(
            surface,
            expected,
            generated_utc="2026-04-15T17:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_snapshot_check_rejects_snapshot_id_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_reopen_journal_path": "registry-research-framework/audit/etw-stackwalk-reopen-journal.json",
            "snapshot_scope": "current-reopen-state",
            "snapshot_status": "deferred",
            "snapshot_id": "abc123def456",
            "operator": {
                "blocker": "acknowledge-deferred-holds",
                "next_action": "Keep the blocked lanes deferred until their prerequisites land.",
            },
            "counts": {
                "candidate_count": 0,
                "deferred_count": 0,
                "review_pending_count": 0,
                "ack_required_count": 0,
            },
            "focus": {
                "top_deferred_candidate": None,
                "top_review_pending_candidate": None,
                "top_next_unlock_prerequisite": None,
            },
            "history_markers": {
                "state_signature": [],
                "blocker_signature": [],
                "run_id_signature": [],
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["snapshot_id"] = "deadbeef0000"

        payload = etw_stackwalk_reopen_snapshot_check.compare_reopen_snapshot(
            surface,
            expected,
            generated_utc="2026-04-15T17:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("snapshot_id mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_transition_summary_defaults_to_baseline_without_previous(self) -> None:
        current_snapshot = {
            "snapshot_status": "deferred",
            "snapshot_id": "ec5b6c91b4e6",
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "remaining_to_ready_count": 2,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                }
            ],
        }

        payload = etw_stackwalk_reopen_transition_summary.build_reopen_transition_summary(
            current_snapshot,
            None,
            generated_utc="2026-04-15T18:00:00Z",
        )

        self.assertEqual(payload["transition_status"], "baseline")
        self.assertEqual(payload["counts"]["changed_candidate_count"], 1)
        self.assertEqual(payload["counts"]["added_candidate_count"], 1)
        self.assertEqual(payload["focus"]["top_changed_candidate"], "power.control.allow-system-required-power-requests")

    def test_etw_stackwalk_reopen_transition_summary_marks_unchanged_snapshot_ids(self) -> None:
        current_snapshot = {
            "snapshot_status": "deferred",
            "snapshot_id": "same123",
            "entries": [
                {
                    "candidate_id": "example.candidate",
                    "feature_area": "Example",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "remaining_to_ready_count": 1,
                    "next_unlock_prerequisite": "Land the prerequisite.",
                }
            ],
        }
        previous_snapshot = {
            "snapshot_status": "deferred",
            "snapshot_id": "same123",
            "entries": [
                {
                    "candidate_id": "example.candidate",
                    "feature_area": "Example",
                    "journal_state": "deferred",
                    "operator_blocker": "outstanding-prerequisites",
                    "remaining_to_ready_count": 1,
                    "next_unlock_prerequisite": "Land the prerequisite.",
                }
            ],
        }

        payload = etw_stackwalk_reopen_transition_summary.build_reopen_transition_summary(
            current_snapshot,
            previous_snapshot,
            generated_utc="2026-04-15T18:00:00Z",
        )

        self.assertEqual(payload["transition_status"], "unchanged")
        self.assertEqual(payload["counts"]["changed_candidate_count"], 0)
        self.assertEqual(payload["focus"]["top_changed_candidate"], None)

    def test_etw_stackwalk_reopen_transition_summary_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_previous_snapshot_path": None,
            "transition_status": "baseline",
            "operator": {
                "blocker": "no-previous-snapshot",
                "next_action": "Treat the current reopen snapshot as the baseline until a previous snapshot is retained.",
            },
            "counts": {
                "current_candidate_count": 1,
                "previous_candidate_count": 0,
                "changed_candidate_count": 1,
                "added_candidate_count": 1,
                "removed_candidate_count": 0,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": None,
                "current_snapshot_status": "deferred",
                "previous_snapshot_status": None,
                "top_changed_candidate": "power.control.allow-system-required-power-requests",
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "transition_type": "added",
                    "current_journal_state": "deferred",
                    "previous_journal_state": None,
                    "current_operator_blocker": "outstanding-prerequisites",
                    "previous_operator_blocker": None,
                    "current_remaining_to_ready_count": 2,
                    "previous_remaining_to_ready_count": None,
                    "current_snapshot_id": "ec5b6c91b4e6",
                    "previous_snapshot_id": None,
                    "next_unlock_prerequisite": "Land a current-build boot/init reader or registry seeding caller proof.",
                }
            ],
        }
        expected = dict(surface)

        payload = etw_stackwalk_reopen_transition_summary_check.compare_reopen_transition_summary(
            surface,
            expected,
            generated_utc="2026-04-15T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_reopen_transition_summary_check_rejects_changed_count_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_previous_snapshot_path": None,
            "transition_status": "baseline",
            "operator": {
                "blocker": "no-previous-snapshot",
                "next_action": "Treat the current reopen snapshot as the baseline until a previous snapshot is retained.",
            },
            "counts": {
                "current_candidate_count": 1,
                "previous_candidate_count": 0,
                "changed_candidate_count": 1,
                "added_candidate_count": 1,
                "removed_candidate_count": 0,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": None,
                "current_snapshot_status": "deferred",
                "previous_snapshot_status": None,
                "top_changed_candidate": "power.control.allow-system-required-power-requests",
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["counts"] = dict(expected["counts"])
        surface["counts"]["changed_candidate_count"] = 0

        payload = etw_stackwalk_reopen_transition_summary_check.compare_reopen_transition_summary(
            surface,
            expected,
            generated_utc="2026-04-15T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("counts.changed_candidate_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_baseline_archive_materializes_pack(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            snapshot_path = temp_root / "etw-stackwalk-reopen-snapshot.json"
            transition_path = temp_root / "etw-stackwalk-reopen-transition-summary.json"
            snapshot_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "snapshot_status": "deferred",
                        "snapshot_id": "ec5b6c91b4e6",
                        "counts": {"candidate_count": 2},
                    }
                ),
                encoding="utf-8",
            )
            snapshot_path.with_suffix(".md").write_text("# snapshot\n", encoding="utf-8")
            transition_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "transition_status": "baseline",
                    }
                ),
                encoding="utf-8",
            )
            transition_path.with_suffix(".md").write_text("# transition\n", encoding="utf-8")
            output_root = temp_root / "archive"
            summary_path = temp_root / "archive.json"
            markdown_path = temp_root / "archive.md"
            archive_path = temp_root / "archive.zip"

            payload = etw_stackwalk_reopen_baseline_archive.materialize_baseline_archive(
                etw_stackwalk_reopen_baseline_archive.load_json(snapshot_path),
                etw_stackwalk_reopen_baseline_archive.load_json(transition_path),
                snapshot_path=snapshot_path,
                transition_path=transition_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=markdown_path,
                archive_path=archive_path,
                generated_utc="2026-04-15T19:00:00Z",
            )

            self.assertEqual(payload["archive_status"], "baseline-ready")
            self.assertEqual(payload["retained_snapshot_id"], "ec5b6c91b4e6")
            self.assertTrue((output_root / "manifests" / "etw-stackwalk-reopen-snapshot.json").exists())
            self.assertTrue(archive_path.exists())

    def test_etw_stackwalk_reopen_baseline_archive_check_accepts_matching_surface(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            snapshot_path = temp_root / "etw-stackwalk-reopen-snapshot.json"
            transition_path = temp_root / "etw-stackwalk-reopen-transition-summary.json"
            snapshot_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "snapshot_status": "deferred",
                        "snapshot_id": "ec5b6c91b4e6",
                        "counts": {"candidate_count": 2},
                    }
                ),
                encoding="utf-8",
            )
            snapshot_path.with_suffix(".md").write_text("# snapshot\n", encoding="utf-8")
            transition_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "transition_status": "baseline",
                    }
                ),
                encoding="utf-8",
            )
            transition_path.with_suffix(".md").write_text("# transition\n", encoding="utf-8")
            output_root = temp_root / "archive"
            summary_path = temp_root / "archive.json"
            markdown_path = temp_root / "archive.md"
            archive_path = temp_root / "archive.zip"

            etw_stackwalk_reopen_baseline_archive.materialize_baseline_archive(
                etw_stackwalk_reopen_baseline_archive.load_json(snapshot_path),
                etw_stackwalk_reopen_baseline_archive.load_json(transition_path),
                snapshot_path=snapshot_path,
                transition_path=transition_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=markdown_path,
                archive_path=archive_path,
                generated_utc="2026-04-15T19:00:00Z",
            )
            surface = json.loads(summary_path.read_text(encoding="utf-8"))
            expected = etw_stackwalk_reopen_baseline_archive.build_archive_plan(
                etw_stackwalk_reopen_baseline_archive.load_json(snapshot_path),
                etw_stackwalk_reopen_baseline_archive.load_json(transition_path),
                snapshot_path=snapshot_path,
                transition_path=transition_path,
            )
            payload = etw_stackwalk_reopen_baseline_archive_check.compare_archive_summary(
                surface,
                expected,
                generated_utc="2026-04-15T19:00:00Z",
            )
            asset_errors, _ = etw_stackwalk_reopen_baseline_archive_check.validate_pack_assets(surface)

            self.assertEqual(payload["check_status"], "ok")
            self.assertEqual(asset_errors, [])

    def test_etw_stackwalk_reopen_baseline_archive_check_rejects_snapshot_id_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_current_snapshot_markdown_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.md",
            "source_transition_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.json",
            "source_transition_summary_markdown_path": "registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.md",
            "archive_status": "baseline-ready",
            "transition_status": "baseline",
            "retained_snapshot_id": "ec5b6c91b4e6",
            "operator": {
                "blocker": "retain-baseline-for-next-diff",
                "next_action": "Retain this snapshot as the next previous baseline before expecting diff-driven transition summaries.",
            },
            "promote_previous_snapshot_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json",
            "promote_previous_snapshot_markdown_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
            "archive_candidate_count": 2,
            "focus_snapshot_id": "ec5b6c91b4e6",
        }
        surface = dict(expected)
        surface["retained_snapshot_id"] = "deadbeef"

        payload = etw_stackwalk_reopen_baseline_archive_check.compare_archive_summary(
            surface,
            expected,
            generated_utc="2026-04-15T19:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("retained_snapshot_id mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_history_archive_materializes_seed_required_pack(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            current_path = temp_root / "etw-stackwalk-reopen-snapshot.json"
            transition_path = temp_root / "etw-stackwalk-reopen-transition-summary.json"
            baseline_summary_path = temp_root / "etw-stackwalk-reopen-baseline-archive.json"
            baseline_root = temp_root / "etw-stackwalk-reopen-baseline-archive"
            (baseline_root / "manifests").mkdir(parents=True, exist_ok=True)
            current_payload = {
                "schema_version": "1.0",
                "snapshot_status": "deferred",
                "snapshot_id": "ec5b6c91b4e6",
                "counts": {"candidate_count": 2},
            }
            current_path.write_text(json.dumps(current_payload), encoding="utf-8")
            current_path.with_suffix(".md").write_text("# current\n", encoding="utf-8")
            transition_path.write_text(json.dumps({"schema_version": "1.0", "transition_status": "baseline"}), encoding="utf-8")
            transition_path.with_suffix(".md").write_text("# transition\n", encoding="utf-8")
            baseline_summary_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "retained_snapshot_id": "ec5b6c91b4e6",
                        "output_root": str(baseline_root),
                    }
                ),
                encoding="utf-8",
            )
            baseline_summary_path.with_suffix(".md").write_text("# baseline\n", encoding="utf-8")
            (baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.json").write_text(
                json.dumps(current_payload),
                encoding="utf-8",
            )
            (baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.md").write_text("# seed\n", encoding="utf-8")

            output_root = temp_root / "history"
            summary_path = temp_root / "history.json"
            markdown_path = temp_root / "history.md"
            archive_path = temp_root / "history.zip"

            payload = etw_stackwalk_reopen_history_archive.materialize_history_archive(
                etw_stackwalk_reopen_history_archive.load_json(current_path),
                None,
                etw_stackwalk_reopen_history_archive.load_json(transition_path),
                etw_stackwalk_reopen_history_archive.load_json(baseline_summary_path),
                current_snapshot_path=current_path,
                previous_snapshot_path=temp_root / "missing.previous.json",
                transition_summary_path=transition_path,
                baseline_archive_summary_path=baseline_summary_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=markdown_path,
                archive_path=archive_path,
                generated_utc="2026-04-15T20:00:00Z",
            )

            self.assertEqual(payload["history_status"], "seed-required")
            self.assertEqual(payload["history_seed_source"], "baseline-archive")
            self.assertEqual(payload["current_snapshot_id"], "ec5b6c91b4e6")
            self.assertTrue((output_root / "seed" / "retained-baseline" / "etw-stackwalk-reopen-snapshot.json").exists())
            self.assertTrue(archive_path.exists())

    def test_etw_stackwalk_reopen_history_archive_check_accepts_matching_surface(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            current_path = temp_root / "etw-stackwalk-reopen-snapshot.json"
            previous_path = temp_root / "etw-stackwalk-reopen-snapshot.previous.json"
            transition_path = temp_root / "etw-stackwalk-reopen-transition-summary.json"
            baseline_summary_path = temp_root / "etw-stackwalk-reopen-baseline-archive.json"
            baseline_root = temp_root / "etw-stackwalk-reopen-baseline-archive"
            (baseline_root / "manifests").mkdir(parents=True, exist_ok=True)
            current_payload = {
                "schema_version": "1.0",
                "snapshot_status": "deferred",
                "snapshot_id": "ec5b6c91b4e6",
                "counts": {"candidate_count": 2},
            }
            previous_payload = {
                "schema_version": "1.0",
                "snapshot_status": "deferred",
                "snapshot_id": "ab12cd34ef56",
                "counts": {"candidate_count": 2},
            }
            current_path.write_text(json.dumps(current_payload), encoding="utf-8")
            current_path.with_suffix(".md").write_text("# current\n", encoding="utf-8")
            previous_path.write_text(json.dumps(previous_payload), encoding="utf-8")
            previous_path.with_suffix(".md").write_text("# previous\n", encoding="utf-8")
            transition_path.write_text(json.dumps({"schema_version": "1.0", "transition_status": "changed"}), encoding="utf-8")
            transition_path.with_suffix(".md").write_text("# transition\n", encoding="utf-8")
            baseline_summary_path.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0",
                        "retained_snapshot_id": "ec5b6c91b4e6",
                        "output_root": str(baseline_root),
                    }
                ),
                encoding="utf-8",
            )
            baseline_summary_path.with_suffix(".md").write_text("# baseline\n", encoding="utf-8")
            (baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.json").write_text(
                json.dumps(current_payload),
                encoding="utf-8",
            )
            (baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.md").write_text("# seed\n", encoding="utf-8")

            output_root = temp_root / "history"
            summary_path = temp_root / "history.json"
            markdown_path = temp_root / "history.md"
            archive_path = temp_root / "history.zip"

            etw_stackwalk_reopen_history_archive.materialize_history_archive(
                etw_stackwalk_reopen_history_archive.load_json(current_path),
                etw_stackwalk_reopen_history_archive.load_json(previous_path),
                etw_stackwalk_reopen_history_archive.load_json(transition_path),
                etw_stackwalk_reopen_history_archive.load_json(baseline_summary_path),
                current_snapshot_path=current_path,
                previous_snapshot_path=previous_path,
                transition_summary_path=transition_path,
                baseline_archive_summary_path=baseline_summary_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=markdown_path,
                archive_path=archive_path,
                generated_utc="2026-04-15T20:00:00Z",
            )
            surface = json.loads(summary_path.read_text(encoding="utf-8"))
            expected = etw_stackwalk_reopen_history_archive.build_history_plan(
                etw_stackwalk_reopen_history_archive.load_json(current_path),
                etw_stackwalk_reopen_history_archive.load_json(previous_path),
                etw_stackwalk_reopen_history_archive.load_json(transition_path),
                etw_stackwalk_reopen_history_archive.load_json(baseline_summary_path),
                current_snapshot_path=current_path,
                previous_snapshot_path=previous_path,
                transition_summary_path=transition_path,
                baseline_archive_summary_path=baseline_summary_path,
            )
            payload = etw_stackwalk_reopen_history_archive_check.compare_history_summary(
                surface,
                expected,
                generated_utc="2026-04-15T20:00:00Z",
            )
            asset_errors, _ = etw_stackwalk_reopen_history_archive_check.validate_pack_assets(surface)

            self.assertEqual(payload["check_status"], "ok")
            self.assertEqual(asset_errors, [])

    def test_etw_stackwalk_reopen_history_archive_check_rejects_status_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_previous_snapshot_path": None,
            "source_transition_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.json",
            "source_transition_summary_markdown_path": "registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.md",
            "source_baseline_archive_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.json",
            "source_baseline_archive_markdown_path": "registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive.md",
            "history_status": "seed-required",
            "history_seed_source": "baseline-archive",
            "transition_status": "baseline",
            "current_snapshot_id": "ec5b6c91b4e6",
            "previous_snapshot_id": None,
            "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            "operator": {
                "blocker": "seed-previous-snapshot-from-baseline-archive",
                "next_action": "Promote the retained baseline snapshot into snapshot.previous before expecting history-driven reopen diffs.",
            },
            "seed_previous_snapshot_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json",
            "seed_previous_snapshot_markdown_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md",
            "persist_current_snapshot_history_command": "mkdir -p registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6 && cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6/etw-stackwalk-reopen-snapshot.json && cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6/etw-stackwalk-reopen-snapshot.md",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
            "history_candidate_count": 2,
            "focus_snapshot_id": "ec5b6c91b4e6",
        }
        surface = dict(expected)
        surface["history_status"] = "rotation-ready"

        payload = etw_stackwalk_reopen_history_archive_check.compare_history_summary(
            surface,
            expected,
            generated_utc="2026-04-15T20:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("history_status mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_rotation_ledger_defaults_to_seed_pending_without_previous(self) -> None:
        current = {
            "schema_version": "1.0",
            "snapshot_id": "ec5b6c91b4e6",
            "counts": {"candidate_count": 2},
        }
        transition = {
            "schema_version": "1.0",
            "transition_status": "baseline",
            "counts": {"changed_candidate_count": 2},
            "focus": {"top_changed_candidate": "power.control.allow-system-required-power-requests"},
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Power",
                    "transition_type": "added",
                    "current_journal_state": "deferred",
                    "previous_journal_state": None,
                    "current_operator_blocker": "await-seeding-pivot",
                    "previous_operator_blocker": None,
                    "next_unlock_prerequisite": "await-seeding-pivot",
                },
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Power",
                    "transition_type": "added",
                    "current_journal_state": "deferred",
                    "previous_journal_state": None,
                    "current_operator_blocker": "await-primary-doc",
                    "previous_operator_blocker": None,
                    "next_unlock_prerequisite": "await-primary-doc",
                },
            ],
        }
        history = {
            "schema_version": "1.0",
            "history_status": "seed-required",
            "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            "seed_previous_snapshot_command": "cp retained current.previous",
            "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
            "persist_current_snapshot_history_command": "mkdir -p history-store",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        }

        payload = etw_stackwalk_reopen_rotation_ledger.build_rotation_ledger(
            current,
            None,
            transition,
            history,
            current_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json"),
            previous_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"),
            transition_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.json"),
            history_archive_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json"),
            generated_utc="2026-04-15T21:00:00Z",
        )

        self.assertEqual(payload["rotation_status"], "seed-pending")
        self.assertEqual(payload["rotation_mode"], "seed-from-baseline")
        self.assertEqual(payload["counts"]["rotation_candidate_count"], 2)
        self.assertEqual(payload["entries"][0]["rotation_disposition"], "seed-baseline")

    def test_etw_stackwalk_reopen_seed_receipt_pending_without_previous(self) -> None:
        current = {
            "schema_version": "1.0",
            "snapshot_id": "ec5b6c91b4e6",
            "counts": {"candidate_count": 2},
        }
        history = {
            "schema_version": "1.0",
            "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            "seed_previous_snapshot_command": "cp retained current.previous",
            "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        }
        payload = etw_stackwalk_reopen_seed_receipt.build_seed_receipt(
            current,
            None,
            history,
            current_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json"),
            previous_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"),
            history_archive_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json"),
            generated_utc="2026-04-15T21:30:00Z",
        )

        self.assertEqual(payload["receipt_status"], "pending")
        self.assertEqual(payload["receipt_mode"], "await-seed")
        self.assertFalse(payload["verification"]["previous_snapshot_present"])

    def test_etw_stackwalk_reopen_seed_receipt_check_accepts_matching_surface(self) -> None:
        current = {
            "schema_version": "1.0",
            "snapshot_id": "ec5b6c91b4e6",
            "counts": {"candidate_count": 2},
        }
        previous = {
            "schema_version": "1.0",
            "snapshot_id": "ec5b6c91b4e6",
            "counts": {"candidate_count": 2},
        }
        history = {
            "schema_version": "1.0",
            "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            "seed_previous_snapshot_command": "cp retained current.previous",
            "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        }
        expected = etw_stackwalk_reopen_seed_receipt.build_seed_receipt(
            current,
            previous,
            history,
            current_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json"),
            previous_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"),
            history_archive_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json"),
            generated_utc="2026-04-15T21:30:00Z",
        )

        payload = etw_stackwalk_reopen_seed_receipt_check.compare_seed_receipt(
            json.loads(json.dumps(expected)),
            expected,
            generated_utc="2026-04-15T21:30:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")

    def test_etw_stackwalk_reopen_seed_receipt_check_rejects_receipt_status_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_previous_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json",
            "source_history_archive_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json",
            "receipt_status": "seeded-retained-baseline",
            "receipt_mode": "baseline-seed-confirmed",
            "operator": {
                "blocker": "refresh-transition-after-seed",
                "next_action": "Seed receipt is confirmed; refresh the transition summary and rotation ledger so the lane leaves seed-pending.",
            },
            "seed_commands": {
                "seed_previous_snapshot_command": "cp retained current.previous",
                "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
                "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
            },
            "verification": {
                "previous_snapshot_present": True,
                "previous_matches_current_snapshot": True,
                "previous_matches_retained_baseline": True,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": "ec5b6c91b4e6",
                "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            },
            "counts": {
                "candidate_count": 2,
                "verification_true_count": 3,
            },
        }
        surface = json.loads(json.dumps(expected))
        surface["receipt_status"] = "pending"

        payload = etw_stackwalk_reopen_seed_receipt_check.compare_seed_receipt(
            surface,
            expected,
            generated_utc="2026-04-15T21:30:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("receipt_status mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_seed_ack_journal_awaits_application_when_receipt_pending(self) -> None:
        receipt = {
            "schema_version": "1.0",
            "receipt_status": "pending",
            "receipt_mode": "await-seed",
            "seed_commands": {
                "seed_previous_snapshot_command": "cp retained current.previous",
                "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
                "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
            },
            "verification": {
                "previous_snapshot_present": False,
                "previous_matches_current_snapshot": False,
                "previous_matches_retained_baseline": False,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": None,
                "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            },
            "counts": {"candidate_count": 2},
        }
        rotation = {
            "schema_version": "1.0",
            "rotation_status": "seed-pending",
            "rotation_mode": "seed-from-baseline",
            "counts": {"prerequisite_count": 2},
            "focus": {"top_rotation_candidate": "power.control.allow-system-required-power-requests"},
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "transition_type": "added",
                    "rotation_disposition": "seed-baseline",
                    "current_journal_state": "deferred",
                    "next_unlock_prerequisite": "await-seeding-pivot",
                }
            ],
        }

        payload = etw_stackwalk_reopen_seed_ack_journal.build_seed_ack_journal(
            receipt,
            rotation,
            seed_receipt_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-seed-receipt.json"),
            rotation_ledger_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-rotation-ledger.json"),
            generated_utc="2026-04-15T22:00:00Z",
        )

        self.assertEqual(payload["ack_status"], "awaiting-application")
        self.assertEqual(payload["ack_mode"], "apply-seed")
        self.assertEqual(payload["counts"]["ack_required_candidate_count"], 1)

    def test_etw_stackwalk_reopen_seed_ack_journal_check_accepts_matching_surface(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_seed_receipt_path": "registry-research-framework/audit/etw-stackwalk-reopen-seed-receipt.json",
            "source_rotation_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-rotation-ledger.json",
            "ack_status": "awaiting-refresh",
            "ack_mode": "refresh-after-seed",
            "receipt_status": "seeded-retained-baseline",
            "rotation_status": "seed-pending",
            "rotation_mode": "seed-from-baseline",
            "operator": {
                "blocker": "refresh-transition-and-ledger",
                "next_action": "Regenerate the transition summary and rotation ledger so the lane can leave seed-pending.",
            },
            "commands": {
                "seed_previous_snapshot_command": "cp retained current.previous",
                "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
                "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
                "regenerate_seed_receipt_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_seed_receipt.py",
                "regenerate_rotation_ledger_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_rotation_ledger.py",
            },
            "verification": {
                "previous_snapshot_present": True,
                "previous_matches_current_snapshot": True,
                "previous_matches_retained_baseline": True,
                "rotation_prerequisites_pending": True,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": "ec5b6c91b4e6",
                "retained_baseline_snapshot_id": "ec5b6c91b4e6",
                "top_rotation_candidate": "power.control.allow-system-required-power-requests",
            },
            "counts": {
                "candidate_count": 2,
                "ack_required_candidate_count": 1,
                "rotation_candidate_count": 1,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "transition_type": "added",
                    "rotation_disposition": "seed-baseline",
                    "current_journal_state": "deferred",
                    "next_unlock_prerequisite": "await-seeding-pivot",
                    "ack_required": True,
                }
            ],
        }

        payload = etw_stackwalk_reopen_seed_ack_journal_check.compare_seed_ack_journal(
            json.loads(json.dumps(expected)),
            expected,
            generated_utc="2026-04-15T22:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")

    def test_etw_stackwalk_reopen_seed_ack_journal_check_rejects_ack_status_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_seed_receipt_path": "registry-research-framework/audit/etw-stackwalk-reopen-seed-receipt.json",
            "source_rotation_ledger_path": "registry-research-framework/audit/etw-stackwalk-reopen-rotation-ledger.json",
            "ack_status": "complete",
            "ack_mode": "steady",
            "receipt_status": "current-matches-previous",
            "rotation_status": "seed-complete",
            "rotation_mode": "receipt-confirmed-steady",
            "operator": {
                "blocker": "await-new-current-snapshot",
                "next_action": "Seed alignment is complete; wait for a new current reopen snapshot.",
            },
            "commands": {
                "seed_previous_snapshot_command": "cp retained current.previous",
                "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
                "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
                "regenerate_seed_receipt_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_seed_receipt.py",
                "regenerate_rotation_ledger_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_rotation_ledger.py",
            },
            "verification": {
                "previous_snapshot_present": True,
                "previous_matches_current_snapshot": True,
                "previous_matches_retained_baseline": True,
                "rotation_prerequisites_pending": False,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": "ec5b6c91b4e6",
                "retained_baseline_snapshot_id": "ec5b6c91b4e6",
                "top_rotation_candidate": None,
            },
            "counts": {
                "candidate_count": 2,
                "ack_required_candidate_count": 0,
                "rotation_candidate_count": 0,
            },
            "entries": [],
        }
        surface = json.loads(json.dumps(expected))
        surface["ack_status"] = "manual-review"

        payload = etw_stackwalk_reopen_seed_ack_journal_check.compare_seed_ack_journal(
            surface,
            expected,
            generated_utc="2026-04-15T22:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("ack_status mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_reopen_rotation_ledger_check_accepts_matching_surface(self) -> None:
        current = {
            "schema_version": "1.0",
            "snapshot_id": "ec5b6c91b4e6",
            "counts": {"candidate_count": 2},
        }
        previous = {
            "schema_version": "1.0",
            "snapshot_id": "ab12cd34ef56",
            "counts": {"candidate_count": 2},
        }
        transition = {
            "schema_version": "1.0",
            "transition_status": "changed",
            "counts": {"changed_candidate_count": 1},
            "focus": {"top_changed_candidate": "power.control.allow-system-required-power-requests"},
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Power",
                    "transition_type": "blocker-changed",
                    "current_journal_state": "deferred",
                    "previous_journal_state": "deferred",
                    "current_operator_blocker": "await-seeding-pivot",
                    "previous_operator_blocker": "await-kd-lane",
                    "next_unlock_prerequisite": "await-seeding-pivot",
                }
            ],
        }
        history = {
            "schema_version": "1.0",
            "history_status": "rotation-ready",
            "retained_baseline_snapshot_id": "ec5b6c91b4e6",
            "seed_previous_snapshot_command": "cp retained current.previous",
            "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
            "persist_current_snapshot_history_command": "mkdir -p history-store",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        }
        seed_receipt = {
            "schema_version": "1.0",
            "receipt_status": "custom-previous-present",
        }
        expected = etw_stackwalk_reopen_rotation_ledger.build_rotation_ledger(
            current,
            previous,
            transition,
            history,
            seed_receipt,
            current_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json"),
            previous_snapshot_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"),
            transition_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.json"),
            history_archive_summary_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json"),
            seed_receipt_path=Path("registry-research-framework/audit/etw-stackwalk-reopen-seed-receipt.json"),
            generated_utc="2026-04-15T21:00:00Z",
        )
        surface = json.loads(json.dumps(expected))

        payload = etw_stackwalk_reopen_rotation_ledger_check.compare_rotation_ledger(
            surface,
            expected,
            generated_utc="2026-04-15T21:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")

    def test_etw_stackwalk_reopen_rotation_ledger_check_rejects_rotation_status_mismatch(self) -> None:
        expected = {
            "schema_version": "1.0",
            "source_current_snapshot_path": "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json",
            "source_previous_snapshot_path": None,
            "source_transition_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-transition-summary.json",
            "source_history_archive_summary_path": "registry-research-framework/audit/etw-stackwalk-reopen-history-archive.json",
            "source_history_archive_markdown_path": "registry-research-framework/audit/etw-stackwalk-reopen-history-archive.md",
            "source_seed_receipt_path": None,
            "rotation_status": "seed-pending",
            "rotation_mode": "seed-from-baseline",
            "history_status": "seed-required",
            "transition_status": "baseline",
            "seed_receipt_status": None,
            "operator": {
                "blocker": "seed-previous-snapshot-from-history-archive",
                "next_action": "Seed snapshot.previous from the retained baseline snapshot before expecting rotation-aware reopen diffs.",
            },
            "seed_previous_snapshot_command": "cp retained current.previous",
            "seed_previous_snapshot_markdown_command": "cp retained-md current.previous.md",
            "persist_current_snapshot_history_command": "mkdir -p history-store",
            "rotate_previous_snapshot_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json",
            "rotate_previous_snapshot_markdown_command": "cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md",
            "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
            "prerequisite_codes": ["seed-previous-snapshot", "refresh-transition-summary"],
            "counts": {
                "current_candidate_count": 2,
                "previous_candidate_count": 0,
                "changed_candidate_count": 2,
                "rotation_candidate_count": 2,
                "prerequisite_count": 2,
            },
            "focus": {
                "current_snapshot_id": "ec5b6c91b4e6",
                "previous_snapshot_id": None,
                "retained_baseline_snapshot_id": "ec5b6c91b4e6",
                "top_changed_candidate": "power.control.allow-system-required-power-requests",
                "top_rotation_candidate": "power.control.allow-system-required-power-requests",
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Power",
                    "transition_type": "added",
                    "current_journal_state": "deferred",
                    "previous_journal_state": None,
                    "current_operator_blocker": "await-seeding-pivot",
                    "previous_operator_blocker": None,
                    "next_unlock_prerequisite": "await-seeding-pivot",
                    "requires_rotation_review": True,
                    "rotation_disposition": "seed-baseline",
                },
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Power",
                    "transition_type": "added",
                    "current_journal_state": "deferred",
                    "previous_journal_state": None,
                    "current_operator_blocker": "await-primary-doc",
                    "previous_operator_blocker": None,
                    "next_unlock_prerequisite": "await-primary-doc",
                    "requires_rotation_review": True,
                    "rotation_disposition": "seed-baseline",
                },
            ],
        }
        surface = json.loads(json.dumps(expected))
        surface["rotation_status"] = "steady"

        payload = etw_stackwalk_reopen_rotation_ledger_check.compare_rotation_ledger(
            surface,
            expected,
            generated_utc="2026-04-15T21:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("rotation_status mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_execution_manifest_defaults_to_idle_for_hold_only_set(self) -> None:
        batch = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "actionability": "hold",
                    "dispatch_recommended": False,
                    "promotion_blockers": ["intentional-hold"],
                    "next_action_hint": "Reopen only when a boot/init reader pivot exists.",
                    "capture_plan": {
                        "run": {
                            "run_id": "wave4-allow-system-required-e2e",
                            "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                        },
                        "target": {
                            "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                            "value_name": "AllowSystemRequiredPowerRequests",
                        },
                    },
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                }
            ]
        }
        run_payload = {
            "selected_job_count": 0,
            "skipped_hold_count": 1,
        }
        reopen_payload = {
            "items": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "reopen_prerequisites": ["Explicitly reopen the lane before dispatching runtime capture."],
                }
            ]
        }

        payload = etw_stackwalk_execution_manifest.build_execution_manifest(
            batch,
            run_payload,
            reopen_payload,
            candidate_ids={"power.control.allow-system-required-power-requests"},
            include_holds=False,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["status"], "idle")
        self.assertEqual(payload["selected_count"], 0)
        self.assertEqual(payload["excluded_count"], 1)
        self.assertIn("Review excluded hold candidates", payload["operator"]["next_action"])

    def test_etw_stackwalk_execution_manifest_can_select_hold_reopen_subset(self) -> None:
        batch = {
            "items": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "actionability": "hold",
                    "dispatch_recommended": False,
                    "promotion_blockers": ["intentional-hold", "audio-execution-required-no-primary-current-build-doc"],
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "capture_plan": {
                        "run": {
                            "run_id": "wave4-allow-audio-e2e",
                            "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl",
                        },
                        "target": {
                            "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                            "value_name": "AllowAudioToEnableExecutionRequiredPowerRequests",
                        },
                    },
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --ingest-to-repo --refresh-ghidra",
                }
            ]
        }
        run_payload = {
            "selected_job_count": 0,
            "skipped_hold_count": 1,
        }
        reopen_payload = {
            "items": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run",
                    "reopen_prerequisites": [
                        "Land a current-build boot/init reader or registry seeding caller proof.",
                        "Land a primary current-build Microsoft document for the exact value semantics.",
                    ],
                }
            ]
        }

        payload = etw_stackwalk_execution_manifest.build_execution_manifest(
            batch,
            run_payload,
            reopen_payload,
            candidate_ids={"power.control.allow-audio-to-enable-execution-required-power-requests"},
            include_holds=True,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["status"], "ready")
        self.assertEqual(payload["selected_count"], 1)
        self.assertEqual(payload["entries"][0]["selection_reason"], "hold-reopen")
        self.assertIn("--include-holds", payload["entries"][0]["include_holds_run_command"])

    def test_etw_stackwalk_execution_manifest_blocks_missing_candidates(self) -> None:
        payload = etw_stackwalk_execution_manifest.build_execution_manifest(
            {"items": []},
            {"selected_job_count": 0, "skipped_hold_count": 0},
            {"items": []},
            candidate_ids={"missing.candidate"},
            include_holds=False,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["status"], "blocked")
        self.assertEqual(payload["missing_candidate_ids"], ["missing.candidate"])

    def test_etw_stackwalk_execution_manifest_check_accepts_matching_surface(self) -> None:
        surface = {
            "schema_version": "1.0",
            "status": "idle",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "include_holds": False,
            "requested_candidate_ids": ["power.control.allow-system-required-power-requests"],
            "missing_candidate_ids": [],
            "selected_count": 0,
            "excluded_count": 1,
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 1,
            "operator": {
                "next_action": "Review excluded hold candidates and reopen intentionally if needed.",
                "include_holds_required": False,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "feature_area": "Control Power Requests",
                    "actionability": "hold",
                    "selected": False,
                    "selection_reason": "excluded",
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "queue_state": "blocked",
                    "promotion_state": "blocked",
                    "next_missing_layer": "intentional-hold",
                    "promotion_blockers": ["intentional-hold"],
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "run_id": "wave4-allow-system-required-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_plan_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "next_action_hint": "Reopen only when a boot/init reader or registry seeding caller pivot becomes available.",
                    "reopen_prerequisites": ["Explicitly reopen the lane before dispatching runtime capture."],
                }
            ],
        }

        payload = etw_stackwalk_execution_manifest_check.compare_execution_manifest(
            surface,
            surface,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "ok")
        self.assertEqual(payload["errors"], [])

    def test_etw_stackwalk_execution_manifest_check_rejects_mismatched_selected_count(self) -> None:
        expected = {
            "schema_version": "1.0",
            "status": "ready",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "include_holds": True,
            "requested_candidate_ids": ["power.control.allow-audio-to-enable-execution-required-power-requests"],
            "missing_candidate_ids": [],
            "selected_count": 1,
            "excluded_count": 0,
            "default_selected_job_count": 0,
            "default_skipped_hold_count": 1,
            "operator": {
                "next_action": "Run the selected dispatch commands.",
                "include_holds_required": True,
            },
            "entries": [],
        }
        surface = dict(expected)
        surface["selected_count"] = 0

        payload = etw_stackwalk_execution_manifest_check.compare_execution_manifest(
            surface,
            expected,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("selected_count mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_execution_pack_materializes_ready_pack(self) -> None:
        manifest = {
            "schema_version": "1.0",
            "status": "ready",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "include_holds": True,
            "requested_candidate_ids": ["power.control.allow-audio-to-enable-execution-required-power-requests"],
            "operator": {
                "next_action": "Run the selected dispatch commands.",
                "include_holds_required": True,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-audio-to-enable-execution-required-power-requests",
                    "selected": True,
                    "selection_reason": "hold-reopen",
                    "actionability": "hold",
                    "profile_id": "execution-required-audio-stackwalk-v1",
                    "run_id": "wave4-allow-audio-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl",
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowAudioToEnableExecutionRequiredPowerRequests",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run",
                    "next_action_hint": "Reopen intentionally before dispatch.",
                    "promotion_blockers": ["intentional-hold"],
                    "reopen_prerequisites": ["Explicitly reopen the lane before dispatching runtime capture."],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            manifest_path = base / "etw-stackwalk-execution-manifest.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            manifest_path.with_suffix(".md").write_text("# manifest\n", encoding="utf-8")

            payload = etw_stackwalk_execution_pack.materialize_execution_pack(
                manifest,
                manifest_path=manifest_path,
                output_root=base / "pack",
                summary_path=base / "pack.json",
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-14T18:00:00Z",
            )

            self.assertEqual(payload["pack_status"], "ready")
            self.assertEqual(payload["counts"]["selected_candidates"], 1)
            self.assertEqual(payload["counts"]["command_files_written"], 1)
            self.assertEqual(payload["selected_candidate_ids"], ["power.control.allow-audio-to-enable-execution-required-power-requests"])
            command_path = base / "pack" / "commands" / payload["command_files"][0]
            self.assertTrue(command_path.exists())
            self.assertIn("--include-holds", command_path.read_text(encoding="utf-8"))
            self.assertTrue((base / "pack.zip").exists())

    def test_etw_stackwalk_execution_pack_defaults_to_idle_for_unselected_manifest(self) -> None:
        manifest = {
            "schema_version": "1.0",
            "status": "idle",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "include_holds": False,
            "requested_candidate_ids": ["power.control.allow-system-required-power-requests"],
            "operator": {
                "next_action": "Review excluded hold candidates and reopen intentionally if needed.",
                "include_holds_required": False,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "selected": False,
                    "selection_reason": "excluded",
                    "actionability": "hold",
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "run_id": "wave4-allow-system-required-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "next_action_hint": "Reopen only when a pivot appears.",
                    "promotion_blockers": ["intentional-hold"],
                    "reopen_prerequisites": ["Explicitly reopen the lane before dispatching runtime capture."],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            manifest_path = base / "etw-stackwalk-execution-manifest.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            manifest_path.with_suffix(".md").write_text("# manifest\n", encoding="utf-8")

            payload = etw_stackwalk_execution_pack.materialize_execution_pack(
                manifest,
                manifest_path=manifest_path,
                output_root=base / "pack",
                summary_path=base / "pack.json",
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-14T18:00:00Z",
            )

            self.assertEqual(payload["pack_status"], "idle")
            self.assertEqual(payload["counts"]["selected_candidates"], 0)
            self.assertEqual(payload["counts"]["command_files_written"], 0)

    def test_etw_stackwalk_execution_pack_check_accepts_matching_surface(self) -> None:
        manifest = {
            "schema_version": "1.0",
            "status": "ready",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "include_holds": False,
            "requested_candidate_ids": ["power.control.allow-system-required-power-requests"],
            "operator": {
                "next_action": "Run the selected dispatch commands.",
                "include_holds_required": False,
            },
            "entries": [
                {
                    "candidate_id": "power.control.allow-system-required-power-requests",
                    "selected": True,
                    "selection_reason": "default-dispatch",
                    "actionability": "active",
                    "profile_id": "execution-required-system-stackwalk-v1",
                    "run_id": "wave4-allow-system-required-e2e",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl",
                    "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "effective_config_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config",
                    "dispatch_command": "python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra",
                    "include_holds_run_command": "python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run",
                    "next_action_hint": "Ready to dispatch.",
                    "promotion_blockers": ["needs-focused-etw"],
                    "reopen_prerequisites": [],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            base = Path(temp_dir)
            manifest_path = base / "etw-stackwalk-execution-manifest.json"
            summary_path = base / "etw-stackwalk-execution-pack.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            manifest_path.with_suffix(".md").write_text("# manifest\n", encoding="utf-8")

            etw_stackwalk_execution_pack.materialize_execution_pack(
                manifest,
                manifest_path=manifest_path,
                output_root=base / "pack",
                summary_path=summary_path,
                markdown_path=base / "pack.md",
                archive_path=base / "pack.zip",
                generated_utc="2026-04-14T18:00:00Z",
            )
            summary = json.loads(summary_path.read_text(encoding="utf-8"))

            payload = etw_stackwalk_execution_pack_check.compare_pack_summary(
                summary,
                etw_stackwalk_execution_pack.build_pack_plan(manifest, manifest_path=manifest_path),
                generated_utc="2026-04-14T18:00:00Z",
            )
            asset_errors, counts = etw_stackwalk_execution_pack_check.validate_pack_assets(summary)

            self.assertEqual(payload["check_status"], "ok")
            self.assertEqual(asset_errors, [])
            self.assertGreaterEqual(counts["checked_pack_files"], 1)

    def test_etw_stackwalk_execution_pack_check_rejects_selected_candidate_mismatch(self) -> None:
        expected = {
            "source_manifest_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.json",
            "source_manifest_markdown_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.md",
            "source_batch_path": "registry-research-framework/audit/etw-stackwalk-dispatch-batch.json",
            "source_run_path": "registry-research-framework/audit/etw-stackwalk-dispatch-run.json",
            "source_hold_reopen_plan_path": "registry-research-framework/audit/etw-stackwalk-hold-reopen-plan.json",
            "manifest_status": "ready",
            "pack_status": "ready",
            "include_holds": False,
            "operator": {"next_action": "Run the selected dispatch commands."},
            "requested_candidate_ids": ["example.candidate"],
            "selected_candidate_ids": ["example.candidate"],
            "excluded_candidate_ids": [],
            "required_repo_paths": ["scripts/vm-kvm/run-guest-etw-stackwalk-capture.py"],
            "entries": [
                {
                    "candidate_id": "example.candidate",
                    "selected": True,
                    "selection_reason": "default-dispatch",
                    "actionability": "active",
                    "profile_id": "example-profile",
                    "run_id": "example-run",
                    "host_etl_repo_path": "evidence/raw/etw-stackwalk/example/example.etl",
                    "registry_path": r"HKLM\Software\Example",
                    "value_name": "Enabled",
                    "selected_command": "python3 example.py --run",
                    "effective_config_command": "python3 example.py --print-effective-config",
                    "dispatch_command": "python3 example.py --run",
                    "include_holds_run_command": None,
                    "next_action_hint": "Ready to dispatch.",
                    "promotion_blockers": [],
                    "reopen_prerequisites": [],
                }
            ],
        }
        surface = {
            "schema_version": "1.0",
            "source_manifest_path": expected["source_manifest_path"],
            "source_manifest_markdown_path": expected["source_manifest_markdown_path"],
            "source_batch_path": expected["source_batch_path"],
            "source_run_path": expected["source_run_path"],
            "source_hold_reopen_plan_path": expected["source_hold_reopen_plan_path"],
            "manifest_status": "ready",
            "pack_status": "ready",
            "include_holds": False,
            "operator": {"next_action": "Run the selected dispatch commands."},
            "counts": {
                "requested_candidates": 1,
                "selected_candidates": 0,
                "excluded_candidates": 1,
                "repo_files_copied": 0,
                "command_files_written": 0,
                "manifest_files_written": 0,
                "pack_files_checksummed": 0,
            },
            "requested_candidate_ids": ["example.candidate"],
            "selected_candidate_ids": [],
            "excluded_candidate_ids": ["example.candidate"],
            "required_repo_paths": ["scripts/vm-kvm/run-guest-etw-stackwalk-capture.py"],
            "copied_repo_paths": [],
            "command_files": [],
            "manifest_files": [],
            "entries": [],
            "pack_files": [],
        }

        payload = etw_stackwalk_execution_pack_check.compare_pack_summary(
            surface,
            expected,
            generated_utc="2026-04-14T18:00:00Z",
        )

        self.assertEqual(payload["check_status"], "error")
        self.assertTrue(any("selected_candidate_ids mismatch" in error for error in payload["errors"]))

    def test_etw_stackwalk_bundle_preserves_caller_stack_events(self) -> None:
        parse_result = {
            "etl_path": "evidence/raw/etw-stackwalk/sample/sample.etl",
            "xml_output": "evidence/raw/etw-stackwalk/sample/sample.xml",
            "status": "parsed-sidecar-xml",
            "notes": ["parsed from sidecar"],
            "registry_touches": [
                {
                    "process_name": "System",
                    "process_id": "4",
                    "operation": "RegQueryValue",
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
                    "value_name": "TimerCheckFlags",
                    "raw_excerpt": "TimerCheckFlags read",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopReadRegKeyValue"],
                }
            ],
        }

        bundle = etw_stackwalk_bundle.bundle_from_parse_result(
            parse_result,
            run_id="wave4-stackwalk",
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(bundle["status"], "ok")
        self.assertEqual(bundle["source_tool"], "etw")
        self.assertEqual(bundle["stack_capture"]["captured_event_count"], 1)
        self.assertEqual(bundle["events"][0]["key_path"], "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel")
        self.assertEqual(bundle["events"][0]["hive"], "HKLM")
        self.assertEqual(bundle["events"][0]["pid"], 4)
        self.assertEqual(bundle["events"][0]["caller_stack"], ["ntoskrnl.exe+0x1F234", "nt!PopReadRegKeyValue"])

    def test_parse_etl_registry_touches_uses_existing_xml_sidecar_when_tracerpt_missing(self) -> None:
        xml_payload = """<?xml version="1.0" encoding="utf-8"?>
<Events>
  <Event>
    <System>
      <Provider Guid="{AE53722E-C863-11D2-8659-00C04FA321A1}" />
      <EventID>10</EventID>
      <Execution ProcessID="4242" />
    </System>
    <EventData>
      <Data Name="KeyName">\\REGISTRY\\MACHINE\\System\\CurrentControlSet\\Control\\Power</Data>
      <Data Name="ValueName">AllowSystemRequiredPowerRequests</Data>
      <Data Name="ProcessName">svchost.exe</Data>
      <Data Name="Operation">QueryValueKey</Data>
    </EventData>
  </Event>
</Events>
"""
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_dir:
            base = Path(temp_dir)
            etl_path = base / "sample.etl"
            xml_path = base / "sample.etl.xml"
            etl_path.write_bytes(b"ETL")
            xml_path.write_text(xml_payload, encoding="utf-8")

            original_loader = research_v36_lib.load_etl_parser_config

            def fake_config() -> dict[str, object]:
                return {
                    "default_parser": "tracerpt",
                    "provider_guid": "{AE53722E-C863-11D2-8659-00C04FA321A1}",
                    "parser_commands": {"tracerpt": "/definitely/missing/tracerpt.exe"},
                }

            research_v36_lib.load_etl_parser_config = fake_config
            try:
                parsed = research_v36_lib.parse_etl_registry_touches(
                    etl_path,
                    parser="tracerpt",
                    provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
                )
            finally:
                research_v36_lib.load_etl_parser_config = original_loader

        self.assertEqual(parsed["status"], "parsed-sidecar-xml")
        self.assertEqual(parsed["normalized_touch_count"], 1)
        self.assertIn("parsed existing XML sidecar", " ".join(parsed["notes"]))
        self.assertEqual(parsed["registry_touches"][0]["key_path"], "HKLM\\System\\CurrentControlSet\\Control\\Power")

    def test_parse_etl_registry_touches_uses_touch_sidecar_when_tracerpt_missing(self) -> None:
        payload = {
            "generated_utc": "2026-04-09T00:00:00Z",
            "parser_source": "tracerpt-xml-primary",
            "touch_count": 1,
            "registry_touches": [
                {
                    "provider": "{AE53722E-C863-11D2-8659-00C04FA321A1}",
                    "event_id": "10",
                    "process_name": "svchost.exe",
                    "operation": "QueryValueKey",
                    "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                }
            ],
        }
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_dir:
            base = Path(temp_dir)
            etl_path = base / "sample.etl"
            sidecar_path = base / "sample.etl.registry-touches.json"
            etl_path.write_bytes(b"ETL")
            sidecar_path.write_text(json.dumps(payload), encoding="utf-8")

            original_loader = research_v36_lib.load_etl_parser_config

            def fake_config() -> dict[str, object]:
                return {
                    "default_parser": "tracerpt",
                    "provider_guid": "{AE53722E-C863-11D2-8659-00C04FA321A1}",
                    "parser_commands": {"tracerpt": "/definitely/missing/tracerpt.exe"},
                }

            research_v36_lib.load_etl_parser_config = fake_config
            try:
                parsed = research_v36_lib.parse_etl_registry_touches(
                    etl_path,
                    parser="tracerpt",
                    provider_guid="{AE53722E-C863-11D2-8659-00C04FA321A1}",
                )
            finally:
                research_v36_lib.load_etl_parser_config = original_loader

        self.assertEqual(parsed["status"], "parsed-sidecar-json")
        self.assertEqual(parsed["normalized_touch_count"], 1)
        self.assertIn("parsed existing touch sidecar", " ".join(parsed["notes"]))
        self.assertEqual(parsed["registry_touches"][0]["value_name"], "AllowSystemRequiredPowerRequests")


class CanonicalBundleProjectionTests(unittest.TestCase):
    def test_projection_contains_required_top_level_contract_fields(self) -> None:
        record = {
            "record_id": "example.bundle",
            "tweak_id": "example.bundle",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKLM\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {"apply_allowed": False, "confidence": "medium"},
            "app_current_implementation": {"status": "not-mapped"},
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
            "last_reviewed_utc": "2026-04-09T00:00:00Z",
        }
        audit = {"next_missing_layer": "runtime-trace"}
        full_evidence = {
            "behavior": {
                "registry_sideeffects": {
                    "format": "semantic-registry",
                    "diff_file": "evidence/files/example/diff.txt",
                    "summary_counts": {
                        "added_keys": 0,
                        "removed_keys": 0,
                        "added_values": 1,
                        "removed_values": 0,
                        "modified_values": 1,
                        "unchanged_values": 2,
                    },
                },
                "benchmark": {
                    "executed": False,
                    "summary": None,
                    "statistics": {},
                    "significance_verdict": "insufficient",
                },
            },
            "negative_evidence": {"eligible": True},
            "rollback_verification": {
                "rollback_declared": True,
                "rollback_executed": True,
                "rollback_verified": True,
                "rollback_verification_method": "state_diff",
                "rollback_failure_reason": None,
            },
            "structured_diff": {
                "key_added": [],
                "key_deleted": [],
                "value_added": [{"key_path": "HKLM\\Software\\Example", "value_name": "Added", "after_value": 1}],
                "value_deleted": [],
                "value_changed": [{"key_path": "HKLM\\Software\\Example", "value_name": "Enabled", "before_value": 0, "after_value": 1}],
            },
            "reproducibility": {"vm_name": "Win25H2Clean", "baseline_snapshot": "snap"},
        }

        payload = research_v36_lib.canonical_bundle_projection(record, audit, full_evidence)
        errors = research_v36_lib.validate_canonical_bundle(payload)

        self.assertFalse(errors, errors)
        self.assertEqual(payload["before_after"]["value_added"], 1)
        self.assertEqual(payload["before_after"]["value_changed"], 1)
        self.assertIn("score_breakdown", payload)
        self.assertEqual(payload["discovery_source"], "imported_record")
        self.assertEqual(payload["discovery_reason"], "existing_research")
        self.assertIn("observed_default", payload)
        self.assertIn("recommended_value", payload)
        self.assertIn("rollback_value", payload)
        self.assertIn("source_enrichment", payload)
        self.assertEqual(len(payload["before_after"]["value_added_entries"]), 1)
        self.assertEqual(payload["rollback_status"]["rollback_verification_method"], "state_diff")
        self.assertEqual(payload["os_build"], "26100")
        self.assertIn("os_edition", payload)
        self.assertIn("architecture", payload)
        self.assertEqual(payload["elevation_context"], "elevated")
        self.assertEqual(payload["machine_user_scope"], "machine")


class BlockerNamingRegressionTests(unittest.TestCase):
    def test_blocked_records_do_not_reintroduce_deprecated_generic_blockers(self) -> None:
        gates_path = REPO_ROOT / "research" / "promotion-gates.json"
        gates = json.loads(gates_path.read_text(encoding="utf-8"))
        entries = gates.get("entries") or gates.get("gates") or []
        blocked = [
            entry for entry in entries
            if entry.get("promotion_state") == "blocked" or entry.get("state") == "blocked"
        ]

        deprecated = {
            "documentation-first-review",
            "no-primary-current-build-doc",
            "runtime_no_read",
            "wpr-boot-registry-no-hit-current-build",
            "execution-required-init-walker-not-symbol-resolved",
            "execution-required-megatrigger-etw-no-hit-current-build",
            "execution-required-wpr-boot-no-hit-current-build",
            "research-only-raw-policy-system-value",
            "research-only-raw-power-manager-value",
            "hibernate-trigger-not-available-on-current-vm",
            "drips-trigger-not-available-on-current-vm",
            "boot-unsafe-dedicated-boot-lane-required",
            "boot-unsafe-on-isolated-pilot-profile",
            "bounded-s1-registry-etw-no-hit-current-build",
            "procmon-saveas-timeout-on-bounded-callout-lane",
            "procmon-saveas-timeout-on-dedicated-timer-dpc-stress-lane",
            "watchdog-timeouts-exact-runtime-read-unresolved",
            "watchdog-timeouts-specific-caller-unresolved",
            "powerrequestoverride-leaf-semantics-unresolved",
            "force-bugcheck-for-dpc-watchdog-semantics-unproven",
            "audio-execution-required-init-walker-not-symbol-resolved",
            "system-execution-required-init-walker-not-symbol-resolved",
        }

        found: dict[str, list[str]] = {}
        for entry in blocked:
            present = sorted(set(entry.get("promotion_blockers") or []) & deprecated)
            if present:
                found[str(entry.get("candidate_id"))] = present

        self.assertEqual(found, {}, found)


class BlockedWorklistTests(unittest.TestCase):
    def test_blocked_worklist_payload_is_structurally_sound(self) -> None:
        payload = blocked_worklist_lib.build_worklist()

        self.assertGreaterEqual(int(payload.get("blocked_count") or 0), 0)
        self.assertIsInstance(payload.get("top_actionable_candidates"), list)
        self.assertIsInstance(payload.get("top_hold_candidates"), list)
        self.assertIsInstance(payload.get("actionability_counts"), dict)
        self.assertIsInstance(payload.get("lane_suggested_commands"), dict)
        self.assertIsInstance(payload.get("lane_focus"), dict)
        self.assertLessEqual(len(payload.get("top_actionable_candidates") or []), 5)
        self.assertLessEqual(len(payload.get("top_hold_candidates") or []), 5)
        self.assertEqual(
            sum(int(value) for value in (payload.get("lane_counts") or {}).values()),
            int(payload.get("blocked_count") or 0),
        )

        for item in payload.get("items") or []:
            self.assertTrue(item.get("candidate_id"))
            self.assertTrue(item.get("next_missing_layer"))
            self.assertTrue(item.get("next_action_hint"))
            self.assertTrue(item.get("suggested_command"))
            self.assertIn(item.get("actionability"), {"active", "hold"})
            self.assertIsInstance(item.get("priority_score"), int)
            self.assertIsInstance(item.get("promotion_blockers"), list)
            self.assertIsInstance(item.get("recent_audit_artifacts"), list)
            self.assertLessEqual(len(item.get("recent_audit_artifacts") or []), 3)

    def test_blocked_worklist_validator_accepts_generated_payload(self) -> None:
        payload = blocked_worklist_lib.build_worklist()

        self.assertEqual(blocked_worklist_check.validate_payload(payload), [])

    def test_blocked_worklist_validator_rejects_count_mismatch(self) -> None:
        payload = blocked_worklist_lib.build_worklist()
        payload["blocked_count"] = int(payload.get("blocked_count") or 0) + 1

        errors = blocked_worklist_check.validate_payload(payload)

        self.assertTrue(any("blocked_count mismatch" in error for error in errors))

    def test_blocked_worklist_validator_rejects_top_list_mismatch(self) -> None:
        payload = blocked_worklist_lib.build_worklist()
        payload["top_actionable_candidates"] = ["wrong.candidate"]

        errors = blocked_worklist_check.validate_payload(payload)

        self.assertTrue(any("top_actionable_candidates" in error for error in errors))

    def test_blocked_worklist_surface_status_accepts_generated_payload(self) -> None:
        status = research_v36_lib.blocked_worklist_surface_status(blocked_worklist_lib.build_worklist())

        self.assertTrue(status["pass"], status)
        self.assertEqual(status["errors"], [])

    def test_core_cli_surface_status_requires_blocked_operator_commands(self) -> None:
        status = research_v36_lib.core_cli_surface_status("list-blocked show-stale apply rollback")

        self.assertFalse(status["pass"])
        self.assertIn("show-blocked", status["missing_commands"])
        self.assertIn("--actionability", status["missing_commands"])

    def test_core_cli_surface_status_scans_split_cli_sources(self) -> None:
        status = research_v36_lib.core_cli_surface_status()

        self.assertTrue(status["pass"], status)
        self.assertEqual(status["missing_commands"], [])

    def test_blocker_hint_prefers_restore_story_guidance(self) -> None:
        hint = blocked_worklist_lib.blocker_hint(
            ["powerrequestoverride-restore-story-leaf-model-unproven"],
            "restore-story",
        )

        self.assertIn("restore", hint.lower())

    def test_actionability_for_lane_flags_intentional_hold(self) -> None:
        self.assertEqual(blocked_worklist_lib.actionability_for_lane("ghidra"), "active")
        self.assertEqual(blocked_worklist_lib.actionability_for_lane("intentional-hold"), "hold")

    def test_publish_metrics_include_blocked_worklist_summary(self) -> None:
        payload = metrics_publish_v36_lib.build_publish_metrics(
            gate_payload={"summary": {"promotion_state_counts": {"promoted": 250, "blocked": 18}}},
            audit_payload={"entries": []},
            validation_summary={"missing_docs_count": 0},
            gate_metrics={"schema_complete_ratio": 1.0, "bench_not_run_count": 0},
            blocked_worklist={
                "lane_counts": {"ghidra": 5, "runtime-trace": 7},
                "items": [
                    {"candidate_id": "a", "actionability": "active"},
                    {"candidate_id": "b", "actionability": "hold"},
                    {"candidate_id": "c", "actionability": "active"},
                ],
            },
            generated_at="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["blocked_lane_counts"], {"ghidra": 5, "runtime-trace": 7})
        self.assertEqual(payload["blocked_actionability_counts"], {"active": 2, "hold": 1})
        self.assertEqual(payload["top_actionable_blocked_candidates"], ["a", "c"])
        self.assertEqual(payload["top_hold_blocked_candidates"], ["b"])

    def test_research_health_markdown_includes_blocked_actionability(self) -> None:
        block = metrics_publish_v36_lib.research_health_markdown(
            publish_metrics={
                "promoted_candidate_count": 250,
                "blocked_candidate_count": 18,
                "revalidation_pending_count": 0,
                "blocked_actionability_counts": {"active": 13, "hold": 5},
                "blocked_worklist_status": "PASS",
            },
            gate_metrics={"schema_complete_ratio": 1.0},
            validation_summary={"missing_docs_count": 0},
            gate_health="green",
        )

        self.assertIn("| Blocked Actionability | 13 active, 5 hold |", block)
        self.assertIn("| Blocked Worklist Gate | PASS |", block)

    def test_candidate_slug_tokens_prioritize_specific_prefixes(self) -> None:
        tokens = blocked_worklist_lib.candidate_slug_tokens("power.control.allow-system-required-power-requests")

        self.assertEqual(tokens[0], "power-control-allow-system-required-power-requests")
        self.assertIn("allow-system-required-power-requests", tokens)
        self.assertNotIn("power-control", tokens)

    def test_audit_artifact_match_score_prefers_specific_hits(self) -> None:
        candidate_id = "power.control.power-request-override-subtree"

        specific = blocked_worklist_lib.audit_artifact_match_score(
            candidate_id,
            "power-request-override-runtime-audit-20260408.json",
        )
        generic = blocked_worklist_lib.audit_artifact_match_score(
            candidate_id,
            "power-control-windbg-execution-20260403.json",
        )

        self.assertGreater(specific, generic)

    def test_suggested_command_prefers_show_blocked_for_active_lanes(self) -> None:
        self.assertEqual(
            blocked_worklist_lib.suggested_command_for("power.test-gate", "ghidra"),
            "winopt research show-blocked power.test-gate --json",
        )
        self.assertEqual(
            blocked_worklist_lib.suggested_command_for("power.test-gate", "intentional-hold"),
            "winopt research list-blocked --worklist --lane intentional-hold",
        )

    def test_lane_suggested_command_prefers_top_five_for_active_lanes(self) -> None:
        self.assertEqual(
            blocked_worklist_lib.lane_suggested_command_for("runtime-trace"),
            "winopt research list-blocked --worklist --lane runtime-trace --top 5",
        )
        self.assertEqual(
            blocked_worklist_lib.lane_suggested_command_for("intentional-hold"),
            "winopt research list-blocked --worklist --lane intentional-hold",
        )

    def test_lane_focus_prefers_highest_priority_entry_per_lane(self) -> None:
        focus = blocked_worklist_lib.lane_focus_for([
            {
                "candidate_id": "power.top",
                "next_missing_layer": "ghidra",
                "suggested_command": "cmd top",
                "next_action_hint": "hint top",
            },
            {
                "candidate_id": "power.second",
                "next_missing_layer": "ghidra",
                "suggested_command": "cmd second",
                "next_action_hint": "hint second",
            },
        ])

        self.assertEqual(focus["ghidra"]["candidate_id"], "power.top")
        self.assertEqual(focus["ghidra"]["suggested_command"], "cmd top")
        self.assertEqual(focus["ghidra"]["next_action_hint"], "hint top")


class GhidraJobQueueTests(unittest.TestCase):
    def test_ghidra_job_queue_only_includes_active_ghidra_items(self) -> None:
        payload = {
            "items": [
                {
                    "candidate_id": "power.keep",
                    "next_missing_layer": "ghidra",
                    "actionability": "active",
                    "feature_area": "Power",
                    "key_path": "HKLM\\System\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                    "suggested_command": "winopt research show-blocked power.keep --json",
                    "next_action_hint": "Resolve seeding path.",
                },
                {
                    "candidate_id": "power.hold",
                    "next_missing_layer": "ghidra",
                    "actionability": "hold",
                    "promotion_blockers": ["powerwatchdog-timeout-family-intentional-hold-no-current-build-pivot"],
                },
                {
                    "candidate_id": "runtime.other",
                    "next_missing_layer": "runtime-trace",
                    "actionability": "active",
                    "promotion_blockers": ["timer-check-flags-wpr-boot-no-hit-current-build"],
                },
            ]
        }

        jobs = ghidra_job_queue.ghidra_jobs_from_worklist(payload, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(len(jobs), 1)
        self.assertEqual(jobs[0]["candidate_id"], "power.keep")
        self.assertEqual(jobs[0]["status"], "queued")
        self.assertEqual(jobs[0]["job_type"], "ghidra-decompile-context")
        self.assertEqual(jobs[0]["priority_rank"], 1)


class GhidraDispatchBatchTests(unittest.TestCase):
    def test_dispatch_batch_prepares_headless_kernel_jobs(self) -> None:
        rows = [
            {
                "candidate_id": "power.keep",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Power",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            },
            {
                "candidate_id": "power.watchdog",
                "status": "queued",
                "priority_rank": 2,
                "feature_area": "Kernel",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power",
                "value_name": "WatchdogResumeTimeout / WatchdogSleepTimeout",
                "promotion_blockers": ["power-session-watchdog-timeouts-specific-caller-unresolved"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Name the exact reader.",
            },
        ]

        payload = ghidra_dispatch_batch.dispatch_batch_from_queue(rows, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(payload["job_count"], 2)
        self.assertEqual(payload["jobs"][0]["target_binary"], "ntoskrnl.exe")
        self.assertEqual(payload["jobs"][0]["analysis_mode"], "registry-string-xref")
        self.assertTrue(payload["jobs"][0]["can_run_headless"])
        self.assertEqual(payload["jobs"][0]["command_argv"][:3], ["pwsh", "-File", "registry-research-framework/tools/ghidra-headless-analyze.ps1"])
        self.assertIn("ghidra-headless-analyze.ps1", payload["jobs"][0]["suggested_command"])
        self.assertEqual(
            payload["jobs"][1]["patterns"],
            ["WatchdogResumeTimeout", "WatchdogSleepTimeout"],
        )

    def test_dispatch_batch_enriches_matching_job_with_autotrigger_context(self) -> None:
        rows = [
            {
                "candidate_id": "power.keep",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Power",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]
        autotrigger_rows = [
            {
                "candidate_id": "power.keep",
                "trigger": "caller-stack-unresolved-frame",
                "source_bundle_path": "evidence/files/example/normalized-registry-bundle.json",
                "source_run_id": "example-run",
                "event_index": 1,
                "unresolved_frames": ["ntoskrnl.exe+0x1F234"],
                "resolved_frames": ["nt!PopPowerRequestInitialize"],
            },
            {
                "candidate_id": "power.other",
                "trigger": "caller-stack-unresolved-frame",
            },
        ]

        payload = ghidra_dispatch_batch.dispatch_batch_from_queue(
            rows,
            generated_utc="2026-04-13T00:00:00Z",
            autotrigger_rows=autotrigger_rows,
        )

        self.assertEqual(payload["job_count"], 1)
        self.assertEqual(payload["autotrigger_seed_count"], 2)
        self.assertEqual(payload["autotrigger_matched_job_count"], 1)
        self.assertEqual(payload["autotrigger_unmatched_seed_count"], 1)
        self.assertEqual(payload["jobs"][0]["analysis_mode"], "registry-string-xref+caller-stack-pivot")
        self.assertEqual(payload["jobs"][0]["autotrigger_seed_count"], 1)
        self.assertEqual(
            payload["jobs"][0]["autotrigger_context"][0]["unresolved_frames"],
            ["ntoskrnl.exe+0x1F234"],
        )

    def test_dispatch_batch_marks_missing_inputs_when_binary_cannot_be_inferred(self) -> None:
        rows = [
            {
                "candidate_id": "user.unknown",
                "status": "queued",
                "priority_rank": 1,
                "key_path": "HKCU\\Software\\Example",
                "value_name": "",
            }
        ]

        payload = ghidra_dispatch_batch.dispatch_batch_from_queue(rows, generated_utc="2026-04-13T00:00:00Z")

        self.assertFalse(payload["jobs"][0]["can_run_headless"])
        self.assertEqual(payload["jobs"][0]["missing_inputs"], ["target_binary", "patterns"])
        self.assertIsNone(payload["jobs"][0]["command_argv"])
        self.assertIsNone(payload["jobs"][0]["suggested_command"])


class GhidraDispatchRunnerTests(unittest.TestCase):
    def test_build_run_plan_selects_only_runnable_jobs(self) -> None:
        payload = {
            "jobs": [
                {
                    "job_id": "job-1",
                    "candidate_id": "power.keep",
                    "dispatch_status": "prepared",
                    "can_run_headless": True,
                    "command_argv": ["pwsh", "-File", "tool.ps1"],
                    "suggested_command": "pwsh -File tool.ps1",
                    "output_dir": "evidence/raw/ghidra/job-1",
                },
                {
                    "job_id": "job-2",
                    "candidate_id": "power.skip",
                    "dispatch_status": "prepared",
                    "can_run_headless": False,
                    "command_argv": None,
                },
            ]
        }

        plan = ghidra_dispatch_runner.build_run_plan(payload, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(plan["selected_job_count"], 1)
        self.assertEqual(plan["blocked_job_count"], 1)
        self.assertEqual(plan["jobs"][0]["candidate_id"], "power.keep")
        self.assertEqual(plan["jobs"][0]["command_argv"], ["pwsh", "-File", "tool.ps1"])

    def test_build_run_plan_prioritizes_autotrigger_enriched_jobs(self) -> None:
        payload = {
            "jobs": [
                {
                    "job_id": "job-low",
                    "candidate_id": "power.low",
                    "dispatch_status": "prepared",
                    "can_run_headless": True,
                    "command_argv": ["pwsh", "-File", "tool.ps1"],
                    "analysis_mode": "registry-string-xref",
                    "autotrigger_seed_count": 0,
                    "source_job": {"priority_rank": 1},
                },
                {
                    "job_id": "job-high",
                    "candidate_id": "power.high",
                    "dispatch_status": "prepared",
                    "can_run_headless": True,
                    "command_argv": ["pwsh", "-File", "tool.ps1"],
                    "analysis_mode": "registry-string-xref+caller-stack-pivot",
                    "autotrigger_seed_count": 2,
                    "source_job": {"priority_rank": 5},
                },
            ]
        }

        plan = ghidra_dispatch_runner.build_run_plan(payload, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(plan["jobs"][0]["candidate_id"], "power.high")
        self.assertEqual(plan["jobs"][0]["autotrigger_seed_count"], 2)
        self.assertEqual(plan["jobs"][0]["analysis_mode"], "registry-string-xref+caller-stack-pivot")


class GhidraAutotriggerTests(unittest.TestCase):
    def test_frame_resolution_kind_classifies_common_shapes(self) -> None:
        self.assertEqual(ghidra_autotrigger.frame_resolution_kind("nt!PopReadRegKeyValue"), "resolved_symbol")
        self.assertEqual(ghidra_autotrigger.frame_resolution_kind("ntoskrnl.exe+0x1F234"), "module_offset")
        self.assertEqual(ghidra_autotrigger.frame_resolution_kind("0xfffff80512345678"), "raw_address")
        self.assertEqual(ghidra_autotrigger.frame_resolution_kind("UNKNOWN"), "unknown_marker")

    def test_autotrigger_seeds_match_queued_candidate_and_unresolved_frames(self) -> None:
        bundle = {
            "run_id": "wpr-boot-power-test",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {
                "source_fields": ["Stack", "CallStack"],
            },
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": [
                        "ntoskrnl.exe+0x1F234",
                        "nt!PopPowerRequestInitialize",
                    ],
                },
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": [
                        "nt!PopReadRegKeyValue",
                        "nt!PopPowerRequestInitialize",
                    ],
                },
            ],
        }
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
            }
        ]

        seeds = ghidra_autotrigger.autotrigger_seeds_from_bundle(
            bundle,
            bundle_path="evidence/files/example/normalized-registry-bundle.json",
            queue_rows=queue_rows,
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(len(seeds), 1)
        self.assertEqual(seeds[0]["candidate_id"], "power.control.allow-system-required-power-requests")
        self.assertEqual(seeds[0]["target_binary"], "ntoskrnl.exe")
        self.assertEqual(seeds[0]["unresolved_frames"], ["ntoskrnl.exe+0x1F234"])
        self.assertEqual(seeds[0]["resolved_frames"], ["nt!PopPowerRequestInitialize"])
        self.assertEqual(seeds[0]["suggested_patterns"], ["AllowSystemRequiredPowerRequests"])
        self.assertEqual(seeds[0]["trigger"], "caller-stack-unresolved-frame")

    def test_autotrigger_seeds_resolve_raw_frames_with_module_map(self) -> None:
        bundle = {
            "run_id": "wpr-boot-power-test",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {
                "source_fields": ["Stack"],
                "module_map_count": 1,
            },
            "module_map": [
                {
                    "module_name": "ntoskrnl.exe",
                    "image_path": "\\SystemRoot\\system32\\ntoskrnl.exe",
                    "image_base": "0xFFFFF803C3C00000",
                    "image_end": "0xFFFFF803C5050000",
                }
            ],
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["0xFFFFF803C3FEDD84"],
                }
            ],
        }
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
            }
        ]

        seeds = ghidra_autotrigger.autotrigger_seeds_from_bundle(
            bundle,
            bundle_path="evidence/files/example/normalized-registry-bundle.json",
            queue_rows=queue_rows,
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(seeds[0]["unresolved_frames"], ["ntoskrnl.exe+0x3EDD84"])
        self.assertEqual(seeds[0]["module_map_count"], 1)
        self.assertEqual(seeds[0]["frame_resolution"][0]["resolution_kind"], "module_offset")
        self.assertEqual(seeds[0]["frame_resolution"][0]["resolution_source"], "module_map")

    def test_collect_bundle_paths_supports_bundle_root(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            first = root / "a" / "normalized-registry-bundle.json"
            second = root / "b" / "normalized-registry-bundle.json"
            first.parent.mkdir(parents=True, exist_ok=True)
            second.parent.mkdir(parents=True, exist_ok=True)
            first.write_text("{}", encoding="utf-8")
            second.write_text("{}", encoding="utf-8")

            paths = ghidra_autotrigger.collect_bundle_paths(bundle_root=root)

        self.assertEqual(len(paths), 2)
        self.assertEqual(paths[0].name, "normalized-registry-bundle.json")

    def test_autotrigger_seeds_from_bundle_paths_aggregates_multiple_bundles(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
            }
        ]
        bundle_one = {
            "run_id": "run-one",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"]},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }
        bundle_two = {
            "run_id": "run-two",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"]},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F999", "nt!PopPowerRequestInitialize"],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            one_path = root / "one" / "normalized-registry-bundle.json"
            two_path = root / "two" / "normalized-registry-bundle.json"
            one_path.parent.mkdir(parents=True, exist_ok=True)
            two_path.parent.mkdir(parents=True, exist_ok=True)
            one_path.write_text(json.dumps(bundle_one), encoding="utf-8")
            two_path.write_text(json.dumps(bundle_two), encoding="utf-8")

            seeds = ghidra_autotrigger.autotrigger_seeds_from_bundle_paths(
                [one_path, two_path],
                queue_rows=queue_rows,
                generated_utc="2026-04-13T00:00:00Z",
            )

        self.assertEqual(len(seeds), 2)
        self.assertEqual(seeds[0]["source_run_id"], "run-one")
        self.assertEqual(seeds[1]["source_run_id"], "run-two")


class GhidraSymbolResolutionQueueTests(unittest.TestCase):
    def test_symbol_resolution_queue_groups_actionable_unresolved_frames(self) -> None:
        seed_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "target_binary": "ntoskrnl.exe",
                "source_bundle_path": "evidence/files/example/normalized-registry-bundle.json",
                "source_run_id": "run-one",
                "event_index": 1,
                "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                "unresolved_frames": [
                    "ntoskrnl.exe+0x1F234",
                    "UNKNOWN",
                ],
            },
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "target_binary": "ntoskrnl.exe",
                "source_bundle_path": "evidence/files/example/normalized-registry-bundle.json",
                "source_run_id": "run-two",
                "event_index": 2,
                "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                "unresolved_frames": [
                    "ntoskrnl.exe+0x1F234",
                    "0xfffff80512345678",
                ],
            },
        ]

        payload = ghidra_symbol_queue.symbol_resolution_queue_from_seeds(
            seed_rows,
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["request_count"], 2)
        self.assertEqual(payload["diagnostics"]["actionable_frame_count"], 3)
        self.assertEqual(payload["diagnostics"]["skipped_frame_counts"]["unknown_marker"], 1)
        self.assertEqual(payload["requests"][0]["lookup_key"], "ntoskrnl.exe+0x1f234")
        self.assertEqual(payload["requests"][0]["occurrence_count"], 2)
        self.assertEqual(payload["requests"][0]["suggested_patterns"], ["AllowSystemRequiredPowerRequests"])
        self.assertEqual(
            payload["requests"][0]["candidate_ids"],
            ["power.control.allow-system-required-power-requests"],
        )
        self.assertIn("microsoft-public-symbol-server", payload["requests"][0]["suggested_symbol_sources"])


class GhidraSymbolResolutionBatchTests(unittest.TestCase):
    def test_symbol_resolution_batch_prepares_kvm_guest_jobs(self) -> None:
        queue_payload = {
            "requests": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                    "priority_rank": 1,
                    "lookup_key": "ntoskrnl.exe+0x1f234",
                    "resolution_kind": "module_offset",
                    "target_binary": "ntoskrnl.exe",
                    "candidate_ids": ["power.control.allow-system-required-power-requests"],
                    "candidate_count": 1,
                    "occurrence_count": 2,
                    "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                    "frame_variants": ["ntoskrnl.exe+0x1F234"],
                    "next_action_hint": "Resolve caller.",
                    "source_bundle_paths": ["evidence/files/example/normalized-registry-bundle.json"],
                    "source_run_ids": ["run-one"],
                    "source_event_indices": [1],
                }
            ]
        }
        tool_status = {
            "python3": {"present": True, "path": "/usr/bin/python3"},
            "curl": {"present": True, "path": "/usr/bin/curl"},
            "virsh": {"present": True, "path": "/usr/bin/virsh"},
        }

        batch = ghidra_symbol_batch.symbol_resolution_batch_from_queue(
            queue_payload,
            generated_utc="2026-04-13T00:00:00Z",
            tool_status=tool_status,
        )

        self.assertEqual(batch["job_count"], 1)
        self.assertEqual(batch["runnable_job_count"], 1)
        self.assertEqual(batch["blocked_job_count"], 0)
        self.assertEqual(batch["jobs"][0]["guest_binary_path"], r"C:\Windows\System32\ntoskrnl.exe")
        self.assertTrue(batch["jobs"][0]["can_run_guest_orchestrator"])
        self.assertEqual(batch["jobs"][0]["patterns"], ["AllowSystemRequiredPowerRequests"])
        self.assertEqual(batch["diagnostics"]["resolution_kind_counts"], {"module_offset": 1})
        self.assertEqual(
            batch["jobs"][0]["command_argv"][:3],
            ["python3", "scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py", "--binary-path"],
        )
        self.assertIn("--module-offset", batch["jobs"][0]["command_argv"])
        self.assertIn("ntoskrnl.exe+0x1F234", batch["jobs"][0]["command_argv"])
        self.assertEqual(batch["jobs"][0]["module_offsets"], ["ntoskrnl.exe+0x1F234"])

    def test_symbol_resolution_batch_marks_missing_inputs(self) -> None:
        queue_payload = {
            "requests": [
                {
                    "request_id": "ghidra-symbol-plain-text",
                    "priority_rank": 1,
                    "lookup_key": "ntoskrnl.exe!PlainTextHint",
                    "resolution_kind": "plain_text",
                    "target_binary": "ntoskrnl.exe",
                    "candidate_ids": ["power.keep"],
                    "candidate_count": 1,
                    "occurrence_count": 1,
                    "suggested_patterns": [],
                }
            ]
        }
        tool_status = {
            "python3": {"present": True, "path": "/usr/bin/python3"},
            "curl": {"present": True, "path": "/usr/bin/curl"},
            "virsh": {"present": True, "path": "/usr/bin/virsh"},
        }

        batch = ghidra_symbol_batch.symbol_resolution_batch_from_queue(
            queue_payload,
            generated_utc="2026-04-13T00:00:00Z",
            tool_status=tool_status,
        )

        self.assertFalse(batch["jobs"][0]["can_run_guest_orchestrator"])
        self.assertEqual(batch["blocked_job_count"], 1)
        self.assertEqual(batch["jobs"][0]["missing_inputs"], ["patterns"])
        self.assertEqual(batch["diagnostics"]["missing_input_counts"], {"patterns": 1})
        self.assertEqual(batch["diagnostics"]["blocked_examples"][0]["request_id"], "ghidra-symbol-plain-text")
        self.assertIsNone(batch["jobs"][0]["command_argv"])

    def test_symbol_resolution_batch_blocks_raw_addresses_without_module_base(self) -> None:
        queue_payload = {
            "requests": [
                {
                    "request_id": "ghidra-symbol-raw-address",
                    "priority_rank": 1,
                    "lookup_key": "ntoskrnl.exe@0xfffff80512345678",
                    "resolution_kind": "raw_address",
                    "target_binary": "ntoskrnl.exe",
                    "candidate_ids": ["power.keep"],
                    "candidate_count": 1,
                    "occurrence_count": 1,
                    "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                    "address": "0xfffff80512345678",
                }
            ]
        }
        tool_status = {
            "python3": {"present": True, "path": "/usr/bin/python3"},
            "curl": {"present": True, "path": "/usr/bin/curl"},
            "virsh": {"present": True, "path": "/usr/bin/virsh"},
        }

        batch = ghidra_symbol_batch.symbol_resolution_batch_from_queue(
            queue_payload,
            generated_utc="2026-04-13T00:00:00Z",
            tool_status=tool_status,
        )

        self.assertFalse(batch["jobs"][0]["can_run_guest_orchestrator"])
        self.assertEqual(batch["blocked_job_count"], 1)
        self.assertEqual(batch["jobs"][0]["missing_inputs"], ["module_base"])
        self.assertEqual(batch["diagnostics"]["missing_input_counts"], {"module_base": 1})
        self.assertIsNone(batch["jobs"][0]["command_argv"])

    def test_symbol_resolution_batch_infers_system32_dll_paths(self) -> None:
        self.assertEqual(
            ghidra_symbol_batch.infer_guest_binary_path("KernelBase.dll"),
            r"C:\Windows\System32\KernelBase.dll",
        )
        self.assertEqual(
            ghidra_symbol_batch.infer_guest_binary_path("ntdll.dll"),
            r"C:\Windows\System32\ntdll.dll",
        )

    def test_symbol_resolution_batch_coalesces_module_offsets_for_same_target(self) -> None:
        queue_payload = {
            "requests": [
                {
                    "request_id": "ghidra-symbol-01-kernelbase-dll-0x2e436",
                    "priority_rank": 1,
                    "lookup_key": "KernelBase.dll+0x2e436",
                    "resolution_kind": "module_offset",
                    "target_binary": "KernelBase.dll",
                    "candidate_ids": ["power.control.allow-system-required-power-requests"],
                    "candidate_count": 1,
                    "occurrence_count": 1,
                    "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                    "frame_variants": ["KernelBase.dll+0x2E436"],
                },
                {
                    "request_id": "ghidra-symbol-02-kernelbase-dll-0x2edab",
                    "priority_rank": 2,
                    "lookup_key": "KernelBase.dll+0x2edab",
                    "resolution_kind": "module_offset",
                    "target_binary": "KernelBase.dll",
                    "candidate_ids": ["power.control.allow-system-required-power-requests"],
                    "candidate_count": 1,
                    "occurrence_count": 1,
                    "suggested_patterns": ["AllowSystemRequiredPowerRequests"],
                    "frame_variants": ["KernelBase.dll+0x2EDAB"],
                },
            ]
        }
        tool_status = {
            "python3": {"present": True, "path": "/usr/bin/python3"},
            "curl": {"present": True, "path": "/usr/bin/curl"},
            "virsh": {"present": True, "path": "/usr/bin/virsh"},
        }

        batch = ghidra_symbol_batch.symbol_resolution_batch_from_queue(
            queue_payload,
            generated_utc="2026-04-13T00:00:00Z",
            tool_status=tool_status,
        )

        self.assertEqual(batch["job_count"], 1)
        self.assertEqual(batch["runnable_job_count"], 1)
        self.assertEqual(batch["jobs"][0]["request_count"], 2)
        self.assertEqual(
            batch["jobs"][0]["request_ids"],
            [
                "ghidra-symbol-01-kernelbase-dll-0x2e436",
                "ghidra-symbol-02-kernelbase-dll-0x2edab",
            ],
        )
        self.assertEqual(
            batch["jobs"][0]["module_offsets"],
            ["KernelBase.dll+0x2E436", "KernelBase.dll+0x2EDAB"],
        )


class GhidraSymbolResolutionRunnerTests(unittest.TestCase):
    def test_symbol_resolution_run_plan_selects_runnable_jobs(self) -> None:
        payload = {
            "jobs": [
                {
                    "job_id": "job-1",
                    "request_id": "request-1",
                    "priority_rank": 1,
                    "candidate_count": 1,
                    "occurrence_count": 2,
                    "dispatch_status": "prepared",
                    "can_run_guest_orchestrator": True,
                    "command_argv": ["python3", "scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                    "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                    "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                    "output_dir": "evidence/raw/ghidra/job-1",
                },
                {
                    "job_id": "job-2",
                    "request_id": "request-2",
                    "priority_rank": 2,
                    "candidate_count": 1,
                    "occurrence_count": 1,
                    "dispatch_status": "prepared",
                    "can_run_guest_orchestrator": False,
                    "command_argv": None,
                },
            ]
        }

        plan = ghidra_symbol_runner.build_run_plan(payload, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(plan["selected_job_count"], 1)
        self.assertEqual(plan["blocked_job_count"], 1)
        self.assertEqual(plan["jobs"][0]["request_id"], "request-1")
        self.assertEqual(plan["blocked_jobs"][0]["request_id"], "request-2")

    def test_symbol_resolution_run_plan_skips_completed_jobs(self) -> None:
        with tempfile.TemporaryDirectory() as tmpdir:
            completed_output_dir = Path(tmpdir) / "completed-job"
            completed_output_dir.mkdir(parents=True, exist_ok=True)
            (completed_output_dir / "run-summary.json").write_text(
                json.dumps(
                    {
                        "status": "ok",
                        "ghidra_exit_code": 0,
                    }
                ),
                encoding="utf-8",
            )

            payload = {
                "jobs": [
                    {
                        "job_id": "job-1",
                        "request_id": "request-1",
                        "priority_rank": 1,
                        "candidate_count": 1,
                        "occurrence_count": 2,
                        "dispatch_status": "prepared",
                        "can_run_guest_orchestrator": True,
                        "command_argv": ["python3", "scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                        "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                        "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                        "output_dir": str(completed_output_dir),
                    },
                    {
                        "job_id": "job-2",
                        "request_id": "request-2",
                        "priority_rank": 2,
                        "candidate_count": 1,
                        "occurrence_count": 1,
                        "dispatch_status": "prepared",
                        "can_run_guest_orchestrator": True,
                        "command_argv": ["python3", "scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                        "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                        "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                        "output_dir": "evidence/raw/ghidra/job-2",
                    },
                ]
            }

            plan = ghidra_symbol_runner.build_run_plan(payload, generated_utc="2026-04-13T00:00:00Z")

        self.assertEqual(plan["selected_job_count"], 1)
        self.assertEqual(plan["completed_job_count"], 1)
        self.assertEqual(plan["blocked_job_count"], 0)
        self.assertEqual(plan["jobs"][0]["request_id"], "request-2")
        self.assertEqual(plan["completed_jobs"][0]["request_id"], "request-1")

    def test_symbol_resolution_run_executes_only_incomplete_jobs(self) -> None:
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            completed_output_dir = Path(tmpdir) / "completed-job"
            completed_output_dir.mkdir(parents=True, exist_ok=True)
            (completed_output_dir / "run-summary.json").write_text(
                json.dumps(
                    {
                        "status": "ok",
                        "ghidra_exit_code": 0,
                    }
                ),
                encoding="utf-8",
            )

            payload = {
                "jobs": [
                    {
                        "job_id": "job-1",
                        "request_id": "request-1",
                        "dispatch_status": "prepared",
                        "can_run_guest_orchestrator": True,
                        "command_argv": ["python3", "completed.py"],
                        "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                        "output_dir": str(completed_output_dir),
                    },
                    {
                        "job_id": "job-2",
                        "request_id": "request-2",
                        "dispatch_status": "prepared",
                        "can_run_guest_orchestrator": True,
                        "command_argv": ["python3", "pending.py"],
                        "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                        "output_dir": str(tmp_path / "job-2"),
                    },
                ]
            }
            bridge_dir = tmp_path / "bridge"
            bridge_dir.mkdir(parents=True, exist_ok=True)
            (bridge_dir / "job-2-summary.json").write_text(
                json.dumps({"status": "ok", "ghidra_exit_code": 0}),
                encoding="utf-8",
            )
            (bridge_dir / "job-2-evidence.json").write_text('{"probe":"job-2"}', encoding="utf-8")
            (bridge_dir / "job-2-ghidra-matches.md").write_text("# job-2", encoding="utf-8")

            with unittest.mock.patch.object(ghidra_symbol_runner, "runner_available", return_value=True):
                with unittest.mock.patch.object(
                    ghidra_symbol_runner.subprocess,
                    "run",
                    return_value=subprocess.CompletedProcess(
                        args=["python3", "pending.py"],
                        returncode=0,
                        stdout="ok",
                        stderr="",
                    ),
                ) as mock_run:
                    result = ghidra_symbol_runner.run_jobs(
                        payload,
                        generated_utc="2026-04-13T00:00:00Z",
                    )

        self.assertEqual(result["selected_job_count"], 1)
        self.assertEqual(result["executed_job_count"], 1)
        self.assertEqual(result["completed_job_count"], 1)
        self.assertEqual(result["jobs"][0]["request_id"], "request-2")
        self.assertEqual(
            result["jobs"][0]["materialized_files"],
            [],
        )
        mock_run.assert_called_once()

    def test_materialize_bridge_artifacts_copies_expected_files(self) -> None:
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            bridge_dir = tmp_path / "bridge"
            bridge_dir.mkdir(parents=True, exist_ok=True)
            output_dir = tmp_path / "evidence" / "job-3"

            (bridge_dir / "job-3-summary.json").write_text(
                json.dumps({"status": "ok", "ghidra_exit_code": 0}),
                encoding="utf-8",
            )
            (bridge_dir / "job-3-evidence.json").write_text('{"probe":"job-3"}', encoding="utf-8")
            (bridge_dir / "job-3-ghidra-matches.md").write_text("# job-3", encoding="utf-8")
            (bridge_dir / "job-3-symchk.txt").write_text("symchk", encoding="utf-8")

            materialized = ghidra_symbol_runner.materialize_bridge_artifacts(
                {"output_dir": str(output_dir)},
                bridge_dir=bridge_dir,
            )
            self.assertEqual(
                materialized,
                ["evidence.json", "ghidra-matches.md", "run-summary.json", "symchk.txt"],
            )
            self.assertTrue((output_dir / "evidence.json").exists())
            self.assertTrue((output_dir / "ghidra-matches.md").exists())
            self.assertTrue((output_dir / "run-summary.json").exists())
            self.assertTrue((output_dir / "symchk.txt").exists())


class GhidraSymbolResolutionHandoffTests(unittest.TestCase):
    def test_handoff_payload_summarizes_selected_and_blocked_jobs(self) -> None:
        batch = {
            "job_count": 2,
            "runnable_job_count": 1,
            "blocked_job_count": 1,
            "missing_host_tools": [],
            "diagnostics": {
                "resolution_kind_counts": {"module_offset": 1, "plain_text": 1},
                "missing_input_counts": {"patterns": 1},
                "blocked_examples": [{"request_id": "request-2"}],
            },
            "jobs": [
                {
                    "job_id": "job-1",
                    "request_id": "request-1",
                    "lookup_key": "ntoskrnl.exe+0x1f234",
                    "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                    "candidate_ids": ["power.keep"],
                    "target_binary": "ntoskrnl.exe",
                    "guest_binary_path": r"C:\Windows\System32\ntoskrnl.exe",
                    "patterns": ["AllowSystemRequiredPowerRequests"],
                    "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                    "output_dir": "evidence/raw/ghidra/job-1",
                    "missing_inputs": [],
                    "missing_host_tools": [],
                    "can_run_guest_orchestrator": True,
                },
                {
                    "job_id": "job-2",
                    "request_id": "request-2",
                    "lookup_key": "ntoskrnl.exe!PlainTextHint",
                    "analysis_mode": "pdb-symbolized-branch+caller-stack-resolution",
                    "candidate_ids": ["power.other"],
                    "target_binary": "ntoskrnl.exe",
                    "guest_binary_path": r"C:\Windows\System32\ntoskrnl.exe",
                    "patterns": [],
                    "suggested_command": None,
                    "output_dir": "evidence/raw/ghidra/job-2",
                    "missing_inputs": ["patterns"],
                    "missing_host_tools": [],
                    "can_run_guest_orchestrator": False,
                },
            ],
        }
        run = {
            "mode": "dry-run",
            "runner_available": True,
            "selected_job_count": 1,
            "blocked_job_count": 1,
            "jobs": [
                {
                    "job_id": "job-1",
                    "request_id": "request-1",
                }
            ],
            "blocked_jobs": [
                {
                    "job_id": "job-2",
                    "request_id": "request-2",
                }
            ],
        }

        payload = ghidra_symbol_handoff.handoff_payload(
            batch,
            run,
            batch_path=Path("/tmp/batch.json"),
            run_path=Path("/tmp/run.json"),
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["handoff_status"], "ready")
        self.assertEqual(payload["operator"]["blocker"], "symbol-resolution-ready")
        self.assertEqual(payload["counts"]["prepared_jobs"], 2)
        self.assertEqual(payload["counts"]["selected_jobs"], 1)
        self.assertEqual(payload["counts"]["blocked_jobs"], 1)
        self.assertEqual(payload["candidate_ids"], ["power.keep", "power.other"])
        self.assertEqual(payload["selected_jobs"][0]["request_id"], "request-1")
        self.assertEqual(payload["blocked_jobs"][0]["request_id"], "request-2")

        markdown = ghidra_symbol_handoff.render_markdown(payload)
        self.assertIn("# Ghidra Symbol Resolution Handoff", markdown)
        self.assertIn("request-1", markdown)
        self.assertIn("request-2", markdown)


class GhidraSymbolResolutionTransferTests(unittest.TestCase):
    def test_transfer_payload_packages_selected_jobs_and_repo_paths(self) -> None:
        handoff = {
            "handoff_status": "ready",
            "required_host_tools": ["python3", "curl", "virsh"],
            "counts": {
                "selected_jobs": 1,
                "blocked_jobs": 1,
            },
            "selected_jobs": [
                {
                    "request_id": "request-1",
                    "job_id": "job-1",
                    "target_binary": "ntoskrnl.exe",
                    "guest_binary_path": r"C:\Windows\System32\ntoskrnl.exe",
                    "patterns": ["AllowSystemRequiredPowerRequests"],
                    "candidate_ids": ["power.keep"],
                    "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --pattern AllowSystemRequiredPowerRequests",
                    "output_dir": "evidence/raw/ghidra/job-1",
                }
            ],
            "blocked_jobs": [
                {
                    "request_id": "request-2",
                    "missing_inputs": ["patterns"],
                    "missing_host_tools": [],
                }
            ],
        }

        payload = ghidra_symbol_transfer.transfer_payload(
            handoff,
            handoff_path=Path("/tmp/handoff.json"),
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["transfer_status"], "ready")
        self.assertEqual(payload["operator"]["blocker"], "transfer-pack-ready")
        self.assertEqual(payload["counts"]["selected_jobs"], 1)
        self.assertEqual(payload["counts"]["candidate_count"], 1)
        self.assertIn("scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py", payload["required_repo_paths"])
        self.assertEqual(payload["jobs"][0]["request_id"], "request-1")

        markdown = ghidra_symbol_transfer.render_markdown(payload)
        self.assertIn("# Ghidra Symbol Resolution Transfer", markdown)
        self.assertIn("request-1", markdown)


class GhidraSymbolResolutionTransferPackTests(unittest.TestCase):
    def test_materialize_transfer_pack_writes_repo_files_commands_and_archive(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer = {
                "source_handoff_path": handoff_path.as_posix(),
                "transfer_status": "ready",
                "operator": {
                    "blocker": "transfer-pack-ready",
                    "next_action": "Copy the listed repo files and use the exported commands on the destination KVM-capable host.",
                },
                "counts": {
                    "selected_jobs": 1,
                },
                "required_repo_paths": [
                    "scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                    "scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1",
                ],
                "jobs": [
                    {
                        "request_id": "request-1",
                        "target_binary": "ntoskrnl.exe",
                        "guest_binary_path": r"C:\Windows\System32\ntoskrnl.exe",
                        "candidate_ids": ["power.keep"],
                        "patterns": ["AllowSystemRequiredPowerRequests"],
                        "suggested_command": "python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --pattern AllowSystemRequiredPowerRequests",
                    }
                ],
                "candidate_ids": ["power.keep"],
            }
            transfer_path.write_text(json.dumps(transfer), encoding="utf-8")
            transfer_markdown_path.write_text("# transfer\n", encoding="utf-8")
            handoff_path.write_text(json.dumps({"handoff_status": "ready"}), encoding="utf-8")
            handoff_markdown_path.write_text("# handoff\n", encoding="utf-8")

            output_root = temp_root / "pack"
            summary_path = temp_root / "pack-summary.json"
            markdown_path = temp_root / "pack-summary.md"
            archive_path = temp_root / "pack.zip"

            payload = ghidra_symbol_transfer_pack.materialize_transfer_pack(
                transfer,
                transfer_path=transfer_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=markdown_path,
                archive_path=archive_path,
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["pack_status"], "ready")
            self.assertEqual(payload["counts"]["selected_jobs"], 1)
            self.assertTrue((output_root / "repo" / "scripts" / "vm-kvm" / "run-guest-ghidra-symbolized-probe.py").exists())
            self.assertTrue((output_root / "commands" / "01-request-1.txt").exists())
            self.assertTrue((output_root / "README.md").exists())
            self.assertIn(
                "check_ghidra_transfer_pack_execution_run.py",
                (output_root / "README.md").read_text(encoding="utf-8"),
            )
            self.assertTrue((output_root / "CHECKSUMS.json").exists())
            self.assertGreater(payload["counts"]["pack_files_checksummed"], 0)
            self.assertTrue(any(item["path"] == "CHECKSUMS.json" for item in payload["pack_files"]))
            self.assertEqual(len(payload["archive"]["sha256"]), 64)
            self.assertTrue(summary_path.exists())
            self.assertTrue(markdown_path.exists())
            self.assertTrue(archive_path.exists())

            check_payload = ghidra_symbol_transfer_pack_check.validate_transfer_pack(
                json.loads(summary_path.read_text(encoding="utf-8")),
                summary_path=summary_path,
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(check_payload["check_status"], "ok")
            self.assertEqual(check_payload["counts"]["checked_pack_files"], payload["counts"]["pack_files_checksummed"])
            self.assertEqual(check_payload["counts"]["checked_archive_files"], payload["counts"]["pack_files_checksummed"])
            self.assertEqual(check_payload["counts"]["archive_entries"], payload["counts"]["pack_files_checksummed"])
            self.assertEqual(check_payload["counts"]["command_files"], 1)

    def test_transfer_pack_check_reports_hash_mismatch(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer = {
                "transfer_status": "ready",
                "counts": {"selected_jobs": 1},
                "required_repo_paths": ["scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                "jobs": [{"request_id": "request-1", "suggested_command": "echo hi"}],
                "candidate_ids": ["power.keep"],
            }
            transfer_path.write_text(json.dumps(transfer), encoding="utf-8")
            output_root = temp_root / "pack"
            summary_path = temp_root / "pack-summary.json"
            payload = ghidra_symbol_transfer_pack.materialize_transfer_pack(
                transfer,
                transfer_path=transfer_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=temp_root / "pack-summary.md",
                archive_path=temp_root / "pack.zip",
                generated_utc="2026-04-13T00:00:00Z",
            )
            command_path = output_root / "commands" / payload["command_files"][0]
            command_path.write_text("tampered\n", encoding="utf-8")

            check_payload = ghidra_symbol_transfer_pack_check.validate_transfer_pack(
                json.loads(summary_path.read_text(encoding="utf-8")),
                summary_path=summary_path,
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(check_payload["check_status"], "error")
            self.assertTrue(any("sha256 mismatch" in error for error in check_payload["errors"]))

    def test_transfer_pack_check_can_validate_archive_without_extracted_pack_root(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer = {
                "transfer_status": "ready",
                "counts": {"selected_jobs": 1},
                "required_repo_paths": ["scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                "jobs": [{"request_id": "request-1", "suggested_command": "echo hi"}],
                "candidate_ids": ["power.keep"],
            }
            transfer_path.write_text(json.dumps(transfer), encoding="utf-8")
            output_root = temp_root / "pack"
            summary_path = temp_root / "pack-summary.json"
            ghidra_symbol_transfer_pack.materialize_transfer_pack(
                transfer,
                transfer_path=transfer_path,
                output_root=output_root,
                summary_path=summary_path,
                markdown_path=temp_root / "pack-summary.md",
                archive_path=temp_root / "pack.zip",
                generated_utc="2026-04-13T00:00:00Z",
            )
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            shutil.rmtree(output_root)

            check_payload = ghidra_symbol_transfer_pack_check.validate_transfer_pack(
                summary,
                summary_path=summary_path,
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(check_payload["check_status"], "ok")
            self.assertEqual(check_payload["counts"]["checked_pack_files"], 0)
            self.assertEqual(check_payload["counts"]["checked_archive_files"], summary["counts"]["pack_files_checksummed"])

    def test_unpack_transfer_pack_validates_and_extracts_archive(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer = {
                "transfer_status": "ready",
                "counts": {"selected_jobs": 1},
                "required_repo_paths": ["scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                "jobs": [{"request_id": "request-1", "suggested_command": "echo hi"}],
                "candidate_ids": ["power.keep"],
            }
            transfer_path.write_text(json.dumps(transfer), encoding="utf-8")
            pack_root = temp_root / "pack"
            summary_path = temp_root / "pack-summary.json"
            ghidra_symbol_transfer_pack.materialize_transfer_pack(
                transfer,
                transfer_path=transfer_path,
                output_root=pack_root,
                summary_path=summary_path,
                markdown_path=temp_root / "pack-summary.md",
                archive_path=temp_root / "pack.zip",
                generated_utc="2026-04-13T00:00:00Z",
            )
            summary = json.loads(summary_path.read_text(encoding="utf-8"))
            shutil.rmtree(pack_root)

            import_root = temp_root / "import"
            payload = ghidra_symbol_transfer_pack_unpack.unpack_transfer_pack(
                summary,
                summary_path=summary_path,
                output_root=import_root,
                output_path=temp_root / "import.json",
                markdown_path=temp_root / "import.md",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["import_status"], "ok")
            self.assertEqual(payload["counts"]["extracted_files"], summary["counts"]["pack_files_checksummed"])
            self.assertTrue((import_root / "CHECKSUMS.json").exists())
            self.assertTrue((import_root / "commands" / summary["command_files"][0]).exists())

    def test_execution_plan_from_import_rewrites_commands_to_imported_repo_root(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            import_root = temp_root / "import"
            command_root = import_root / "commands"
            script_path = import_root / "repo" / "scripts" / "vm-kvm" / "run-guest-ghidra-symbolized-probe.py"
            command_root.mkdir(parents=True, exist_ok=True)
            script_path.parent.mkdir(parents=True, exist_ok=True)
            script_path.write_text("# runner\n", encoding="utf-8")
            (command_root / "01-request-1.txt").write_text(
                "\n".join(
                    [
                        "request_id: request-1",
                        "target_binary: ntoskrnl.exe",
                        r"guest_binary_path: C:\Windows\System32\ntoskrnl.exe",
                        "candidate_ids: power.keep",
                        "patterns: AllowSystemRequiredPowerRequests",
                        "",
                        r"python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --pattern AllowSystemRequiredPowerRequests",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            import_payload = {
                "import_status": "ok",
                "output_root": import_root.as_posix(),
                "errors": [],
            }

            payload = ghidra_transfer_pack_execution.execution_plan_from_import(
                import_payload,
                import_path=temp_root / "import.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["execution_plan_status"], "ready")
            self.assertEqual(payload["counts"]["ready_jobs"], 1)
            self.assertEqual(payload["jobs"][0]["request_id"], "request-1")
            self.assertTrue(payload["jobs"][0]["destination_command"].startswith("python3 repo/scripts/"))
            self.assertEqual(payload["jobs"][0]["destination_argv"][3], r"C:\Windows\System32\ntoskrnl.exe")
            self.assertIn("'C:\\Windows\\System32\\ntoskrnl.exe'", payload["jobs"][0]["destination_shell_command"])
            self.assertEqual(payload["jobs"][0]["missing_inputs"], [])

    def test_execution_plan_blocks_missing_import_runner_script(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            import_root = temp_root / "import"
            command_root = import_root / "commands"
            command_root.mkdir(parents=True, exist_ok=True)
            (command_root / "01-request-1.txt").write_text(
                "request_id: request-1\n\npython3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --pattern Example\n",
                encoding="utf-8",
            )

            payload = ghidra_transfer_pack_execution.execution_plan_from_import(
                {"import_status": "ok", "output_root": import_root.as_posix(), "errors": []},
                import_path=temp_root / "import.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["execution_plan_status"], "blocked")
            self.assertEqual(payload["counts"]["blocked_jobs"], 1)
            self.assertTrue(any(item.startswith("missing-script:") for item in payload["blocked_jobs"][0]["missing_inputs"]))

    def test_execution_run_from_plan_dry_run_surfaces_ready_commands(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            import_root = temp_root / "import"
            import_root.mkdir(parents=True, exist_ok=True)
            plan_payload = {
                "execution_plan_status": "ready",
                "import_root": import_root.as_posix(),
                "errors": [],
                "jobs": [
                    {
                        "request_id": "request-1",
                        "candidate_ids": ["power.keep"],
                        "destination_argv": [
                            "python3",
                            "repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                            "--binary-path",
                            r"C:\Windows\System32\ntoskrnl.exe",
                        ],
                        "destination_shell_command": r"python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path 'C:\Windows\System32\ntoskrnl.exe'",
                        "missing_inputs": [],
                    }
                ],
                "blocked_jobs": [],
            }

            payload = ghidra_transfer_pack_execution_run.execution_run_from_plan(
                plan_payload,
                plan_path=temp_root / "execution-plan.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["execution_run_status"], "ready")
            self.assertEqual(payload["mode"], "dry-run")
            self.assertEqual(payload["operator"]["blocker"], "execution-run-ready")
            self.assertEqual(payload["counts"]["planned_jobs"], 1)
            self.assertEqual(payload["counts"]["ready_jobs"], 1)
            self.assertEqual(payload["counts"]["executed_jobs"], 0)
            self.assertEqual(payload["jobs"][0]["cwd"], import_root.resolve().as_posix())
            self.assertEqual(payload["jobs"][0]["argv"][3], r"C:\Windows\System32\ntoskrnl.exe")

    def test_execution_run_blocks_when_plan_is_not_ready(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            import_root = temp_root / "import"
            import_root.mkdir(parents=True, exist_ok=True)
            payload = ghidra_transfer_pack_execution_run.execution_run_from_plan(
                {
                    "execution_plan_status": "blocked",
                    "import_root": import_root.as_posix(),
                    "errors": [],
                    "jobs": [],
                    "blocked_jobs": [
                        {
                            "request_id": "request-1",
                            "destination_argv": [],
                            "destination_shell_command": "",
                            "missing_inputs": ["missing-script:repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                        }
                    ],
                },
                plan_path=temp_root / "execution-plan.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["execution_run_status"], "blocked")
            self.assertEqual(payload["operator"]["blocker"], "execution-run-blocked")
            self.assertEqual(payload["counts"]["blocked_jobs"], 1)
            self.assertIn("execution_plan_status is not ready", payload["errors"])

    def test_execution_run_check_accepts_ready_dry_run(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            import_root = temp_root / "import"
            import_root.mkdir(parents=True, exist_ok=True)
            payload = ghidra_transfer_pack_execution_run_check.validate_execution_run(
                {
                    "execution_run_status": "ready",
                    "mode": "dry-run",
                    "counts": {
                        "planned_jobs": 1,
                        "ready_jobs": 1,
                        "blocked_jobs": 0,
                        "executed_jobs": 0,
                    },
                    "jobs": [
                        {
                            "request_id": "request-1",
                            "cwd": import_root.as_posix(),
                            "argv": ["python3", "repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py"],
                            "command": "python3 repo/scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py",
                            "ready": True,
                            "executed": False,
                            "missing_inputs": [],
                        }
                    ],
                    "blocked_jobs": [],
                },
                run_path=temp_root / "execution-run.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["check_status"], "ok")
            self.assertEqual(payload["counts"]["ready_jobs"], 1)

    def test_execution_run_check_rejects_count_mismatch(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            payload = ghidra_transfer_pack_execution_run_check.validate_execution_run(
                {
                    "execution_run_status": "ready",
                    "mode": "dry-run",
                    "counts": {
                        "planned_jobs": 2,
                        "ready_jobs": 2,
                        "blocked_jobs": 0,
                        "executed_jobs": 0,
                    },
                    "jobs": [
                        {
                            "request_id": "request-1",
                            "cwd": (temp_root / "missing").as_posix(),
                            "argv": [],
                            "command": "",
                            "ready": True,
                            "missing_inputs": [],
                        }
                    ],
                    "blocked_jobs": [],
                },
                run_path=temp_root / "execution-run.json",
                generated_utc="2026-04-13T00:00:00Z",
            )

            self.assertEqual(payload["check_status"], "error")
            self.assertTrue(any("planned_jobs mismatch" in error for error in payload["errors"]))
            self.assertTrue(any("argv" in error for error in payload["errors"]))


class GhidraAutotriggerPipelineTests(unittest.TestCase):
    def test_refresh_pipeline_writes_seed_batch_and_run_surfaces(self) -> None:
        bundle = {
            "run_id": "synthetic-stack-seed",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"]},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            bundle_path = temp_root / "normalized-registry-bundle.json"
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            seeds_path = temp_root / "ghidra-autotrigger-seeds.jsonl"
            symbol_queue_path = temp_root / "ghidra-symbol-resolution-queue.json"
            symbol_batch_path = temp_root / "ghidra-symbol-resolution-batch.json"
            symbol_run_path = temp_root / "ghidra-symbol-resolution-run.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            batch_path = temp_root / "ghidra-dispatch-batch.json"
            run_path = temp_root / "ghidra-dispatch-run.json"
            health_path = temp_root / "ghidra-autotrigger-health.json"
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")

            payload = ghidra_refresh_pipeline.refresh_pipeline(
                bundle_path,
                queue_path=queue_path,
                seeds_path=seeds_path,
                symbol_queue_path=symbol_queue_path,
                symbol_batch_path=symbol_batch_path,
                symbol_run_path=symbol_run_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                batch_path=batch_path,
                run_path=run_path,
                health_path=health_path,
            )

            self.assertEqual(payload["seed_count"], 1)
            self.assertEqual(payload["symbol_resolution_request_count"], 1)
            self.assertEqual(payload["symbol_resolution_batch_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_run_selected_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_handoff_status"], "ready")
            self.assertEqual(payload["symbol_resolution_handoff_selected_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_transfer_status"], "ready")
            self.assertEqual(payload["symbol_resolution_transfer_selected_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_transfer_pack_status"], "ready")
            self.assertEqual(payload["symbol_resolution_transfer_pack_selected_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_transfer_pack_check_status"], "ok")
            self.assertEqual(payload["symbol_resolution_transfer_pack_check_error_count"], 0)
            self.assertEqual(payload["dispatch_autotrigger_matched_job_count"], 1)
            self.assertEqual(payload["run_plan_selected_job_count"], 1)

            seeds_rows = [json.loads(line) for line in seeds_path.read_text(encoding="utf-8").splitlines() if line.strip()]
            self.assertEqual(seeds_rows[0]["candidate_id"], "power.control.allow-system-required-power-requests")

            symbol_queue_payload = json.loads(symbol_queue_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_queue_payload["request_count"], 1)
            self.assertEqual(symbol_queue_payload["requests"][0]["lookup_key"], "ntoskrnl.exe+0x1f234")
            symbol_batch_payload = json.loads(symbol_batch_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_batch_payload["job_count"], 1)
            symbol_run_payload = json.loads(symbol_run_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_run_payload["selected_job_count"], 1)
            handoff_payload = json.loads(handoff_path.read_text(encoding="utf-8"))
            self.assertEqual(handoff_payload["handoff_status"], "ready")
            self.assertTrue(handoff_markdown_path.exists())
            transfer_payload = json.loads(transfer_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_payload["transfer_status"], "ready")
            self.assertTrue(transfer_markdown_path.exists())
            transfer_pack_payload = json.loads(transfer_pack_summary_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_pack_payload["pack_status"], "ready")
            self.assertTrue(transfer_pack_markdown_path.exists())
            self.assertTrue(transfer_pack_archive_path.exists())
            transfer_pack_check_payload = json.loads(transfer_pack_check_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_pack_check_payload["check_status"], "ok")
            self.assertTrue(transfer_pack_check_markdown_path.exists())
            execution_run_path = temp_root / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
            execution_run_payload = json.loads(execution_run_path.read_text(encoding="utf-8"))
            self.assertEqual(execution_run_payload["execution_run_status"], "ready")
            self.assertEqual(execution_run_payload["counts"]["ready_jobs"], 1)
            execution_run_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json"
            execution_run_check_payload = json.loads(execution_run_check_path.read_text(encoding="utf-8"))
            self.assertEqual(execution_run_check_payload["check_status"], "ok")

            batch_payload = json.loads(batch_path.read_text(encoding="utf-8"))
            self.assertEqual(batch_payload["jobs"][0]["autotrigger_seed_count"], 1)

            run_payload = json.loads(run_path.read_text(encoding="utf-8"))
            self.assertEqual(run_payload["selected_job_count"], 1)

            health_payload = json.loads(health_path.read_text(encoding="utf-8"))
            self.assertEqual(health_payload["counts"]["autotrigger_seeds"], 1)
            self.assertEqual(health_payload["counts"]["symbol_resolution_requests"], 1)
            self.assertEqual(health_payload["counts"]["symbol_resolution_transfer_pack_selected_jobs"], 1)
            self.assertEqual(health_payload["symbol_resolution_transfer_pack_check"]["status"], "ok")
            self.assertEqual(health_payload["symbol_resolution_execution_run"]["status"], "ready")
            self.assertEqual(health_payload["symbol_resolution_execution_run_check"]["status"], "ok")
            self.assertEqual(payload["outputs"]["health_path"], health_path.as_posix())

    def test_refresh_pipeline_supports_bundle_root(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]
        bundle = {
            "run_id": "synthetic-stack-seed",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"]},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            bundle_root = temp_root / "evidence"
            bundle_path = bundle_root / "sample" / "normalized-registry-bundle.json"
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            seeds_path = temp_root / "ghidra-autotrigger-seeds.jsonl"
            symbol_queue_path = temp_root / "ghidra-symbol-resolution-queue.json"
            symbol_batch_path = temp_root / "ghidra-symbol-resolution-batch.json"
            symbol_run_path = temp_root / "ghidra-symbol-resolution-run.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            batch_path = temp_root / "ghidra-dispatch-batch.json"
            run_path = temp_root / "ghidra-dispatch-run.json"
            health_path = temp_root / "ghidra-autotrigger-health.json"
            bundle_path.parent.mkdir(parents=True, exist_ok=True)
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")

            payload = ghidra_refresh_pipeline.refresh_pipeline(
                bundle_root=bundle_root,
                queue_path=queue_path,
                seeds_path=seeds_path,
                symbol_queue_path=symbol_queue_path,
                symbol_batch_path=symbol_batch_path,
                symbol_run_path=symbol_run_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                batch_path=batch_path,
                run_path=run_path,
                health_path=health_path,
            )

            self.assertEqual(payload["bundle_count"], 1)
            self.assertEqual(payload["seed_count"], 1)
            self.assertEqual(payload["dispatch_autotrigger_matched_job_count"], 1)
            self.assertEqual(payload["symbol_resolution_transfer_pack_status"], "ready")
            self.assertTrue(health_path.exists())

    def test_refresh_pipeline_supports_bundle_manifest(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]
        bundle = {
            "run_id": "synthetic-stack-seed",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"], "captured_event_count": 1},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            bundle_path = temp_root / "evidence" / "sample" / "normalized-registry-bundle.json"
            manifest_path = temp_root / "ghidra-autotrigger-inputs.json"
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            seeds_path = temp_root / "ghidra-autotrigger-seeds.jsonl"
            symbol_queue_path = temp_root / "ghidra-symbol-resolution-queue.json"
            symbol_batch_path = temp_root / "ghidra-symbol-resolution-batch.json"
            symbol_run_path = temp_root / "ghidra-symbol-resolution-run.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            batch_path = temp_root / "ghidra-dispatch-batch.json"
            run_path = temp_root / "ghidra-dispatch-run.json"
            health_path = temp_root / "ghidra-autotrigger-health.json"
            bundle_path.parent.mkdir(parents=True, exist_ok=True)
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")
            manifest_path.write_text(
                json.dumps({"entries": [{"path": bundle_path.as_posix()}]}),
                encoding="utf-8",
            )

            payload = ghidra_refresh_pipeline.refresh_pipeline(
                bundle_manifest_path=manifest_path,
                queue_path=queue_path,
                seeds_path=seeds_path,
                symbol_queue_path=symbol_queue_path,
                symbol_batch_path=symbol_batch_path,
                symbol_run_path=symbol_run_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                batch_path=batch_path,
                run_path=run_path,
                health_path=health_path,
            )

            self.assertEqual(payload["bundle_count"], 1)
            self.assertEqual(payload["outputs"]["bundle_manifest_path"], manifest_path.as_posix())

    def test_refresh_pipeline_can_refresh_manifest_from_discovered_roots(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]
        bundle = {
            "run_id": "synthetic-stack-seed",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"], "captured_event_count": 1},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            evidence_root = temp_root / "evidence"
            bundle_path = evidence_root / "sample" / "normalized-registry-bundle.json"
            manifest_path = temp_root / "ghidra-autotrigger-inputs.json"
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            seeds_path = temp_root / "ghidra-autotrigger-seeds.jsonl"
            symbol_queue_path = temp_root / "ghidra-symbol-resolution-queue.json"
            symbol_batch_path = temp_root / "ghidra-symbol-resolution-batch.json"
            symbol_run_path = temp_root / "ghidra-symbol-resolution-run.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            batch_path = temp_root / "ghidra-dispatch-batch.json"
            run_path = temp_root / "ghidra-dispatch-run.json"
            health_path = temp_root / "ghidra-autotrigger-health.json"
            bundle_path.parent.mkdir(parents=True, exist_ok=True)
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")

            payload = ghidra_refresh_pipeline.refresh_pipeline(
                bundle_manifest_path=manifest_path,
                refresh_bundle_manifest=True,
                input_roots=[evidence_root],
                queue_path=queue_path,
                seeds_path=seeds_path,
                symbol_queue_path=symbol_queue_path,
                symbol_batch_path=symbol_batch_path,
                symbol_run_path=symbol_run_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                batch_path=batch_path,
                run_path=run_path,
                health_path=health_path,
            )

            manifest_payload = json.loads(manifest_path.read_text(encoding="utf-8"))
            self.assertTrue(payload["bundle_manifest_refreshed"])
            self.assertEqual(payload["bundle_manifest_selected_count"], 1)
            self.assertEqual(manifest_payload["selected_count"], 1)


class GhidraAutotriggerInputTests(unittest.TestCase):
    def test_input_manifest_discovers_matching_stack_bundles(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
            }
        ]
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            with_stack = root / "with-stack" / "normalized-registry-bundle.json"
            no_stack = root / "no-stack" / "normalized-registry-bundle.json"
            with_stack.parent.mkdir(parents=True, exist_ok=True)
            no_stack.parent.mkdir(parents=True, exist_ok=True)
            with_stack.write_text(
                json.dumps(
                    {
                        "run_id": "with-stack",
                        "source_tool": "wpr",
                        "capture_phase": "boot",
                        "stack_capture": {"captured_event_count": 2},
                        "event_count": 2,
                        "events": [
                            {
                                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                                "value_name": "AllowSystemRequiredPowerRequests",
                                "operation": "RegQueryValue",
                                "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                            }
                        ],
                    }
                ),
                encoding="utf-8",
            )
            no_stack.write_text(
                json.dumps(
                    {
                        "run_id": "no-stack",
                        "source_tool": "wpr",
                        "capture_phase": "boot",
                        "stack_capture": {"captured_event_count": 0},
                        "event_count": 1,
                        "events": [],
                    }
                ),
                encoding="utf-8",
            )

            payload = ghidra_autotrigger_inputs.input_manifest(
                [root],
                queue_rows=queue_rows,
                require_caller_stack=True,
                require_queue_match=True,
                generated_utc="2026-04-13T00:00:00Z",
            )

        self.assertEqual(payload["selected_count"], 1)
        self.assertEqual(payload["entries"][0]["run_id"], "with-stack")
        self.assertEqual(payload["entries"][0]["caller_stack_event_count"], 2)
        self.assertEqual(payload["entries"][0]["matched_candidate_count"], 1)
        self.assertEqual(
            payload["entries"][0]["matched_candidate_ids"],
            ["power.control.allow-system-required-power-requests"],
        )
        self.assertEqual(payload["diagnostics"]["scanned_bundle_count"], 2)
        self.assertEqual(payload["diagnostics"]["caller_stack_capable_bundle_count"], 1)
        self.assertEqual(payload["diagnostics"]["skipped_reason_counts"]["no-caller-stack"], 1)
        self.assertEqual(payload["diagnostics"]["skipped_reason_counts"]["no-queue-match"], 0)


class GhidraAutotriggerHealthTests(unittest.TestCase):
    def test_health_payload_summarizes_queue_seed_batch_and_run_surfaces(self) -> None:
        input_manifest = {
            "entries": [
                {"path": "evidence/files/example/normalized-registry-bundle.json"},
            ]
        }
        queue_rows = [
            {"candidate_id": "power.keep"},
            {"candidate_id": "power.other"},
        ]
        seed_rows = [
            {"candidate_id": "power.keep"},
        ]
        symbol_queue = {
            "requests": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                    "lookup_key": "ntoskrnl.exe+0x1f234",
                    "candidate_ids": ["power.keep"],
                }
            ]
        }
        symbol_batch = {
            "job_count": 1,
            "runnable_job_count": 1,
            "jobs": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                }
            ],
        }
        symbol_run = {
            "mode": "dry-run",
            "runner_available": True,
            "selected_job_count": 1,
            "blocked_job_count": 0,
            "error": None,
        }
        handoff = {
            "handoff_status": "ready",
            "operator": {"blocker": "symbol-resolution-ready"},
            "counts": {"selected_jobs": 1, "blocked_jobs": 0},
            "selected_jobs": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                }
            ],
            "blocked_jobs": [],
        }
        transfer = {
            "transfer_status": "ready",
            "operator": {"blocker": "transfer-pack-ready"},
            "counts": {
                "selected_jobs": 1,
                "repo_file_count": 9,
                "missing_repo_file_count": 0,
            },
            "jobs": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                }
            ],
        }
        transfer_pack = {
            "pack_status": "ready",
            "operator": {"blocker": "transfer-pack-ready"},
            "counts": {
                "selected_jobs": 1,
                "repo_files_copied": 9,
                "command_files_written": 1,
            },
            "request_ids": [
                "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
            ],
            "archive_path": "registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack.zip",
        }
        transfer_pack_check = {
            "check_status": "ok",
            "errors": [],
            "counts": {
                "checked_pack_files": 19,
                "checked_archive_files": 19,
                "archive_entries": 19,
            },
        }
        execution_run = {
            "execution_run_status": "ready",
            "mode": "dry-run",
            "operator": {"blocker": "execution-run-ready"},
            "counts": {
                "planned_jobs": 1,
                "ready_jobs": 1,
                "blocked_jobs": 0,
                "executed_jobs": 0,
            },
            "jobs": [
                {
                    "request_id": "ghidra-symbol-01-ntoskrnl-exe-0x1f234",
                }
            ],
            "blocked_jobs": [],
        }
        execution_run_check = {
            "check_status": "ok",
            "errors": [],
            "counts": {
                "jobs_with_blockers": 0,
                "blocked_jobs_with_blockers": 0,
            },
        }
        etw_stackwalk_plan = {
            "plan_status": "ready",
            "profile_id": "kernel-registry-stackwalk-v1",
            "errors": [],
            "run": {
                "run_id": "registry-stackwalk",
                "host_etl_repo_path": "evidence/raw/etw-stackwalk/registry-stackwalk/registry-stackwalk.etl",
            },
            "stack_capture": {
                "expected": True,
                "stackwalk_events": ["RegQueryValue", "RegSetValue"],
            },
        }
        etw_stackwalk_plan_check = {
            "check_status": "ok",
            "errors": [],
        }
        batch = {
            "job_count": 2,
            "jobs": [
                {
                    "candidate_id": "power.keep",
                    "autotrigger_seed_count": 1,
                    "missing_inputs": [],
                },
                {
                    "candidate_id": "power.other",
                    "autotrigger_seed_count": 0,
                    "missing_inputs": ["target_binary"],
                },
            ],
        }
        run = {
            "mode": "dry-run",
            "runner_available": False,
            "selected_job_count": 1,
            "blocked_job_count": 1,
            "error": "pwsh-not-found",
        }

        payload = ghidra_autotrigger_health.health_payload(
            input_manifest,
            queue_rows,
            seed_rows,
            symbol_queue,
            symbol_batch,
            symbol_run,
            handoff,
            transfer,
            transfer_pack,
            transfer_pack_check,
            batch,
            run,
            execution_run=execution_run,
            execution_run_check=execution_run_check,
            etw_stackwalk_plan=etw_stackwalk_plan,
            etw_stackwalk_plan_check=etw_stackwalk_plan_check,
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["counts"]["input_manifest_selected"], 1)
        self.assertEqual(payload["counts"]["queue_jobs"], 2)
        self.assertEqual(payload["counts"]["autotrigger_seeds"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_requests"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_batch_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_blocked_jobs"], 0)
        self.assertEqual(payload["counts"]["symbol_resolution_run_selected_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_handoff_selected_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_transfer_selected_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_transfer_pack_selected_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_transfer_pack_check_errors"], 0)
        self.assertEqual(payload["counts"]["symbol_resolution_execution_run_ready_jobs"], 1)
        self.assertEqual(payload["counts"]["symbol_resolution_execution_run_check_errors"], 0)
        self.assertEqual(payload["counts"]["etw_stackwalk_plan_errors"], 0)
        self.assertEqual(payload["counts"]["etw_stackwalk_plan_check_errors"], 0)
        self.assertEqual(payload["counts"]["autotrigger_dispatch_jobs"], 1)
        self.assertFalse(payload["runner"]["available"])
        self.assertTrue(payload["symbol_resolution_runner"]["available"])
        self.assertEqual(payload["symbol_resolution_handoff"]["status"], "ready")
        self.assertEqual(payload["symbol_resolution_transfer"]["status"], "ready")
        self.assertEqual(payload["symbol_resolution_transfer_pack"]["status"], "ready")
        self.assertEqual(payload["symbol_resolution_transfer_pack_check"]["status"], "ok")
        self.assertEqual(payload["symbol_resolution_transfer_pack_check"]["checked_archive_files"], 19)
        self.assertEqual(payload["symbol_resolution_execution_run"]["status"], "ready")
        self.assertEqual(payload["symbol_resolution_execution_run"]["ready_jobs"], 1)
        self.assertEqual(payload["symbol_resolution_execution_run_check"]["status"], "ok")
        self.assertEqual(payload["etw_stackwalk_capture"]["plan_status"], "ready")
        self.assertEqual(payload["etw_stackwalk_capture"]["check_status"], "ok")
        self.assertEqual(payload["etw_stackwalk_capture"]["stackwalk_event_count"], 2)
        self.assertEqual(payload["symbol_resolution_batch"]["resolution_kind_counts"], {})
        self.assertEqual(payload["focus"]["top_input_bundle"], "evidence/files/example/normalized-registry-bundle.json")
        self.assertEqual(payload["focus"]["top_queue_candidate"], "power.keep")
        self.assertEqual(payload["focus"]["top_autotrigger_candidate"], "power.keep")
        self.assertEqual(payload["focus"]["top_symbol_resolution_request"], "ntoskrnl.exe+0x1f234")
        self.assertEqual(payload["focus"]["top_symbol_resolution_batch_request"], "ghidra-symbol-01-ntoskrnl-exe-0x1f234")
        self.assertEqual(payload["focus"]["top_symbol_resolution_handoff_request"], "ghidra-symbol-01-ntoskrnl-exe-0x1f234")
        self.assertEqual(payload["focus"]["top_symbol_resolution_transfer_request"], "ghidra-symbol-01-ntoskrnl-exe-0x1f234")
        self.assertEqual(payload["focus"]["top_symbol_resolution_transfer_pack_request"], "ghidra-symbol-01-ntoskrnl-exe-0x1f234")
        self.assertEqual(payload["focus"]["top_symbol_resolution_execution_run_request"], "ghidra-symbol-01-ntoskrnl-exe-0x1f234")
        self.assertEqual(payload["focus"]["missing_input_jobs"][0]["candidate_id"], "power.other")

        markdown = ghidra_autotrigger_health.render_markdown(payload)
        self.assertIn("# Ghidra Autotrigger Health", markdown)
        self.assertIn("Top queue candidate", markdown)
        self.assertIn("`power.keep`", markdown)
        self.assertIn("Top symbol resolution request", markdown)
        self.assertIn("ETW Stackwalk Capture", markdown)
        self.assertIn("registry-stackwalk", markdown)

    def test_validate_health_rejects_inconsistent_counts(self) -> None:
        payload = {
            "counts": {
                "input_manifest_selected": 1,
                "queue_jobs": 2,
                "autotrigger_seeds": 1,
                "symbol_resolution_requests": 1,
                "symbol_resolution_batch_jobs": 1,
                "symbol_resolution_runnable_jobs": 2,
                "symbol_resolution_blocked_jobs": 0,
                "symbol_resolution_run_selected_jobs": 0,
                "symbol_resolution_run_blocked_jobs": 0,
                "symbol_resolution_handoff_selected_jobs": 2,
                "symbol_resolution_transfer_selected_jobs": 2,
                "symbol_resolution_transfer_pack_selected_jobs": 2,
                "symbol_resolution_transfer_pack_check_errors": 1,
                "dispatch_jobs": 1,
                "autotrigger_dispatch_jobs": 0,
                "run_selected_jobs": 0,
                "run_blocked_jobs": 0,
            },
            "coverage": {
                "input_bundle_paths": [],
                "queued_candidate_ids": ["one"],
                "seed_candidate_ids": [],
                "symbol_resolution_request_ids": [],
                "symbol_resolution_lookup_keys": [],
                "symbol_resolution_batch_request_ids": [],
                "symbol_resolution_handoff_request_ids": [],
                "symbol_resolution_transfer_request_ids": [],
                "symbol_resolution_transfer_pack_request_ids": [],
                "autotrigger_dispatch_candidate_ids": [],
            },
            "focus": {
                "top_input_bundle": "wrong-bundle",
                "top_queue_candidate": "wrong",
                "top_autotrigger_candidate": None,
                "top_symbol_resolution_request": "wrong-symbol",
                "top_symbol_resolution_batch_request": "wrong-batch",
                "top_symbol_resolution_handoff_request": "wrong-handoff",
                "top_symbol_resolution_transfer_request": "wrong-transfer",
                "top_symbol_resolution_transfer_pack_request": "wrong-transfer-pack",
                "missing_input_jobs": [],
            },
            "symbol_resolution_runner": {
                "available": True,
                "error": "bad",
            },
            "symbol_resolution_handoff": {
                "selected_jobs": 1,
            },
            "symbol_resolution_transfer": {
                "selected_jobs": 1,
            },
            "symbol_resolution_transfer_pack": {
                "selected_jobs": 1,
            },
            "symbol_resolution_transfer_pack_check": {
                "status": "ok",
                "error_count": 0,
            },
            "runner": {
                "available": False,
                "error": None,
            },
        }

        errors = ghidra_autotrigger_health_check.validate_health(payload)

        self.assertTrue(any("input_manifest_selected mismatch" in error for error in errors))
        self.assertTrue(any("top_input_bundle does not match" in error for error in errors))
        self.assertTrue(any("queue_jobs mismatch" in error for error in errors))
        self.assertTrue(any("symbol_resolution_requests mismatch" in error for error in errors))
        self.assertTrue(any("symbol_resolution_batch_jobs mismatch" in error for error in errors))
        self.assertTrue(any("symbol_resolution_runnable_jobs exceeds" in error for error in errors))
        self.assertTrue(any("symbol_resolution selected+blocked does not cover batch jobs" in error for error in errors))
        self.assertTrue(any("top_symbol_resolution_request does not match" in error for error in errors))
        self.assertTrue(any("top_symbol_resolution_batch_request does not match" in error for error in errors))
        self.assertTrue(any("top_symbol_resolution_handoff_request" in error for error in errors))
        self.assertTrue(any("top_symbol_resolution_transfer_request" in error for error in errors))
        self.assertTrue(any("top_symbol_resolution_transfer_pack_request" in error for error in errors))
        self.assertTrue(any("symbol_resolution_runner cannot be available and errored" in error for error in errors))
        self.assertTrue(any("symbol_resolution_handoff selected_jobs does not match counts" in error for error in errors))
        self.assertTrue(any("symbol_resolution_transfer selected_jobs does not match counts" in error for error in errors))
        self.assertTrue(any("symbol_resolution_transfer_pack selected_jobs does not match counts" in error for error in errors))
        self.assertTrue(any("symbol_resolution_transfer_pack_check error_count does not match counts" in error for error in errors))
        self.assertTrue(any("symbol_resolution_transfer_pack_check cannot be ok" in error for error in errors))
        self.assertTrue(any("selected+blocked does not cover dispatch jobs" in error for error in errors))
        self.assertTrue(any("top_queue_candidate does not match" in error for error in errors))

    def test_validate_health_rejects_inconsistent_etw_stackwalk_status(self) -> None:
        payload = {
            "counts": {
                "input_manifest_selected": 0,
                "queue_jobs": 0,
                "autotrigger_seeds": 0,
                "symbol_resolution_requests": 0,
                "symbol_resolution_batch_jobs": 0,
                "symbol_resolution_runnable_jobs": 0,
                "symbol_resolution_blocked_jobs": 0,
                "symbol_resolution_run_selected_jobs": 0,
                "symbol_resolution_run_blocked_jobs": 0,
                "symbol_resolution_handoff_selected_jobs": 0,
                "symbol_resolution_transfer_selected_jobs": 0,
                "symbol_resolution_transfer_pack_selected_jobs": 0,
                "symbol_resolution_transfer_pack_check_errors": 0,
                "symbol_resolution_execution_run_planned_jobs": 0,
                "symbol_resolution_execution_run_ready_jobs": 0,
                "symbol_resolution_execution_run_blocked_jobs": 0,
                "symbol_resolution_execution_run_check_errors": 0,
                "etw_stackwalk_plan_errors": 0,
                "etw_stackwalk_plan_check_errors": 1,
                "dispatch_jobs": 0,
                "autotrigger_dispatch_jobs": 0,
                "run_selected_jobs": 0,
                "run_blocked_jobs": 0,
            },
            "coverage": {
                "input_bundle_paths": [],
                "queued_candidate_ids": [],
                "seed_candidate_ids": [],
                "symbol_resolution_request_ids": [],
                "symbol_resolution_lookup_keys": [],
                "symbol_resolution_batch_request_ids": [],
                "symbol_resolution_handoff_request_ids": [],
                "symbol_resolution_transfer_request_ids": [],
                "symbol_resolution_transfer_pack_request_ids": [],
                "symbol_resolution_execution_run_request_ids": [],
                "autotrigger_dispatch_candidate_ids": [],
            },
            "focus": {
                "top_input_bundle": None,
                "top_queue_candidate": None,
                "top_autotrigger_candidate": None,
                "top_symbol_resolution_request": None,
                "top_symbol_resolution_batch_request": None,
                "top_symbol_resolution_handoff_request": None,
                "top_symbol_resolution_transfer_request": None,
                "top_symbol_resolution_transfer_pack_request": None,
                "top_symbol_resolution_execution_run_request": None,
                "missing_input_jobs": [],
            },
            "runner": {"available": False, "error": None},
            "symbol_resolution_runner": {"available": False, "error": None},
            "etw_stackwalk_capture": {
                "plan_status": "ready",
                "check_status": "error",
                "stack_expected": False,
                "stackwalk_event_count": 0,
                "plan_errors": [],
                "check_errors": ["missing required stackwalk events: RegSetValue"],
            },
        }

        errors = ghidra_autotrigger_health_check.validate_health(payload)

        self.assertTrue(any("etw_stackwalk check must be ok" in error for error in errors))
        self.assertTrue(any("etw_stackwalk stack_expected must be true" in error for error in errors))
        self.assertTrue(any("etw_stackwalk stackwalk_event_count must be positive" in error for error in errors))

    def test_validate_health_allows_missing_optional_etw_stackwalk_surface(self) -> None:
        payload = {
            "counts": {
                "input_manifest_selected": 0,
                "queue_jobs": 0,
                "autotrigger_seeds": 0,
                "symbol_resolution_requests": 0,
                "symbol_resolution_batch_jobs": 0,
                "symbol_resolution_runnable_jobs": 0,
                "symbol_resolution_blocked_jobs": 0,
                "symbol_resolution_run_selected_jobs": 0,
                "symbol_resolution_run_blocked_jobs": 0,
                "symbol_resolution_handoff_selected_jobs": 0,
                "symbol_resolution_transfer_selected_jobs": 0,
                "symbol_resolution_transfer_pack_selected_jobs": 0,
                "symbol_resolution_transfer_pack_check_errors": 0,
                "symbol_resolution_execution_run_planned_jobs": 0,
                "symbol_resolution_execution_run_ready_jobs": 0,
                "symbol_resolution_execution_run_blocked_jobs": 0,
                "symbol_resolution_execution_run_check_errors": 0,
                "dispatch_jobs": 0,
                "autotrigger_dispatch_jobs": 0,
                "run_selected_jobs": 0,
                "run_blocked_jobs": 0,
            },
            "coverage": {
                "input_bundle_paths": [],
                "queued_candidate_ids": [],
                "seed_candidate_ids": [],
                "symbol_resolution_request_ids": [],
                "symbol_resolution_lookup_keys": [],
                "symbol_resolution_batch_request_ids": [],
                "symbol_resolution_handoff_request_ids": [],
                "symbol_resolution_transfer_request_ids": [],
                "symbol_resolution_transfer_pack_request_ids": [],
                "symbol_resolution_execution_run_request_ids": [],
                "autotrigger_dispatch_candidate_ids": [],
            },
            "focus": {
                "top_input_bundle": None,
                "top_queue_candidate": None,
                "top_autotrigger_candidate": None,
                "top_symbol_resolution_request": None,
                "top_symbol_resolution_batch_request": None,
                "top_symbol_resolution_handoff_request": None,
                "top_symbol_resolution_transfer_request": None,
                "top_symbol_resolution_transfer_pack_request": None,
                "top_symbol_resolution_execution_run_request": None,
                "missing_input_jobs": [],
            },
            "runner": {"available": False, "error": None},
            "symbol_resolution_runner": {"available": False, "error": None},
            "symbol_resolution_handoff": {"selected_jobs": 0},
            "symbol_resolution_transfer": {"selected_jobs": 0},
            "symbol_resolution_transfer_pack": {"selected_jobs": 0},
            "symbol_resolution_transfer_pack_check": {"status": None, "error_count": 0},
            "etw_stackwalk_capture": {
                "plan_status": None,
                "check_status": None,
                "stack_expected": False,
                "stackwalk_event_count": 0,
                "plan_errors": [],
                "check_errors": [],
            },
        }

        errors = ghidra_autotrigger_health_check.validate_health(payload)

        self.assertEqual(errors, [])


class GhidraAutotriggerSyncTests(unittest.TestCase):
    def test_sync_lane_refreshes_and_validates_health(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "status": "queued",
                "priority_rank": 1,
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
                "trigger": "blocked-worklist-ghidra-lane",
                "next_action_hint": "Resolve seeding path.",
            }
        ]
        bundle = {
            "run_id": "synthetic-stack-seed",
            "source_tool": "wpr",
            "capture_phase": "boot",
            "stack_capture": {"source_fields": ["Stack"], "captured_event_count": 1},
            "events": [
                {
                    "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                    "value_name": "AllowSystemRequiredPowerRequests",
                    "operation": "RegQueryValue",
                    "caller_stack": ["ntoskrnl.exe+0x1F234", "nt!PopPowerRequestInitialize"],
                }
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            evidence_root = temp_root / "evidence"
            bundle_path = evidence_root / "sample" / "normalized-registry-bundle.json"
            bundle_path.parent.mkdir(parents=True, exist_ok=True)
            bundle_path.write_text(json.dumps(bundle), encoding="utf-8")
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")
            bundle_manifest_path = temp_root / "ghidra-autotrigger-inputs.json"
            seeds_path = temp_root / "ghidra-autotrigger-seeds.jsonl"
            symbol_queue_path = temp_root / "ghidra-symbol-resolution-queue.json"
            symbol_batch_path = temp_root / "ghidra-symbol-resolution-batch.json"
            symbol_run_path = temp_root / "ghidra-symbol-resolution-run.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            batch_path = temp_root / "ghidra-dispatch-batch.json"
            run_path = temp_root / "ghidra-dispatch-run.json"
            health_path = temp_root / "ghidra-autotrigger-health.json"
            output_path = temp_root / "ghidra-autotrigger-sync.json"
            markdown_path = temp_root / "ghidra-autotrigger-sync.md"

            payload = ghidra_autotrigger_sync.sync_lane(
                discover_input_roots=[evidence_root],
                queue_path=queue_path,
                bundle_manifest_path=bundle_manifest_path,
                seeds_path=seeds_path,
                symbol_queue_path=symbol_queue_path,
                symbol_batch_path=symbol_batch_path,
                symbol_run_path=symbol_run_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                batch_path=batch_path,
                run_path=run_path,
                health_path=health_path,
                markdown_path=markdown_path,
                output_path=output_path,
            )

            self.assertEqual(payload["sync_status"], "ok")
            self.assertEqual(payload["health_check"]["status"], "ok")
            self.assertEqual(payload["handoff"]["status"], "ready")
            self.assertEqual(payload["handoff"]["selected_jobs"], 1)
            self.assertEqual(payload["transfer"]["status"], "ready")
            self.assertEqual(payload["transfer"]["selected_jobs"], 1)
            self.assertEqual(payload["transfer_pack"]["status"], "ready")
            self.assertEqual(payload["transfer_pack"]["selected_jobs"], 1)
            self.assertEqual(payload["transfer_pack_check"]["status"], "ok")
            self.assertEqual(payload["transfer_pack_check"]["error_count"], 0)
            self.assertEqual(payload["execution_run"]["status"], "ready")
            self.assertEqual(payload["execution_run"]["ready_jobs"], 1)
            self.assertEqual(payload["execution_run_check"]["status"], "ok")
            self.assertEqual(payload["execution_run_check"]["error_count"], 0)
            self.assertEqual(payload["operator"]["blocker"], "symbol-resolution-ready")
            self.assertIn("Run the symbol-resolution batch", payload["operator"]["next_action"])
            symbol_queue_payload = json.loads(symbol_queue_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_queue_payload["request_count"], 1)
            symbol_batch_payload = json.loads(symbol_batch_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_batch_payload["job_count"], 1)
            symbol_run_payload = json.loads(symbol_run_path.read_text(encoding="utf-8"))
            self.assertEqual(symbol_run_payload["selected_job_count"], 1)
            handoff_payload = json.loads(handoff_path.read_text(encoding="utf-8"))
            self.assertEqual(handoff_payload["handoff_status"], "ready")
            transfer_payload = json.loads(transfer_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_payload["transfer_status"], "ready")
            transfer_pack_payload = json.loads(transfer_pack_summary_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_pack_payload["pack_status"], "ready")
            transfer_pack_check_payload = json.loads(transfer_pack_check_path.read_text(encoding="utf-8"))
            self.assertEqual(transfer_pack_check_payload["check_status"], "ok")
            execution_run_path = temp_root / "ghidra-symbol-resolution-transfer-pack-execution-run.json"
            execution_run_payload = json.loads(execution_run_path.read_text(encoding="utf-8"))
            self.assertEqual(execution_run_payload["execution_run_status"], "ready")
            execution_run_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json"
            execution_run_check_payload = json.loads(execution_run_check_path.read_text(encoding="utf-8"))
            self.assertEqual(execution_run_check_payload["check_status"], "ok")
            self.assertTrue(transfer_pack_archive_path.exists())
            self.assertTrue(output_path.exists())
            self.assertTrue(markdown_path.exists())

    def test_sync_lane_returns_idle_when_no_discovered_inputs_exist(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            evidence_root = temp_root / "evidence"
            evidence_root.mkdir(parents=True, exist_ok=True)
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            queue_path.write_text("", encoding="utf-8")
            bundle_manifest_path = temp_root / "ghidra-autotrigger-inputs.json"
            output_path = temp_root / "ghidra-autotrigger-sync.json"
            markdown_path = temp_root / "ghidra-autotrigger-sync.md"

            payload = ghidra_autotrigger_sync.sync_lane(
                discover_input_roots=[evidence_root],
                queue_path=queue_path,
                bundle_manifest_path=bundle_manifest_path,
                markdown_path=markdown_path,
                output_path=output_path,
            )

            self.assertEqual(payload["sync_status"], "idle")
            self.assertEqual(payload["bundle_manifest"]["selected_count"], 0)
            self.assertEqual(payload["bundle_manifest"]["diagnostics"]["scanned_bundle_count"], 0)
            self.assertEqual(payload["operator"]["blocker"], "no-bundles-discovered")
            self.assertIsNone(payload["transfer_pack"])
            self.assertIsNone(payload["transfer_pack_check"])
            self.assertTrue(markdown_path.exists())
            self.assertTrue(output_path.exists())

    def test_sync_lane_idle_surfaces_can_report_cached_transfer_state(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            evidence_root = temp_root / "evidence"
            evidence_root.mkdir(parents=True, exist_ok=True)
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            queue_path.write_text("", encoding="utf-8")
            bundle_manifest_path = temp_root / "ghidra-autotrigger-inputs.json"
            handoff_path = temp_root / "ghidra-symbol-resolution-handoff.json"
            handoff_markdown_path = temp_root / "ghidra-symbol-resolution-handoff.md"
            transfer_path = temp_root / "ghidra-symbol-resolution-transfer.json"
            transfer_markdown_path = temp_root / "ghidra-symbol-resolution-transfer.md"
            transfer_pack_output_root = temp_root / "ghidra-symbol-resolution-transfer-pack"
            transfer_pack_summary_path = temp_root / "ghidra-symbol-resolution-transfer-pack.json"
            transfer_pack_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack.md"
            transfer_pack_archive_path = temp_root / "ghidra-symbol-resolution-transfer-pack.zip"
            transfer_pack_check_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.json"
            transfer_pack_check_markdown_path = temp_root / "ghidra-symbol-resolution-transfer-pack-check.md"
            output_path = temp_root / "ghidra-autotrigger-sync.json"
            markdown_path = temp_root / "ghidra-autotrigger-sync.md"

            handoff_path.write_text(json.dumps({"handoff_status": "idle", "counts": {"selected_jobs": 0}}), encoding="utf-8")
            handoff_markdown_path.write_text("# handoff\n", encoding="utf-8")
            transfer_path.write_text(json.dumps({"transfer_status": "idle", "counts": {"selected_jobs": 0}}), encoding="utf-8")
            transfer_markdown_path.write_text("# transfer\n", encoding="utf-8")
            transfer_pack_output_root.mkdir(parents=True, exist_ok=True)
            transfer_pack_summary_path.write_text(
                json.dumps(
                    {
                        "pack_status": "idle",
                        "counts": {"selected_jobs": 0},
                        "archive_path": transfer_pack_archive_path.as_posix(),
                        "output_root": transfer_pack_output_root.as_posix(),
                    }
                ),
                encoding="utf-8",
            )
            transfer_pack_markdown_path.write_text("# pack\n", encoding="utf-8")
            transfer_pack_archive_path.write_bytes(b"PK")
            transfer_pack_check_path.write_text(
                json.dumps({"check_status": "ok", "errors": [], "counts": {"checked_archive_files": 0}}),
                encoding="utf-8",
            )
            transfer_pack_check_markdown_path.write_text("# check\n", encoding="utf-8")

            payload = ghidra_autotrigger_sync.sync_lane(
                discover_input_roots=[evidence_root],
                queue_path=queue_path,
                bundle_manifest_path=bundle_manifest_path,
                handoff_path=handoff_path,
                handoff_markdown_path=handoff_markdown_path,
                transfer_path=transfer_path,
                transfer_markdown_path=transfer_markdown_path,
                transfer_pack_output_root=transfer_pack_output_root,
                transfer_pack_summary_path=transfer_pack_summary_path,
                transfer_pack_markdown_path=transfer_pack_markdown_path,
                transfer_pack_archive_path=transfer_pack_archive_path,
                transfer_pack_check_path=transfer_pack_check_path,
                transfer_pack_check_markdown_path=transfer_pack_check_markdown_path,
                markdown_path=markdown_path,
                output_path=output_path,
            )

            self.assertEqual(payload["sync_status"], "idle")
            self.assertEqual(payload["handoff"]["status"], "idle")
            self.assertEqual(payload["handoff"]["source"], "cached")
            self.assertEqual(payload["transfer"]["status"], "idle")
            self.assertEqual(payload["transfer"]["source"], "cached")
            self.assertEqual(payload["transfer_pack"]["status"], "idle")
            self.assertEqual(payload["transfer_pack"]["source"], "cached")
            self.assertEqual(payload["transfer_pack"]["archive_path"], transfer_pack_archive_path.as_posix())
            self.assertEqual(payload["transfer_pack_check"]["status"], "ok")
            self.assertEqual(payload["transfer_pack_check"]["source"], "cached")


class PowerRequestOverrideAuditTests(unittest.TestCase):
    def test_normalize_registry_path_collapses_hklm_alias(self) -> None:
        self.assertEqual(
            power_request_override_audit.normalize_registry_path(
                r"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride"
            ),
            power_request_override_audit.normalize_registry_path(
                r"HKLM\System\CurrentControlSet\Control\Power\PowerRequestOverride"
            ),
        )

    def test_subtree_present_in_dump_accepts_retained_root_dump_format(self) -> None:
        root_dump = "\n".join([
            r"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power",
            r"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride",
        ])

        self.assertTrue(power_request_override_audit.subtree_present_in_dump(root_dump))


class GhidraAutotriggerSmokeTests(unittest.TestCase):
    def test_smoke_run_produces_symbol_resolution_ready_summary(self) -> None:
        queue_rows = [
            {
                "candidate_id": "power.control.allow-system-required-power-requests",
                "priority_rank": 1,
                "status": "queued",
                "feature_area": "Control Power Requests",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                "value_name": "AllowSystemRequiredPowerRequests",
                "promotion_blockers": ["system-execution-required-no-current-build-registry-seeding-path"],
            },
            {
                "candidate_id": "power.session-watchdog-timeouts",
                "priority_rank": 2,
                "status": "queued",
                "feature_area": "Directed Power Watchdog Timeouts",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power",
                "value_name": "WatchdogResumeTimeout / WatchdogSleepTimeout",
                "promotion_blockers": ["power-session-watchdog-timeouts-specific-caller-unresolved"],
            },
            {
                "candidate_id": "system.kernel-dpc-watchdog-profile-cluster",
                "priority_rank": 3,
                "status": "queued",
                "feature_area": "Session Manager Kernel DPC Watchdog Profile",
                "key_path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
                "value_name": "DpcWatchdogProfileBufferSizeBytes",
                "promotion_blockers": ["dpc-watchdog-profile-conditional-initialization-unproven"],
            },
        ]

        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            queue_path = temp_root / "ghidra-job-queue.jsonl"
            queue_path.write_text("".join(json.dumps(row) + "\n" for row in queue_rows), encoding="utf-8")
            output_root = temp_root / "smoke"
            output_path = temp_root / "ghidra-autotrigger-smoke.json"
            markdown_path = temp_root / "ghidra-autotrigger-smoke.md"

            payload = ghidra_autotrigger_smoke.run_smoke(
                queue_path=queue_path,
                output_root=output_root,
                output_path=output_path,
                markdown_path=markdown_path,
            )

            self.assertEqual(payload["smoke_status"], "ok")
            self.assertEqual(payload["sync_status"], "ok")
            self.assertEqual(payload["selected_candidate_count"], 3)
            self.assertEqual(payload["counts"]["manifest_selected_count"], 1)
            self.assertEqual(payload["counts"]["seed_count"], 3)
            self.assertGreaterEqual(payload["counts"]["symbol_resolution_request_count"], 3)
            self.assertEqual(payload["operator"]["blocker"], "symbol-resolution-ready")
            self.assertEqual(payload["handoff_status"], "ready")
            self.assertEqual(payload["transfer_status"], "ready")
            self.assertEqual(payload["transfer_pack_status"], "ready")
            self.assertEqual(payload["transfer_pack_check_status"], "ok")
            self.assertEqual(payload["transfer_pack_import_status"], "ok")
            self.assertEqual(payload["execution_plan_status"], "ready")
            self.assertEqual(payload["execution_run_status"], "ready")
            self.assertEqual(payload["execution_run_check_status"], "ok")
            self.assertIn("module_offset", payload["frame_resolution_counts"])
            self.assertIn("raw_address", payload["frame_resolution_counts"])
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-check.json").exists())
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-import.json").exists())
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-import" / "CHECKSUMS.json").exists())
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-execution-plan.json").exists())
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-execution-run.json").exists())
            self.assertTrue((output_root / "ghidra-symbol-resolution-transfer-pack-execution-run-check.json").exists())
            self.assertTrue(output_path.exists())
            self.assertTrue(markdown_path.exists())
            self.assertTrue((temp_root / "ghidra-autotrigger-smoke-check.json").exists())
            smoke_check_payload = json.loads((temp_root / "ghidra-autotrigger-smoke-check.json").read_text(encoding="utf-8"))
            self.assertEqual(smoke_check_payload["check_status"], "ok")

            check_payload = ghidra_autotrigger_smoke_check.validate_smoke(
                json.loads(output_path.read_text(encoding="utf-8")),
                smoke_path=output_path,
                generated_utc="2026-04-13T00:00:00Z",
            )
            self.assertEqual(check_payload["check_status"], "ok")
            self.assertEqual(check_payload["counts"]["selected_candidates"], 3)

    def test_smoke_check_rejects_failed_assertions_and_bad_status(self) -> None:
        payload = {
            "smoke_status": "error",
            "sync_status": "ok",
            "handoff_status": "ready",
            "transfer_status": "ready",
            "transfer_pack_status": "ready",
            "transfer_pack_check_status": "ok",
            "transfer_pack_import_status": "ok",
            "execution_plan_status": "ready",
            "execution_run_status": "ready",
            "execution_run_check_status": "ok",
            "selected_candidate_count": 1,
            "selected_candidate_ids": ["power.keep"],
            "failed_assertions": ["boom"],
            "operator": {"blocker": "wrong"},
            "counts": {
                "manifest_selected_count": 0,
                "seed_count": 0,
                "symbol_resolution_request_count": 0,
                "symbol_resolution_batch_job_count": 0,
                "dispatch_job_count": 0,
                "dispatch_selected_job_count": 0,
                "transfer_pack_selected_job_count": 0,
            },
            "paths": {},
        }

        check_payload = ghidra_autotrigger_smoke_check.validate_smoke(
            payload,
            smoke_path=Path("smoke.json"),
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(check_payload["check_status"], "error")
        self.assertTrue(any("smoke_status" in error for error in check_payload["errors"]))
        self.assertTrue(any("failed_assertions" in error for error in check_payload["errors"]))


class ResearchQualityGateTests(unittest.TestCase):
    def test_quality_gate_payload_reports_pass_and_skipped_steps(self) -> None:
        payload = research_quality_gate.quality_gate_payload(
            [
                research_quality_gate.GateStep(
                    "pass-step",
                    "Pass Step",
                    [sys.executable, "-c", "print('ok')"],
                ),
                research_quality_gate.GateStep(
                    "skip-step",
                    "Skip Step",
                    [sys.executable, "-c", "raise SystemExit(99)"],
                    skipped=True,
                    skip_reason="unit-test",
                ),
            ],
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["quality_gate_status"], "PASS")
        self.assertEqual(payload["counts"]["passed"], 1)
        self.assertEqual(payload["counts"]["skipped"], 1)

    def test_quality_gate_payload_reports_failed_step(self) -> None:
        payload = research_quality_gate.quality_gate_payload(
            [
                research_quality_gate.GateStep(
                    "fail-step",
                    "Fail Step",
                    [sys.executable, "-c", "import sys; print('bad'); sys.exit(7)"],
                )
            ],
            generated_utc="2026-04-13T00:00:00Z",
        )

        self.assertEqual(payload["quality_gate_status"], "FAIL")
        self.assertEqual(payload["failed_step_ids"], ["fail-step"])
        self.assertEqual(payload["steps"][0]["returncode"], 7)
        self.assertIn("bad", payload["steps"][0]["stdout_tail"])

    def test_quality_gate_payload_fails_fast_on_step_timeout(self) -> None:
        def fake_run(*args, **kwargs):
            raise subprocess.TimeoutExpired(
                cmd=args[0],
                timeout=kwargs["timeout"],
                output=b"partial stdout",
                stderr=b"partial stderr",
            )

        with unittest.mock.patch.object(research_quality_gate.subprocess, "run", side_effect=fake_run):
            payload = research_quality_gate.quality_gate_payload(
                [
                    research_quality_gate.GateStep(
                        "timeout-step",
                        "Timeout Step",
                        [sys.executable, "-c", "import time; time.sleep(60)"],
                    )
                ],
                generated_utc="2026-04-13T00:00:00Z",
                step_timeout_seconds=7,
            )

        self.assertEqual(payload["quality_gate_status"], "FAIL")
        self.assertEqual(payload["step_timeout_seconds"], 7)
        self.assertEqual(payload["failed_step_ids"], ["timeout-step"])
        self.assertTrue(payload["steps"][0]["timed_out"])
        self.assertEqual(payload["steps"][0]["timeout_seconds"], 7)
        self.assertEqual(payload["steps"][0]["error_kind"], "timeout")
        self.assertIn("partial stdout", payload["steps"][0]["stdout_tail"])
        self.assertIn("partial stderr", payload["steps"][0]["stderr_tail"])


if __name__ == "__main__":
    unittest.main()
