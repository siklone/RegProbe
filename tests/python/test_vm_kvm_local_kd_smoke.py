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


local_kd_smoke = load_module(
    "run_guest_local_kd_smoke_for_tests",
    VM_KVM_SCRIPTS / "run-guest-local-kd-smoke.py",
)


class VmKvmLocalKdSmokeTests(unittest.TestCase):
    def test_timeout_summary_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-local-kd-smoke.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "local-kd-test",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                local_kd_smoke,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                local_kd_smoke,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                local_kd_smoke.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                local_kd_smoke.time,
                "time",
                side_effect=[0.0, 999.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = local_kd_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-local-kd-smoke")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            upload_dir.mkdir()
            (upload_dir / "local-kd-test-summary.json").write_text("{not-json", encoding="utf-8")
            argv = [
                "run-guest-local-kd-smoke.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "local-kd-test",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                local_kd_smoke,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                local_kd_smoke,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                local_kd_smoke.time,
                "time",
                side_effect=[0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = local_kd_smoke.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "local-kd-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-local-kd-smoke")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)


if __name__ == "__main__":
    unittest.main()
