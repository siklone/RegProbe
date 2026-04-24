from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "verify_power_kernel_symbol_hunt_bundle.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


verifier = load_module("power_kernel_symbol_hunt_handoff_verifier", SCRIPT_PATH)


class PowerKernelSymbolHuntHandoffVerifierTests(unittest.TestCase):
    def test_verify_bundle_reports_ready_for_execute(self) -> None:
        payload = verifier.verify_bundle()

        self.assertEqual(payload["status"], "ok")
        self.assertTrue(payload["ready_for_execute"])
        self.assertEqual(payload["blockers"], [])
        self.assertEqual(
            payload["output_contract"],
            ["ready_for_execute", "summary", "blockers", "operator_checklist", "next_steps"],
        )
        self.assertEqual(
            payload["next_steps"]["recommended_example"],
            "python3 scripts/vm-kvm/run-power-kernel-symbol-hunt-pipeline.py",
        )


if __name__ == "__main__":
    unittest.main()
