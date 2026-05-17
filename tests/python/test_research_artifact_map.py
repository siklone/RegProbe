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
    / "generate_research_artifact_map.py"
)


def load_module():
    spec = importlib.util.spec_from_file_location("generate_research_artifact_map_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def write_json(repo: Path, relative_path: str, payload: dict):
    path = repo / relative_path
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload), encoding="utf-8")


def seed_clean_artifacts(repo: Path):
    write_json(
        repo,
        "registry-research-framework/audit/app-retest-readiness-latest.json",
        {
            "check_status": "PASS",
            "summary": {
                "app_surface_entry_count": 258,
                "apply_allowed_record_count": 258,
                "missing_rollback_story_count": 0,
                "kvm_app_smoke_status": "ok",
                "kvm_lane_health_status": "ok",
            },
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/app-card-evidence-contracts-latest.json",
        {"status": "PASS", "summary": {"candidate_count": 258, "pass_count": 258, "fail_count": 0}},
    )
    write_json(
        repo,
        "registry-research-framework/audit/promoted-app-qa-batch-latest.json",
        {"status": "PASS", "summary": {"planned_count": 14, "live_success_count": 14, "live_failure_count": 0}},
    )
    write_json(
        repo,
        "registry-research-framework/audit/promoted-app-qa-coverage-latest.json",
        {"summary": {"coverage_percent": 100.0, "uncovered_categories": {}}},
    )
    write_json(
        repo,
        "registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json",
        {
            "status": "ok",
            "summary": {
                "result_count": 2,
                "non_ok_count": 0,
                "noisy_result_count": 0,
                "host_noise_counts": {"ok": 2},
            },
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/operator96-app-surface-review-20260510.json",
        {
            "status": "PASS",
            "summary": {
                "record_count": 96,
                "ready_for_bounded_app_card": 0,
                "needs_low_noise_rerun": 0,
                "aggregate_surface_blocked": False,
            },
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/cleanup-quarantine-ledger-20260514.json",
        {
            "summary": {
                "total_items": 1,
                "delete_candidate_count": 0,
                "retained_inventory_count": 1,
                "referenced_count": 1,
                "blocking_referenced_count": 1,
                "audit_only_referenced_count": 0,
                "delete_eligible_count": 0,
            }
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/cleanup-retained-inventory-plan-20260514.json",
        {
            "summary": {
                "item_count": 1,
                "delete_ready_count": 0,
                "reference_migration_needed_count": 1,
                "active_cleanup_action_count": 1,
                "retention_decision_queue_count": 0,
                "audit_only_retained_count": 0,
                "intentional_reference_keep_count": 0,
                "source_of_record_retained_count": 0,
                "historical_audit_retained_count": 0,
                "archive_history_retained_count": 0,
                "vm_rerun_required_count": 0,
                "raw_trace_backfill_required_count": 0,
                "needs_replacement_or_retention_decision_count": 0,
                "retained_pending_review_count": 0,
                "release_state_counts": {"reference-migration-needed": 1},
                "decision_track_counts": {"reference-migration": 1},
            }
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/vm-health-check-latest.json",
        {"status": "ok", "guest_health": "stable", "failed_checks": []},
    )
    write_json(
        repo,
        "registry-research-framework/audit/kvm-app-publish-deploy-smoke-latest.json",
        {
            "status": "ok",
            "self_contained": True,
            "publish_returncode": 0,
            "deploy_smoke_returncode": 0,
            "guest_health": "stable",
        },
    )
    write_json(
        repo,
        "registry-research-framework/audit/kvm-app-contributor-lab-smoke-latest.json",
        {
            "status": "ok",
            "app_args": ["--contributor-lab"],
            "deploy_smoke_payload": {
                "smoke_payload": {
                    "status": "ok",
                    "new_crash_log_detected": False,
                }
            },
            "guest_health": "stable",
        },
    )


class ResearchArtifactMapTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_operator96_clean_but_not_app_card_ready_is_research_only_ok(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            seed_clean_artifacts(repo)

            payload = self.module.build_artifact_map(repo)
            by_id = {item["id"]: item for item in payload["artifacts"]}

            self.assertEqual(payload["summary"]["attention_count"], 0)
            self.assertEqual(payload["summary"]["custom_value_noisy_result_count"], 0)
            self.assertEqual(payload["summary"]["custom_value_normal_app_card_ready"], 0)
            self.assertEqual(payload["summary"]["custom_value_legacy_artifact_prefix"], "operator96")
            self.assertTrue(payload["summary"]["operator96_legacy_alias"])
            self.assertEqual(by_id["custom-value-low-noise-aggregate"]["status"], "ok")
            self.assertEqual(by_id["custom-value-app-surface-review"]["status"], "research-only-ok")
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["status"], "no-delete-eligible")
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["delete_candidate_count"], 0)
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["cleanup_candidate_count"], 0)
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["review_inventory_count"], 1)
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["retained_inventory_count"], 1)
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["retained_not_candidate_count"], 1)
            self.assertIn(
                "not deletion candidates",
                by_id["cleanup-quarantine-ledger"]["details"]["candidate_semantics"],
            )
            self.assertEqual(by_id["cleanup-quarantine-ledger"]["details"]["blocking_referenced_count"], 1)
            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["status"], "retained-plan-ready")
            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["details"]["delete_ready_count"], 0)
            self.assertEqual(
                by_id["cleanup-retained-inventory-plan"]["details"]["reference_migration_needed_count"], 1
            )
            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["details"]["audit_only_retained_count"], 0)
            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["details"]["active_cleanup_action_count"], 1)
            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["details"]["retention_decision_queue_count"], 0)
            self.assertEqual(
                by_id["cleanup-retained-inventory-plan"]["details"]["decision_track_counts"],
                {"reference-migration": 1},
            )
            self.assertEqual(payload["summary"]["cleanup_reference_migration_needed_count"], 1)
            self.assertEqual(payload["summary"]["cleanup_active_action_count"], 1)
            self.assertEqual(payload["summary"]["cleanup_retention_decision_queue_count"], 0)
            self.assertEqual(payload["summary"]["cleanup_audit_only_retained_count"], 0)
            self.assertEqual(by_id["kvm-contributor-lab-smoke"]["status"], "ok")
            self.assertIn("evidence/raw/**", payload["raw_parse_do_not_start_here"])

    def test_cleanup_delete_eligible_requires_attention(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            seed_clean_artifacts(repo)
            write_json(
                repo,
                "registry-research-framework/audit/cleanup-quarantine-ledger-20260514.json",
                {
                    "summary": {
                        "total_items": 1,
                        "delete_candidate_count": 1,
                        "retained_inventory_count": 0,
                        "referenced_count": 0,
                        "delete_eligible_count": 1,
                    }
                },
            )

            payload = self.module.build_artifact_map(repo)
            by_id = {item["id"]: item for item in payload["artifacts"]}

            self.assertEqual(by_id["cleanup-quarantine-ledger"]["status"], "review-delete-eligible")
            self.assertEqual(payload["summary"]["attention_count"], 1)

    def test_cleanup_retained_plan_delete_ready_requires_attention(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            seed_clean_artifacts(repo)
            write_json(
                repo,
                "registry-research-framework/audit/cleanup-retained-inventory-plan-20260514.json",
                {
                    "summary": {
                        "item_count": 1,
                        "delete_ready_count": 1,
                        "reference_migration_needed_count": 0,
                        "audit_only_retained_count": 0,
                    }
                },
            )

            payload = self.module.build_artifact_map(repo)
            by_id = {item["id"]: item for item in payload["artifacts"]}

            self.assertEqual(by_id["cleanup-retained-inventory-plan"]["status"], "review-delete-ready")
            self.assertEqual(payload["summary"]["attention_count"], 1)

    def test_noisy_operator96_aggregate_requires_attention(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            seed_clean_artifacts(repo)
            write_json(
                repo,
                "registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json",
                {
                    "status": "ok",
                    "summary": {
                        "result_count": 2,
                        "non_ok_count": 0,
                        "noisy_result_count": 1,
                        "host_noise_counts": {"ok": 1, "noisy": 1},
                    },
                },
            )

            payload = self.module.build_artifact_map(repo)
            by_id = {item["id"]: item for item in payload["artifacts"]}

            self.assertEqual(by_id["custom-value-low-noise-aggregate"]["status"], "attention")
            self.assertEqual(payload["summary"]["custom_value_noisy_result_count"], 1)
            self.assertEqual(payload["summary"]["attention_count"], 1)


if __name__ == "__main__":
    unittest.main()
