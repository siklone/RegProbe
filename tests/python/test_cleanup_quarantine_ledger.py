import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = (
    Path(__file__).resolve().parents[2]
    / "registry-research-framework"
    / "scripts"
    / "generate_cleanup_quarantine_ledger.py"
)


def load_module():
    spec = importlib.util.spec_from_file_location("generate_cleanup_quarantine_ledger_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class CleanupQuarantineLedgerTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_live_references_use_explicit_search_root_and_exclude_self_and_outputs(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            target_dir = repo / "registry-research-framework" / "audit"
            target_dir.mkdir(parents=True)
            target = target_dir / "pilot-demo.json"
            target.write_text('{"id": "pilot-demo"}\n', encoding="utf-8")

            docs = repo / "README.md"
            docs.write_text("Safety example: pilot-demo\n", encoding="utf-8")
            ledger = target_dir / "cleanup-quarantine-ledger-20260510.json"
            ledger.write_text("pilot-demo should not count from generated output\n", encoding="utf-8")

            item = self.module.item_for(
                target,
                category="test",
                stale_reason="superseded by test fixture",
                replacement_artifacts=["replacement.json"],
                recommended_action="delete-after-review",
                repo_root=repo,
                output_paths={"registry-research-framework/audit/cleanup-quarantine-ledger-20260510.json"},
            )

            self.assertEqual(item["live_reference_count"], 1)
            self.assertEqual(item["references_sample"], ["README.md"])
            self.assertFalse(item["delete_eligible"])

    def test_unreferenced_replaced_file_is_delete_eligible_after_review(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            target_dir = repo / "registry-research-framework" / "audit"
            target_dir.mkdir(parents=True)
            target = target_dir / "obsolete-pilot.json"
            target.write_text("{}\n", encoding="utf-8")

            item = self.module.item_for(
                target,
                category="test",
                stale_reason="superseded by test fixture",
                replacement_artifacts=["replacement.json"],
                recommended_action="delete-after-review",
                repo_root=repo,
                output_paths=set(),
            )

            self.assertEqual(item["live_reference_count"], 0)
            self.assertTrue(item["delete_eligible"])

    def test_audit_trail_references_are_counted_separately_from_blocking_references(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            target_dir = repo / "registry-research-framework" / "audit"
            target_dir.mkdir(parents=True)
            target = target_dir / "obsolete-pilot.json"
            target.write_text("{}\n", encoding="utf-8")

            old_ledger = target_dir / "cleanup-quarantine-ledger-20260510.md"
            old_ledger.write_text("obsolete-pilot is tracked here\n", encoding="utf-8")

            item = self.module.item_for(
                target,
                category="test",
                stale_reason="superseded by test fixture",
                replacement_artifacts=["replacement.json"],
                recommended_action="delete-after-review",
                repo_root=repo,
                output_paths={"registry-research-framework/audit/cleanup-quarantine-ledger-20260514.md"},
            )

            self.assertEqual(item["live_reference_count"], 1)
            self.assertEqual(item["audit_reference_count"], 1)
            self.assertEqual(item["blocking_reference_count"], 0)
            self.assertEqual(item["audit_references_sample"], ["registry-research-framework/audit/cleanup-quarantine-ledger-20260510.md"])
            self.assertFalse(item["delete_eligible"])


if __name__ == "__main__":
    unittest.main()
