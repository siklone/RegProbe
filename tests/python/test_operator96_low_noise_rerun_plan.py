from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "generate_operator96_low_noise_rerun_plan.py"


def load_module():
    spec = importlib.util.spec_from_file_location("generate_operator96_low_noise_rerun_plan_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class Operator96LowNoiseRerunPlanTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_real_repo_first_tranche_uses_first_five_low_noise_records(self):
        plan = self.module.build_plan(tranche_size=5)

        self.assertEqual(plan["status"], "PASS")
        self.assertEqual(plan["summary"]["candidate_record_count"], 85)
        self.assertEqual(plan["summary"]["first_tranche_indexes"], [1, 2, 6, 9, 10])
        self.assertIn("--host-noise-max-retries", plan["commands"]["run"])
        self.assertIn("--run", plan["commands"]["run"])
        self.assertNotIn("--run", plan["commands"]["plan_only"])

    def test_build_campaign_command_uses_separate_output_dir_and_indexes(self):
        command = self.module.build_campaign_command([1, 2], run=True)

        self.assertIn("--output-dir", command)
        self.assertIn("registry-research-framework/audit/registry-value-experiments-low-noise-20260510", command)
        self.assertEqual(command.count("--only-index"), 2)
        self.assertIn("--rerun", command)

    def test_custom_review_filters_only_low_noise_records(self):
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            review_path = Path(temp_root) / "review.json"
            review_path.write_text(
                json.dumps(
                    {
                        "records": [
                            {"index": 2, "value_name": "B", "app_surface_bucket": "blocked_by_gate"},
                            {"index": 1, "value_name": "A", "app_surface_bucket": "needs_low_noise_rerun"},
                            {"index": 3, "value_name": "C", "app_surface_bucket": "ready_for_bounded_app_card"},
                        ]
                    }
                ),
                encoding="utf-8",
            )

            plan = self.module.build_plan(review_path, tranche_size=2)

        self.assertEqual(plan["summary"]["candidate_record_count"], 1)
        self.assertEqual(plan["summary"]["first_tranche_indexes"], [1])


if __name__ == "__main__":
    unittest.main()
