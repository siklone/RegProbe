from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "generate_operator96_app_surface_review.py"


def load_module():
    spec = importlib.util.spec_from_file_location("generate_operator96_app_surface_review_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def record(**overrides):
    payload = {
        "index": 1,
        "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
        "value_name": "EnableThing",
        "default_status": "known-absent",
        "source_quality": "vm-observed",
        "app_surface_gate": {
            "eligible_for_app_card": True,
            "blockers": [],
            "claim_boundary": "bounded",
        },
        "candidates": [
            {
                "value": 1,
                "vm_validated": True,
                "baseline_proof": {
                    "status": "ok",
                    "verdict": "no_effect",
                    "confidence": "high",
                    "host_noise": "ok",
                },
            }
        ],
    }
    payload.update(overrides)
    return payload


class Operator96AppSurfaceReviewTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_real_repo_keeps_low_confidence_records_out_of_app_cards(self):
        review = self.module.build_review()

        self.assertEqual(review["status"], "PASS")
        self.assertEqual(review["summary"]["record_count"], 96)
        self.assertEqual(review["summary"]["ready_for_bounded_app_card"], 0)
        self.assertEqual(review["summary"]["needs_low_noise_rerun"], 79)
        self.assertEqual(review["summary"]["blocked_by_gate"], 17)

        watchdog = next(
            record
            for record in review["records"]
            if record["value_name"] == "PowerWatchdogPoCalloutTimeoutMsec"
        )
        self.assertEqual(watchdog["app_surface_bucket"], "blocked_by_gate")
        self.assertIn("rollback-not-tested", watchdog["reasons"])

    def test_gate_blocked_record_stays_out_of_app_surface(self):
        result = self.module.classify_record(
            record(
                app_surface_gate={
                    "eligible_for_app_card": False,
                    "blockers": ["security-mitigation-override"],
                }
            )
        )

        self.assertEqual(result["app_surface_bucket"], "blocked_by_gate")
        self.assertFalse(result["app_surface_ready"])
        self.assertIn("security-mitigation-override", result["reasons"])

    def test_low_confidence_or_unknown_noise_requires_rerun(self):
        result = self.module.classify_record(
            record(
                candidates=[
                    {
                        "value": 1,
                        "vm_validated": True,
                        "baseline_proof": {
                            "status": "ok",
                            "verdict": "harmful",
                            "confidence": "low",
                            "host_noise": "unknown",
                        },
                    }
                ]
            )
        )

        self.assertEqual(result["app_surface_bucket"], "needs_low_noise_rerun")
        self.assertEqual(result["recommended_action"], "rerun-low-noise-before-any-app-card-or-performance-claim")

    def test_safety_verdict_blocks_app_surface(self):
        result = self.module.classify_record(
            record(
                candidates=[
                    {
                        "value": 1,
                        "vm_validated": True,
                        "baseline_proof": {
                            "status": "ok",
                            "verdict": "rollback_failure",
                            "confidence": "high",
                            "host_noise": "ok",
                        },
                    }
                ]
            )
        )

        self.assertEqual(result["app_surface_bucket"], "blocked_by_safety")

    def test_clean_high_confidence_record_is_ready(self):
        result = self.module.classify_record(record())

        self.assertEqual(result["app_surface_bucket"], "ready_for_bounded_app_card")
        self.assertTrue(result["app_surface_ready"])


if __name__ == "__main__":
    unittest.main()
