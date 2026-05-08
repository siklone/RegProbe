from __future__ import annotations

import importlib.util
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


registry_value_experiment = load_module(
    "run_guest_registry_value_experiment_for_tests",
    VM_KVM_SCRIPTS / "run-guest-registry-value-experiment.py",
)


class VmKvmRegistryValueExperimentTests(unittest.TestCase):
    def test_recover_from_snapshot_reverts_starts_and_waits_for_qga(self) -> None:
        calls: list[list[str]] = []

        def fake_run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
            calls.append(cmd)
            return subprocess.CompletedProcess(cmd, 0, stdout=f"ok:{cmd[-1]}", stderr="")

        with mock.patch.object(registry_value_experiment, "run", side_effect=fake_run), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
            return_value={"status": "ok", "guest_health": "stable"},
        ) as wait_for_qga:
            recovery = registry_value_experiment.recover_from_snapshot(
                domain="regprobe-win11-25h2-session",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
                wait_timeout=600,
            )

        self.assertEqual(recovery["status"], "ok")
        self.assertEqual([step["action"] for step in recovery["steps"]], ["destroy-runtime", "snapshot-revert", "start-domain"])
        self.assertEqual(calls[0], ["virsh", "-c", "qemu:///session", "destroy", "regprobe-win11-25h2-session"])
        self.assertEqual(
            calls[1],
            [
                "virsh",
                "-c",
                "qemu:///session",
                "snapshot-revert",
                "regprobe-win11-25h2-session",
                "clean-25h2-qga",
                "--force",
            ],
        )
        self.assertEqual(calls[2], ["virsh", "-c", "qemu:///session", "start", "regprobe-win11-25h2-session"])
        wait_for_qga.assert_called_once_with("regprobe-win11-25h2-session", "qemu:///session", 600)

    def test_recover_from_snapshot_reports_revert_failure_without_starting(self) -> None:
        calls: list[list[str]] = []

        def fake_run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
            calls.append(cmd)
            if "snapshot-revert" in cmd:
                return subprocess.CompletedProcess(cmd, 1, stdout="", stderr="snapshot failed")
            return subprocess.CompletedProcess(cmd, 0, stdout="", stderr="")

        with mock.patch.object(registry_value_experiment, "run", side_effect=fake_run), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
        ) as wait_for_qga:
            recovery = registry_value_experiment.recover_from_snapshot(
                domain="regprobe-win11-25h2-session",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
                wait_timeout=600,
            )

        self.assertEqual(recovery["status"], "error")
        self.assertEqual(recovery["error"], "snapshot-revert-failed")
        self.assertEqual(len(calls), 2)
        wait_for_qga.assert_not_called()


if __name__ == "__main__":
    unittest.main()
