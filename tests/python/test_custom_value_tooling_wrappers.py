from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_DIR = REPO_ROOT / "registry-research-framework" / "scripts"


def load_module(script_name: str):
    path = SCRIPT_DIR / script_name
    spec = importlib.util.spec_from_file_location(script_name.replace(".py", "_for_tests"), path)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class CustomValueToolingWrapperTests(unittest.TestCase):
    def test_neutral_wrappers_export_legacy_implementation_functions(self):
        enriched = load_module("generate_custom_value_enriched_matrix.py")
        review = load_module("generate_custom_value_app_surface_review.py")
        plan = load_module("generate_custom_value_low_noise_rerun_plan.py")
        aggregate = load_module("aggregate_custom_value_low_noise_rerun_campaign.py")

        self.assertTrue(callable(enriched.build_candidates))
        self.assertTrue(callable(review.build_review))
        self.assertTrue(callable(plan.build_plan))
        self.assertTrue(callable(aggregate.build_aggregate))

    def test_custom_value_app_surface_review_cli_keeps_existing_json_contract(self):
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_dir:
            output = Path(temp_dir) / "review.json"
            markdown = Path(temp_dir) / "review.md"
            result = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_DIR / "generate_custom_value_app_surface_review.py"),
                    "--output",
                    str(output),
                    "--markdown-output",
                    str(markdown),
                    "--json",
                ],
                cwd=REPO_ROOT,
                text=True,
                capture_output=True,
                check=True,
            )

        self.assertIn('"status": "PASS"', result.stdout)
        self.assertIn('"ready_for_bounded_app_card": 0', result.stdout)
        self.assertIn("operator96-app-surface-review-20260510", result.stdout)


if __name__ == "__main__":
    unittest.main()
