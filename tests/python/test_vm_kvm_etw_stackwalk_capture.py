from __future__ import annotations

import importlib.util
import argparse
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


etw_stackwalk_capture = load_module(
    "run_guest_etw_stackwalk_capture_for_tests",
    VM_KVM_SCRIPTS / "run-guest-etw-stackwalk-capture.py",
)


class VmKvmEtwStackwalkCaptureTests(unittest.TestCase):
    def effective_capture_settings(self) -> dict[str, object]:
        return {
            "profile_id": "default",
            "run_id": "stackwalk-test",
            "duration_seconds": 60,
            "registry_path": r"HKLM\SOFTWARE\RegProbe",
            "value_name": "Enabled",
            "guest_output_root": r"C:\RegProbe-Diag\etw-stackwalk",
            "kernel_flags": ["REGISTRY"],
            "stackwalk_events": ["RegQueryValue"],
            "buffer_size_kb": 1024,
            "min_buffers": 64,
            "max_buffers": 256,
        }

    def test_guest_helper_quotes_native_arguments_for_spaced_registry_paths(self) -> None:
        helper = REPO_ROOT / "scripts" / "vm" / "guest-tools" / "run-etw-registry-stackwalk-capture.ps1"
        text = helper.read_text(encoding="utf-8")

        self.assertIn("function ConvertTo-QuotedArgumentString", text)
        self.assertIn("ConvertTo-QuotedArgumentString -Arguments $ArgumentList", text)
        self.assertIn("$probeArgs = @('query', $RegistryPath)", text)

    def test_guest_helper_publishes_stage_artifacts(self) -> None:
        helper = REPO_ROOT / "scripts" / "vm" / "guest-tools" / "run-etw-registry-stackwalk-capture.ps1"
        text = helper.read_text(encoding="utf-8")

        self.assertIn("function Publish-Stage", text)
        self.assertIn("Invoke-ArtifactUpload -Path $stagePath -RemoteName ($safeRunId + '-stage.json')", text)
        self.assertIn("Publish-Stage -Stage 'summary-upload' -Status 'starting'", text)
        self.assertIn("Publish-Stage -Stage 'completed' -Status 'ok'", text)

    def test_ingest_capture_artifacts_missing_etl_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "summary.json"
            summary_path.write_text('{"status":"ok"}\n', encoding="utf-8")

            payload = etw_stackwalk_capture.ingest_capture_artifacts(
                repo_root=REPO_ROOT,
                run_id="stackwalk-test",
                summary_path=summary_path,
                xml_path=None,
                etl_path=temp_dir / "missing.etl",
                ingest_root=temp_dir / "ingest",
                refresh_ghidra=False,
            )

        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "ingest-missing-etl")
        self.assertEqual(payload["recovery_action"], "rerun-etw-stackwalk-capture")
        self.assertEqual(payload["transport_blocker"], "missing-etl")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "ingest-preflight")

    def test_ingest_capture_artifacts_retries_after_guest_xml_backfill(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "summary.json"
            summary_path.write_text(
                json.dumps(
                    {
                        "status": "ok",
                        "tracerpt_exists": True,
                        "etl_path": r"C:\RegProbe-Diag\etw-stackwalk\stackwalk-test\stackwalk-test.etl",
                        "xml_path": r"C:\RegProbe-Diag\etw-stackwalk\stackwalk-test\stackwalk-test.xml",
                        "upload_base_url": "http://10.0.2.2:8766",
                    }
                )
                + "\n",
                encoding="utf-8",
            )
            etl_path = temp_dir / "stackwalk-test.etl"
            etl_path.write_bytes(b"etl")

            bundle_invocations: list[int] = []
            backfill_timeouts: list[int] = []

            def fake_run_bundle_generation(*, target_bundle: Path, **kwargs):  # noqa: ANN003
                bundle_invocations.append(1)
                if len(bundle_invocations) == 1:
                    target_bundle.write_text(
                        json.dumps(
                            {
                                "status": "error",
                                "error_kind": "parser-unavailable",
                                "errors": ["tracerpt.exe is not available in this environment."],
                            }
                        )
                        + "\n",
                        encoding="utf-8",
                    )
                    return subprocess.CompletedProcess(["bundle"], 1, "", "")

                target_bundle.write_text(
                    json.dumps(
                        {
                            "status": "ok",
                            "error_kind": None,
                            "event_count": 1,
                            "events": [{"operation": "RegQueryValue"}],
                        }
                    )
                    + "\n",
                    encoding="utf-8",
                )
                return subprocess.CompletedProcess(["bundle"], 0, '{"status":"ok"}', "")

            def fake_try_guest_xml_backfill(*, target_xml: Path, artifact_upload_timeout_seconds: int, **kwargs):  # noqa: ANN003
                backfill_timeouts.append(artifact_upload_timeout_seconds)
                target_xml.write_text("<Events />\n", encoding="utf-8")
                return {
                    "status": "ok",
                    "target_xml": str(target_xml),
                }

            with mock.patch.object(
                etw_stackwalk_capture,
                "run_bundle_generation",
                side_effect=fake_run_bundle_generation,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "try_guest_xml_backfill",
                side_effect=fake_try_guest_xml_backfill,
            ):
                payload = etw_stackwalk_capture.ingest_capture_artifacts(
                    repo_root=REPO_ROOT,
                    run_id="stackwalk-test",
                    summary_path=summary_path,
                    xml_path=None,
                    etl_path=etl_path,
                    ingest_root=temp_dir / "ingest",
                    refresh_ghidra=False,
                    guest_launch_context={
                        "domain": "regprobe-win11-25h2-session",
                        "connect": "qemu:///session",
                        "bridge_base_url": "http://10.0.2.2:8766",
                        "guest_scripts_root": r"C:\RegProbe-Diag\bootstrap",
                        "upload_dir": str(temp_dir / "uploads"),
                        "qga_wait_timeout": 300,
                    },
                )

        self.assertEqual(payload["status"], "ok")
        self.assertEqual(len(bundle_invocations), 2)
        self.assertEqual(backfill_timeouts, [300])
        self.assertEqual((payload.get("xml_backfill") or {}).get("status"), "ok")
        self.assertTrue(payload["xml_path"])
        self.assertEqual(payload.get("bundle_error_kind"), None)

    def test_ingest_capture_artifacts_passes_artifact_upload_timeout_override(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "summary.json"
            summary_path.write_text(
                json.dumps(
                    {
                        "status": "ok",
                        "tracerpt_exists": True,
                        "etl_path": r"C:\RegProbe-Diag\etw-stackwalk\stackwalk-test\stackwalk-test.etl",
                        "xml_path": r"C:\RegProbe-Diag\etw-stackwalk\stackwalk-test\stackwalk-test.xml",
                        "upload_base_url": "http://10.0.2.2:8766",
                    }
                )
                + "\n",
                encoding="utf-8",
            )
            etl_path = temp_dir / "stackwalk-test.etl"
            etl_path.write_bytes(b"etl")
            seen_timeouts: list[int] = []

            def fake_run_bundle_generation(*, target_bundle: Path, **kwargs):  # noqa: ANN003
                target_bundle.write_text(
                    json.dumps({"status": "error", "error_kind": "parser-unavailable"})
                    + "\n",
                    encoding="utf-8",
                )
                return subprocess.CompletedProcess(["bundle"], 1, "", "")

            def fake_try_guest_xml_backfill(*, artifact_upload_timeout_seconds: int, **kwargs):  # noqa: ANN003
                seen_timeouts.append(artifact_upload_timeout_seconds)
                return {"status": "error", "reason": "uploaded-xml-missing"}

            with mock.patch.object(
                etw_stackwalk_capture,
                "run_bundle_generation",
                side_effect=fake_run_bundle_generation,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "try_guest_xml_backfill",
                side_effect=fake_try_guest_xml_backfill,
            ):
                payload = etw_stackwalk_capture.ingest_capture_artifacts(
                    repo_root=REPO_ROOT,
                    run_id="stackwalk-test",
                    summary_path=summary_path,
                    xml_path=None,
                    etl_path=etl_path,
                    ingest_root=temp_dir / "ingest",
                    refresh_ghidra=False,
                    artifact_upload_timeout_seconds=42,
                    guest_launch_context={
                        "domain": "regprobe-win11-25h2-session",
                        "connect": "qemu:///session",
                        "bridge_base_url": "http://10.0.2.2:8766",
                        "guest_scripts_root": r"C:\RegProbe-Diag\bootstrap",
                        "upload_dir": str(temp_dir / "uploads"),
                        "qga_wait_timeout": 300,
                    },
                )

        self.assertEqual(payload["status"], "error")
        self.assertEqual(seen_timeouts, [42])

    def test_timeout_summary_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
                "--timeout-seconds",
                "180",
                "--first-artifact-timeout-seconds",
                "999",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 2.0, 181.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-etw-stackwalk-capture")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-timeout")

    def test_first_artifact_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
                "--first-artifact-timeout-seconds",
                "60",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                side_effect=[0.0, 0.0, 1.0, 61.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "bridge-artifact-timeout")
        self.assertEqual(payload["recovery_action"], "inspect-bridge-upload")
        self.assertEqual(payload["transport_blocker"], "bridge-artifact-timeout")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "first-artifact-timeout")

    def test_invalid_stage_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "stackwalk-test-stage.json"

            def fake_launch(**kwargs):  # noqa: ANN003
                upload_dir.mkdir(parents=True, exist_ok=True)
                stage_path.write_text("{not-json", encoding="utf-8")
                return "qga"

            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=fake_launch,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "etw-stackwalk-stage-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-etw-stackwalk-capture")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_stage_stall_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            stage_path = upload_dir / "stackwalk-test-stage.json"

            def fake_launch(**kwargs):  # noqa: ANN003
                upload_dir.mkdir(parents=True, exist_ok=True)
                stage_path.write_text(
                    json.dumps(
                        {
                            "generated_utc": "1970-01-01T00:00:00Z",
                            "stage": "xperf-start",
                            "status": "starting",
                        }
                    ),
                    encoding="utf-8",
                )
                return "qga"

            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
                "--first-artifact-timeout-seconds",
                "60",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=fake_launch,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                return_value=200.0,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "guest-stage-stall")
        self.assertEqual(payload["recovery_action"], "inspect-stage-upload")
        self.assertEqual(payload["transport_blocker"], "stage-stall")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "stage-timeout")
        self.assertEqual(payload["stage"]["stage"], "xperf-start")

    def test_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "stackwalk-test-summary.json"

            def fake_launch(**kwargs):  # noqa: ANN003
                summary_path.write_text("{not-json", encoding="utf-8")
                return "qga"

            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=fake_launch,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "etw-stackwalk-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-etw-stackwalk-capture")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_non_object_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "stackwalk-test-summary.json"

            def fake_launch(**kwargs):  # noqa: ANN003
                summary_path.write_text('["not","object"]', encoding="utf-8")
                return "qga"

            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=fake_launch,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture.time,
                "time",
                side_effect=[0.0, 0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["error_kind"], "etw-stackwalk-summary-parse-error")
        self.assertIn("is not an object", payload["summary_parse_error"])

    def test_launch_failure_reports_contract_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
            ]
            failure = subprocess.CalledProcessError(9, ["qga-run-powershell.py"], output="stdout-text", stderr="stderr-text")
            setattr(failure, "stage", "qga-launch")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=failure,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "etw-stackwalk-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-etw-stackwalk-capture")
        self.assertEqual(payload["transport_blocker"], "launch-failed")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-launch-failure")
        self.assertEqual(payload["launch_transport"], "auto")
        self.assertEqual(payload["host_step"], "qga-launch")
        self.assertEqual(payload["exit_code"], 9)

    def test_qga_preflight_failure_reports_fail_fast_contract(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-etw-stackwalk-capture.py",
                "--upload-dir",
                str(upload_dir),
                "--profile-id",
                "default",
                "--run-id",
                "stackwalk-test",
            ]
            preflight = {
                "status": "error",
                "summary_source": "qga-preflight",
                "failed_checks": ["guest_ping"],
                "error": "QGA preflight failed before guest launch.",
            }
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                etw_stackwalk_capture,
                "load_profile_config",
                return_value={"profiles": []},
            ), mock.patch.object(
                etw_stackwalk_capture,
                "resolve_effective_capture_settings",
                return_value=self.effective_capture_settings(),
            ), mock.patch.object(
                etw_stackwalk_capture,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                etw_stackwalk_capture,
                "launch_generated_script",
                side_effect=etw_stackwalk_capture.QgaPreflightError(preflight),
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = etw_stackwalk_capture.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["summary_source"], "qga-preflight")
        self.assertEqual(payload["error_kind"], "qga-preflight-failed")
        self.assertEqual(payload["transport_blocker"], "qga-agent-command")
        self.assertEqual(payload["recovery_action"], "repair-qga-or-run-vm-health-check")
        self.assertEqual(payload["launch_transport"], "auto")

    def test_auto_launch_qga_failure_does_not_fall_back_to_send_key(self) -> None:
        args = argparse.Namespace(
            launch_transport="auto",
            preflight="off",
            domain="vm",
            connect="qemu:///session",
            qga_retry_seconds=0,
            qga_retry_interval_seconds=1,
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            delay_ms="18",
            wake_key="KEY_ENTER",
        )
        qga_failure = subprocess.CompletedProcess(["qga"], 1, "qga stdout", "qga stderr")

        with mock.patch.object(
            etw_stackwalk_capture.subprocess,
            "run",
            return_value=qga_failure,
        ), mock.patch.object(etw_stackwalk_capture, "run") as send_key_run:
            with self.assertRaises(subprocess.CalledProcessError) as raised:
                etw_stackwalk_capture.launch_generated_script(
                    repo_root=REPO_ROOT,
                    generated_path=REPO_ROOT / "dist" / "kvm-generated" / "guest.ps1",
                    guest_launcher="Write-Host launch",
                    guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
                    marker_name="ready",
                    args=args,
                )

        self.assertEqual(getattr(raised.exception, "stage"), "qga-launch")
        send_key_run.assert_not_called()


if __name__ == "__main__":
    unittest.main()
