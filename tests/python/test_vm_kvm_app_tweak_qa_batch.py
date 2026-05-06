from __future__ import annotations

import importlib.util
import io
import json
import sys
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[2]
VM_KVM_SCRIPTS = REPO_ROOT / "scripts" / "vm-kvm"
if str(VM_KVM_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(VM_KVM_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


app_tweak_qa_batch = load_module(
    "run_guest_app_tweak_qa_batch_for_tests",
    VM_KVM_SCRIPTS / "run-guest-app-tweak-qa-batch.py",
)


class VmKvmAppTweakQaBatchTests(unittest.TestCase):
    def run_batch_with_report(self, report: dict[str, object]) -> tuple[int, list[dict[str, object]]]:
        qga_payload = {
            "status": "completed",
            "execution": {
                "exitcode": 0,
                "stdout": json.dumps(report),
            },
        }
        completed = mock.Mock(returncode=0, stdout=json.dumps(qga_payload), stderr="")
        argv = [
            "run-guest-app-tweak-qa-batch.py",
            "--id",
            "system.alpha",
            "--wait-timeout",
            "1",
        ]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_tweak_qa_batch.subprocess,
            "run",
            return_value=completed,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_tweak_qa_batch.main()

        return exit_code, json.loads(stdout.getvalue())

    def test_successful_app_report_without_card_snapshot_fails_contract(self) -> None:
        exit_code, payload = self.run_batch_with_report(
            {
                "Success": True,
                "Status": "ok",
                "Summary": "Apply/verify path completed.",
            }
        )

        result = payload[0]
        self.assertEqual(exit_code, 2)
        self.assertTrue(result["report_app_success"])
        self.assertFalse(result["report_success"])
        self.assertFalse(result["report_card_present"])
        self.assertIn("missing-card-snapshot", result["report_contract_failures"])

    def test_successful_app_report_with_card_snapshot_passes_contract(self) -> None:
        exit_code, payload = self.run_batch_with_report(
            {
                "Success": True,
                "Status": "ok",
                "Summary": "Apply/verify path completed.",
                "Card": {
                    "TweakId": "system.alpha",
                    "Name": "System Alpha",
                    "Category": "System",
                    "EvidenceClass": "A",
                    "ResearchStatus": "PROMOTED",
                    "RollbackSnapshotState": "ready",
                    "HasClaimBoundary": True,
                    "WhatWeKnowSummary": "Known bounded claim.",
                    "WhatWeDoNotClaimSummary": "No benchmark claim.",
                    "ProofLanes": [
                        {"Key": "docs"},
                        {"Key": "runtime"},
                        {"Key": "source"},
                        {"Key": "rollback"},
                    ],
                },
            }
        )

        result = payload[0]
        self.assertEqual(exit_code, 0)
        self.assertTrue(result["report_success"])
        self.assertEqual(result["report_contract_status"], "ok")
        self.assertTrue(result["report_card_has_claim_boundary"])
        self.assertEqual(result["report_card_missing_fields"], [])
        self.assertEqual(result["report_card_missing_proof_lanes"], [])


if __name__ == "__main__":
    unittest.main()
