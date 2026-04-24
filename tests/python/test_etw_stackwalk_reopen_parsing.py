from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


reopen_snapshot = load_module(
    "etw_stackwalk_reopen_snapshot_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_snapshot.py",
)
decision_ledger = load_module(
    "etw_stackwalk_reopen_decision_ledger_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_decision_ledger.py",
)
seed_receipt = load_module(
    "etw_stackwalk_reopen_seed_receipt_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_seed_receipt.py",
)
seed_ack_journal = load_module(
    "etw_stackwalk_reopen_seed_ack_journal_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_seed_ack_journal.py",
)
rotation_ledger = load_module(
    "etw_stackwalk_reopen_rotation_ledger_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_rotation_ledger.py",
)
operator_brief = load_module(
    "etw_stackwalk_reopen_operator_brief_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_operator_brief.py",
)
transition_summary = load_module(
    "etw_stackwalk_reopen_transition_summary_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_transition_summary.py",
)
readiness_scoreboard = load_module(
    "etw_stackwalk_reopen_readiness_scoreboard_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_readiness_scoreboard.py",
)


class EtwStackwalkReopenParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_snapshot_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(reopen_snapshot)

    def test_decision_ledger_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(decision_ledger)

    def test_seed_receipt_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(seed_receipt)

    def test_seed_ack_journal_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(seed_ack_journal)

    def test_rotation_ledger_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(rotation_ledger)

    def test_operator_brief_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(operator_brief)

    def test_transition_summary_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(transition_summary)

    def test_readiness_scoreboard_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(readiness_scoreboard)


if __name__ == "__main__":
    unittest.main()
