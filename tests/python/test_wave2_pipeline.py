from __future__ import annotations

import importlib.util
import sys
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


behavior_stats_lib = load_module("behavior_stats_lib", SCRIPTS_ROOT / "behavior_stats_lib.py")
wave2_research_lib = load_module("wave2_research_lib", SCRIPTS_ROOT / "wave2_research_lib.py")
negative_evidence = load_module("generate_negative_evidence", SCRIPTS_ROOT / "generate_negative_evidence.py")


class Wave2MetadataTests(unittest.TestCase):
    def test_evidence_freshness_prefers_tested_on_build(self) -> None:
        record = {
            "tested_on": [
                {"environment": "vm", "os": "Windows 11", "build": "26100", "notes": "pilot"},
            ],
            "last_reviewed_utc": "2026-04-02T00:00:00Z",
        }

        freshness = wave2_research_lib.evidence_freshness(record)

        self.assertEqual(freshness["os_build"], "26100")
        self.assertEqual(freshness["evidence_collected_utc"], "2026-04-02T00:00:00Z")
        self.assertTrue(freshness["revalidation_needed_on_major_update"])
        self.assertIsNone(freshness["expires_after_build"])

    def test_interaction_graph_resolves_known_group(self) -> None:
        groups = wave2_research_lib.interaction_groups_for_tweak("power.disable-fast-startup")
        self.assertTrue(groups)
        self.assertEqual(groups[0]["group_id"], "hibernate-disable")


class BehaviorStatisticsTests(unittest.TestCase):
    def test_statistics_mark_large_difference_as_significant(self) -> None:
        summary = behavior_stats_lib.summarize_before_after(
            [100.0, 102.0, 101.0, 99.0, 100.5],
            [60.0, 59.5, 61.0, 60.5, 59.0],
        )

        self.assertTrue(summary["significant"])
        self.assertEqual(summary["effect_size"], "large")
        self.assertLess(summary["p_value"], 0.05)

    def test_statistics_mark_small_difference_as_not_significant(self) -> None:
        summary = behavior_stats_lib.summarize_before_after(
            [100.0, 101.0, 99.5, 100.5, 100.0],
            [100.2, 100.8, 99.7, 100.6, 100.1],
        )

        self.assertFalse(summary["significant"])
        self.assertGreater(summary["p_value"], 0.05)


class NegativeEvidenceTests(unittest.TestCase):
    def test_negative_payload_marks_class_e_record(self) -> None:
        record = {
            "record_id": "example.dead-flag",
            "tweak_id": "example.dead-flag",
            "record_status": "deprecated",
            "summary": "Did not capture any supporting runtime read.",
            "decision": {"why": "No supporting evidence found."},
            "evidence": [],
        }
        class_entry = {
            "evidence_class": "E",
            "record_status": "deprecated",
            "tested_build": "26100",
            "runtime_proof": {"has_runtime_evidence": False},
            "validated_semantics": {"has_validation_proof": False},
            "anticheat_risk": {"anticheat_risk": "unknown", "gaming_safe": None},
            "gating_reason": "Archived audit trail.",
        }

        self.assertTrue(negative_evidence.is_negative_evidence_candidate(record, class_entry))
        payload = negative_evidence.build_negative_payload(record, class_entry, {"layers_used": ["runtime"], "tools_used": ["etw"]})

        self.assertEqual(payload["negative_reason"], "class-e")
        self.assertEqual(payload["tested_build"], "26100")
        self.assertEqual(payload["attempted_tools"], ["etw"])


if __name__ == "__main__":
    unittest.main()
