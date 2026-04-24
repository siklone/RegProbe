from __future__ import annotations

import argparse
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


wpr_boot_registry = load_module(
    "run_guest_wpr_boot_registry_for_tests",
    VM_KVM_SCRIPTS / "run-guest-wpr-boot-registry.py",
)
summary_contract = load_module("summary_contract_lib_for_wpr_tests", VM_KVM_SCRIPTS / "summary_contract_lib.py")


class VmKvmWprBootRegistryTests(unittest.TestCase):
    def test_try_qga_download_records_stdout_parse_error_for_non_object_json(self) -> None:
        completed = subprocess.CompletedProcess(["qga-get-file.py"], 1, '["not","object"]', "")
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            host_path = Path(temp_root) / "sample.bin"
            with mock.patch.object(wpr_boot_registry.subprocess, "run", return_value=completed):
                payload = wpr_boot_registry.try_qga_download(
                    repo_root=REPO_ROOT,
                    args=argparse.Namespace(
                        domain="vm",
                        connect="qemu:///session",
                        salvage_qga_timeout_seconds=30,
                    ),
                    guest_path=r"C:\sample.bin",
                    host_path=host_path,
                )

        self.assertEqual(payload["returncode"], 1)
        self.assertEqual(payload["stdout_parse_error"], "stdout JSON payload is not an object")
        self.assertEqual(payload["stdout"], '["not","object"]')

    def test_try_qga_download_records_stdout_parse_error_for_invalid_json(self) -> None:
        completed = subprocess.CompletedProcess(["qga-get-file.py"], 1, "{not-json", "")
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            host_path = Path(temp_root) / "sample.bin"
            with mock.patch.object(wpr_boot_registry.subprocess, "run", return_value=completed):
                payload = wpr_boot_registry.try_qga_download(
                    repo_root=REPO_ROOT,
                    args=argparse.Namespace(
                        domain="vm",
                        connect="qemu:///session",
                        salvage_qga_timeout_seconds=30,
                    ),
                    guest_path=r"C:\sample.bin",
                    host_path=host_path,
        )

        self.assertEqual(payload["returncode"], 1)
        self.assertIn("Expecting property name", payload["stdout_parse_error"])
        self.assertEqual(payload["stdout"], "{not-json")

    def test_describe_downloaded_file_reports_zero_byte_state(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            zero_path = Path(temp_root) / "summary.json"
            zero_path.write_text("", encoding="utf-8")

            described = wpr_boot_registry.describe_downloaded_file(zero_path)

        self.assertTrue(described["exists"])
        self.assertEqual(described["size_bytes"], 0)
        self.assertTrue(described["is_zero_byte"])

    def test_salvage_timeout_artifacts_records_zero_byte_guest_artifacts(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root)
            args = argparse.Namespace(
                salvage_timeout_artifacts=True,
                launch_transport="qga",
                domain="dummy",
                connect="dummy",
                salvage_qga_timeout_seconds=30,
                output_name="sample-run",
                registry_path=r"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
                value_name="GlobalTimerResolutionRequests",
            )

            def fake_download(*, host_path: Path, guest_path: str, **_: object) -> dict[str, object]:
                if host_path.name.endswith("-summary-guest.json"):
                    host_path.write_text("", encoding="utf-8")
                elif host_path.name.endswith(".normalized.json"):
                    host_path.write_text("", encoding="utf-8")
                elif host_path.name.endswith(".hits.csv"):
                    host_path.write_text("Event Name,Type\n", encoding="utf-8")
                elif host_path.name.endswith("-stage-guest.json"):
                    host_path.write_text('{"status":"running"}\n', encoding="utf-8")
                else:
                    host_path.write_text('{"status":"collect-tracerpt"}\n', encoding="utf-8")

                return {
                    "guest_path": guest_path,
                    "host_path": str(host_path),
                    "returncode": 0,
                    **wpr_boot_registry.describe_downloaded_file(host_path),
                }

            with mock.patch.object(wpr_boot_registry, "try_qga_download", side_effect=fake_download):
                payload = wpr_boot_registry.salvage_timeout_artifacts(
                    repo_root=REPO_ROOT,
                    args=args,
                    upload_dir=upload_dir,
                    guest_output_root=r"C:\RegProbe-Diag\wpr-boot-registry\sample-run",
                )

        self.assertTrue(payload["attempted"])
        self.assertTrue(payload["artifact_health"]["guest_summary"]["is_zero_byte"])
        self.assertTrue(payload["artifact_health"]["guest_normalized"]["is_zero_byte"])
        self.assertEqual(payload["hits_csv"]["hit_line_count"], 0)
        self.assertTrue(payload["normalized_salvage"]["created"])
        self.assertEqual(payload["normalized_salvage"]["normalizer_name"], "HostTimeoutSalvageNormalizer")

    def test_summarize_timeout_salvage_exposes_top_level_no_hit_fields(self) -> None:
        payload = wpr_boot_registry.summarize_timeout_salvage(
            {
                "artifact_health": {
                    "guest_summary": {"exists": True, "size_bytes": 0, "is_zero_byte": True},
                    "guest_normalized": {"exists": True, "size_bytes": 0, "is_zero_byte": True},
                    "guest_hits_csv": {"exists": True, "size_bytes": 16, "is_zero_byte": False},
                },
                "hits_csv": {
                    "exists": True,
                    "line_count": 1,
                    "hit_line_count": 0,
                    "contains_value_name": False,
                },
                "normalized_salvage": {
                    "created": True,
                    "path": "/tmp/sample.normalized.json",
                    "event_count": 0,
                    "normalizer_name": "HostTimeoutSalvageNormalizer",
                },
            }
        )

        self.assertEqual(payload["summary_source"], "timeout-salvage")
        self.assertEqual(payload["salvage_classification"], "header-only-no-hit")
        self.assertTrue(payload["guest_summary_zero_byte"])
        self.assertTrue(payload["guest_normalized_zero_byte"])
        self.assertTrue(payload["normalized_bundle_exists"])
        self.assertEqual(payload["normalization_status"], "ok")
        self.assertEqual(payload["normalizer_name"], "HostTimeoutSalvageNormalizer")

    def test_timeout_summary_contract_can_embed_salvage_surface_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            output_path = Path(temp_root) / "summary.json"
            timeout_salvage = {
                "artifact_health": {
                    "guest_summary": {"exists": True, "size_bytes": 0, "is_zero_byte": True},
                    "guest_normalized": {"exists": True, "size_bytes": 0, "is_zero_byte": True},
                    "guest_hits_csv": {"exists": True, "size_bytes": 16, "is_zero_byte": False},
                },
                "hits_csv": {"exists": True, "line_count": 1, "hit_line_count": 0, "contains_value_name": False},
                "normalized_salvage": {
                    "created": True,
                    "path": str(Path(temp_root) / "sample.normalized.json"),
                    "event_count": 0,
                    "normalizer_name": "HostTimeoutSalvageNormalizer",
                },
            }

            payload = summary_contract.write_summary_contract(
                output_path,
                {
                    "status": "timeout",
                    "output_name": "sample-run",
                    "timeout_salvage": timeout_salvage,
                    **wpr_boot_registry.summarize_timeout_salvage(timeout_salvage),
                },
                default_error_kind="runner-timeout",
                default_recovery_action="rerun-wpr-boot-registry",
                default_transport_blocker="timeout",
                default_guest_health="unknown",
            )

            written = json.loads(output_path.read_text(encoding="utf-8"))
        self.assertEqual(written["summary_source"], "timeout-salvage")
        self.assertEqual(written["salvage_classification"], "header-only-no-hit")
        self.assertTrue(written["guest_summary_zero_byte"])
        self.assertTrue(written["guest_normalized_zero_byte"])
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")

    def test_prepare_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                return_value=False,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "prepare-timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "wpr-prepare-timeout")

    def test_launch_failure_reports_contract_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]
            failure = subprocess.CalledProcessError(8, ["qga-run-powershell.py"], output="stdout-text", stderr="stderr-text")
            setattr(failure, "stage", "qga-launch")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=failure,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-host-step-error")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "host-step-error")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-step-failure")
        self.assertEqual(payload["host_step"], "qga-launch")
        self.assertEqual(payload["exit_code"], 8)

    def test_arm_error_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path.name == "boot-registry-test-summary-arm.json":
                    path.write_text(
                        json.dumps({"status": "error", "error": "arm stage failed"}, indent=2) + "\n",
                        encoding="utf-8",
                    )
                    return True
                return False

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-arm-error")
        self.assertEqual(payload["recovery_action"], "inspect-wpr-arm")
        self.assertEqual(payload["transport_blocker"], "arm-stage-error")
        self.assertEqual(payload["summary_source"], "arm-summary")

    def test_invalid_arm_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text("{not-json", encoding="utf-8")
                    return True
                return False

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-arm-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "arm-summary-parse")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_arm_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text('["not","object"]', encoding="utf-8")
                    return True
                return False

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "wpr-arm-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_stage_fallback_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            stage_path = upload_dir / "boot-registry-test-stage.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                stage_path.write_text(
                    json.dumps(
                        {
                            "status": "error",
                            "stage": "collect",
                            "message": "collect stage failed",
                            "generated_utc": "2026-04-18T00:00:00Z",
                        },
                        indent=2,
                    )
                    + "\n",
                    encoding="utf-8",
                )
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-stage-error")
        self.assertEqual(payload["recovery_action"], "inspect-wpr-stage")
        self.assertEqual(payload["transport_blocker"], "stage-error")
        self.assertEqual(payload["summary_source"], "stage-fallback")

    def test_invalid_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            stage_path = upload_dir / "boot-registry-test-stage.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                stage_path.write_text("{not-json", encoding="utf-8")
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-stage-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            stage_path = upload_dir / "boot-registry-test-stage.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                stage_path.write_text('["not","object"]', encoding="utf-8")
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "wpr-stage-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_first_artifact_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--first-artifact-timeout-seconds",
                "60",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 61.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "bridge-artifact-timeout")
        self.assertEqual(payload["recovery_action"], "inspect-bridge-upload")
        self.assertEqual(payload["transport_blocker"], "bridge-artifact-timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "first-artifact-timeout")

    def test_stage_stall_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            stage_path = upload_dir / "boot-registry-test-stage.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--first-artifact-timeout-seconds",
                "60",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                stage_path.write_text(
                    json.dumps(
                        {
                            "generated_utc": "1970-01-01T00:00:00Z",
                            "stage": "collect-tracerpt",
                            "status": "starting",
                            "message": "",
                        },
                        indent=2,
                    )
                    + "\n",
                    encoding="utf-8",
                )
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 200.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-stage-stall")
        self.assertEqual(payload["recovery_action"], "inspect-wpr-stage")
        self.assertEqual(payload["transport_blocker"], "stage-stall")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "stage-timeout")
        self.assertEqual(payload["stage"]["stage"], "collect-tracerpt")

    def test_expect_caller_stack_missing_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            summary_path = upload_dir / "boot-registry-test-summary.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--expect-caller-stack",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                summary_path.write_text(
                    json.dumps(
                        {
                            "status": "ok",
                            "normalization_status": "ok",
                            "caller_stack_event_count": 0,
                            "etl_exists": True,
                            "csv_exists": True,
                            "normalized_bundle_exists": True,
                        },
                        indent=2,
                    )
                    + "\n",
                    encoding="utf-8",
                )
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "caller-stack-missing")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-with-caller-stack")
        self.assertEqual(payload["transport_blocker"], "caller-stack-missing")
        self.assertEqual(payload["summary_source"], "caller-stack-check")

    def test_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            summary_path = upload_dir / "boot-registry-test-summary.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                summary_path.write_text("{not-json", encoding="utf-8")
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_invalid_legacy_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            legacy_summary_path = upload_dir / "wpr-boot-registry-summary.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                legacy_summary_path.write_text("{not-json", encoding="utf-8")
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "wpr-legacy-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-wpr-boot-registry")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["summary_source"], "legacy-summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_legacy_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_arm_path = upload_dir / "boot-registry-test-summary-arm.json"
            legacy_summary_path = upload_dir / "wpr-boot-registry-summary.json"
            argv = [
                "run-guest-wpr-boot-registry.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "boot-registry-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
            ]

            def fake_wait_for_file(path: Path, _timeout: int) -> bool:
                if path == summary_arm_path:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text(json.dumps({"status": "ok"}, indent=2) + "\n", encoding="utf-8")
                    return True
                return False

            def fake_run(_cmd: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
                legacy_summary_path.write_text('["not","object"]', encoding="utf-8")
                return subprocess.CompletedProcess(_cmd, 0, "", "")

            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                wpr_boot_registry,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                wpr_boot_registry,
                "launch_generated_script",
                side_effect=["qga", "qga"],
            ), mock.patch.object(
                wpr_boot_registry,
                "wait_for_file",
                side_effect=fake_wait_for_file,
            ), mock.patch.object(
                wpr_boot_registry,
                "run",
                side_effect=fake_run,
            ), mock.patch.object(
                wpr_boot_registry.time,
                "sleep",
                return_value=None,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = wpr_boot_registry.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "wpr-legacy-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

if __name__ == "__main__":
    unittest.main()
