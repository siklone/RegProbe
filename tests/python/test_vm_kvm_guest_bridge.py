from __future__ import annotations

import importlib.util
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


guest_bridge = load_module("guest_bridge_for_tests", VM_KVM_SCRIPTS / "guest_bridge.py")


class VmKvmGuestBridgeTests(unittest.TestCase):
    def test_ensure_guest_bridge_reuses_healthy_bridge_without_launching(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root, mock.patch.object(
            guest_bridge,
            "bridge_is_healthy",
            return_value=True,
        ), mock.patch.object(guest_bridge.subprocess, "Popen") as popen:
            result = guest_bridge.ensure_guest_bridge(
                repo_root=Path(temp_root),
                bridge_base_url="http://10.0.2.2:8766",
                upload_root=Path(temp_root) / "uploads",
            )

        self.assertTrue(result["already_healthy"])
        self.assertTrue(result["ready"])
        self.assertFalse(result["launched"])
        self.assertEqual("", result["error"])
        popen.assert_not_called()

    def test_ensure_guest_bridge_reports_launch_failure_without_raising(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root, mock.patch.object(
            guest_bridge,
            "bridge_is_healthy",
            return_value=False,
        ), mock.patch.object(
            guest_bridge.subprocess,
            "Popen",
            side_effect=OSError("python launch failed"),
        ):
            result = guest_bridge.ensure_guest_bridge(
                repo_root=Path(temp_root),
                bridge_base_url="http://10.0.2.2:8766",
                upload_root=Path(temp_root) / "uploads",
            )

        self.assertFalse(result["ready"])
        self.assertFalse(result["launched"])
        self.assertEqual("bridge-launch-error", result["error_kind"])
        self.assertIn("python launch failed", result["error"])
        self.assertIn("serve-guest-bridge-8766.log", result["log_path"])


if __name__ == "__main__":
    unittest.main()
