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
    @staticmethod
    def card_snapshot(**overrides: object) -> dict[str, object]:
        card: dict[str, object] = {
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
        }
        card.update(overrides)
        return card

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
                "Card": self.card_snapshot(),
            }
        )

        result = payload[0]
        self.assertEqual(exit_code, 0)
        self.assertTrue(result["report_success"])
        self.assertEqual(result["report_contract_status"], "ok")
        self.assertTrue(result["report_card_has_claim_boundary"])
        self.assertEqual(result["report_card_missing_fields"], [])
        self.assertEqual(result["report_card_missing_proof_lanes"], [])

    def test_allow_gated_mutation_passes_explicit_guest_script_switch(self) -> None:
        report = {
            "Success": False,
            "Status": "mutation-blocked",
            "Summary": "blocked",
            "Card": self.card_snapshot(EvidenceClass="B", ResearchStatus="INTENTIONAL HOLD"),
        }
        qga_payload = {
            "status": "completed",
            "execution": {
                "exitcode": 2,
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
            "--allow-gated-mutation",
        ]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_tweak_qa_batch.subprocess,
            "run",
            return_value=completed,
        ) as run_mock, mock.patch("sys.stdout", new_callable=io.StringIO):
            app_tweak_qa_batch.main()

        command = run_mock.call_args.args[0]
        self.assertIn("--ps-arg=-AllowGatedMutation", command)

    def test_empty_stdout_downloads_guest_report_fallback(self) -> None:
        report = {
            "Success": True,
            "Status": "ok",
            "Summary": "Apply/verify path completed.",
            "Card": self.card_snapshot(),
        }
        qga_payload = {
            "status": "completed",
            "execution": {
                "exitcode": 0,
                "stdout": "",
            },
        }

        def fake_run(cmd, cwd, capture_output, text):
            if "qga-get-file.py" in str(cmd[1]):
                destination = Path(cmd[cmd.index("--destination") + 1])
                destination.write_text(json.dumps(report), encoding="utf-8")
                return mock.Mock(returncode=0, stdout=json.dumps({"status": "downloaded"}), stderr="")
            return mock.Mock(returncode=0, stdout=json.dumps(qga_payload), stderr="")

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
            side_effect=fake_run,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_tweak_qa_batch.main()

        payload = json.loads(stdout.getvalue())
        result = payload[0]
        self.assertEqual(exit_code, 0)
        self.assertTrue(result["report_success"])
        self.assertEqual(result["report_source"], "guest-file")
        self.assertEqual(result["report_fetch_status"], "ok")

    def test_registry_path_splitter_treats_last_segment_as_value_name(self) -> None:
        split = app_tweak_qa_batch.split_registry_value_path(
            r"HKLM\SOFTWARE\Microsoft\Windows\Dwm\OverlayMinFPS"
        )

        self.assertEqual(split["key_path"], r"HKLM\SOFTWARE\Microsoft\Windows\Dwm")
        self.assertEqual(split["value_name"], "OverlayMinFPS")
        self.assertEqual(split["powershell_key_path"], r"HKLM:\SOFTWARE\Microsoft\Windows\Dwm")

    def test_post_run_registry_probe_verifies_rollback_absent_value(self) -> None:
        report = {
            "Success": True,
            "Status": "ok",
            "Summary": "Apply/verify path completed and rollback restored the tweak.",
            "Card": self.card_snapshot(RegistryPath=r"HKLM\SOFTWARE\Microsoft\Windows\Dwm\OverlayMinFPS"),
            "Stages": [
                {"Stage": "detect-after", "CurrentValue": "Not set"},
            ],
        }
        qga_payload = {
            "status": "completed",
            "execution": {
                "exitcode": 0,
                "stdout": json.dumps(report),
            },
        }
        probe_payload = {
            "status": "exited",
            "exitcode": 0,
            "stdout": json.dumps(
                {
                    "status": "absent-value",
                    "exists": False,
                    "key_path": r"HKLM:\SOFTWARE\Microsoft\Windows\Dwm",
                    "value_name": "OverlayMinFPS",
                    "value": None,
                    "value_text": "",
                }
            ),
        }

        def fake_run(cmd, cwd, capture_output, text):
            if "qga-exec.py" in str(cmd[1]):
                return mock.Mock(returncode=0, stdout=json.dumps(probe_payload), stderr="")
            return mock.Mock(returncode=0, stdout=json.dumps(qga_payload), stderr="")

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
            side_effect=fake_run,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_tweak_qa_batch.main()

        result = json.loads(stdout.getvalue())[0]
        self.assertEqual(exit_code, 0)
        self.assertTrue(result["report_success"])
        self.assertEqual(result["post_run_registry_verification"], "verified")
        self.assertFalse(result["post_run_registry_probe"]["exists"])
        self.assertEqual(result["post_run_registry_probe"]["value_name"], "OverlayMinFPS")

    def test_post_run_registry_mismatch_fails_contract(self) -> None:
        report = {
            "Success": True,
            "Status": "ok",
            "Summary": "Apply/verify path completed and rollback restored the tweak.",
            "Card": self.card_snapshot(RegistryPath=r"HKLM\SOFTWARE\Microsoft\Windows\Dwm\OverlayMinFPS"),
            "Stages": [
                {"Stage": "detect-after", "CurrentValue": "Not set"},
            ],
        }
        qga_payload = {
            "status": "completed",
            "execution": {
                "exitcode": 0,
                "stdout": json.dumps(report),
            },
        }
        probe_payload = {
            "status": "exited",
            "exitcode": 0,
            "stdout": json.dumps(
                {
                    "status": "ok",
                    "exists": True,
                    "value_name": "OverlayMinFPS",
                    "value": 0,
                    "value_text": "0",
                }
            ),
        }

        def fake_run(cmd, cwd, capture_output, text):
            if "qga-exec.py" in str(cmd[1]):
                return mock.Mock(returncode=0, stdout=json.dumps(probe_payload), stderr="")
            return mock.Mock(returncode=0, stdout=json.dumps(qga_payload), stderr="")

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
            side_effect=fake_run,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_tweak_qa_batch.main()

        result = json.loads(stdout.getvalue())[0]
        self.assertEqual(exit_code, 2)
        self.assertFalse(result["report_success"])
        self.assertEqual(result["post_run_registry_verification"], "mismatch")
        self.assertIn("post-run-registry-mismatch", result["report_contract_failures"])


if __name__ == "__main__":
    unittest.main()
