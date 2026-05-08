from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "generate_rejected_closure_ledger.py"


def load_module():
    spec = importlib.util.spec_from_file_location("generate_rejected_closure_ledger", SCRIPT_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules["generate_rejected_closure_ledger"] = module
    spec.loader.exec_module(module)
    return module


ledger_module = load_module()


class RejectedClosureLedgerTests(unittest.TestCase):
    def test_build_ledger_classifies_rejected_records_without_active_gap_language(self) -> None:
        gate_payload = {
            "entries": [
                {
                    "record_id": "example.rejected",
                    "tweak_id": "example.rejected",
                    "promotion_state": "rejected",
                    "promotion_disposition": "rejected",
                    "promotion_blockers": ["promotion-disposition-protected-acl-not-actionable"],
                    "closure_status": "evidence-backed-rejected",
                    "closure_kind": "protected-acl-not-actionable",
                    "closure_reason": "Protected ACL lane proves this should fail closed.",
                    "rejection_closure": {
                        "status": "evidence-backed-rejected",
                        "kind": "protected-acl-not-actionable",
                        "closure_blocker": "promotion-disposition-protected-acl-not-actionable",
                        "superseded_blockers": ["no-runtime-proof"],
                        "evidence_count": 2,
                        "confidence": "high",
                    },
                },
                {
                    "record_id": "example.deprecated",
                    "tweak_id": "example.deprecated",
                    "promotion_state": "rejected",
                    "promotion_blockers": ["deprecated-record"],
                    "closure_status": "deprecated-record",
                    "closure_kind": "deprecated-record",
                    "rejection_closure": {
                        "status": "deprecated-record",
                        "kind": "deprecated-record",
                        "closure_blocker": "deprecated-record",
                        "superseded_blockers": ["deprecated-record"],
                        "evidence_count": 1,
                        "confidence": "medium",
                    },
                },
            ]
        }
        records_by_id = {
            "example.rejected": {
                "evidence": [
                    {
                        "evidence_id": "acl-dump",
                        "kind": "vm",
                        "location": "evidence/captures/example.json",
                    }
                ]
            }
        }

        ledger = ledger_module.build_ledger(gate_payload, records_by_id, generated_utc="2026-05-08T00:00:00Z")

        self.assertTrue(ledger["summary"]["all_rejected_have_closure"])
        self.assertEqual(ledger["summary"]["evidence_backed_rejected"], 1)
        self.assertEqual(ledger["summary"]["deprecated_records"], 1)
        self.assertEqual(ledger["summary"]["unclassified_rejected"], 0)
        rejected = next(item for item in ledger["items"] if item["record_id"] == "example.rejected")
        self.assertEqual(rejected["superseded_blockers"], ["no-runtime-proof"])
        self.assertEqual(rejected["evidence_refs"][0]["evidence_id"], "acl-dump")

        markdown = ledger_module.render_markdown(ledger)
        self.assertIn("Rejected records are not treated as active evidence gaps", markdown)
        self.assertIn("`example.rejected`", markdown)


if __name__ == "__main__":
    unittest.main()
