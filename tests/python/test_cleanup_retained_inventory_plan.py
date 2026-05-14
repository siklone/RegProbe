import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = (
    Path(__file__).resolve().parents[2]
    / "registry-research-framework"
    / "scripts"
    / "generate_cleanup_retained_inventory_plan.py"
)


def load_module():
    spec = importlib.util.spec_from_file_location("generate_cleanup_retained_inventory_plan_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def write_ledger(path: Path, items: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps({"items": items}), encoding="utf-8")


class CleanupRetainedInventoryPlanTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_reference_migration_needed_when_replacement_exists_but_refs_block_delete(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "registry-research-framework/audit/old-report.json",
                        "category": "old-dated-audit-output-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "delete-after-review",
                        "replacement_artifacts": ["registry-research-framework/audit/new-report.json"],
                        "blocking_reference_count": 2,
                        "blocking_references_sample": [
                            "README.md",
                            "research/evidence-index.json",
                        ],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(plan["summary"]["reference_migration_needed_count"], 1)
            self.assertEqual(plan["summary"]["delete_ready_count"], 0)
            self.assertEqual(item["release_state"], "reference-migration-needed")
            self.assertTrue(item["can_become_delete_candidate"])
            self.assertEqual(
                item["blocking_reference_classes"],
                {"public-doc-reference": 1, "research-index-reference": 1},
            )
            self.assertIn(item, plan["reference_migration_queue"])

    def test_keep_referenced_items_are_explicit_retention_not_delete_candidates(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/raw/reference.etl",
                        "category": "large-raw-trace-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "keep-referenced",
                        "blocking_reference_count": 1,
                        "blocking_references_sample": ["research/records/example.json"],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(plan["summary"]["intentional_reference_keep_count"], 1)
            self.assertEqual(item["release_state"], "intentional-reference-keep")
            self.assertFalse(item["can_become_delete_candidate"])
            self.assertEqual(item["blocking_reference_classes"], {"research-record-reference": 1})

    def test_no_replacement_is_a_retention_decision_queue_not_a_delete_queue(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/sample.zip",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "delete-after-review",
                        "blocking_reference_count": 3,
                        "blocking_references_sample": ["evidence/raw/foo.json"],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(plan["summary"]["needs_replacement_or_retention_decision_count"], 1)
            self.assertEqual(plan["summary"]["reference_migration_needed_count"], 0)
            self.assertEqual(item["release_state"], "needs-replacement-or-retention-decision")
            self.assertFalse(item["can_become_delete_candidate"])

    def test_audit_only_references_have_explicit_release_state(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/sample.txt",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-audit-trail-reference",
                        "recommended_action": "delete-after-review",
                        "replacement_artifacts": ["evidence/raw/procmon/sample/sample.txt"],
                        "blocking_reference_count": 0,
                        "audit_reference_count": 2,
                        "blocking_references_sample": [],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(plan["summary"]["audit_only_retained_count"], 1)
            self.assertEqual(item["release_state"], "audit-only-retained")
            self.assertFalse(item["can_become_delete_candidate"])
            self.assertIn("audit trail", item["next_action"])

    def test_markdown_separates_reference_migration_from_retained_worklist(self):
        plan = {
            "generated_utc": "2026-05-14T00:00:00Z",
            "ledger": "registry-research-framework/audit/cleanup-quarantine-ledger.json",
            "purpose": "test",
            "rules": {"delete_candidate_rule": "test rule"},
            "summary": {
                "item_count": 1,
                "delete_ready_count": 0,
                "reference_migration_needed_count": 1,
                "audit_only_retained_count": 0,
                "intentional_reference_keep_count": 0,
                "needs_replacement_or_retention_decision_count": 0,
                "retained_pending_review_count": 0,
                "release_state_counts": {"reference-migration-needed": 1},
                "top_blocking_reference_paths": [{"path": "README.md", "count": 1}],
            },
            "reference_migration_queue": [
                {
                    "path": "old.json",
                    "category": "old-dated-audit-output-sample",
                    "blocking_reference_count": 1,
                    "replacement_artifacts": ["new.json"],
                    "next_action": "Move refs.",
                }
            ],
            "retained_inventory": [
                {
                    "path": "old.json",
                    "release_state": "reference-migration-needed",
                    "category": "old-dated-audit-output-sample",
                    "blocking_reference_count": 1,
                    "next_action": "Move refs.",
                }
            ],
        }

        markdown = self.module.render_markdown(plan)

        self.assertIn("## Reference Migration Queue", markdown)
        self.assertIn("## Retained Inventory Worklist", markdown)
        self.assertIn("old.json", markdown)
        self.assertIn("new.json", markdown)


if __name__ == "__main__":
    unittest.main()
