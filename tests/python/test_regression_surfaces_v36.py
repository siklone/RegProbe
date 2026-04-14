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
generate_regression_pack = load_module("generate_regression_pack", FRAMEWORK_SCRIPTS / "generate_regression_pack.py")
validate_research_batch = load_module("validate_research_batch", FRAMEWORK_SCRIPTS / "validate_research_batch.py")


def promoted_record(candidate_id: str = "example.promoted") -> dict:
    return {
        "record_id": candidate_id,
        "tweak_id": candidate_id,
        "record_status": "validated",
        "last_reviewed_utc": "2026-04-09T00:00:00Z",
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
            "restore_default_supported": True,
        },
        "app_current_implementation": {"status": "matches-research"},
        "validation_proof": {
            "source_url": "https://example.com/reference",
            "exact_quote_or_path": "Docs/example.md:1",
            "notes": "Shows the behavior and the default/override contract.",
        },
        "windows_defaults": [
            {
                "label": "Windows default",
                "states": [
                    {
                        "state_kind": "dword",
                        "value": 0,
                        "rationale": "Default disabled",
                    }
                ],
            }
        ],
        "recommended_profiles": [
            {
                "label": "Recommended",
                "apply_allowed": True,
                "states": [
                    {
                        "state_kind": "dword",
                        "value": 1,
                        "rationale": "Recommended enabled",
                    }
                ],
            }
        ],
        "evidence": [
            {
                "evidence_id": "official-doc",
                "kind": "official-doc",
                "title": "Official documentation",
                "location": "https://example.com/reference",
                "supports": ["behavior", "api_semantics"],
                "summary": "Documents the registry behavior and effective value semantics.",
                "strength": "high",
            }
        ],
    }


def minimal_full_evidence() -> dict:
    return {
        "behavior": {
            "registry_sideeffects": {
                "executed": True,
                "summary_counts": {
                    "added_keys": 1,
                    "removed_keys": 1,
                    "added_values": 1,
                    "removed_values": 1,
                    "modified_values": 1,
                    "unchanged_values": 0,
                },
                "structured_diff": {
                    "key_added": ["HKLM\\Software\\Example\\NewKey"],
                    "key_deleted": ["HKLM\\Software\\Example\\OldKey"],
                    "value_added": ["Enabled"],
                    "value_deleted": ["Disabled"],
                    "value_changed": ["Enabled"],
                },
            }
        },
        "negative_evidence": {},
        "reproducibility": {},
        "rollback_status": {
            "rollback_declared": True,
            "rollback_executed": True,
            "rollback_verified": True,
            "rollback_verification_method": "state_diff",
            "rollback_failure_reason": None,
            "rollback_value": {"rollback_strategy": "restore_default"},
        },
        "bench_results": {
            "feature_area_group": "power",
            "profiles": ["boot_time_relative", "functional_throughput"],
            "bench_vm_capable": True,
            "bench_bare_metal_required": False,
        },
    }


class RegressionPackTests(unittest.TestCase):
    def test_regression_pack_contains_all_expected_artifacts(self) -> None:
        record = promoted_record()
        audit = {"next_missing_layer": "none"}
        full_evidence = minimal_full_evidence()
        gate = research_v36_lib.evaluate_candidate_gate(record, audit, full_evidence)

        pack = research_v36_lib.build_regression_pack(record, audit, full_evidence, gate)

        self.assertEqual(
            set(pack),
            {
                "schema_test.json",
                "gate_test.json",
                "docs_test.json",
                "before_after_parse_test.json",
                "rollback_presence_test.json",
                "rollback_verification_test.json",
                "bench_profile_consistency_test.json",
            },
        )
        self.assertTrue(pack["schema_test.json"]["pass"])
        self.assertTrue(pack["gate_test.json"]["pass"])
        self.assertTrue(pack["docs_test.json"]["documentation_quality_pass"])

        with tempfile.TemporaryDirectory() as temp_dir:
            output_dir = research_v36_lib.write_regression_pack(record["record_id"], pack, Path(temp_dir))
            self.assertTrue((output_dir / "schema_test.json").exists())
            self.assertEqual(len(list(output_dir.glob("*.json"))), 7)


class ValidationSurfaceTests(unittest.TestCase):
    def test_validate_batch_reports_invalid_undocumented_and_blocked(self) -> None:
        valid = promoted_record("example.valid")
        undocumented = promoted_record("example.undocumented")
        undocumented.pop("validation_proof", None)
        gate_map = {
            "example.valid": {
                "promotion_state": "promoted",
                "promotion_blockers": [],
            },
            "example.undocumented": {
                "promotion_state": "blocked",
                "promotion_blockers": ["documentation-first-review"],
            },
        }
        summary = validate_research_batch.build_validation_summary(
            records=[valid, undocumented],
            audit_map={"example.valid": {"next_missing_layer": "none"}, "example.undocumented": {"next_missing_layer": "decision-gate"}},
            gate_map=gate_map,
            full_evidence_loader=lambda _candidate_id: {},
        )

        self.assertEqual(summary["invalid_count"], 0)
        self.assertEqual(summary["undocumented_count"], 1)
        self.assertEqual(summary["blocked_count"], 1)
        self.assertEqual(summary["missing_docs_count"], 1)


