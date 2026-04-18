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


qga_exec = load_module("qga_exec_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-exec.py")
qga_run_powershell = load_module("qga_run_powershell_for_timeout_contract_tests", VM_KVM_SCRIPTS / "qga-run-powershell.py")


class VmKvmQgaTimeoutContractTests(unittest.TestCase):
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


if __name__ == "__main__":
    unittest.main()
