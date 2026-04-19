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
        self.assertEqual(
            payload["fill_after_run"]["artifacts"]["response_reacquire"]["strong_markers_seen"],
            ["CmQueryValueKey"],
        )
        self.assertEqual(
            payload["fill_after_run"]["artifacts"]["umpo_message_reacquire"]["strong_markers_seen"],
            ["opcode"],
        )


if __name__ == "__main__":
    unittest.main()
