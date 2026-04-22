from __future__ import annotations

import importlib.util
import io
import json
import subprocess
import sys
import tempfile
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


reboot_observation = load_module(
    "run_guest_reboot_observation_for_tests",
    VM_KVM_SCRIPTS / "run-guest-reboot-observation.py",
)


class VmKvmRebootObservationTests(unittest.TestCase):
    def test_prepare_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-reboot-observation.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "reboot-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                reboot_observation,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                reboot_observation,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                reboot_observation,
                "wait_for_file",
                return_value=False,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = reboot_observation.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "prepare-timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-reboot-observation")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "reboot-observation-prepare-timeout")

    def test_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            before_path = upload_dir / "reboot-test-before.json"
            summary_path = upload_dir / "reboot-test-summary.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == before_path:
                    before_path.write_text("{}", encoding="utf-8")
                if path == summary_path:
                    summary_path.write_text("{not-json", encoding="utf-8")
                return True

            argv = [
                "run-guest-reboot-observation.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "reboot-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                reboot_observation,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                reboot_observation,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                reboot_observation,
                "run",
                return_value=None,
            ), mock.patch.object(reboot_observation.time, "sleep", return_value=None), mock.patch.object(
                reboot_observation,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = reboot_observation.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "reboot-observation-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-reboot-observation")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            before_path = upload_dir / "reboot-test-before.json"
            summary_path = upload_dir / "reboot-test-summary.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == before_path:
                    before_path.write_text("{}", encoding="utf-8")
                if path == summary_path:
                    summary_path.write_text('["not","object"]', encoding="utf-8")
                return True

            argv = [
                "run-guest-reboot-observation.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "reboot-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                reboot_observation,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                reboot_observation,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                reboot_observation,
                "run",
                return_value=None,
            ), mock.patch.object(reboot_observation.time, "sleep", return_value=None), mock.patch.object(
                reboot_observation,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = reboot_observation.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "reboot-observation-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_launch_failure_reports_contract_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-reboot-observation.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "reboot-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            failure = subprocess.CalledProcessError(9, ["qga-run-powershell.py"], output="stdout-text", stderr="stderr-text")
            setattr(failure, "stage", "qga-launch")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                reboot_observation,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                reboot_observation,
                "launch_generated_script",
                side_effect=failure,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = reboot_observation.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "reboot-observation-host-step-error")
        self.assertEqual(payload["recovery_action"], "rerun-reboot-observation")
        self.assertEqual(payload["transport_blocker"], "host-step-error")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-step-failure")
        self.assertEqual(payload["host_step"], "qga-launch")
        self.assertEqual(payload["exit_code"], 9)


if __name__ == "__main__":
    unittest.main()
