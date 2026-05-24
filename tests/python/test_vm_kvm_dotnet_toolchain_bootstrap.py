from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


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


bootstrap = load_module(
    "run_guest_dotnet_toolchain_bootstrap_for_tests",
    VM_KVM_SCRIPTS / "run-guest-dotnet-toolchain-bootstrap.py",
)


class VmKvmDotnetToolchainBootstrapTests(unittest.TestCase):
    def test_parse_guest_bootstrap_result_promotes_success_to_pass(self) -> None:
        payload = {
            "execution": {
                "exitcode": 0,
                "stdout": json.dumps(
                    {
                        "success": True,
                        "dotnet_after_exists": True,
                        "core_runtime_after": ["8.0.27"],
                        "desktop_runtime_after": ["8.0.26"],
                    }
                ),
            }
        }

        parsed = bootstrap.parse_guest_bootstrap_result(payload)

        self.assertEqual(parsed["status"], "PASS")
        self.assertEqual(parsed["core_runtime_after"], ["8.0.27"])
        self.assertEqual(parsed["desktop_runtime_after"], ["8.0.26"])

    def test_parse_guest_bootstrap_result_reports_missing_json(self) -> None:
        parsed = bootstrap.parse_guest_bootstrap_result({"execution": {"exitcode": 1, "stdout": ""}})

        self.assertEqual(parsed["status"], "error")
        self.assertEqual(parsed["error_kind"], "missing-guest-bootstrap-json")
        self.assertEqual(parsed["execution_exit"], 1)

    def test_qga_runner_command_targets_portable_dotnet_toolchain(self) -> None:
        command = bootstrap.build_qga_runner_command(
            script_path=Path("/tmp/bootstrap.ps1"),
            domain="vm",
            connect="qemu:///session",
            install_dir=r"C:\Tools\DotNetSDK\8.0.416",
            sdk_version="8.0.416",
            desktop_runtime_channel="8.0",
            desktop_runtime_version="",
            wait_timeout=1800,
        )

        self.assertIn("scripts/vm-kvm/qga-run-powershell.py", " ".join(command))
        self.assertIn("--connect", command)
        self.assertIn("qemu:///session", command)
        self.assertIn(r"C:\Tools\DotNetSDK\8.0.416", command)
        self.assertIn("8.0.416", command)
        self.assertIn("8.0", command)
        self.assertIn("--propagate-exit-code", command)

    def test_generated_powershell_installs_windowsdesktop_runtime(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            script_path = Path(temp_dir) / "bootstrap.ps1"
            bootstrap.write_guest_bootstrap_script(script_path)
            text = script_path.read_text(encoding="utf-8")

        self.assertIn("windowsdesktop", text)
        self.assertIn("Microsoft.NETCore.App", text)
        self.assertIn("Microsoft.WindowsDesktop.App", text)
        self.assertIn("dotnet-install.ps1", text)


if __name__ == "__main__":
    unittest.main()
