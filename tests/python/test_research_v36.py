from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
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
        self.assertGreaterEqual(score["total"], 0.57)

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

    def test_specific_execution_required_init_walker_blockers_stay_ghidra(self) -> None:
        for blocker in [
            "audio-execution-required-init-walker-not-symbol-resolved",
            "system-execution-required-init-walker-not-symbol-resolved",
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
                "blocking_issues": ["powerrequestoverride-restore-story-unproven-subtree-presence-only"],
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
            },
        }

        gate = research_v36_lib.evaluate_candidate_gate(record, {"next_missing_layer": "decision-gate"}, {})

        self.assertEqual(gate["next_missing_layer"], "restore-story")
        self.assertIn("powerrequestoverride-restore-story-unproven-subtree-presence-only", gate["promotion_blockers"])

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
        }

        found: dict[str, list[str]] = {}
        for entry in blocked:
            present = sorted(set(entry.get("promotion_blockers") or []) & deprecated)
            if present:
                found[str(entry.get("candidate_id"))] = present

        self.assertEqual(found, {}, found)


if __name__ == "__main__":
    unittest.main()
