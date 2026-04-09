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
validate_contracts = load_module("validate_research_contracts", FRAMEWORK_SCRIPTS / "validate_research_contracts.py")


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
        self.assertGreater(score["overall_score"], 0)
        self.assertEqual(score["next_missing_layer"], "none")

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


if __name__ == "__main__":
    unittest.main()
