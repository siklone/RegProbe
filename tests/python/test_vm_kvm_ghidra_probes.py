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


ghidra_string_xref = load_module(
    "run_guest_ghidra_string_xref_for_tests",
    VM_KVM_SCRIPTS / "run-guest-ghidra-string-xref-probe.py",
)
ghidra_symbolized = load_module(
    "run_guest_ghidra_symbolized_for_tests",
    VM_KVM_SCRIPTS / "run-guest-ghidra-symbolized-probe.py",
)


class VmKvmGhidraProbeTests(unittest.TestCase):
    def test_string_xref_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-ghidra-string-xref-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-string-test",
                "--binary-path",
                r"C:\Windows\System32\ntoskrnl.exe",
                "--pattern",
                "AllowSystemRequiredPowerRequests",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                side_effect=[0.0, 1000.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-string-xref-probe")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_symbolized_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-ghidra-symbolized-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-symbolized-test",
                "--binary-path",
                r"C:\Windows\System32\ntoskrnl.exe",
                "--pattern",
                "AllowSystemRequiredPowerRequests",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_symbolized,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_symbolized,
                "run",
                return_value=None,
            ), mock.patch.object(
                ghidra_symbolized.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                ghidra_symbolized.time,
                "time",
                side_effect=[0.0, 1000.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-symbolized-probe")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_string_xref_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            upload_dir.mkdir()
            (upload_dir / "ghidra-string-test-summary.json").write_text("{not-json", encoding="utf-8")
            argv = [
                "run-guest-ghidra-string-xref-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-string-test",
                "--binary-path",
                r"C:\Windows\System32\ntoskrnl.exe",
                "--pattern",
                "AllowSystemRequiredPowerRequests",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                side_effect=[0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-string-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-string-xref-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_symbolized_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "ghidra-symbolized-test-summary.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    upload_dir.mkdir(parents=True, exist_ok=True)
                    summary_path.write_text("{not-json", encoding="utf-8")
                return None

            argv = [
                "run-guest-ghidra-symbolized-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-symbolized-test",
                "--binary-path",
                r"C:\Windows\System32\ntoskrnl.exe",
                "--pattern",
                "AllowSystemRequiredPowerRequests",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_symbolized,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_symbolized,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_symbolized.time,
                "time",
                side_effect=[0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-symbolized-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-symbolized-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)


if __name__ == "__main__":
    unittest.main()
