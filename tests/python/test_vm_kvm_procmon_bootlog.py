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


procmon_bootlog = load_module(
    "run_guest_procmon_bootlog_for_tests",
    VM_KVM_SCRIPTS / "run-guest-procmon-bootlog.py",
)


class VmKvmProcmonBootlogTests(unittest.TestCase):
    def test_guest_helper_publishes_stage_artifacts(self) -> None:
        helper = REPO_ROOT / "scripts" / "vm" / "guest-tools" / "run-procmon-bootlog-probe.ps1"
        text = helper.read_text(encoding="utf-8")

        self.assertIn("function Publish-Stage", text)
        self.assertIn("Invoke-ArtifactUpload -Path $stagePath -RemoteName ($OutputName + '-stage.json')", text)
        self.assertIn("Publish-Stage -StageName 'artifact-upload' -Status 'starting'", text)
        self.assertIn("Publish-Stage -StageName 'collect-complete' -Status $summary.status -ErrorMessage $summary.error", text)

    def test_prepare_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                return_value=False,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "prepare-timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-procmon-bootlog")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "procmon-prepare-timeout")

    def test_first_artifact_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            arm_summary_path = upload_dir / "procmon-test-summary-arm.json"
            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--first-artifact-timeout-seconds",
                "60",
            ]

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == arm_summary_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text('{"status":"ok"}\n', encoding="utf-8")
                    return True
                return False

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch.object(
                procmon_bootlog.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 61.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "bridge-artifact-timeout")
        self.assertEqual(payload["recovery_action"], "inspect-bridge-upload")
        self.assertEqual(payload["transport_blocker"], "bridge-artifact-timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "first-artifact-timeout")

    def test_invalid_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            arm_summary_path = upload_dir / "procmon-test-summary-arm.json"
            stage_path = upload_dir / "procmon-test-stage.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == arm_summary_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text('{"status":"ok"}\n', encoding="utf-8")
                    return True
                return False

            def fake_run(*args, **kwargs):  # noqa: ANN002, ANN003
                stage_path.write_text("{not-json", encoding="utf-8")
                return None

            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch.object(
                procmon_bootlog.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "procmon-stage-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-procmon-bootlog")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_stage_stall_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            arm_summary_path = upload_dir / "procmon-test-summary-arm.json"
            stage_path = upload_dir / "procmon-test-stage.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == arm_summary_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text('{"status":"ok"}\n', encoding="utf-8")
                    return True
                return False

            def fake_run(*args, **kwargs):  # noqa: ANN002, ANN003
                stage_path.write_text(
                    json.dumps(
                        {
                            "generated_utc": "1970-01-01T00:00:00Z",
                            "stage": "collect-save-as-csv",
                            "status": "starting",
                            "error": None,
                        }
                    ),
                    encoding="utf-8",
                )
                return None

            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--first-artifact-timeout-seconds",
                "60",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch.object(
                procmon_bootlog.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 200.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-stage-stall")
        self.assertEqual(payload["recovery_action"], "inspect-procmon-stage")
        self.assertEqual(payload["transport_blocker"], "stage-stall")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "stage-timeout")
        self.assertEqual(payload["stage"]["stage"], "collect-save-as-csv")

    def test_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            arm_summary_path = upload_dir / "procmon-test-summary-arm.json"
            summary_path = upload_dir / "procmon-test-summary.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == arm_summary_path:
                    arm_summary_path.parent.mkdir(parents=True, exist_ok=True)
                    arm_summary_path.write_text('{"status":"ok"}\n', encoding="utf-8")
                    return True
                return False

            def fake_run(*args, **kwargs):  # noqa: ANN002, ANN003
                summary_path.parent.mkdir(parents=True, exist_ok=True)
                summary_path.write_text("{not-json", encoding="utf-8")
                return None

            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                procmon_bootlog.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch.object(
                procmon_bootlog.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "procmon-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-procmon-bootlog")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            arm_summary_path = upload_dir / "procmon-test-summary-arm.json"
            summary_path = upload_dir / "procmon-test-summary.json"

            def fake_wait(path, timeout_seconds):  # noqa: ANN001
                if path == arm_summary_path:
                    arm_summary_path.parent.mkdir(parents=True, exist_ok=True)
                    arm_summary_path.write_text('{"status":"ok"}\n', encoding="utf-8")
                    return True
                return False

            def fake_run(*args, **kwargs):  # noqa: ANN002, ANN003
                summary_path.parent.mkdir(parents=True, exist_ok=True)
                summary_path.write_text('["not","object"]', encoding="utf-8")
                return None

            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                procmon_bootlog.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "wait_for_file",
                side_effect=fake_wait,
            ), mock.patch.object(
                procmon_bootlog.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "procmon-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_host_step_failure_reports_contract_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-procmon-bootlog.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "procmon-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            failure = subprocess.CalledProcessError(7, ["type-to-guest.py"], output="typed", stderr="no focus")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                procmon_bootlog,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                procmon_bootlog,
                "run",
                side_effect=failure,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = procmon_bootlog.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "procmon-host-step-error")
        self.assertEqual(payload["recovery_action"], "rerun-procmon-bootlog")
        self.assertEqual(payload["transport_blocker"], "host-step-error")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-step-failure")
        self.assertEqual(payload["host_step"], "ensure-admin-shell-arm")
        self.assertEqual(payload["exit_code"], 7)


if __name__ == "__main__":
    unittest.main()
