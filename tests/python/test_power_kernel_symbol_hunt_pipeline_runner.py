from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-kernel-symbol-hunt-pipeline.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


pipeline = load_module("power_kernel_symbol_hunt_pipeline_runner", SCRIPT_PATH)


class PowerKernelSymbolHuntPipelineRunnerTests(unittest.TestCase):
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
        payload = json.loads(proc.stdout)

        self.assertEqual(payload["mode"], "dry-run")
        self.assertEqual(payload["runner"], "scripts/vm-kvm/run-power-kernel-symbol-hunt.py")
        self.assertEqual(
            payload["ledger_generator"],
            "registry-research-framework/scripts/generate_power_kernel_symbol_hunt_result_ledger.py",
        )
        self.assertEqual(
            payload["ledger_promoter"],
            "registry-research-framework/scripts/promote_power_kernel_symbol_hunt_result_ledger.py",
        )
        self.assertEqual(
            payload["bundle_verifier"],
            "registry-research-framework/scripts/verify_power_kernel_symbol_hunt_bundle.py",
        )
        self.assertIn("init_walker", payload["expected_artifacts"])
        self.assertIn("global_timer", payload["expected_artifacts"])
        self.assertEqual(
            payload["promote_after_review"]["current_run_id"],
            "power-kernel-symbol-hunt-reacquire",
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
        payload = json.loads(proc.stdout)

        self.assertEqual(payload["mode"], "verify-only")
        self.assertEqual(payload["bundle_verifier_returncode"], 0)
        self.assertTrue(payload["ready_for_execute"])
        self.assertEqual(payload["bundle_verifier_blockers"], [])
        self.assertEqual(
            payload["next_steps"]["recommended_example"],
            "python3 scripts/vm-kvm/run-power-kernel-symbol-hunt-pipeline.py",
        )


if __name__ == "__main__":
    unittest.main()
