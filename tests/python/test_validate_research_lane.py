from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[2]
VM_KVM_SCRIPTS = REPO_ROOT / "scripts" / "vm-kvm"
if str(VM_KVM_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(VM_KVM_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


validate_research_lane = load_module(
    "validate_research_lane_for_tests",
    VM_KVM_SCRIPTS / "validate-research-lane.py",
)


class ValidateResearchLaneTests(unittest.TestCase):
    def test_load_json_reports_invalid_json_without_raising(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "broken.json"
            path.write_text("{not-json", encoding="utf-8")
            load_errors: list[dict[str, str]] = []

            payload = validate_research_lane.load_json(path, load_errors, "tool-health-summary")

        self.assertIsNone(payload)
        self.assertEqual(1, len(load_errors))
        self.assertEqual("tool-health-summary", load_errors[0]["label"])
        self.assertIn("broken.json", load_errors[0]["path"])

    def test_load_json_returns_dict_payloads(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "ok.json"
            path.write_text(json.dumps({"status": "ok"}), encoding="utf-8")
            load_errors: list[dict[str, str]] = []

            payload = validate_research_lane.load_json(path, load_errors, "bootstrap-summary")

        self.assertEqual({"status": "ok"}, payload)
        self.assertEqual([], load_errors)

    def test_utc_timestamp_uses_python_datetime(self) -> None:
        original_datetime = validate_research_lane.datetime

        class FixedDateTime:
            @classmethod
            def now(cls, tz):  # noqa: ANN001
                return original_datetime(2026, 4, 22, 12, 34, 56, tzinfo=tz)

        with mock.patch.object(validate_research_lane, "datetime", FixedDateTime):
            timestamp = validate_research_lane.utc_timestamp()

        self.assertEqual("2026-04-22T12:34:56Z", timestamp)


if __name__ == "__main__":
    unittest.main()
