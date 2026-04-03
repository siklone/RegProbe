from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


source_enrichment_scan = load_module("source_enrichment_scan", SCRIPTS_ROOT / "source_enrichment_scan.py")


class SourceEnrichmentTests(unittest.TestCase):
    def test_score_candidate_tracks_weighted_support_and_trigger_family(self) -> None:
        candidate = {
            "candidate_id": "power.control.allow-system-required-power-requests",
            "family": "power-control",
            "suspected_layer": "kernel",
            "boot_phase_relevant": True,
            "registry_path": r"HKLM\SYSTEM\CurrentControlSet\Control\Power",
            "value_name": "AllowSystemRequiredPowerRequests",
            "route_bucket": "docs-first-new-candidate",
        }
        source_results = [
            {
                "id": "admx",
                "label": "ADMX",
                "surface_group": "policy-templates",
                "kind": "local-source",
                "weight": 2,
                "hits_by_candidate": {
                    candidate["candidate_id"]: [
                        {
                            "file": "PolicyDefinitions/power.admx",
                            "line_number": 17,
                            "value_name": candidate["value_name"],
                            "content": candidate["value_name"],
                        }
                    ]
                },
                "root": "C:/Windows/PolicyDefinitions",
                "missing_reason": None,
            }
        ]

        scored = source_enrichment_scan.score_candidate(candidate, source_results)

        self.assertEqual(scored["support_count"], 1)
        self.assertEqual(scored["enrichment_score"], 2)
        self.assertEqual(scored["trigger_family"], "power-request-simulation")
        self.assertEqual(scored["suggested_queue_bucket"], "runtime")
        self.assertIn("PowerCreateRequest(SystemRequired)", scored["suggested_trigger"])

    def test_build_priority_queue_orders_by_score_and_bucket(self) -> None:
        candidates = [
            {
                "candidate_id": "alpha",
                "enrichment_score": 1,
                "support_count": 1,
                "suggested_runtime_priority": "medium",
                "suggested_queue_bucket": "runtime",
            },
            {
                "candidate_id": "beta",
                "enrichment_score": 4,
                "support_count": 2,
                "suggested_runtime_priority": "high",
                "suggested_queue_bucket": "runtime",
            },
            {
                "candidate_id": "gamma",
                "enrichment_score": 3,
                "support_count": 1,
                "suggested_runtime_priority": "low",
                "suggested_queue_bucket": "windbg",
            },
        ]

        queue = source_enrichment_scan.build_priority_queue(candidates)

        self.assertEqual(queue["high_priority_runtime"], ["beta", "alpha"])
        self.assertEqual(queue["high_priority_windbg"], ["gamma"])

    def test_scan_source_marks_missing_roots_honestly(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            missing_source = {
                "id": "ghost",
                "label": "Ghost",
                "surface_group": "reference-cache",
                "kind": "reference-cache",
                "root": str(Path(temp_root) / "does-not-exist"),
                "patterns": ["*.txt"],
                "enrichment_weight": 1,
            }
            result = source_enrichment_scan.scan_source(missing_source, [])

            self.assertFalse(result["exists"])
            self.assertEqual(result["missing_reason"], "root-missing")


if __name__ == "__main__":
    unittest.main()
