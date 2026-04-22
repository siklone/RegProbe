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


registry_policy_probe = load_module(
    "run_guest_registry_policy_probe_for_tests",
    VM_KVM_SCRIPTS / "run-guest-registry-policy-probe.py",
)


class VmKvmRegistryPolicyProbeTests(unittest.TestCase):
    def test_main_timeout_uses_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-registry-policy-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "policy-probe-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--trigger-profile",
                "default",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                registry_policy_probe,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                registry_policy_probe,
                "launch_generated_script",
                return_value="qga",
            ), mock.patch.object(
                registry_policy_probe,
                "try_probe_stage_fallback",
                return_value=None,
            ), mock.patch.object(
                registry_policy_probe.time,
                "sleep",
                return_value=None,
            ), mock.patch.object(
                registry_policy_probe.time,
                "time",
                side_effect=[0.0, 999.0, 1000.0, 1001.0, 1002.0, 1003.0, 1004.0, 1005.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = registry_policy_probe.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 2)
        self.assertEqual(payload["status"], "timeout")
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-registry-policy-probe")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_try_probe_stage_fallback_preserves_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "probe-summary.json"
            probe_stage_path = temp_dir / "probe-stage.json"
            result_path = temp_dir / "probe-result.txt"

            probe_stage_path.write_text(
                json.dumps(
                    {
                        "generated_utc": "2026-04-18T00:00:00Z",
                        "stage": "collect",
                        "status": "error",
                        "message": "collect stage failed",
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            result_path.write_text("ERROR=saveas timed out\n", encoding="utf-8")

            args = argparse.Namespace(
                registry_path=r"HKLM\SOFTWARE\RegProbe",
                value_name="Enabled",
                output_name="policy-probe-test",
                trigger_profile="default",
            )

            summary, payload = registry_policy_probe.try_probe_stage_fallback(
                summary_path=summary_path,
                probe_stage_path=probe_stage_path,
                result_path=result_path,
                args=args,
            )

        self.assertEqual(summary["status"], "error")
        self.assertEqual(summary["error_kind"], "probe-stage-error")
        self.assertEqual(summary["recovery_action"], "inspect-probe-stage")
        self.assertEqual(summary["transport_blocker"], "probe-stage-error")
        self.assertEqual(summary["guest_health"], "degraded")
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "probe-stage-error")
        self.assertEqual(payload["recovery_action"], "inspect-probe-stage")
        self.assertEqual(payload["transport_blocker"], "probe-stage-error")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "probe-stage-fallback")

    def test_try_probe_stage_fallback_reports_stage_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "probe-summary.json"
            probe_stage_path = temp_dir / "probe-stage.json"
            result_path = temp_dir / "probe-result.txt"
            probe_stage_path.write_text("{not-json", encoding="utf-8")

            args = argparse.Namespace(
                registry_path=r"HKLM\SOFTWARE\RegProbe",
                value_name="Enabled",
                output_name="policy-probe-test",
                trigger_profile="default",
            )

            summary, payload = registry_policy_probe.try_probe_stage_fallback(
                summary_path=summary_path,
                probe_stage_path=probe_stage_path,
                result_path=result_path,
                args=args,
            )

        self.assertEqual(summary["status"], "error")
        self.assertEqual(summary["error_kind"], "probe-stage-parse-error")
        self.assertEqual(summary["recovery_action"], "rerun-registry-policy-probe")
        self.assertEqual(summary["transport_blocker"], "summary-parse-error")
        self.assertEqual(summary["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "probe-stage-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_try_probe_stage_fallback_reports_non_object_stage_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "probe-summary.json"
            probe_stage_path = temp_dir / "probe-stage.json"
            result_path = temp_dir / "probe-result.txt"
            probe_stage_path.write_text('["not","object"]', encoding="utf-8")

            args = argparse.Namespace(
                registry_path=r"HKLM\SOFTWARE\RegProbe",
                value_name="Enabled",
                output_name="policy-probe-test",
                trigger_profile="default",
            )

            summary, payload = registry_policy_probe.try_probe_stage_fallback(
                summary_path=summary_path,
                probe_stage_path=probe_stage_path,
                result_path=result_path,
                args=args,
            )

        self.assertEqual(summary["error_kind"], "probe-stage-parse-error")
        self.assertIn("is not an object", summary["summary_parse_error"])
        self.assertEqual(payload["summary_source"], "probe-stage-parse-error")

    def test_main_invalid_summary_reports_parse_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            summary_path = upload_dir / "policy-probe-test-summary.json"

            def fake_launch(**kwargs):  # noqa: ANN003
                upload_dir.mkdir(parents=True, exist_ok=True)
                summary_path.write_text("{not-json", encoding="utf-8")
                return "qga"

            argv = [
                "run-guest-registry-policy-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "policy-probe-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--trigger-profile",
                "default",
            ]
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                registry_policy_probe,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                registry_policy_probe,
                "launch_generated_script",
                side_effect=fake_launch,
            ), mock.patch.object(
                registry_policy_probe.time,
                "time",
                side_effect=[0.0, 1.0],
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = registry_policy_probe.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "registry-policy-summary-parse-error")
        self.assertEqual(payload["recovery_action"], "rerun-registry-policy-probe")
        self.assertEqual(payload["transport_blocker"], "summary-parse-error")
        self.assertIn("summary_parse_error", payload)

    def test_main_launch_failure_reports_contract_error(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            upload_dir = Path(temp_root) / "upload"
            argv = [
                "run-guest-registry-policy-probe.py",
                "--upload-dir",
                str(upload_dir),
                "--output-name",
                "policy-probe-test",
                "--registry-path",
                r"HKLM\SOFTWARE\RegProbe",
                "--value-name",
                "Enabled",
                "--trigger-profile",
                "default",
            ]
            failure = subprocess.CalledProcessError(7, ["type-to-guest.py"], output="typed", stderr="focus-lost")
            setattr(failure, "stage", "type-to-guest")
            with mock.patch.object(sys, "argv", argv), mock.patch.object(
                registry_policy_probe,
                "ensure_guest_bridge",
                return_value=None,
            ), mock.patch.object(
                registry_policy_probe,
                "launch_generated_script",
                side_effect=failure,
            ), mock.patch("sys.stdout", new_callable=io.StringIO) as stdout:
                exit_code = registry_policy_probe.main()

        payload = json.loads(stdout.getvalue())
        self.assertEqual(exit_code, 1)
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "registry-policy-launch-error")
        self.assertEqual(payload["recovery_action"], "rerun-registry-policy-probe")
        self.assertEqual(payload["transport_blocker"], "launch-failed")
        self.assertEqual(payload["guest_health"], "unknown")
        self.assertEqual(payload["summary_source"], "host-launch-failure")
        self.assertEqual(payload["host_step"], "type-to-guest")
        self.assertEqual(payload["exit_code"], 7)


if __name__ == "__main__":
    unittest.main()
