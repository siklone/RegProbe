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


autotrigger_seeds = load_module(
    "ghidra_jsonl_autotrigger_seeds_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_seeds.py",
)
autotrigger_health = load_module(
    "ghidra_jsonl_autotrigger_health_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_health.py",
)
dispatch_batch = load_module(
    "ghidra_jsonl_dispatch_batch_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_ghidra_dispatch_batch.py",
)


class GhidraJsonlParsingTests(unittest.TestCase):
    def assert_rejects_non_object_jsonl(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.jsonl"
            path.write_text('{"ok": true}\n["not","object"]\n', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSONL payload is not an object"):
                module.load_jsonl(path)

    def test_autotrigger_seed_jsonl_rejects_non_object_line(self) -> None:
        self.assert_rejects_non_object_jsonl(autotrigger_seeds)

    def test_autotrigger_health_jsonl_rejects_non_object_line(self) -> None:
        self.assert_rejects_non_object_jsonl(autotrigger_health)

    def test_dispatch_batch_jsonl_rejects_non_object_line(self) -> None:
        self.assert_rejects_non_object_jsonl(dispatch_batch)


if __name__ == "__main__":
    unittest.main()
