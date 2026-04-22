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


transfer = load_module("ghidra_symbol_transfer_parsing_tests", FRAMEWORK_SCRIPTS / "generate_ghidra_symbol_resolution_transfer.py")
materialize = load_module("ghidra_symbol_transfer_pack_parsing_tests", FRAMEWORK_SCRIPTS / "materialize_ghidra_symbol_resolution_transfer_pack.py")
pack_check = load_module("ghidra_symbol_transfer_pack_check_parsing_tests", FRAMEWORK_SCRIPTS / "check_ghidra_symbol_resolution_transfer_pack.py")
unpack = load_module("ghidra_symbol_transfer_unpack_parsing_tests", FRAMEWORK_SCRIPTS / "unpack_ghidra_symbol_resolution_transfer_pack.py")


class GhidraSymbolTransferParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_transfer_load_json_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(transfer)

    def test_materialize_load_json_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(materialize)

    def test_pack_check_load_json_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(pack_check)

    def test_unpack_load_json_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(unpack)


if __name__ == "__main__":
    unittest.main()
