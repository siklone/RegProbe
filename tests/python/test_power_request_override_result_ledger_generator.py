from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "generate_power_request_override_result_ledger.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


generator = load_module("power_request_override_result_ledger_generator", SCRIPT_PATH)


class PowerRequestOverrideResultLedgerGeneratorTests(unittest.TestCase):
    def test_infer_outcome_prefers_direct_registry_read(self) -> None:
        outcome = generator.infer_outcome(["CmQueryValueKey"], [], [])
        self.assertEqual(outcome, "direct-registry-read")

    def test_build_prefilled_payload_collects_markers(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            response_stdout = root / "response.stdout.txt"
            response_summary = root / "response.summary.json"
            umpo_stdout = root / "umpo.stdout.txt"
            umpo_summary = root / "umpo.summary.json"

            response_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "PopPowerRequestHandleRequestOverrideQueryResponse",
                        "CmQueryValueKey",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            umpo_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "PopUmpoSendPowerMessage",
                        "opcode",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            response_summary.write_text("{}", encoding="utf-8")
            umpo_summary.write_text("{}", encoding="utf-8")

            payload = generator.build_prefilled_payload(
                run_id="test-run",
                response_stdout=response_stdout,
                response_summary=response_summary,
                umpo_stdout=umpo_stdout,
                umpo_summary=umpo_summary,
            )

        self.assertEqual(payload["fill_after_run"]["review_outcome"]["chosen_outcome"], "direct-registry-read")
        response_artifact = payload["fill_after_run"]["artifacts"]["response_reacquire"]
        umpo_artifact = payload["fill_after_run"]["artifacts"]["umpo_message_reacquire"]
        self.assertFalse(response_artifact["stdout_path"].startswith("/"))
        self.assertFalse(response_artifact["summary_path"].startswith("/"))
        self.assertTrue(response_artifact["stdout_path"].endswith("/response.stdout.txt"))
        self.assertTrue(umpo_artifact["summary_path"].endswith("/umpo.summary.json"))
        self.assertEqual(
            response_artifact["strong_markers_seen"],
            ["CmQueryValueKey"],
        )
        self.assertEqual(
            umpo_artifact["strong_markers_seen"],
            ["opcode"],
        )

    def test_missing_external_artifact_red_flag_does_not_leak_absolute_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            missing_stdout = Path(temp_root) / "missing.stdout.txt"
            missing_summary = Path(temp_root) / "missing.summary.json"
            review, red_flags = generator.build_artifact_review(
                artifact_id="missing-test",
                stdout_path=missing_stdout,
                summary_path=missing_summary,
                command_file="command.txt",
                rubric_entry={"required_markers": ["REGPROBE_LOCALKD_BEGIN"]},
            )

        self.assertFalse(review["stdout_path"].startswith("/"))
        self.assertFalse(review["summary_path"].startswith("/"))
        self.assertFalse(any(str(temp_root) in flag for flag in red_flags))
        self.assertTrue(any("external-artifacts/" in flag for flag in red_flags))


if __name__ == "__main__":
    unittest.main()
