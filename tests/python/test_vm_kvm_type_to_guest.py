from __future__ import annotations

import importlib.util
import io
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


type_to_guest = load_module("type_to_guest_for_tests", VM_KVM_SCRIPTS / "type-to-guest.py")


class VmKvmTypeToGuestTests(unittest.TestCase):
    def test_main_reports_unsupported_character_without_traceback(self) -> None:
        argv = ["type-to-guest.py", "vm", "snowman: " + chr(0x2603)]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            type_to_guest.subprocess,
            "run",
            return_value=subprocess.CompletedProcess(["virsh"], 0),
        ), mock.patch("sys.stderr", new_callable=io.StringIO) as stderr:
            exit_code = type_to_guest.main()

        self.assertEqual(2, exit_code)
        self.assertIn("unsupported character", stderr.getvalue())

    def test_main_reports_virsh_send_key_failure_without_traceback(self) -> None:
        argv = ["type-to-guest.py", "vm", "A", "--enter"]
        with mock.patch.object(sys, "argv", argv), mock.patch.object(
            type_to_guest.subprocess,
            "run",
            side_effect=subprocess.CalledProcessError(7, ["virsh"]),
        ), mock.patch("sys.stderr", new_callable=io.StringIO) as stderr:
            exit_code = type_to_guest.main()

        self.assertEqual(7, exit_code)
        self.assertIn("virsh send-key failed with exit code 7", stderr.getvalue())


if __name__ == "__main__":
    unittest.main()
