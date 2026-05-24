from __future__ import annotations

import base64
import importlib.util
import io
import json
import subprocess
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


qga_preflight = load_module("qga_preflight_lib_for_tests", VM_KVM_SCRIPTS / "qga_preflight_lib.py")
vm_health_check = load_module("vm_health_check_for_tests", VM_KVM_SCRIPTS / "vm-health-check.py")


def cp(args: list[str], returncode: int, stdout: str = "", stderr: str = "") -> subprocess.CompletedProcess[str]:
    return subprocess.CompletedProcess(args, returncode, stdout, stderr)


def qga(return_payload: object) -> str:
    return json.dumps({"return": return_payload})


class VmKvmQgaPreflightTests(unittest.TestCase):
    def test_guest_ping_failure_reports_qga_contract(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 1, "", "Guest agent is not responding"),
            ],
        ):
            payload = qga_preflight.run_qga_preflight(domain="vm", connect="qemu:///session")

        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["summary_source"], "qga-preflight")
        self.assertEqual(payload["error_kind"], "qga-preflight-failed")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["recovery_action"], "repair-qga-or-run-vm-health-check")
        self.assertIn("guest_ping", payload["failed_checks"])
        self.assertEqual(payload["checks"]["guest_info"]["status"], "skipped")

    def test_guest_exec_failure_reports_decoded_output(self) -> None:
        encoded_error = base64.b64encode(b"access denied").decode("ascii")
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 1, "err-data": encoded_error})),
            ],
        ), mock.patch.object(qga_preflight.time, "time", return_value=0.0):
            payload = qga_preflight.run_qga_preflight(domain="vm", connect="qemu:///session")

        self.assertEqual(payload["status"], "error")
        self.assertIn("guest_exec", payload["failed_checks"])
        self.assertEqual(payload["checks"]["guest_exec"]["exitcode"], 1)
        self.assertEqual(payload["checks"]["guest_exec"]["err_data"], "access denied")

    def test_guest_exec_status_timeout_reports_preflight_timeout(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": False})),
            ],
        ), mock.patch.object(
            qga_preflight.time,
            "time",
            side_effect=[0.0, 0.0, 2.0],
        ), mock.patch.object(qga_preflight.time, "sleep", return_value=None):
            payload = qga_preflight.run_qga_preflight(
                domain="vm",
                connect="qemu:///session",
                wait_timeout=1,
            )

        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["checks"]["guest_exec"]["status"], "timeout")
        self.assertEqual(payload["checks"]["guest_exec"]["phase"], "guest-exec-status")

    def test_malformed_qga_json_is_a_preflight_failure(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, "not-json"),
            ],
        ):
            payload = qga_preflight.run_qga_preflight(domain="vm", connect="qemu:///session")

        self.assertEqual(payload["status"], "error")
        self.assertIn("valid JSON", payload["checks"]["guest_ping"]["error"])
        self.assertIn("guest_ping", payload["failed_checks"])

    def test_vm_shutoff_fails_without_mutating_guest(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            return_value=cp(["virsh"], 0, "shut off\n"),
        ) as run_mock:
            payload = qga_preflight.run_qga_preflight(domain="vm", connect="qemu:///session")

        self.assertEqual(run_mock.call_count, 1)
        self.assertEqual(payload["status"], "error")
        self.assertIn("domstate", payload["failed_checks"])
        self.assertEqual(payload["checks"]["guest_ping"]["status"], "skipped")

    def test_snapshot_name_adds_non_mutating_snapshot_check(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0, "out-data": base64.b64encode(b"nt authority\\system").decode("ascii")})),
                cp(["virsh"], 0, "Name:           clean-25h2-qga\nState:          running\n"),
            ],
        ), mock.patch.object(qga_preflight.time, "time", return_value=0.0):
            payload = qga_preflight.run_qga_preflight(
                domain="vm",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
            )

        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["checks"]["snapshot"]["status"], "ok")
        self.assertTrue(payload["checks"]["snapshot"]["exists"])
        self.assertEqual(payload["checks"]["snapshot"]["snapshot_name"], "clean-25h2-qga")
        self.assertEqual(payload["checks"]["snapshot"]["info"]["name"], "clean-25h2-qga")

    def test_guest_dotnet_toolchain_check_passes_when_dotnet_and_desktop_runtime_exist(self) -> None:
        toolchain_json = json.dumps(
            {
                "configured_dotnet_path": r"C:\Tools\DotNetSDK\8.0.416\dotnet.exe",
                "configured_dotnet_path_exists": True,
                "dotnet_on_path": False,
                "dotnet_path": r"C:\Tools\DotNetSDK\8.0.416\dotnet.exe",
                "desktop_runtime_present": True,
                "desktop_runtime_versions": ["8.0.0"],
            }
        ).encode("utf-8")
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0, "out-data": base64.b64encode(b"nt authority\\system").decode("ascii")})),
                cp(["virsh"], 0, qga({"pid": 77})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0, "out-data": base64.b64encode(toolchain_json).decode("ascii")})),
            ],
        ), mock.patch.object(qga_preflight.time, "time", return_value=0.0):
            payload = qga_preflight.run_qga_preflight(
                domain="vm",
                connect="qemu:///session",
                check_guest_dotnet=True,
            )

        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["checks"]["guest_dotnet_toolchain"]["status"], "ok")
        self.assertTrue(payload["checks"]["guest_dotnet_toolchain"]["desktop_runtime_present"])
        self.assertEqual(payload["checks"]["guest_dotnet_toolchain"]["desktop_runtime_versions"], ["8.0.0"])

    def test_guest_dotnet_toolchain_check_fails_when_desktop_runtime_is_missing(self) -> None:
        toolchain_json = json.dumps(
            {
                "configured_dotnet_path": r"C:\Tools\DotNetSDK\8.0.416\dotnet.exe",
                "configured_dotnet_path_exists": True,
                "dotnet_on_path": False,
                "dotnet_path": r"C:\Tools\DotNetSDK\8.0.416\dotnet.exe",
                "desktop_runtime_present": False,
                "desktop_runtime_versions": [],
            }
        ).encode("utf-8")
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0, "out-data": base64.b64encode(b"nt authority\\system").decode("ascii")})),
                cp(["virsh"], 0, qga({"pid": 77})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0, "out-data": base64.b64encode(toolchain_json).decode("ascii")})),
            ],
        ), mock.patch.object(qga_preflight.time, "time", return_value=0.0):
            payload = qga_preflight.run_qga_preflight(
                domain="vm",
                connect="qemu:///session",
                check_guest_dotnet=True,
            )

        self.assertEqual(payload["status"], "error")
        self.assertIn("guest_dotnet_toolchain", payload["failed_checks"])
        self.assertEqual(payload["checks"]["guest_dotnet_toolchain"]["status"], "error")
        self.assertIn("Microsoft.WindowsDesktop.App", payload["checks"]["guest_dotnet_toolchain"]["error"])

    def test_missing_snapshot_fails_health_contract_when_requested(self) -> None:
        with mock.patch.object(
            qga_preflight.subprocess,
            "run",
            side_effect=[
                cp(["virsh"], 0, "running\n"),
                cp(["virsh"], 0, qga({})),
                cp(["virsh"], 0, qga({"version": "qga-test"})),
                cp(["virsh"], 0, qga({"pid": 42})),
                cp(["virsh"], 0, qga({"exited": True, "exitcode": 0})),
                cp(["virsh"], 1, "", "Domain snapshot not found"),
            ],
        ), mock.patch.object(qga_preflight.time, "time", return_value=0.0):
            payload = qga_preflight.run_qga_preflight(
                domain="vm",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
            )

        self.assertEqual(payload["status"], "error")
        self.assertIn("snapshot", payload["failed_checks"])
        self.assertFalse(payload["checks"]["snapshot"]["exists"])

    def test_vm_health_check_cli_prints_json_contract(self) -> None:
        with mock.patch.object(
            sys,
            "argv",
            ["vm-health-check.py", "--domain", "vm", "--connect", "qemu:///session", "--snapshot-name", "clean", "--json"],
        ), mock.patch.object(
            vm_health_check,
            "run_qga_preflight",
            return_value={"status": "ok", "summary_source": "qga-preflight", "checks": {}},
        ) as preflight_mock, mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = vm_health_check.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["summary_source"], "qga-preflight")
        self.assertEqual(preflight_mock.call_args.kwargs["snapshot_name"], "clean")
        self.assertFalse(preflight_mock.call_args.kwargs["check_guest_dotnet"])

    def test_vm_health_check_cli_forwards_guest_dotnet_options(self) -> None:
        with mock.patch.object(
            sys,
            "argv",
            [
                "vm-health-check.py",
                "--domain",
                "vm",
                "--connect",
                "qemu:///session",
                "--snapshot-name",
                "clean",
                "--check-guest-dotnet",
                "--guest-dotnet-path",
                r"C:\DotNet\dotnet.exe",
                "--json",
            ],
        ), mock.patch.object(
            vm_health_check,
            "run_qga_preflight",
            return_value={"status": "ok", "summary_source": "qga-preflight", "checks": {}},
        ) as preflight_mock, mock.patch("sys.stdout", new_callable=io.StringIO):
            exit_code = vm_health_check.main()

        self.assertEqual(exit_code, 0)
        self.assertTrue(preflight_mock.call_args.kwargs["check_guest_dotnet"])
        self.assertEqual(preflight_mock.call_args.kwargs["guest_dotnet_path"], r"C:\DotNet\dotnet.exe")


if __name__ == "__main__":
    unittest.main()
