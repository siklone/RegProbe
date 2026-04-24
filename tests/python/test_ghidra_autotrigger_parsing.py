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
    "ghidra_autotrigger_seeds_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_seeds.py",
)
autotrigger_smoke = load_module(
    "ghidra_autotrigger_smoke_parsing_tests",
    FRAMEWORK_SCRIPTS / "run_ghidra_autotrigger_smoke.py",
)
autotrigger_smoke_check = load_module(
    "ghidra_autotrigger_smoke_check_parsing_tests",
    FRAMEWORK_SCRIPTS / "check_ghidra_autotrigger_smoke.py",
)
autotrigger_health = load_module(
    "ghidra_autotrigger_health_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_ghidra_autotrigger_health.py",
)
autotrigger_health_check = load_module(
    "ghidra_autotrigger_health_check_parsing_tests",
    FRAMEWORK_SCRIPTS / "check_ghidra_autotrigger_health.py",
)
autotrigger_sync = load_module(
    "ghidra_autotrigger_sync_parsing_tests",
    FRAMEWORK_SCRIPTS / "sync_ghidra_autotrigger_lane.py",
)


class GhidraAutotriggerParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_seed_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_seeds)

    def test_smoke_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_smoke)

    def test_smoke_check_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_smoke_check)

    def test_health_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_health)

    def test_health_check_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_health_check)

    def test_sync_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(autotrigger_sync)


if __name__ == "__main__":
    unittest.main()
