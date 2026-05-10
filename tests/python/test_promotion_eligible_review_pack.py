from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "generate_promotion_eligible_review_pack.py"


def load_module():
    spec = importlib.util.spec_from_file_location("generate_promotion_eligible_review_pack", SCRIPT_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules["generate_promotion_eligible_review_pack"] = module
    spec.loader.exec_module(module)
    return module


review_pack = load_module()


class PromotionEligibleReviewPackTests(unittest.TestCase):
    def test_build_pack_summarizes_decision_ready_records(self) -> None:
        gate_payload = {
            "summary": {
                "promotion_state_counts": {
                    "blocked": 0,
                    "promotion-eligible": 2,
                }
            },
            "entries": [
                {
                    "record_id": "power.control.mf-buffering-threshold",
                    "tweak_id": "power.control.mf-buffering-threshold",
                    "promotion_state": "promotion-eligible",
                    "next_missing_layer": "none",
                    "app_mapping_status": "matches-research",
                    "tweak_origin": "legacy-curated",
                    "documentation_status": {"confidence": "high"},
                    "evidence_status": {
                        "evidence_count": 3,
                        "has_procmon_evidence": True,
                        "has_ghidra_evidence": True,
                        "has_reboot_evidence": True,
                    },
                    "rollback_status": {
                        "rollback_verified": True,
                        "rollback_verification_method": "record-restore-story",
                        "rollback_value": {"state_kind": "value", "value": 0},
                    },
                    "score_breakdown": {"overall_score": 3.7},
                },
                {
                    "record_id": "system.kernel.disable-exception-chain-validation",
                    "tweak_id": "system.kernel.disable-exception-chain-validation",
                    "promotion_state": "promotion-eligible",
                    "next_missing_layer": "none",
                    "app_mapping_status": "matches-research",
                    "tweak_origin": "legacy-curated",
                    "documentation_status": {"confidence": "high"},
                    "evidence_status": {
                        "evidence_count": 2,
                        "has_procmon_evidence": False,
                        "has_ghidra_evidence": True,
                        "has_reboot_evidence": True,
                    },
                    "rollback_status": {
                        "rollback_verified": True,
                        "rollback_verification_method": "record-restore-story",
                        "rollback_value": {"state_kind": "missing", "value": None},
                    },
                    "score_breakdown": {"overall_score": 3.6},
                },
                {
                    "record_id": "example.promoted",
                    "promotion_state": "promoted",
                },
            ],
        }
        records = {
            "power.control.mf-buffering-threshold": {
                "record_id": "power.control.mf-buffering-threshold",
                "setting": {
                    "targets": [
                        {
                            "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
                            "value_name": "MfBufferingThreshold",
                            "value_type": "REG_DWORD",
                        }
                    ]
                },
                "evidence": [
                    {"id": "procmon", "kind": "procmon-trace", "location": "evidence/procmon.json"},
                    {"id": "ghidra", "kind": "decompilation", "location": "evidence/ghidra.json"},
                ],
            },
            "system.kernel.disable-exception-chain-validation": {
                "record_id": "system.kernel.disable-exception-chain-validation",
                "setting": {
                    "targets": [
                        {
                            "path": "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel",
                            "value_name": "DisableExceptionChainValidation",
                            "value_type": "REG_DWORD",
                        }
                    ]
                },
                "evidence": [
                    {"id": "etw", "kind": "etw-trace", "location": "evidence/etw.json"},
                    {"id": "ghidra", "kind": "decompilation", "location": "evidence/ghidra.json"},
                ],
            },
        }

        pack = review_pack.build_pack(gate_payload, records, generated_utc="2026-05-08T00:00:00Z")

        self.assertEqual(pack["metadata"]["total_records"], 2)
        self.assertTrue(pack["metadata"]["preconditions"]["all_records_confidence_high"])
        self.assertEqual(pack["summary_stats"]["promote_candidates"], 1)
        self.assertEqual(pack["summary_stats"]["hold_candidates"], 1)
        self.assertEqual(pack["summary_stats"]["verdict_counts"]["PROMOTE"], 1)
        self.assertEqual(pack["summary_stats"]["verdict_counts"]["INTENTIONAL-HOLD-CLOSED"], 1)

        security_item = next(
            item
            for item in pack["records"]
            if item["record_id"] == "system.kernel.disable-exception-chain-validation"
        )
        self.assertEqual(security_item["risk_assessment"]["classification"], "critical-security")
        self.assertEqual(security_item["recommended_action"]["verdict"], "INTENTIONAL-HOLD-CLOSED")

        markdown = review_pack.render_markdown(pack)
        self.assertIn("Promotion-Eligible Review Pack", markdown)
        self.assertIn("system.kernel.disable-exception-chain-validation", markdown)


if __name__ == "__main__":
    unittest.main()
