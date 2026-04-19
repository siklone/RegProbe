from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "verify_power_request_override_handoff_bundle.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


verifier = load_module("power_request_override_handoff_verifier", SCRIPT_PATH)


class PowerRequestOverrideHandoffVerifierTests(unittest.TestCase):
    def test_verify_bundle_reports_ok_status(self) -> None:
        payload = verifier.verify_bundle()

        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["record_id"], "power.control.power-request-override-subtree")
        self.assertEqual(
            payload["promotion"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertTrue(payload["checks"]["promotion_blocks_match"])
        self.assertFalse(payload["checks"]["missing_promote_script"])
        self.assertEqual(payload["checks"]["missing_command_files"], [])

    def test_cli_markdown_outputs_current_run_summary(self) -> None:
        proc = subprocess.run(
            [sys.executable, str(SCRIPT_PATH), "--markdown"],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )

        self.assertIn("# PowerRequestOverride Handoff Bundle Verification", proc.stdout)
        self.assertIn("power-request-override-reader-binding-reacquire", proc.stdout)
        self.assertIn(
            "power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json",
            proc.stdout,
        )

    def test_cli_json_outputs_preview_targets(self) -> None:
        proc = subprocess.run(
            [sys.executable, str(SCRIPT_PATH)],
            cwd=str(REPO_ROOT),
            capture_output=True,
            text=True,
            check=True,
        )
        payload = json.loads(proc.stdout)

        self.assertEqual(payload["status"], "ok")
        self.assertTrue(
            payload["promotion"]["preview_targets"]["target_md"].endswith(
                "power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.md"
            )
        )


if __name__ == "__main__":
    unittest.main()
