from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


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


registry_inventory_baseline = load_module(
    "run_guest_registry_inventory_baseline_for_tests",
    VM_KVM_SCRIPTS / "run-guest-registry-inventory-baseline.py",
)


class VmKvmRegistryInventoryBaselineTests(unittest.TestCase):
    def test_load_inventory_filters_parse_errors_and_missing_targets(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "inventory.json"
            path.write_text(
                json.dumps(
                    {
                        "entries": [
                            {
                                "index": 1,
                                "path": r"HKLM\Software\Test",
                                "value_name": "Present",
                                "parse_status": "ok",
                            },
                            {
                                "index": 2,
                                "path": r"HKLM\Software\Test",
                                "value_name": "Bad",
                                "parse_status": "error",
                            },
                            {"index": 3, "path": r"HKLM\Software\Test", "parse_status": "ok"},
                        ]
                    }
                ),
                encoding="utf-8",
            )

            entries = registry_inventory_baseline.load_inventory(path)

        self.assertEqual([entry["index"] for entry in entries], [1])

    def test_summarize_status_counts_presence_and_repo_hits(self) -> None:
        counts = registry_inventory_baseline.summarize_status(
            [
                {"key_exists": True, "value_exists": True, "repo_exact_target_match_count": 1, "status": "value-present"},
                {"key_exists": True, "value_exists": False, "repo_exact_target_match_count": 0, "status": "value-missing"},
                {"key_exists": False, "value_exists": False, "repo_exact_target_match_count": 0, "status": "key-missing"},
                {"key_exists": True, "value_exists": False, "repo_exact_target_match_count": 0, "status": "error"},
            ]
        )

        self.assertEqual(counts["total_entries"], 4)
        self.assertEqual(counts["key_present_count"], 3)
        self.assertEqual(counts["key_missing_count"], 1)
        self.assertEqual(counts["value_present_count"], 1)
        self.assertEqual(counts["value_missing_count"], 2)
        self.assertEqual(counts["error_count"], 1)
        self.assertEqual(counts["repo_exact_target_match_count"], 1)

    def test_guest_script_embeds_entries_and_converts_hklm(self) -> None:
        script = registry_inventory_baseline.build_guest_script(
            [
                {
                    "index": 1,
                    "path": r"HKLM\Software\Test",
                    "value_name": "Example",
                    "requested_data": "1",
                    "parse_status": "ok",
                }
            ]
        )

        self.assertIn("Convert-ToPsRegistryPath", script)
        self.assertIn("HKLM:\\", script)
        self.assertIn("FromBase64String", script)
        self.assertNotIn(r"HKLM\Software\Test", script)


if __name__ == "__main__":
    unittest.main()
