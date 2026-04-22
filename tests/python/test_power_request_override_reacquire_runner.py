from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from argparse import Namespace
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-reacquire.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


runner = load_module("power_request_override_reacquire_runner", SCRIPT_PATH)


class PowerRequestOverrideReacquireRunnerTests(unittest.TestCase):
    def test_load_kd_commands_filters_wrapper_lines(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            path = Path(temp_root) / "commands.txt"
            path.write_text(
                "\n".join(
                    [
                        ".echo REGPROBE_LOCALKD_BEGIN",
                        "x nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                        "uf nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                        ".echo REGPROBE_LOCALKD_END",
                        "q",
                        "",
                    ]
                ),
                encoding="utf-8",
            )

            commands = runner.load_kd_commands(path)

        self.assertEqual(
            commands,
            [
                "x nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                "uf nt!PopPowerRequestHandleRequestOverrideQueryResponse",
            ],
        )

    def test_parse_json_object_tolerates_log_prefix(self) -> None:
        payload = runner.parse_json_object('local-kd smoke note\n{"returncode": 0, "status": "ok"}\n')

        self.assertEqual(payload, {"returncode": 0, "status": "ok"})

    def test_parse_json_object_rejects_non_json_stdout(self) -> None:
        with self.assertRaises(ValueError):
            runner.parse_json_object("local-kd smoke note without json")

    def test_run_pass_records_stdout_parse_error_instead_of_raising(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            command_file = Path(temp_root) / "commands.txt"
            command_file.write_text("x nt!PopPowerRequestHandleRequestOverrideQueryResponse\n", encoding="utf-8")
            args = Namespace(
                domain="vm",
                connect="qemu:///session",
                bridge_base_url="http://10.0.2.2:8766",
                upload_dir="/tmp/regprobe-bridge",
                guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
                delay_ms="18",
                wake_key="KEY_ENTER",
                timeout_seconds=240,
                smoke_timeout_seconds=180,
            )
            completed = subprocess.CompletedProcess(args=["runner"], returncode=0, stdout="warning only\nno json here", stderr="stderr-note")

            with mock.patch.object(runner.subprocess, "run", return_value=completed):
                payload = runner.run_pass(
                    repo_root=REPO_ROOT,
                    output_name="reader-pass",
                    command_file=command_file,
                    args=args,
                )

        self.assertEqual(payload["returncode"], 0)
        self.assertEqual(payload["runner_payload"], {})
        self.assertIn("stdout did not contain a JSON object", payload["runner_stdout_parse_error"])
        self.assertEqual(payload["stdout"], "warning only\nno json here")
        self.assertEqual(payload["stderr"], "stderr-note")

    def test_main_returns_failure_when_runner_stdout_is_not_json(self) -> None:
        args = Namespace(
            repo_root=str(REPO_ROOT),
            domain="vm",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            response_output_name="response-pass",
            umpo_output_name="umpo-pass",
            dry_run=False,
        )
        with mock.patch.object(runner.argparse.ArgumentParser, "parse_args", return_value=args), mock.patch.object(
            runner,
            "run_pass",
            side_effect=[
                {
                    "output_name": "response-pass",
                    "command_file": "response.txt",
                    "kd_command_count": 1,
                    "returncode": 0,
                    "runner_payload": {},
                    "runner_stdout_parse_error": "stdout did not contain a JSON object: warning",
                    "stdout": "warning",
                    "stderr": "",
                },
                {
                    "output_name": "umpo-pass",
                    "command_file": "umpo.txt",
                    "kd_command_count": 1,
                    "returncode": 0,
                    "runner_payload": {"status": "ok"},
                    "runner_stdout_parse_error": None,
                    "stdout": '{"status":"ok"}',
                    "stderr": "",
                },
            ],
        ), mock.patch("sys.stdout"):
            exit_code = runner.main()

        self.assertEqual(exit_code, 1)

    def test_dry_run_outputs_two_pass_plan_without_vm_access(self) -> None:
        proc = subprocess.run(
            [
                sys.executable,
                str(SCRIPT_PATH),
                "--dry-run",
                "--repo-root",
                str(REPO_ROOT),
            ],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = runner.json.loads(proc.stdout)

        self.assertEqual(payload["mode"], "dry-run")
        self.assertEqual(payload["record_id"], "power.control.power-request-override-subtree")
        self.assertEqual(len(payload["passes"]), 2)
        self.assertEqual(
            payload["passes"][0]["command_file"],
            "registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt",
        )
        self.assertIn("--kd-command", payload["passes"][0]["command"])
        self.assertEqual(
            payload["passes"][1]["command_file"],
            "registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt",
        )

    def test_dry_run_honors_explicit_repo_root_for_local_kd_runner_path(self) -> None:
        with tempfile.TemporaryDirectory(prefix="regprobe-alt-checkout-") as temp_root:
            explicit_root = Path(temp_root)
            response_file = (
                explicit_root
                / "registry-research-framework"
                / "audit"
                / "power-request-override-response-reacquire-local-kd-20260419.txt"
            )
            umpo_file = (
                explicit_root
                / "registry-research-framework"
                / "audit"
                / "power-request-override-umpo-message-reacquire-local-kd-20260419.txt"
            )
            response_file.parent.mkdir(parents=True, exist_ok=True)
            response_file.write_text("x nt!PopPowerRequestHandleRequestOverrideQueryResponse\n", encoding="utf-8")
            umpo_file.write_text("x nt!PopUmpoSendPowerMessage\n", encoding="utf-8")

            proc = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_PATH),
                    "--dry-run",
                    "--repo-root",
                    str(explicit_root),
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
                check=True,
            )
            payload = runner.json.loads(proc.stdout)

        self.assertEqual(
            payload["passes"][0]["command"][1],
            (explicit_root / "scripts" / "vm-kvm" / "run-guest-local-kd-smoke.py").as_posix(),
        )
        self.assertEqual(
            payload["passes"][1]["command"][1],
            (explicit_root / "scripts" / "vm-kvm" / "run-guest-local-kd-smoke.py").as_posix(),
        )


if __name__ == "__main__":
    unittest.main()
