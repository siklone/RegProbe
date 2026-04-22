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


app_launch_smoke = load_module(
    "run_guest_app_launch_smoke_for_tests",
    VM_KVM_SCRIPTS / "run-guest-app-launch-smoke.py",
)


class VmKvmAppLaunchSmokeTests(unittest.TestCase):
    def test_run_qga_exec_uses_equals_form_for_dash_prefixed_args(self) -> None:
        completed = mock.Mock(returncode=0, stdout='{"status":"exited"}', stderr="")
        with mock.patch.object(app_launch_smoke.subprocess, "run", return_value=completed) as run_mock:
            exit_code, payload = app_launch_smoke.run_qga_exec(
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
        with mock.patch.object(app_launch_smoke.subprocess, "run", return_value=completed):
            exit_code, payload = app_launch_smoke.run_qga_exec(
                REPO_ROOT,
                path="powershell.exe",
                args=["-NoProfile"],
            )

        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertIn("stdout_parse_error", payload)
        self.assertEqual(payload["stderr"], "bad-json")

    def test_main_records_launch_stdout_parse_error(self) -> None:
        argv = ["run-guest-app-launch-smoke.py", "--linger-seconds", "0"]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_launch_smoke,
            "latest_crash_log",
            side_effect=[None, None],
        ), mock.patch.object(
            app_launch_smoke,
            "stop_regprobe_app",
            return_value=None,
        ), mock.patch.object(
            app_launch_smoke,
            "launch_app_process",
            return_value=(0, {"stdout": "{bad-json"}),
        ), mock.patch.object(
            app_launch_smoke,
            "current_process",
            return_value={"ProcessName": "RegProbe.App", "Id": 30068, "SessionId": 0},
        ), mock.patch.object(
            app_launch_smoke.time,
            "sleep",
            return_value=None,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_launch_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertIn("stdout_parse_error", payload["launch_payload"])

    def test_crash_log_changed_detects_new_entry(self) -> None:
        before = {"Name": "crash_a.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"}
        after = {"Name": "crash_b.json", "LastWriteTimeUtc": "2026-04-19T17:05:00Z"}
        self.assertTrue(app_launch_smoke.crash_log_changed(before, after))

    def test_crash_log_changed_ignores_same_entry(self) -> None:
        before = {"Name": "crash_a.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"}
        after = {"Name": "crash_a.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"}
        self.assertFalse(app_launch_smoke.crash_log_changed(before, after))

    def test_main_returns_ok_when_process_survives_without_new_crash(self) -> None:
        argv = ["run-guest-app-launch-smoke.py", "--linger-seconds", "0"]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_launch_smoke,
            "latest_crash_log",
            side_effect=[
                {"Name": "crash_old.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"},
                {"Name": "crash_old.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"},
            ],
        ), mock.patch.object(
            app_launch_smoke,
            "stop_regprobe_app",
            return_value=None,
        ), mock.patch.object(
            app_launch_smoke,
            "run_qga_exec",
            return_value=(124, {"status": "timeout"}),
        ), mock.patch.object(
            app_launch_smoke,
            "current_process",
            return_value={"ProcessName": "RegProbe.App", "Id": 30068, "SessionId": 0},
        ), mock.patch.object(
            app_launch_smoke.time,
            "sleep",
            return_value=None,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_launch_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertFalse(payload["new_crash_log_detected"])
        self.assertEqual(payload["recovery_action"], "none")

    def test_main_returns_error_when_new_crash_log_appears(self) -> None:
        argv = ["run-guest-app-launch-smoke.py", "--linger-seconds", "0"]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_launch_smoke,
            "latest_crash_log",
            side_effect=[
                {"Name": "crash_old.json", "LastWriteTimeUtc": "2026-04-19T17:00:00Z"},
                {"Name": "crash_new.json", "LastWriteTimeUtc": "2026-04-19T17:05:00Z"},
            ],
        ), mock.patch.object(
            app_launch_smoke,
            "stop_regprobe_app",
            return_value=None,
        ), mock.patch.object(
            app_launch_smoke,
            "run_qga_exec",
            return_value=(124, {"status": "timeout"}),
        ), mock.patch.object(
            app_launch_smoke,
            "current_process",
            return_value={"ProcessName": "RegProbe.App", "Id": 30068, "SessionId": 0},
        ), mock.patch.object(
            app_launch_smoke.time,
            "sleep",
            return_value=None,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_launch_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-startup-crash")
        self.assertEqual(payload["recovery_action"], "inspect-app-crash-logs")
        self.assertEqual(payload["transport_blocker"], "app-startup-crash")

    def test_main_returns_error_when_process_is_missing(self) -> None:
        argv = ["run-guest-app-launch-smoke.py", "--linger-seconds", "0"]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            app_launch_smoke,
            "latest_crash_log",
            side_effect=[None, None],
        ), mock.patch.object(
            app_launch_smoke,
            "stop_regprobe_app",
            return_value=None,
        ), mock.patch.object(
            app_launch_smoke,
            "run_qga_exec",
            return_value=(1, {"status": "exited", "exitcode": 1}),
        ), mock.patch.object(
            app_launch_smoke,
            "current_process",
            return_value=None,
        ), mock.patch.object(
            app_launch_smoke.time,
            "sleep",
            return_value=None,
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = app_launch_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "app-launch-failed")
        self.assertEqual(payload["recovery_action"], "inspect-app-launch")
        self.assertEqual(payload["transport_blocker"], "guest-app-launch")


if __name__ == "__main__":
    unittest.main()
