from __future__ import annotations

import importlib.util
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock
from itertools import count


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


qga_exec = load_module("qga_exec_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-exec.py")
qga_run_powershell = load_module("qga_run_powershell_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-run-powershell.py")
ensure_guest_admin_shell = load_module("ensure_guest_admin_shell_for_timeout_contract_tests", VM_KVM_SCRIPTS / "ensure-guest-admin-shell.py")
qga_get_file = load_module("qga_get_file_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-get-file.py")
qga_put_file = load_module("qga_put_file_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-put-file.py")


class VmKvmQgaTimeoutContractTests(unittest.TestCase):
    def test_ensure_guest_admin_shell_host_failure_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "ensure-guest-admin-shell.py",
                "--repo-root",
                str(REPO_ROOT),
                "--upload-dir",
                str(Path(temp_root)),
                "--marker-name",
                "launch-error-marker",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ensure_guest_admin_shell,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ensure_guest_admin_shell,
                "send_key",
                side_effect=RuntimeError("send-key failed"),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ensure_guest_admin_shell.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-admin-shell-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-admin-shell-recovery")
        self.assertEqual(payload["transport_blocker"], "host-launch-error")
        self.assertEqual(payload["summary_source"], "guest-admin-shell-launch-error")
        self.assertEqual(payload["exception_type"], "RuntimeError")

    def test_ensure_guest_admin_shell_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            argv = [
                "ensure-guest-admin-shell.py",
                "--repo-root",
                str(REPO_ROOT),
                "--upload-dir",
                str(Path(temp_root)),
                "--timeout-seconds",
                "0",
                "--marker-name",
                "timeout-marker",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ensure_guest_admin_shell,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(ensure_guest_admin_shell, "send_key", return_value=None), mock.patch.object(
                ensure_guest_admin_shell,
                "type_text",
                return_value=None,
            ), mock.patch.object(ensure_guest_admin_shell.time, "sleep", return_value=None), mock.patch.object(
                ensure_guest_admin_shell.time,
                "time",
                side_effect=(float(value) for value in count()),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ensure_guest_admin_shell.main()

            payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-admin-shell-recovery")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_qga_exec_main_timeout_uses_contract_fields(self) -> None:
        argv = [
            "qga-exec.py",
            "--path",
            "powershell.exe",
            "--wait-timeout",
            "0",
        ]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            qga_exec,
            "run_agent_command",
            side_effect=[{"pid": 123}],
        ), mock.patch.object(qga_exec.time, "sleep", return_value=None), mock.patch.object(
            qga_exec.time,
            "time",
            side_effect=[0.0, 1.0],
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = qga_exec.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 124)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-exec-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-qga-exec")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["summary_source"], "qga-exec-timeout")

    def test_qga_exec_main_launch_error_uses_contract_fields(self) -> None:
        argv = [
            "qga-exec.py",
            "--path",
            "powershell.exe",
            "--arg=-NoProfile",
        ]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            qga_exec,
            "run_agent_command",
            side_effect=RuntimeError("qga unavailable"),
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = qga_exec.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-exec-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-qga-exec")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "qga-exec-launch-error")
        self.assertEqual(payload["exception_type"], "RuntimeError")

    def test_qga_exec_main_status_error_uses_contract_fields(self) -> None:
        argv = [
            "qga-exec.py",
            "--path",
            "powershell.exe",
        ]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            qga_exec,
            "run_agent_command",
            side_effect=[{"pid": 123}, RuntimeError("qga status unavailable")],
        ), mock.patch.object(qga_exec.time, "time", side_effect=[0.0, 1.0]), mock.patch(
            "sys.stdout",
            new_callable=io.StringIO,
        ) as stdout:
            exit_code = qga_exec.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-exec-status-error")
        self.assertEqual(payload["recovery_action"], "rerun-qga-exec")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "qga-exec-status-error")
        self.assertEqual(payload["exception_type"], "RuntimeError")

    def test_qga_run_powershell_wait_guest_exec_timeout_uses_contract_fields(self) -> None:
        with mock.patch.object(
            qga_run_powershell,
            "run_agent_command",
            return_value={"exited": False},
        ), mock.patch.object(qga_run_powershell.time, "sleep", return_value=None), mock.patch.object(
            qga_run_powershell.time,
            "time",
            side_effect=[0.0, 1.0],
        ):
            payload = qga_run_powershell.wait_guest_exec(
                "vm",
                123,
                "powershell.exe",
                ["-NoProfile"],
                connect="",
                timeout=10,
                wait_timeout=0,
                poll_interval=0.1,
            )

        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-exec-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-qga-powershell")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["summary_source"], "qga-guest-exec-timeout")

    def test_qga_run_powershell_main_ensure_guest_dir_failure_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            script_path = Path(temp_root) / "guest-script.ps1"
            script_path.write_text("Write-Host 'hello'\n", encoding="utf-8")
            argv = [
                "qga-run-powershell.py",
                "--script",
                str(script_path),
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                qga_run_powershell,
                "ensure_guest_directory",
                return_value={"status": "exited", "exitcode": 1},
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_run_powershell.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "guest-dir-ensure-failed")
        self.assertEqual(payload["recovery_action"], "rerun-qga-powershell")
        self.assertEqual(payload["transport_blocker"], "guest-dir-ensure")
        self.assertEqual(payload["summary_source"], "qga-ensure-guest-dir-error")

    def test_qga_run_powershell_main_missing_host_script_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            script_path = Path(temp_root) / "missing-script.ps1"
            argv = [
                "qga-run-powershell.py",
                "--script",
                str(script_path),
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_run_powershell.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-powershell-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-qga-powershell")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["summary_source"], "qga-powershell-launch-error")
        self.assertEqual(payload["exception_type"], "FileNotFoundError")

    def test_qga_get_file_main_error_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            destination = Path(temp_root) / "download.bin"
            argv = [
                "qga-get-file.py",
                "--source",
                r"C:\\Windows\\Temp\\proof.bin",
                "--destination",
                str(destination),
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                qga_get_file,
                "run_agent_command",
                side_effect=RuntimeError("guest-file-open failed"),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_get_file.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-file-download-error")
        self.assertEqual(payload["recovery_action"], "rerun-qga-get-file")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["summary_source"], "qga-file-download-error")
        self.assertEqual(payload["stage"], "open")
        self.assertEqual(payload["exception_type"], "RuntimeError")

    def test_qga_get_file_main_read_error_reports_stage(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            destination = Path(temp_root) / "download.bin"
            argv = [
                "qga-get-file.py",
                "--source",
                r"C:\\Windows\\Temp\\proof.bin",
                "--destination",
                str(destination),
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                qga_get_file,
                "run_agent_command",
                side_effect=[123, RuntimeError("guest-file-read failed"), {}],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_get_file.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-file-download-error")
        self.assertEqual(payload["stage"], "read")
        self.assertEqual(payload["exception_type"], "RuntimeError")

    def test_qga_put_file_main_error_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            missing_source = Path(temp_root) / "missing.bin"
            argv = [
                "qga-put-file.py",
                "--source",
                str(missing_source),
                "--destination",
                r"C:\\Windows\\Temp\\proof.bin",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_put_file.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-file-upload-error")
        self.assertEqual(payload["recovery_action"], "rerun-qga-put-file")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["summary_source"], "qga-file-upload-error")
        self.assertEqual(payload["stage"], "source")
        self.assertEqual(payload["exception_type"], "FileNotFoundError")

    def test_qga_put_file_main_write_error_reports_stage(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            source = Path(temp_root) / "upload.bin"
            source.write_bytes(b"proof")
            argv = [
                "qga-put-file.py",
                "--source",
                str(source),
                "--destination",
                r"C:\\Windows\\Temp\\proof.bin",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                qga_put_file,
                "run_agent_command",
                side_effect=[123, RuntimeError("guest-file-write failed"), {}],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = qga_put_file.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "qga-file-upload-error")
        self.assertEqual(payload["stage"], "write")
        self.assertEqual(payload["exception_type"], "RuntimeError")


if __name__ == "__main__":
    unittest.main()
