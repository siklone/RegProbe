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
        self.assertEqual(gate["promotion_blockers"], ["runtime-trace"])
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
            "reproducibility": {"vm_name": "Win25H2Clean", "baseline_snapshot": "snap"},
        }

        payload = research_v36_lib.canonical_bundle_projection(record, audit, full_evidence)
        errors = research_v36_lib.validate_canonical_bundle(payload)

        self.assertFalse(errors, errors)
        self.assertEqual(payload["before_after"]["value_added"], 1)
        self.assertEqual(payload["before_after"]["value_changed"], 1)
        self.assertIn("score_breakdown", payload)


if __name__ == "__main__":
    unittest.main()
