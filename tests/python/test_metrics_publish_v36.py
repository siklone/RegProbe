from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
for path in (SCRIPTS_ROOT, FRAMEWORK_SCRIPTS):
    if str(path) not in sys.path:
        sys.path.insert(0, str(path))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


metrics_lib = load_module("metrics_publish_v36_lib", SCRIPTS_ROOT / "metrics_publish_v36_lib.py")
validate_research_batch = load_module("validate_research_batch", FRAMEWORK_SCRIPTS / "validate_research_batch.py")


def sample_queue_payload() -> dict:
    return {
        "summary": {
            "total_entries": 10,
            "state_counts": {
                "promoted": 2,
                "blocked": 1,
                "revalidation-pending": 1,
                "discarded": 2,
            },
        }
    }


def sample_gate_payload() -> dict:
    return {
        "summary": {
            "total_records": 4,
            "promotion_state_counts": {
                "promoted": 2,
                "blocked": 1,
                "revalidation-pending": 1,
                "rejected": 0,
            },
            "invalid_gate_entries": 0,
        },
        "entries": [
            {
                "candidate_id": "example.promoted",
                "promotion_state": "promoted",
                "promotion_blockers": [],
                "rollback_status": {
                    "rollback_value": {"value": 0},
                    "rollback_declared": True,
                    "rollback_executed": True,
                    "rollback_verified": True,
                },
                "bench_status": {"required": False, "executed": False},
            },
            {
                "candidate_id": "example.blocked",
                "promotion_state": "blocked",
                "promotion_blockers": ["no-runtime-proof"],
                "rollback_status": {
                    "rollback_value": {"value": 0},
                    "rollback_declared": True,
                    "rollback_executed": True,
                    "rollback_verified": True,
                },
                "bench_status": {"required": False, "executed": False},
            },
            {
                "candidate_id": "example.revalidation",
                "promotion_state": "revalidation-pending",
                "promotion_blockers": ["stale-evidence"],
                "rollback_status": {
                    "rollback_value": {"value": 0},
                    "rollback_declared": True,
                    "rollback_executed": True,
                    "rollback_verified": True,
                },
                "bench_status": {"required": False, "executed": False},
            },
        ],
    }


def sample_audit_payload() -> dict:
    return {
        "summary": {
            "promotion_state_counts": {
                "promoted": 2,
                "blocked": 1,
                "revalidation-pending": 1,
            }
        },
        "entries": [
            {
                "record_id": "example.promoted",
                "promotion_state": "promoted",
                "promotion_blockers": [],
                "stale_reason": None,
                "revalidation_need": "none",
                "rollback_verification_status": "verified",
                "conflict_reason": None,
                "bench_status": "not-run",
                "dead_link_count": 0,
            },
            {
                "record_id": "example.blocked",
                "promotion_state": "blocked",
                "promotion_blockers": ["no-runtime-proof"],
                "stale_reason": None,
                "revalidation_need": "none",
                "rollback_verification_status": "verified",
                "conflict_reason": None,
                "bench_status": "not-run",
                "dead_link_count": 0,
            },
            {
                "record_id": "example.revalidation",
                "promotion_state": "revalidation-pending",
                "promotion_blockers": ["stale-evidence"],
                "stale_reason": "build-gap",
                "revalidation_need": "required",
                "rollback_verification_status": "verified",
                "conflict_reason": None,
                "bench_status": "not-run",
                "dead_link_count": 0,
            },
        ],
    }


class MetricsPublishTests(unittest.TestCase):
    def test_metrics_and_final_audit_are_green_when_thresholds_clear(self) -> None:
        validation_summary = {
            "invalid_count": 0,
            "missing_docs_count": 0,
            "details": [],
        }
        gate_metrics = metrics_lib.build_gate_metrics(sample_gate_payload(), sample_audit_payload(), validation_summary, generated_at="2026-04-09T00:00:00Z")
        self.assertEqual(gate_metrics["threshold_violations"], [])

        operational = metrics_lib.build_operational_metrics(
            sample_queue_payload(),
            sample_gate_payload(),
            sample_audit_payload(),
            validation_summary,
            gate_metrics,
            generated_at="2026-04-09T00:00:00Z",
        )
        publish = metrics_lib.build_publish_metrics(
            sample_gate_payload(),
            sample_audit_payload(),
            validation_summary,
            gate_metrics,
            generated_at="2026-04-09T00:00:00Z",
        )
        final_audit = metrics_lib.build_final_audit_payload(sample_audit_payload(), gate_metrics, validation_summary)

        self.assertEqual(operational["total_discovered"], 10)
        self.assertEqual(operational["total_triaged"], 8)
        self.assertEqual(operational["total_discarded"], 2)
        self.assertEqual(publish["promoted_candidate_count"], 2)
        self.assertEqual(final_audit["summary"]["gate_health"], "green")
        self.assertEqual(final_audit["summary"]["stale"], 1)
        self.assertIn("stale_records", final_audit)
        self.assertIn("missing_docs_records", final_audit)

    def test_gate_health_turns_yellow_when_missing_docs_remain(self) -> None:
        validation_summary = {
            "invalid_count": 0,
            "missing_docs_count": 2,
            "details": [
                {
                    "candidate_id": "example.docs",
                    "promotion_state": "promoted",
                    "documentation_issues": ["missing-observed-default"],
                    "missing_docs": True,
                }
            ],
        }
        gate_metrics = metrics_lib.build_gate_metrics(sample_gate_payload(), sample_audit_payload(), validation_summary)
        final_audit = metrics_lib.build_final_audit_payload(sample_audit_payload(), gate_metrics, validation_summary)
        self.assertEqual(final_audit["summary"]["gate_health"], "yellow")

    def test_readme_summary_block_is_inserted(self) -> None:
        publish_metrics = {
            "promoted_candidate_count": 237,
            "blocked_candidate_count": 11,
            "stale_candidate_count": 3,
        }
        gate_metrics = {"schema_complete_ratio": 1.0}
        validation_summary = {"missing_docs_count": 0}
        block = metrics_lib.research_health_markdown(publish_metrics, gate_metrics, validation_summary, "green")

        with tempfile.TemporaryDirectory() as temp_dir:
            readme = Path(temp_dir) / "README.md"
            readme.write_text("# Example\n", encoding="utf-8")
            updated = metrics_lib.update_readme_summary_block(readme, block)
            self.assertIn("## Research Health", updated)
            self.assertIn("| Promoted | 237 |", updated)
            self.assertIn("🟢 green", updated)


class ValidationSurfaceBehaviorTests(unittest.TestCase):
    def test_validate_batch_skips_deprecated_records(self) -> None:
        deprecated = {
            "record_id": "example.deprecated",
            "tweak_id": "example.deprecated",
            "record_status": "deprecated",
        }
        summary = validate_research_batch.build_validation_summary(
            records=[deprecated],
            audit_map={},
            gate_map={},
            full_evidence_loader=lambda _candidate_id: {},
        )
        self.assertEqual(summary["invalid_count"], 0)
        self.assertEqual(summary["undocumented_count"], 0)
        self.assertEqual(summary["missing_docs_count"], 0)
        self.assertEqual(summary["details"], [])
