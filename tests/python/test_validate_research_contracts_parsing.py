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


validate_contracts = load_module(
    "validate_research_contracts_parsing_tests",
    FRAMEWORK_SCRIPTS / "validate_research_contracts.py",
)


class ValidateResearchContractsParsingTests(unittest.TestCase):
    def test_load_json_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                validate_contracts.load_json(path)


if __name__ == "__main__":
    unittest.main()
