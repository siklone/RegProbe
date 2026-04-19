from __future__ import annotations

import importlib.util
import subprocess
import sys
import unittest
from argparse import Namespace
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-pipeline.py"
PROMOTER_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "promote_power_request_override_result_ledger.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


pipeline = load_module("power_request_override_pipeline_runner", SCRIPT_PATH)
promoter = load_module("power_request_override_result_ledger_promoter_for_pipeline_tests", PROMOTER_PATH)


class PowerRequestOverridePipelineRunnerTests(unittest.TestCase):
    def test_artifact_paths_follow_bridge_naming(self) -> None:
        upload_dir = Path("/tmp/regprobe-bridge")
        paths = pipeline.artifact_paths(upload_dir, "local-kd-powerrequest-response-reacquire-20260419a")

        self.assertEqual(
            paths["stdout"].as_posix(),
            "/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a.stdout.txt",
        )
        self.assertEqual(
            paths["summary"].as_posix(),
            "/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a-summary.json",
        )

    def test_parse_json_object_tolerates_log_prefix(self) -> None:
        payload = pipeline.parse_json_object('warning: retrying\n{"status": "ok", "value": 1}\n')

        self.assertEqual(payload, {"status": "ok", "value": 1})

    def test_parse_json_object_rejects_non_json_stdout(self) -> None:
        with self.assertRaises(ValueError):
            pipeline.parse_json_object("warning only\nno json here")

    def test_dry_run_outputs_planned_commands_without_vm_access(self) -> None:
        proc = subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--dry-run",
                "--repo-root",
                str(REPO_ROOT),
                "--upload-dir",
                "/tmp/regprobe-bridge",
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = pipeline.json.loads(proc.stdout)

        self.assertEqual(payload["mode"], "dry-run")
        self.assertEqual(payload["runner"], "scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py")
        self.assertEqual(
            payload["ledger_generator"],
            "registry-research-framework/scripts/generate_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["ledger_promoter"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertEqual(
            payload["bundle_verifier_command"],
            [
                sys.executable,
                str(REPO_ROOT / "registry-research-framework" / "scripts" / "verify_power_request_override_handoff_bundle.py"),
            ],
        )
        self.assertIn("gitignored autofill drafts", payload["scratch_policy"])
        self.assertEqual(
            payload["verify_bundle_first"]["example"],
            "python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertTrue(payload["verify_bundle_first"]["required_before_execute"])
        self.assertEqual(
            payload["promote_after_review"]["script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertIn(
            "--run-id power-request-override-reader-binding-reacquire",
            payload["promote_after_review"]["current_run_example"],
        )
        self.assertTrue(payload["promote_after_review"]["source_json"].endswith("power-request-override-reader-binding-result-ledger-autofill.json"))
        self.assertTrue(payload["promote_after_review"]["target_json"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"))
        self.assertTrue(payload["promote_after_review"]["dry_run_example"].endswith("--dry-run"))
        self.assertTrue(payload["promote_after_review"]["current_run_dry_run_example"].endswith("--dry-run"))
        self.assertIn("refuses to overwrite", payload["promote_after_review"]["overwrite_policy"])
        self.assertIn("--response-stdout", payload["generator_command"])
        self.assertEqual(
            payload["expected_artifacts"]["response"]["stdout"],
            "/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a.stdout.txt",
        )
        self.assertEqual(
            payload["expected_artifacts"]["umpo"]["summary"],
            "/tmp/regprobe-bridge/local-kd-powerrequest-umpo-message-reacquire-20260419a-summary.json",
        )

    def test_verify_only_outputs_execute_readiness_payload(self) -> None:
        proc = subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--verify-only",
                "--repo-root",
                str(REPO_ROOT),
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = pipeline.json.loads(proc.stdout)

        self.assertEqual(payload["mode"], "verify-only")
        self.assertEqual(
            payload["pipeline_runner"]["path"],
            "scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py",
        )
        self.assertEqual(payload["bundle_verifier_returncode"], 0)
        self.assertEqual(
            payload["bundle_verifier"]["script"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertTrue(payload["ready_for_execute"])
        self.assertEqual(payload["bundle_verifier_blockers"], [])
        self.assertEqual(payload["bundle_verifier_output"]["blockers"], [])
        self.assertEqual(payload["bundle_verifier_summary"]["status"], "ok")
        self.assertEqual(
            payload["next_steps"]["recommended_example"],
            "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py",
        )
        self.assertEqual(
            payload["next_steps"]["recommended_reason"],
            "Bundle verifier passed; the normal pipeline execute path is ready.",
        )
        self.assertEqual(
            payload["next_steps"]["dry_run_example"],
            "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run",
        )

    def test_dry_run_honors_explicit_repo_root_for_helper_paths(self) -> None:
        explicit_root = Path("/tmp/regprobe-alt-checkout")
        proc = subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--dry-run",
                "--repo-root",
                str(explicit_root),
                "--upload-dir",
                "/tmp/regprobe-bridge",
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = pipeline.json.loads(proc.stdout)

        self.assertEqual(payload["runner"], "scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py")
        self.assertEqual(
            payload["ledger_generator"],
            "registry-research-framework/scripts/generate_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["ledger_promoter"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertEqual(
            payload["bundle_verifier_command"],
            [
                sys.executable,
                "/tmp/regprobe-alt-checkout/registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
            ],
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["promote_after_review"]["target_md"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.md"))
        self.assertTrue(payload["promote_after_review"]["dry_run_example"].endswith("--dry-run"))
        self.assertEqual(
            payload["runner_command"][1],
            "/tmp/regprobe-alt-checkout/scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py",
        )
        self.assertEqual(
            payload["generator_command"][1],
            "/tmp/regprobe-alt-checkout/registry-research-framework/scripts/generate_power_request_override_result_ledger.py",
        )

    def test_pipeline_preview_targets_match_promoter_targets(self) -> None:
        proc = subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--dry-run",
                "--repo-root",
                str(REPO_ROOT),
                "--upload-dir",
                "/tmp/regprobe-bridge",
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = pipeline.json.loads(proc.stdout)

        expected_json, expected_md = promoter.target_paths(
            "power-request-override-reader-binding-reacquire",
            audit_root=REPO_ROOT / "registry-research-framework" / "audit",
        )
        self.assertEqual(
            payload["promote_after_review"]["target_json"],
            promoter.portable_path(expected_json),
        )
        self.assertEqual(
            payload["promote_after_review"]["target_md"],
            promoter.portable_path(expected_md),
        )

    def test_execute_pipeline_still_generates_ledger_after_runner_failure(self) -> None:
        args = Namespace(
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="local-kd-powerrequest-response-reacquire-20260419a",
            umpo_output_name="local-kd-powerrequest-umpo-message-reacquire-20260419a",
            run_id="power-request-override-reader-binding-reacquire",
            output_json=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.json"),
            output_md=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.md"),
            skip_bundle_verifier=True,
        )
        runner_result = subprocess.CompletedProcess(
            args=["runner"],
            returncode=7,
            stdout='{"passes": [{"returncode": 7}]}',
            stderr="timeout while reacquiring UMPO pass",
        )
        generator_result = subprocess.CompletedProcess(
            args=["generator"],
            returncode=0,
            stdout='{"output_json": "registry-research-framework/audit/autofill.json"}',
            stderr="",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[runner_result, generator_result]) as run_mock:
            payload, exit_code = pipeline.execute_pipeline(args, REPO_ROOT, Path("/tmp/regprobe-bridge"))

        self.assertEqual(run_mock.call_count, 2)
        self.assertEqual(exit_code, 7)
        self.assertEqual(
            payload["promote_after_review"]["script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"]["markdown_example"],
            "python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py --markdown",
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["promote_after_review"]["target_json"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"))
        self.assertIn("refuses to overwrite", payload["promote_after_review"]["overwrite_policy"])
        self.assertEqual(payload["runner_returncode"], 7)
        self.assertIsNone(payload["runner_stdout_parse_error"])
        self.assertEqual(payload["runner_stderr"], "timeout while reacquiring UMPO pass")
        self.assertEqual(payload["runner_output"], {"passes": [{"returncode": 7}]})
        self.assertEqual(
            payload["ledger_output"],
            {"output_json": "registry-research-framework/audit/autofill.json"},
        )

    def test_execute_pipeline_generates_ledger_after_runner_non_json_failure(self) -> None:
        args = Namespace(
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="local-kd-powerrequest-response-reacquire-20260419a",
            umpo_output_name="local-kd-powerrequest-umpo-message-reacquire-20260419a",
            run_id="power-request-override-reader-binding-reacquire",
            output_json=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.json"),
            output_md=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.md"),
            skip_bundle_verifier=True,
        )
        runner_result = subprocess.CompletedProcess(
            args=["runner"],
            returncode=9,
            stdout="timeout before json payload",
            stderr="runner failed before writing JSON",
        )
        generator_result = subprocess.CompletedProcess(
            args=["generator"],
            returncode=0,
            stdout='{"output_json": "registry-research-framework/audit/autofill.json"}',
            stderr="",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[runner_result, generator_result]) as run_mock:
            payload, exit_code = pipeline.execute_pipeline(args, REPO_ROOT, Path("/tmp/regprobe-bridge"))

        self.assertEqual(run_mock.call_count, 2)
        self.assertEqual(exit_code, 9)
        self.assertEqual(
            payload["promote_after_review"]["script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"]["script"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["promote_after_review"]["target_json"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"))
        self.assertIn("refuses to overwrite", payload["promote_after_review"]["overwrite_policy"])
        self.assertEqual(payload["runner_returncode"], 9)
        self.assertEqual(payload["runner_output"], {})
        self.assertIn("stdout did not contain a JSON object", payload["runner_stdout_parse_error"])
        self.assertEqual(
            payload["ledger_output"],
            {"output_json": "registry-research-framework/audit/autofill.json"},
        )

    def test_execute_pipeline_reports_generator_json_parse_error(self) -> None:
        args = Namespace(
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="local-kd-powerrequest-response-reacquire-20260419a",
            umpo_output_name="local-kd-powerrequest-umpo-message-reacquire-20260419a",
            run_id="power-request-override-reader-binding-reacquire",
            output_json=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.json"),
            output_md=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.md"),
            skip_bundle_verifier=True,
        )
        runner_result = subprocess.CompletedProcess(
            args=["runner"],
            returncode=0,
            stdout='{"passes": []}',
            stderr="",
        )
        generator_result = subprocess.CompletedProcess(
            args=["generator"],
            returncode=0,
            stdout="ledger generator wrote non-json output",
            stderr="",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[runner_result, generator_result]):
            payload, exit_code = pipeline.execute_pipeline(args, REPO_ROOT, Path("/tmp/regprobe-bridge"))

        self.assertEqual(exit_code, 1)
        self.assertEqual(
            payload["promote_after_review"]["script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"]["script"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["promote_after_review"]["target_json"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"))
        self.assertIn("refuses to overwrite", payload["promote_after_review"]["overwrite_policy"])
        self.assertEqual(payload["ledger_generator_returncode"], 0)
        self.assertIn("stdout did not contain a JSON object", payload["ledger_generator_stdout_parse_error"])

    def test_execute_pipeline_treats_successful_non_json_runner_output_as_failure(self) -> None:
        args = Namespace(
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="local-kd-powerrequest-response-reacquire-20260419a",
            umpo_output_name="local-kd-powerrequest-umpo-message-reacquire-20260419a",
            run_id="power-request-override-reader-binding-reacquire",
            output_json=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.json"),
            output_md=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.md"),
            skip_bundle_verifier=True,
        )
        runner_result = subprocess.CompletedProcess(
            args=["runner"],
            returncode=0,
            stdout="success log without json",
            stderr="",
        )
        generator_result = subprocess.CompletedProcess(
            args=["generator"],
            returncode=0,
            stdout='{"output_json": "registry-research-framework/audit/autofill.json"}',
            stderr="",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[runner_result, generator_result]):
            payload, exit_code = pipeline.execute_pipeline(args, REPO_ROOT, Path("/tmp/regprobe-bridge"))

        self.assertEqual(exit_code, 1)
        self.assertEqual(
            payload["promote_after_review"]["script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"]["script"],
            "registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py",
        )
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["promote_after_review"]["target_json"].endswith("power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"))
        self.assertIn("refuses to overwrite", payload["promote_after_review"]["overwrite_policy"])
        self.assertEqual(payload["runner_returncode"], 0)
        self.assertEqual(payload["runner_output"], {})
        self.assertIn("stdout did not contain a JSON object", payload["runner_stdout_parse_error"])
        self.assertEqual(
            payload["ledger_output"],
            {"output_json": "registry-research-framework/audit/autofill.json"},
        )

    def test_execute_pipeline_stops_early_when_bundle_verifier_fails(self) -> None:
        args = Namespace(
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="local-kd-powerrequest-response-reacquire-20260419a",
            umpo_output_name="local-kd-powerrequest-umpo-message-reacquire-20260419a",
            run_id="power-request-override-reader-binding-reacquire",
            output_json=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.json"),
            output_md=str(REPO_ROOT / "registry-research-framework" / "audit" / "autofill.md"),
            skip_bundle_verifier=False,
        )
        verifier_result = subprocess.CompletedProcess(
            args=["verifier"],
            returncode=5,
            stdout='{"status":"error","checks":{"promotion_blocks_match":false},"summary":{"status":"error","promotion_blocks_match":false,"missing_read_order_count":0,"missing_command_file_count":0,"missing_review_input_count":0,"missing_reacquire_command_count":0,"missing_promote_script":false},"blockers":["promotion_blocks_mismatch"]}',
            stderr="bundle drift",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[verifier_result]) as run_mock:
            payload, exit_code = pipeline.execute_pipeline(args, REPO_ROOT, Path("/tmp/regprobe-bridge"))

        self.assertEqual(run_mock.call_count, 1)
        self.assertEqual(exit_code, 5)
        self.assertEqual(payload["bundle_verifier_returncode"], 5)
        self.assertEqual(
            payload["bundle_verifier_output"],
            {
                "status": "error",
                "checks": {"promotion_blocks_match": False},
                "summary": {
                    "status": "error",
                    "promotion_blocks_match": False,
                    "missing_read_order_count": 0,
                    "missing_command_file_count": 0,
                    "missing_review_input_count": 0,
                    "missing_reacquire_command_count": 0,
                    "missing_promote_script": False,
                },
                "blockers": ["promotion_blocks_mismatch"],
            },
        )
        self.assertEqual(payload["bundle_verifier_checks"], {"promotion_blocks_match": False})
        self.assertEqual(
            payload["bundle_verifier_summary"],
            {
                "status": "error",
                "promotion_blocks_match": False,
                "missing_read_order_count": 0,
                "missing_command_file_count": 0,
                "missing_review_input_count": 0,
                "missing_reacquire_command_count": 0,
                "missing_promote_script": False,
            },
        )
        self.assertEqual(payload["bundle_verifier_blockers"], ["promotion_blocks_mismatch"])
        self.assertTrue(payload["runner_skipped"])
        self.assertTrue(payload["ledger_generator_skipped"])

    def test_run_bundle_verifier_reports_parse_failure_as_not_ready(self) -> None:
        verifier_result = subprocess.CompletedProcess(
            args=["verifier"],
            returncode=0,
            stdout="warning only",
            stderr="",
        )

        with mock.patch.object(pipeline.subprocess, "run", side_effect=[verifier_result]):
            payload, exit_code = pipeline.run_bundle_verifier(REPO_ROOT)

        self.assertEqual(exit_code, 1)
        self.assertFalse(payload["ready_for_execute"])
        self.assertEqual(payload["bundle_verifier_output"], {})
        self.assertIn("stdout did not contain a JSON object", payload["bundle_verifier_stdout_parse_error"])
        self.assertEqual(
            payload["next_steps"]["recommended_example"],
            "python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py --markdown",
        )
        self.assertEqual(
            payload["next_steps"]["recommended_reason"],
            "Bundle verifier stdout was not machine-readable; inspect the markdown summary first.",
        )


if __name__ == "__main__":
    unittest.main()
