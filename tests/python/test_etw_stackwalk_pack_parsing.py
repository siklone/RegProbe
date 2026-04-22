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


reopen_journal = load_module(
    "etw_stackwalk_reopen_journal_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_journal.py",
)
prerequisite_delta = load_module(
    "etw_stackwalk_reopen_prerequisite_delta_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_reopen_prerequisite_delta.py",
)
baseline_archive = load_module(
    "etw_stackwalk_reopen_baseline_archive_parsing_tests",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_reopen_baseline_archive.py",
)
history_archive = load_module(
    "etw_stackwalk_reopen_history_archive_parsing_tests",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_reopen_history_archive.py",
)
hold_reopen_pack = load_module(
    "etw_stackwalk_hold_reopen_pack_parsing_tests",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_hold_reopen_pack.py",
)
execution_pack = load_module(
    "etw_stackwalk_execution_pack_parsing_tests",
    FRAMEWORK_SCRIPTS / "materialize_etw_stackwalk_execution_pack.py",
)
hold_reopen_plan = load_module(
    "etw_stackwalk_hold_reopen_plan_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_hold_reopen_plan.py",
)


class EtwStackwalkPackParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_reopen_journal_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(reopen_journal)

    def test_prerequisite_delta_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(prerequisite_delta)

    def test_baseline_archive_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(baseline_archive)

    def test_history_archive_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(history_archive)

    def test_hold_reopen_pack_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(hold_reopen_pack)

    def test_execution_pack_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(execution_pack)

    def test_hold_reopen_plan_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(hold_reopen_plan)


if __name__ == "__main__":
    unittest.main()
