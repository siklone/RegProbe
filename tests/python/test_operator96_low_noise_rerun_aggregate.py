from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "aggregate_operator96_low_noise_rerun_campaign.py"


def load_module():
    spec = importlib.util.spec_from_file_location("aggregate_operator96_low_noise_rerun_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def write_json(path: Path, payload: str) -> None:
    path.write_text(payload, encoding="utf-8")


class Operator96LowNoiseRerunAggregateTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_campaign_paths_sort_initial_tranche_before_numbered_tranches(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name in (
                "operator96-low-noise-rerun-tranche-10-20260510.json",
                "operator96-low-noise-rerun-tranche-20260510.json",
                "operator96-low-noise-rerun-tranche-02-20260510.json",
                "operator96-low-noise-rerun-tranche-20260512-03.json",
            ):
                write_json(root / name, "{}")

            paths = self.module.find_campaign_paths(root, pattern="operator96-low-noise-rerun-tranche*.json")

        self.assertEqual([path.name for path in paths], [
            "operator96-low-noise-rerun-tranche-20260510.json",
            "operator96-low-noise-rerun-tranche-02-20260510.json",
            "operator96-low-noise-rerun-tranche-10-20260510.json",
            "operator96-low-noise-rerun-tranche-20260512-03.json",
        ])

    def test_aggregate_deduplicates_results_and_counts_verdicts(self):
        first = {
            "status": "ok",
            "plan": [{"experiment_id": "exp-1"}],
            "results": [
                {
                    "experiment_id": "exp-1",
                    "status": "ok",
                    "observations": {
                        "verdict": "harmful",
                        "host_noise": "ok",
                        "confidence": "low",
                        "smoke_hard_success": {"apply": True},
                    },
                }
            ],
        }
        second = {
            "status": "ok",
            "plan": [{"experiment_id": "exp-1"}, {"experiment_id": "exp-2"}],
            "results": [
                {
                    "experiment_id": "exp-1",
                    "status": "ok",
                    "observations": {
                        "verdict": "harmful",
                        "host_noise": "ok",
                        "confidence": "low",
                        "smoke_hard_success": {"apply": True},
                    },
                },
                {
                    "experiment_id": "exp-2",
                    "status": "ok",
                    "observations": {
                        "verdict": "noisy",
                        "host_noise": "noisy",
                        "confidence": "medium",
                        "smoke_hard_success": {"apply": True},
                    },
                },
            ],
        }

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            one = root / "operator96-low-noise-rerun-tranche-20260510.json"
            two = root / "operator96-low-noise-rerun-tranche-02-20260510.json"
            write_json(one, self.module.json.dumps(first))
            write_json(two, self.module.json.dumps(second))
            aggregate = self.module.build_aggregate(
                self.module.find_campaign_paths(root, pattern="operator96-low-noise-rerun-tranche*.json")
            )

        self.assertEqual(aggregate["status"], "ok")
        self.assertEqual(aggregate["summary"]["result_count"], 2)
        self.assertEqual(aggregate["summary"]["duplicate_result_count"], 1)
        self.assertEqual(aggregate["summary"]["verdict_counts"], {"harmful": 1, "noisy": 1})
        self.assertEqual(aggregate["summary"]["host_noise_counts"], {"noisy": 1, "ok": 1})
        self.assertEqual(aggregate["summary"]["noisy_result_count"], 1)
        self.assertEqual(aggregate["summary"]["noisy_results"][0]["experiment_id"], "exp-2")
        self.assertEqual(aggregate["display_name"], "Custom Registry Value Low-Noise Rerun Aggregate")
        self.assertEqual(aggregate["legacy_artifact_prefix"], "operator96")
        self.assertIn("historical filename prefix", aggregate["legacy_artifact_note"])

    def test_default_glob_points_to_clean_certified_rerun_wave(self):
        self.assertEqual(
            self.module.DEFAULT_GLOB,
            "operator96-low-noise-rerun-tranche-20260512-*.json",
        )

    def test_non_ok_source_marks_aggregate_for_review(self):
        payload = {
            "status": "ok",
            "plan": [],
            "results": [{"experiment_id": "bad", "status": "error", "observations": {}}],
        }

        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "operator96-low-noise-rerun-tranche-20260510.json"
            write_json(path, self.module.json.dumps(payload))
            aggregate = self.module.build_aggregate([path])

        self.assertEqual(aggregate["status"], "review")
        self.assertEqual(aggregate["summary"]["non_ok_count"], 1)


if __name__ == "__main__":
    unittest.main()
