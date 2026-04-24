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


capture_plan = load_module(
    "etw_stackwalk_capture_plan_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_capture_plan.py",
)
dispatch_batch = load_module(
    "etw_stackwalk_dispatch_batch_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_etw_stackwalk_dispatch_batch.py",
)


class EtwStackwalkDispatchParsingTests(unittest.TestCase):
    def non_object_json_path(self, *, encoding: str = "utf-8") -> Path:
        temp_root = tempfile.TemporaryDirectory(dir=REPO_ROOT)
        self.addCleanup(temp_root.cleanup)
        path = Path(temp_root.name) / "payload.json"
        path.write_text('["not","object"]', encoding=encoding)
        return path

    def test_dispatch_loader_rejects_non_object_payload(self) -> None:
        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            dispatch_batch.load_json(self.non_object_json_path(encoding="utf-8-sig"))

    def test_capture_config_loader_rejects_non_object_payload(self) -> None:
        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            capture_plan.load_config(self.non_object_json_path())

    def test_capture_runner_config_loader_rejects_non_object_payload(self) -> None:
        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            capture_plan.load_runner_config(self.non_object_json_path())


if __name__ == "__main__":
    unittest.main()
