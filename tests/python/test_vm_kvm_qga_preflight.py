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

    def test_vm_health_check_cli_prints_json_contract(self) -> None:
        with mock.patch.object(
            sys,
            "argv",
            ["vm-health-check.py", "--domain", "vm", "--connect", "qemu:///session", "--json"],
        ), mock.patch.object(
            vm_health_check,
            "run_qga_preflight",
            return_value={"status": "ok", "summary_source": "qga-preflight", "checks": {}},
        ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
            exit_code = vm_health_check.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["summary_source"], "qga-preflight")


if __name__ == "__main__":
    unittest.main()
