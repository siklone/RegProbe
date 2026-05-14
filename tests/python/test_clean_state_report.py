from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "generate_clean_state_report.py"


def load_module():
    spec = importlib.util.spec_from_file_location("generate_clean_state_report", SCRIPT_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules["generate_clean_state_report"] = module
    spec.loader.exec_module(module)
    return module


clean_state = load_module()


class CleanStateReportTests(unittest.TestCase):
    def test_build_report_marks_zero_pending_state_clean(self) -> None:
        report = clean_state.build_report(
            {
                "summary": {
                    "total_records": 3,
                    "promotion_state_counts": {
                        "promoted": 2,
                        "rejected": 1,
                    },
                    "invalid_gate_entries": 0,
                }
            },
            {
                "summary": {
                    "total_rejected": 1,
                    "evidence_backed_rejected": 1,
                    "deprecated_records": 0,
                    "unclassified_rejected": 0,
                    "closure_status_counts": {"evidence-backed-rejected": 1},
                    "closure_kind_counts": {"environment-limited-validation-lane": 1},
                }
            },
            {
                "metadata": {"total_records": 0},
                "summary_stats": {},
                "records": [],
            },
            {"blocked_count": 0},
            {
                "check_status": "PASS",
                "summary": {
                    "app_surface_entry_count": 2,
                    "apply_allowed_record_count": 2,
                    "missing_rollback_story_count": 0,
                    "kvm_app_smoke_status": "ok",
                    "kvm_lane_health_status": "ok",
                },
                "reports": {
                    "evidence_surfaces": {
                        "summary": {"records_missing_validation_proof": 0},
                    }
                },
            },
            generated_utc="2026-05-08T00:00:00Z",
        )

        self.assertEqual(report["status"], "clean-state")
        self.assertEqual(report["summary"]["active_backlog"], 0)
        self.assertEqual(report["summary"]["limbo_count"], 0)
        self.assertTrue(all(report["checks"].values()))

        markdown = clean_state.render_markdown(report)
        self.assertIn("V36 Clean State Report", markdown)
        self.assertIn("`clean-state`", markdown)

    def test_build_report_flags_unclassified_rejected_as_attention_needed(self) -> None:
        report = clean_state.build_report(
            {
                "summary": {
                    "total_records": 1,
                    "promotion_state_counts": {"rejected": 1},
                    "invalid_gate_entries": 0,
                }
            },
            {"summary": {"total_rejected": 1, "unclassified_rejected": 1}},
            {"metadata": {"total_records": 0}, "records": []},
            {"blocked_count": 0},
            {"check_status": "PASS", "summary": {}, "reports": {}},
            generated_utc="2026-05-08T00:00:00Z",
        )

        self.assertEqual(report["status"], "attention-needed")
        self.assertEqual(report["summary"]["active_backlog"], 1)
        self.assertFalse(report["checks"]["no_unclassified_rejected_records"])


if __name__ == "__main__":
    unittest.main()
