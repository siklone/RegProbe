from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "registry-research-framework" / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


card_contracts = load_module(
    "check_app_card_evidence_contracts",
    SCRIPTS_ROOT / "check_app_card_evidence_contracts.py",
)


class AppCardEvidenceContractsTests(unittest.TestCase):
    def test_real_repo_promoted_app_cards_pass_contract_sweep(self) -> None:
        report = card_contracts.build_report(REPO_ROOT)

        self.assertEqual(report["status"], "PASS")
        self.assertEqual(report["summary"]["fail_count"], 0)
        self.assertGreater(report["summary"]["candidate_count"], 250)

    def test_validate_candidate_plan_accepts_default_only_rollback_metadata(self) -> None:
        plan = {
            "tweak_id": "system.default-only",
            "card_expectations": {
                "name": "Default Only",
                "category": "System",
                "documentation": "research/records/system.aero-shake.json",
            },
            "rollback_expectations": {
                "restore_default_supported": True,
                "restore_previous_supported": False,
            },
            "evidence_expectations": {
                "linked_evidence_count": 1,
            },
            "expected_report": {
                "rollback_requested": True,
                "required_stages": ["detect-before", "apply", "rollback", "detect-after"],
                "required_card_snapshot": {
                    "required_fields": card_contracts.REQUIRED_CARD_FIELDS,
                    "required_proof_lanes": card_contracts.REQUIRED_PROOF_LANES,
                    "claim_boundary_required": True,
                }
            },
            "expected_report_skip_rollback": {
                "required_card_snapshot": {
                    "required_fields": card_contracts.REQUIRED_CARD_FIELDS,
                    "required_proof_lanes": card_contracts.REQUIRED_PROOF_LANES,
                    "claim_boundary_required": True,
                }
            },
            "operator_checklist": ["Open the card."],
        }

        self.assertEqual(card_contracts.validate_candidate_plan(plan, repo_root=REPO_ROOT), [])

    def test_validate_candidate_plan_reports_missing_claim_boundary_and_lanes(self) -> None:
        plan = {
            "tweak_id": "system.bad-card",
            "card_expectations": {
                "name": "Bad Card",
                "category": "System",
                "documentation": "research/records/system.aero-shake.json",
            },
            "rollback_expectations": {
                "restore_default_supported": False,
                "restore_previous_supported": False,
            },
            "evidence_expectations": {
                "linked_evidence_count": 0,
            },
            "expected_report": {
                "rollback_requested": True,
                "required_stages": ["detect-before", "apply", "detect-after"],
                "required_card_snapshot": {
                    "required_fields": ["TweakId"],
                    "required_proof_lanes": ["docs"],
                    "claim_boundary_required": False,
                }
            },
            "expected_report_skip_rollback": {
                "required_card_snapshot": {
                    "required_fields": ["TweakId"],
                    "required_proof_lanes": ["docs"],
                    "claim_boundary_required": False,
                }
            },
            "operator_checklist": [],
        }

        failures = card_contracts.validate_candidate_plan(plan, repo_root=REPO_ROOT)

        self.assertTrue(any("missing required card fields" in failure for failure in failures))
        self.assertTrue(any("missing required proof lanes" in failure for failure in failures))
        self.assertTrue(any("claim boundary" in failure for failure in failures))
        self.assertTrue(any("rollback is requested" in failure for failure in failures))
        self.assertTrue(any("no linked evidence" in failure for failure in failures))


if __name__ == "__main__":
    unittest.main()
