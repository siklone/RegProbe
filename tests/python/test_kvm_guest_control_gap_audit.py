from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "registry-research-framework" / "scripts" / "audit_execution_required_kvm_guest_control_gap.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


audit = load_module("kvm_guest_control_gap_audit_for_tests", SCRIPT_PATH)


class KvmGuestControlGapAuditTests(unittest.TestCase):
    def test_parse_query_chardev_stdout_returns_dict_entries(self) -> None:
        entries, error = audit.parse_query_chardev_stdout(
            '{"return":[{"label":"charchannel1","frontend-open":false},{"label":"ignored"},42]}'
        )

        self.assertEqual(error, "")
        self.assertEqual(len(entries), 2)
        self.assertEqual(entries[0]["label"], "charchannel1")

    def test_parse_query_chardev_stdout_reports_invalid_json(self) -> None:
        entries, error = audit.parse_query_chardev_stdout("{not-json")

        self.assertEqual(entries, [])
        self.assertIn("Expecting property name", error)

    def test_parse_query_chardev_stdout_reports_non_object_json(self) -> None:
        entries, error = audit.parse_query_chardev_stdout('["not","object"]')

        self.assertEqual(entries, [])
        self.assertEqual(error, "query-chardev JSON payload is not an object")

    def test_parse_query_chardev_stdout_reports_non_list_return(self) -> None:
        entries, error = audit.parse_query_chardev_stdout('{"return":{"label":"charchannel1"}}')

        self.assertEqual(entries, [])
        self.assertEqual(error, "query-chardev return payload is not a list")


if __name__ == "__main__":
    unittest.main()