class MutationPreconditionTests(unittest.TestCase):
    def test_apply_rejects_non_promoted_candidate(self) -> None:
        gate_map = {
            "example.blocked": {
                "candidate_id": "example.blocked",
                "promotion_state": "blocked",
                "tweak_origin": "research-derived",
                "debug_override_allowed": True,
            }
        }

        payload = research_v36_lib.apply_candidate("example.blocked", gate_map=gate_map)

        self.assertFalse(payload["allowed"])
        self.assertEqual(payload["message"], "promotion-state:blocked")

    def test_apply_override_writes_audit_log(self) -> None:
        gate_map = {
            "example.override": {
                "candidate_id": "example.override",
                "promotion_state": "blocked",
                "tweak_origin": "research-derived",
                "debug_override_allowed": True,
            }
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            audit_path = Path(temp_dir) / "override.jsonl"
            payload = research_v36_lib.apply_candidate(
                "example.override",
                override=True,
                reason="debug-validation",
                contributor_mode=True,
                gate_map=gate_map,
                audit_path=audit_path,
            )

            self.assertTrue(payload["allowed"])
            self.assertTrue(payload["override_used"])
            lines = audit_path.read_text(encoding="utf-8").splitlines()
            self.assertEqual(len(lines), 1)
            audit_entry = json.loads(lines[0])
            self.assertEqual(audit_entry["action"], "apply")
            self.assertTrue(audit_entry["override_used"])

    def test_rollback_warns_when_unverified(self) -> None:
        gate_map = {
            "example.rollback": {
                "candidate_id": "example.rollback",
                "promotion_state": "promoted",
                "tweak_origin": "research-derived",
                "debug_override_allowed": False,
                "rollback_status": {
                    "rollback_declared": True,
                    "rollback_executed": False,
                    "rollback_verified": False,
                },
            }
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            audit_path = Path(temp_dir) / "rollback.jsonl"
            payload = research_v36_lib.rollback_candidate(
                "example.rollback",
                gate_map=gate_map,
                audit_path=audit_path,
            )

            self.assertTrue(payload["allowed"])
            self.assertIn("rollback-declared-but-not-executed", payload["warnings"])
            self.assertIn("rollback-unverified", payload["warnings"])
            lines = audit_path.read_text(encoding="utf-8").splitlines()
            self.assertEqual(len(lines), 1)


class UrlValidationTests(unittest.TestCase):
    def test_dead_link_status_blocks_gate(self) -> None:
        record = promoted_record("example.dead-link")
        dead_status = research_v36_lib.validate_candidate_urls(
            record,
            {},
            checker=lambda _url, timeout=5.0: (False, 404, "not found"),
        )

        self.assertEqual(dead_status["dead_link_count"], 2)
        gate = research_v36_lib.evaluate_candidate_gate(
            record,
            {"next_missing_layer": "none"},
            {},
            url_validation_status=dead_status,
        )
        self.assertIn("dead-link", gate["promotion_blockers"])


class McpReadinessTests(unittest.TestCase):
    def test_mcp_readiness_reports_ready_for_valid_surface(self) -> None:
        catalog = {
            "summary": {
                "promotion_state_counts": {
                    "promoted": 5,
                    "blocked": 1,
                    "revalidation-pending": 1,
                },
                "invalid_gate_entries": 0,
            },
            "entries": [
                {
                    "supported_schema_versions": ["1.0"],
                    "schema_compatibility_mode": "native",
                }
            ],
        }
        cli_surface = "\n".join(
            [
                "list-blocked",
                "show-blocked --actionability",
                "show-stale",
                "show-revalidation-pending",
                "generate-regression-pack",
                "validate-batch",
                "apply",
                "rollback",
            ]
        )

        payload = research_v36_lib.check_mcp_readiness(catalog, cli_surface)

        self.assertEqual(payload["status"], "MCP_READY")

    def test_mcp_readiness_accepts_empty_revalidation_queue_when_no_stale_backlog(self) -> None:
        catalog = {
            "summary": {
                "promotion_state_counts": {
                    "promoted": 5,
                    "blocked": 1,
                    "revalidation-pending": 0,
                },
                "blocker_counts": {
                    "stale-evidence": 0,
                },
                "invalid_gate_entries": 0,
            },
            "entries": [
                {
                    "supported_schema_versions": ["1.0"],
                    "schema_compatibility_mode": "native",
                }
            ],
        }
        cli_surface = "\n".join(
            [
                "list-blocked",
                "show-blocked --actionability",
                "show-stale",
                "show-revalidation-pending",
                "generate-regression-pack",
                "validate-batch",
                "apply",
                "rollback",
            ]
        )

        payload = research_v36_lib.check_mcp_readiness(catalog, cli_surface)

        self.assertEqual(payload["status"], "MCP_READY")


if __name__ == "__main__":
    unittest.main()
