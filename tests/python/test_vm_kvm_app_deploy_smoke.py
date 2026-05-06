from __future__ import annotations

import importlib.util
import io
import json
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


app_deploy_smoke = load_module(
    "run_guest_app_deploy_smoke_for_tests",
    VM_KVM_SCRIPTS / "run-guest-app-deploy-smoke.py",
)


class VmKvmAppDeploySmokeTests(unittest.TestCase):
    def test_run_qga_exec_uses_equals_form_for_dash_prefixed_args(self) -> None:
        completed = mock.Mock(returncode=0, stdout='{"status":"exited"}', stderr="")
        with mock.patch.object(app_deploy_smoke.subprocess, "run", return_value=completed) as run_mock:
            exit_code, payload = app_deploy_smoke.run_qga_exec(
                REPO_ROOT,
                path="powershell.exe",
                args=["-NoProfile", "-Command", "Write-Host ok"],
            )

        cmd = run_mock.call_args.args[0]
        self.assertIn("--arg=-NoProfile", cmd)
        self.assertIn("--arg=-Command", cmd)
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "exited")

    def test_run_qga_exec_returns_parse_error_payload_for_invalid_json(self) -> None:
        completed = mock.Mock(returncode=1, stdout="{not-json", stderr="bad-json")
        with mock.patch.object(app_deploy_smoke.subprocess, "run", return_value=completed):
            exit_code, payload = app_deploy_smoke.run_qga_exec(
                REPO_ROOT,
                path="powershell.exe",
                args=["-NoProfile"],
            )

        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertIn("stdout_parse_error", payload)
        self.assertEqual(payload["stderr"], "bad-json")

    def test_run_qga_exec_returns_parse_error_payload_for_non_object_json(self) -> None:
        completed = mock.Mock(returncode=0, stdout='["not","object"]', stderr="")
        with mock.patch.object(app_deploy_smoke.subprocess, "run", return_value=completed):
            exit_code, payload = app_deploy_smoke.run_qga_exec(
                REPO_ROOT,
                path="powershell.exe",
                args=["-NoProfile"],
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["stdout_parse_error"], "stdout JSON payload is not an object")

    def test_run_app_launch_smoke_passes_launch_wait_timeout(self) -> None:
        completed = mock.Mock(returncode=0, stdout='{"status":"ok"}', stderr="")
        with mock.patch.object(app_deploy_smoke.subprocess, "run", return_value=completed) as run_mock:
            exit_code, payload = app_deploy_smoke.run_app_launch_smoke(
                REPO_ROOT,
                app_exe=r"C:\Tools\AppSmoke\RegProbe.App.exe",
                launch_wait_timeout=37,
                linger_seconds=5,
                leave_running=False,
            )

        cmd = run_mock.call_args.args[0]
        self.assertIn("--launch-wait-timeout", cmd)
        self.assertIn("37", cmd)
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")

    def test_main_returns_ok_when_upload_deploy_and_smoke_succeed(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip), "--linger-seconds", "1"]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(0, {"status": "uploaded"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "deploy_publish_zip",
                return_value=(0, {"status": "exited", "stdout": '{"ExecutableExists":true}'}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_app_launch_smoke",
                return_value=(0, {"status": "ok", "new_crash_log_detected": False}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["recovery_action"], "none")

    def test_main_returns_error_when_upload_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(1, {"status": "error"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-publish-upload-failed")
        self.assertEqual(payload["recovery_action"], "rerun-guest-app-deploy-smoke")
        self.assertEqual(payload["transport_blocker"], "qga-file-upload")

    def test_main_returns_error_when_publish_zip_is_missing(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            missing_zip = Path(temp_root) / "missing.zip"
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(missing_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "publish-zip-missing")
        self.assertEqual(payload["recovery_action"], "inspect-local-publish-zip")
        self.assertEqual(payload["transport_blocker"], "local-input")

    def test_main_returns_error_when_deploy_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(0, {"status": "uploaded"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "deploy_publish_zip",
                return_value=(1, {"status": "error"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-publish-deploy-failed")
        self.assertEqual(payload["recovery_action"], "inspect-guest-deploy")
        self.assertEqual(payload["transport_blocker"], "guest-publish-deploy")

    def test_main_returns_error_when_deploy_payload_is_error_even_with_zero_exit(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(0, {"status": "uploaded"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "deploy_publish_zip",
                return_value=(0, {"status": "error", "stdout_parse_error": "invalid json"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-publish-deploy-failed")
        self.assertEqual(payload["recovery_action"], "inspect-guest-deploy")
        self.assertEqual(payload["transport_blocker"], "guest-publish-deploy")

    def test_main_returns_error_when_smoke_fails(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(0, {"status": "uploaded"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "deploy_publish_zip",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_app_launch_smoke",
                return_value=(
                    1,
                    {
                        "status": "error",
                        "error_kind": "app-startup-crash",
                        "error": "A new crash log was written during the launch smoke window.",
                        "recovery_action": "inspect-app-crash-logs",
                        "transport_blocker": "app-startup-crash",
                    },
                ),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-startup-crash")
        self.assertEqual(payload["recovery_action"], "inspect-app-crash-logs")
        self.assertEqual(payload["transport_blocker"], "app-startup-crash")

    def test_main_returns_error_when_smoke_payload_is_error_even_with_zero_exit(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            publish_zip = Path(temp_root) / "publish.zip"
            publish_zip.write_bytes(b"zip")
            argv = ["run-guest-app-deploy-smoke.py", "--publish-zip", str(publish_zip)]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                app_deploy_smoke,
                "prepare_guest_paths",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_qga_put_file",
                return_value=(0, {"status": "uploaded"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "deploy_publish_zip",
                return_value=(0, {"status": "exited"}),
            ), mock.patch.object(
                app_deploy_smoke,
                "run_app_launch_smoke",
                return_value=(0, {"status": "error", "stdout_parse_error": "invalid json"}),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = app_deploy_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-app-smoke-failed")
        self.assertEqual(payload["recovery_action"], "inspect-app-launch")
        self.assertEqual(payload["transport_blocker"], "guest-app-launch")
