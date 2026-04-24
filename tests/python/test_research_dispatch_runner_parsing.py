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


ghidra_runner = load_module("ghidra_dispatch_runner_for_parsing_tests", FRAMEWORK_SCRIPTS / "run_ghidra_dispatch_batch.py")
etw_runner = load_module("etw_dispatch_runner_for_parsing_tests", FRAMEWORK_SCRIPTS / "run_etw_stackwalk_dispatch_batch.py")
symbol_runner = load_module(
    "symbol_resolution_runner_for_parsing_tests",
    FRAMEWORK_SCRIPTS / "run_ghidra_symbol_resolution_batch.py",
)


class ResearchDispatchRunnerParsingTests(unittest.TestCase):
    def test_ghidra_load_json_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "batch.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                ghidra_runner.load_json(path)

    def test_etw_load_json_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "batch.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                etw_runner.load_json(path)

    def test_symbol_resolution_load_json_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "batch.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                symbol_runner.load_json(path)


if __name__ == "__main__":
    unittest.main()
