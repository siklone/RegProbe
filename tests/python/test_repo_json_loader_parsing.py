from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


cross_verification = load_module(
    "compare_static_cross_verification_parsing_tests",
    SCRIPTS_ROOT / "compare_static_cross_verification.py",
)
compact_ghidra = load_module(
    "compact_ghidra_branch_output_parsing_tests",
    SCRIPTS_ROOT / "compact_ghidra_branch_output.py",
)
normalize_evidence = load_module(
    "normalize_evidence_layout_parsing_tests",
    SCRIPTS_ROOT / "normalize_evidence_layout.py",
)
runtime_retries = load_module(
    "audit_execution_required_runtime_retries_parsing_tests",
    SCRIPTS_ROOT / "audit_execution_required_runtime_retries.py",
)


class RepoJsonLoaderParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_cross_verification_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(cross_verification)

    def test_compact_ghidra_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(compact_ghidra)

    def test_normalize_evidence_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(normalize_evidence)

    def test_runtime_retries_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(runtime_retries)


if __name__ == "__main__":
    unittest.main()
