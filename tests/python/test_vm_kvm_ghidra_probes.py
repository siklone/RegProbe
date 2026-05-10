from __future__ import annotations

import importlib.util
import io
import json
import subprocess
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
                "--launch-transport",
                "send-key",
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
                "--launch-transport",
                "send-key",
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
            summary_path = upload_dir / "ghidra-string-test-summary.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    summary_path.parent.mkdir(parents=True, exist_ok=True)
                    summary_path.write_text("{not-json", encoding="utf-8")
                return None

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
                "--launch-transport",
                "send-key",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-string-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-string-xref-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_string_xref_non_object_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "ghidra-string-test-summary.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    summary_path.parent.mkdir(parents=True, exist_ok=True)
                    summary_path.write_text('["not","object"]', encoding="utf-8")
                return None

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
                "--launch-transport",
                "send-key",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "ghidra-string-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_string_xref_launch_failure_reports_contract_error(self) -> None:
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
                "--launch-transport",
                "send-key",
            ]
            failure = subprocess.CalledProcessError(5, ["type-to-guest.py"], output="typed", stderr="focus-lost")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=[None, ghidra_string_xref.annotate_process_error(failure, stage="type-to-guest")],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-string-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-string-xref-probe")
        self.assertEqual(payload["transport_blocker"], "launch-failed")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-launch-failure")
        self.assertEqual(payload["host_step"], "type-to-guest")
        self.assertEqual(payload["exit_code"], 5)

    def test_string_xref_invalid_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "ghidra-string-test-launcher-stage.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    stage_path.parent.mkdir(parents=True, exist_ok=True)
                    stage_path.write_text("{not-json", encoding="utf-8")
                return None

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
                "--launch-transport",
                "send-key",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-string-stage-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-string-xref-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "launcher-stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_string_xref_starting_stage_fails_fast_with_launcher_stall(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "ghidra-string-test-launcher-stage.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    stage_path.parent.mkdir(parents=True, exist_ok=True)
                    stage_path.write_text(
                        json.dumps(
                            {
                                "generated_utc": "1970-01-01T00:00:00Z",
                                "stage": "invoke-wrapper",
                                "status": "starting",
                            }
                        ),
                        encoding="utf-8",
                    )
                return None

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
                "--launch-transport",
                "send-key",
                "--launcher-stall-seconds",
                "60",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=200.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-launcher-stall")
        self.assertEqual(payload["recovery_action"], "inspect-launcher-stage")
        self.assertEqual(payload["transport_blocker"], "launcher-stall")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "launcher-stage-timeout")
        self.assertEqual(payload["launcher_stage"]["stage"], "invoke-wrapper")

    def test_string_xref_non_object_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "ghidra-string-test-launcher-stage.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    stage_path.parent.mkdir(parents=True, exist_ok=True)
                    stage_path.write_text('["not","object"]', encoding="utf-8")
                return None

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
                "--launch-transport",
                "send-key",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_string_xref,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "ghidra-string-stage-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

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
                "--launch-transport",
                "send-key",
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
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-symbolized-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-symbolized-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_symbolized_launch_failure_reports_contract_error(self) -> None:
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
                "--launch-transport",
                "send-key",
            ]
            failure = subprocess.CalledProcessError(4, ["ensure-guest-admin-shell.py"], output="stdout", stderr="stderr")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                ghidra_symbolized,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                ghidra_symbolized,
                "run",
                side_effect=ghidra_symbolized.annotate_process_error(failure, stage="ensure-admin-shell"),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-symbolized-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-symbolized-probe")
        self.assertEqual(payload["transport_blocker"], "launch-failed")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-launch-failure")
        self.assertEqual(payload["host_step"], "ensure-admin-shell")
        self.assertEqual(payload["exit_code"], 4)

    def test_symbolized_invalid_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "ghidra-symbolized-test-launcher-stage.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    stage_path.parent.mkdir(parents=True, exist_ok=True)
                    stage_path.write_text("{not-json", encoding="utf-8")
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
                "--launch-transport",
                "send-key",
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
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ghidra-symbolized-stage-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-ghidra-symbolized-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "launcher-stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_symbolized_starting_stage_fails_fast_with_launcher_stall(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "ghidra-symbolized-test-launcher-stage.json"

            def fake_run(cmd, cwd):  # noqa: ANN001
                if any("type-to-guest.py" in str(part) for part in cmd):
                    stage_path.parent.mkdir(parents=True, exist_ok=True)
                    stage_path.write_text(
                        json.dumps(
                            {
                                "generated_utc": "1970-01-01T00:00:00Z",
                                "stage": "invoke-wrapper",
                                "status": "starting",
                            }
                        ),
                        encoding="utf-8",
                    )
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
                "--launch-transport",
                "send-key",
                "--launcher-stall-seconds",
                "60",
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
                return_value=200.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-launcher-stall")
        self.assertEqual(payload["recovery_action"], "inspect-launcher-stage")
        self.assertEqual(payload["transport_blocker"], "launcher-stall")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "launcher-stage-timeout")
        self.assertEqual(payload["launcher_stage"]["stage"], "invoke-wrapper")

    def test_string_xref_auto_launch_uses_qga_after_preflight(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "ghidra-string-qga-summary.json"

            def fake_qga_run(cmd, **kwargs):  # noqa: ANN001
                self.assertTrue(any("qga-run-powershell.py" in str(part) for part in cmd))
                upload_dir.mkdir(parents=True, exist_ok=True)
                summary_path.write_text('{"status":"ok","ghidra_exit_code":0}\n', encoding="utf-8")
                return subprocess.CompletedProcess(cmd, 0, "", "")

            argv = [
                "run-guest-ghidra-string-xref-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-string-qga",
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
                "require_qga_preflight",
                return_value={"status": "ok", "summary_source": "qga-preflight"},
            ), mock.patch.object(
                ghidra_string_xref.subprocess,
                "run",
                side_effect=fake_qga_run,
            ), mock.patch.object(
                ghidra_string_xref,
                "run",
            ) as send_key_run, mock.patch.object(
                ghidra_string_xref.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_string_xref.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["launch_transport"], "qga")
        self.assertEqual(payload["error_kind"], None)
        self.assertEqual(payload["recovery_action"], "none")
        self.assertEqual(payload["transport_blocker"], "none")
        send_key_run.assert_not_called()

    def test_symbolized_auto_launch_uses_qga_after_preflight(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "ghidra-symbolized-qga-summary.json"

            def fake_qga_run(cmd, **kwargs):  # noqa: ANN001
                self.assertTrue(any("qga-run-powershell.py" in str(part) for part in cmd))
                upload_dir.mkdir(parents=True, exist_ok=True)
                summary_path.write_text('{"status":"ok","ghidra_exit_code":0}\n', encoding="utf-8")
                return subprocess.CompletedProcess(cmd, 0, "", "")

            argv = [
                "run-guest-ghidra-symbolized-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "ghidra-symbolized-qga",
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
                "require_qga_preflight",
                return_value={"status": "ok", "summary_source": "qga-preflight"},
            ), mock.patch.object(
                ghidra_symbolized.subprocess,
                "run",
                side_effect=fake_qga_run,
            ), mock.patch.object(
                ghidra_symbolized,
                "run",
            ) as send_key_run, mock.patch.object(
                ghidra_symbolized.time,
                "time",
                return_value=0.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = ghidra_symbolized.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["launch_transport"], "qga")
        self.assertEqual(payload["error_kind"], None)
        self.assertEqual(payload["recovery_action"], "none")
        self.assertEqual(payload["transport_blocker"], "none")
        send_key_run.assert_not_called()


if __name__ == "__main__":
    unittest.main()
