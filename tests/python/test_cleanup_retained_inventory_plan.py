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
            self.assertEqual(plan["summary"]["retention_decision_queue_count"], 1)
            self.assertEqual(plan["summary"]["decision_track_counts"], {"staging-bundle-canonicalization": 1})
            self.assertEqual(plan["summary"]["reference_migration_needed_count"], 0)
            self.assertEqual(item["release_state"], "needs-replacement-or-retention-decision")
            self.assertEqual(item["delete_candidate_state"], "not-a-delete-candidate")
            self.assertEqual(item["decision_track"], "staging-bundle-canonicalization")
            self.assertIn("canonical evidence/raw replacement", item["exit_criteria"])
            self.assertFalse(item["can_become_delete_candidate"])
            self.assertIn(item, plan["retention_decision_queue"])

    def test_staging_canonicalization_metadata_explains_known_items(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/hags_toggle_out.txt",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "keep-pending-review",
                        "blocking_reference_count": 3,
                        "blocking_references_sample": ["research/records/system.enable-hags.review.json"],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["canonicalization_state"], "canonical-raw-replacement-known")
            self.assertEqual(item["owning_records"], ["system.enable-hags"])
            self.assertIn("runtime-diff JSON", item["next_canonicalization_step"])
            self.assertEqual(
                plan["summary"]["staging_canonicalization_state_counts"],
                {"canonical-raw-replacement-known": 1},
            )

    def test_vm_batch_probe_placeholder_has_canonical_raw_replacement(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json..md",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-audit-trail-reference",
                        "recommended_action": "keep-pending-review",
                        "blocking_reference_count": 0,
                        "audit_reference_count": 1,
                        "blocking_references_sample": [],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["release_state"], "audit-only-retained")
            self.assertEqual(item["canonicalization_state"], "canonical-raw-replacement-known")
            self.assertIn(
                "evidence/raw/runtime-diff/vm-batch-probe-20260320/vm-batch-probe-20260320.json",
                item["canonical_replacement_candidates"],
            )
            self.assertIn("multi-record VM batch", item["retention_rationale"])

    def test_active_tool_output_root_is_not_retention_decision_queue(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/ghidra-probes",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "keep-pending-review",
                        "blocking_reference_count": 2,
                        "blocking_references_sample": [
                            "scripts/vm-kvm/run-ghidra-string-probe.py",
                            "scripts/vm-kvm/run-ghidra-symbolized-analysis.py",
                        ],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(plan["summary"]["retention_decision_queue_count"], 0)
            self.assertEqual(plan["summary"]["intentional_reference_keep_count"], 1)
            self.assertEqual(item["release_state"], "intentional-reference-keep")
            self.assertEqual(item["decision_track"], "tooling-output-root")
            self.assertEqual(item["canonicalization_state"], "active-tool-output-root")
            self.assertNotIn(item, plan["retention_decision_queue"])

    def test_defender_cloud_staging_item_has_canonical_raw_metadata(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/defender-cloud-demo-extracted",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-audit-trail-reference",
                        "recommended_action": "delete-after-review",
                        "replacement_artifacts": [
                            "evidence/raw/external/security.threat-file-hash-logging/"
                            "defender-cloud-demo-sample-metadata-20260325.json"
                        ],
                        "blocking_reference_count": 0,
                        "audit_reference_count": 1,
                        "blocking_references_sample": [],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["canonicalization_state"], "canonical-raw-replacement-known")
            self.assertIn(
                "evidence/raw/external/security.threat-file-hash-logging/"
                "defender-cloud-demo-sample-metadata-20260325.json",
                item["canonical_replacement_candidates"],
            )
            self.assertIn("canonical evidence/raw metadata", item["retention_rationale"])
            self.assertNotIn(item, plan["retention_decision_queue"])
            self.assertEqual(plan["summary"]["active_cleanup_action_count"], 0)
            self.assertEqual(plan["summary"]["vm_rerun_required_count"], 0)

    def test_thread_dpc_staging_placeholder_has_runtime_summary_replacement(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md",
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-audit-trail-reference",
                        "recommended_action": "delete-after-review",
                        "replacement_artifacts": [
                            "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/"
                            "thread-dpc-enable-0-cpu3-runtime-summary.json"
                        ],
                        "blocking_reference_count": 0,
                        "audit_reference_count": 1,
                        "blocking_references_sample": [],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["canonicalization_state"], "canonical-raw-replacement-known")
            self.assertIn(
                "evidence/raw/procmon/thread-dpc-enable-vm-suite-20260324/"
                "thread-dpc-enable-0-cpu3-runtime-summary.json",
                item["canonical_replacement_candidates"],
            )
            self.assertIn("external ETL placeholder", item["retention_rationale"])
            self.assertNotIn(item, plan["retention_decision_queue"])
            self.assertEqual(plan["summary"]["active_cleanup_action_count"], 0)
            self.assertEqual(plan["summary"]["raw_trace_backfill_required_count"], 0)

    def test_mpengine_staging_sample_has_canonical_raw_replacement(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": (
                            "evidence/files/vm-tooling-staging/"
                            "defender-threat-file-hash-mpengine-1-20260325-100039"
                        ),
                        "category": "vm-tooling-staging-oldest-sample",
                        "cleanup_status": "retained-audit-trail-reference",
                        "recommended_action": "delete-after-review",
                        "replacement_artifacts": [
                            "evidence/raw/procmon/security.threat-file-hash-logging/"
                            "defender-threat-file-hash-mpengine-reboot-no-read-20260325.txt"
                        ],
                        "blocking_reference_count": 0,
                        "audit_reference_count": 1,
                        "blocking_references_sample": [],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["release_state"], "audit-only-retained")
            self.assertEqual(item["canonicalization_state"], "canonical-raw-replacement-known")
            self.assertIn(
                "evidence/raw/procmon/security.threat-file-hash-logging/"
                "defender-threat-file-hash-mpengine-reboot-no-read-20260325.txt",
                item["canonical_replacement_candidates"],
            )
            self.assertIn("MPENGINE no-read proof", item["retention_rationale"])

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
                "retention_decision_queue_count": 0,
                "release_state_counts": {"reference-migration-needed": 1},
                "decision_track_counts": {"reference-migration": 1},
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
            "retention_decision_queue": [],
            "retained_inventory": [
                {
                    "path": "old.json",
                    "release_state": "reference-migration-needed",
                    "decision_track": "reference-migration",
                    "category": "old-dated-audit-output-sample",
                    "blocking_reference_count": 1,
                    "next_action": "Move refs.",
                }
            ],
        }

        markdown = self.module.render_markdown(plan)

        self.assertIn("## Reference Migration Queue", markdown)
        self.assertIn("## Retention Decision Queue", markdown)
        self.assertIn("## Decision Tracks", markdown)
        self.assertIn("## Retained Inventory Worklist", markdown)
        self.assertIn("old.json", markdown)
        self.assertIn("new.json", markdown)

    def test_raw_trace_needs_decision_gets_source_of_record_track(self):
        with tempfile.TemporaryDirectory() as temp:
            ledger = Path(temp) / "cleanup-quarantine-ledger.json"
            write_ledger(
                ledger,
                [
                    {
                        "path": "evidence/raw/procmon/example/source.pml",
                        "category": "large-raw-trace-sample",
                        "cleanup_status": "retained-live-reference",
                        "recommended_action": "keep-pending-review",
                        "blocking_reference_count": 2,
                        "blocking_references_sample": [
                            "research/evidence-index.json",
                            "research/records/example.json",
                        ],
                    }
                ],
            )

            plan = self.module.build_plan(ledger)
            item = plan["retained_inventory"][0]

            self.assertEqual(item["release_state"], "source-of-record-retained")
            self.assertEqual(item["decision_track"], "raw-trace-source-of-record")
            self.assertEqual(item["retention_owner"], "evidence")
            self.assertIn("technical-evidence-only", item["app_surface_policy"])
            self.assertNotIn(item, plan["retention_decision_queue"])
            self.assertEqual(plan["summary"]["source_of_record_retained_count"], 1)
            self.assertEqual(plan["summary"]["retention_decision_queue_count"], 0)
            self.assertEqual(plan["summary"]["decision_track_counts"], {"raw-trace-source-of-record": 1})


if __name__ == "__main__":
    unittest.main()
