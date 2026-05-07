from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"
if str(FRAMEWORK_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(FRAMEWORK_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


promoted_app_qa_batch = load_module(
    "check_promoted_tweak_app_qa_batch",
    FRAMEWORK_SCRIPTS / "check_promoted_tweak_app_qa_batch.py",
)


class PromotedTweakAppQaBatchTests(unittest.TestCase):
    def test_real_repo_builds_a_batch_plan(self) -> None:
        report = promoted_app_qa_batch.build_report(
            repo_root=REPO_ROOT,
            tweak_ids=["power.disable-fast-startup", "power.disable-windows-search"],
            categories=[],
            limit_per_category=1,
            total_limit=4,
            run_live_kvm=False,
            wait_timeout=300,
        )

        self.assertEqual(report["status"], "PASS")
        self.assertEqual(report["selected_candidate_count"], 2)
        self.assertEqual(report["summary"]["planned_count"], 2)
        first = report["candidates"][0]
        self.assertIn("--qa-run-tweak", first["commands"]["direct_app"])
        self.assertIn("run-guest-app-tweak-qa-batch.py", first["commands"]["kvm_batch"])

    def test_real_repo_includes_legacy_tweak_ids_in_candidate_pool(self) -> None:
        candidates = promoted_app_qa_batch.collect_promoted_candidates(REPO_ROOT)
        tweak_ids = {item["tweak_id"] for item in candidates}
        self.assertIn("privacy.disable-diagnostic-data", tweak_ids)
        self.assertIn("privacy.disable-sync-settings", tweak_ids)

    def test_real_repo_legacy_batch_candidate_uses_runnable_app_id(self) -> None:
        report = promoted_app_qa_batch.build_report(
            repo_root=REPO_ROOT,
            tweak_ids=["privacy.disable-sync-settings"],
            categories=[],
            limit_per_category=1,
            total_limit=2,
            run_live_kvm=False,
            wait_timeout=300,
        )

        self.assertEqual(report["status"], "PASS")
        candidate = report["candidates"][0]
        self.assertEqual(candidate["tweak_id"], "privacy.disable-sync-settings")
        self.assertEqual(candidate["qa_tweak_id"], "privacy.turn-off-sync-by-default-allow-user-override")
        self.assertIn("privacy.turn-off-sync-by-default-allow-user-override", candidate["commands"]["direct_app"])

    def test_temp_repo_respects_explicit_id_selection(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "research" / "records").mkdir(parents=True)
            (repo_root / "Docs" / "tweaks").mkdir(parents=True)
            (repo_root / "Docs" / "research" / "app-surface").mkdir(parents=True)

            (repo_root / "research" / "promotion-gates.json").write_text(
                json.dumps(
                    {
                        "entries": [
                            {
                                "candidate_id": "system.alpha",
                                "record_id": "system.alpha",
                                "tweak_id": "system.alpha",
                                "promotion_state": "promoted",
                                "apply_allowed": True,
                                "app_mapping_status": "matches-research",
                                "rollback_status": {"rollback_verified": True},
                            },
                            {
                                "candidate_id": "privacy.beta",
                                "record_id": "privacy.beta",
                                "tweak_id": "privacy.beta",
                                "promotion_state": "promoted",
                                "apply_allowed": True,
                                "app_mapping_status": "matches-research",
                                "rollback_status": {"rollback_verified": True},
                            },
                        ]
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "tweaks" / "tweak-catalog.csv").write_text(
                "\n".join(
                    [
                        "id,name,description,risk,category,area,source,docs",
                        "system.alpha,System Alpha,Synthetic system tweak,Safe,System,Registry,app/Services/TweakProviders/SystemTweakProvider.cs#L10,Docs/system/system.md",
                        "privacy.beta,Privacy Beta,Synthetic privacy tweak,Safe,Privacy,Registry,app/Services/TweakProviders/PrivacyTweakProvider.cs#L10,Docs/privacy/privacy.md",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json").write_text(
                json.dumps(
                    {
                        "categories": {
                            "system": {
                                "name": "System",
                                "entries": [
                                    {
                                        "id": "system.alpha",
                                        "name": "System Alpha",
                                        "description": "Synthetic surface entry for tests.",
                                        "documentation": "research/records/system.alpha.json",
                                        "verified": True,
                                    }
                                ],
                            },
                            "privacy": {
                                "name": "Privacy",
                                "entries": [
                                    {
                                        "id": "privacy.beta",
                                        "name": "Privacy Beta",
                                        "description": "Synthetic privacy entry for tests.",
                                        "documentation": "research/records/privacy.beta.json",
                                        "verified": True,
                                    }
                                ],
                            },
                        },
                        "metadata": {},
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            for record_id in ("system.alpha", "privacy.beta"):
                (repo_root / "research" / "records" / f"{record_id}.json").write_text(
                    json.dumps(
                        {
                            "record_id": record_id,
                            "tweak_id": record_id,
                            "record_status": "validated",
                            "summary": "Synthetic record for batch planning tests.",
                            "setting": {
                                "name": record_id,
                                "targets": [
                                    {
                                        "target_id": "value",
                                        "path": r"HKCU\\Software\\RegProbe",
                                        "value_name": "Value",
                                        "value_type": "REG_DWORD",
                                        "allowed_values": [{"value": 1, "label": "Enabled"}],
                                    }
                                ],
                            },
                            "app_current_implementation": {
                                "status": "matches-research",
                                "provider_source": "app/Services/TweakProviders/Test.cs",
                                "writes": [
                                    {
                                        "target_id": "value",
                                        "path": r"HKCU\\Software\\RegProbe",
                                        "value_name": "Value",
                                        "value_type": "REG_DWORD",
                                        "value": 1,
                                    }
                                ],
                            },
                            "decision": {
                                "apply_allowed": True,
                                "restore_default_supported": True,
                                "restore_previous_supported": True,
                            },
                            "validation_proof": {
                                "exact_quote_or_path": r"HKCU\\Software\\RegProbe\\Value = 1"
                            },
                            "evidence": [],
                        },
                        indent=2,
                    )
                    + "\n",
                    encoding="utf-8",
                )

            report = promoted_app_qa_batch.build_report(
                repo_root=repo_root,
                tweak_ids=["privacy.beta"],
                categories=[],
                limit_per_category=1,
                total_limit=4,
                run_live_kvm=False,
                wait_timeout=300,
            )

            self.assertEqual(report["status"], "PASS")
            self.assertEqual(report["selected_candidate_count"], 1)
            self.assertEqual(report["candidates"][0]["tweak_id"], "privacy.beta")
            rendered = promoted_app_qa_batch.render_markdown(report)
            self.assertIn("card snapshot:", rendered)
            self.assertIn("claim_boundary=true", rendered)

    def test_write_artifacts_creates_history_and_coverage(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "research" / "records").mkdir(parents=True)
            (repo_root / "Docs" / "tweaks").mkdir(parents=True)
            (repo_root / "Docs" / "research" / "app-surface").mkdir(parents=True)
            audit_dir = repo_root / "registry-research-framework" / "audit"
            audit_dir.mkdir(parents=True)

            (repo_root / "research" / "promotion-gates.json").write_text(
                json.dumps(
                    {
                        "entries": [
                            {
                                "candidate_id": "system.alpha",
                                "record_id": "system.alpha",
                                "tweak_id": "system.alpha",
                                "promotion_state": "promoted",
                                "apply_allowed": True,
                                "app_mapping_status": "matches-research",
                                "rollback_status": {"rollback_verified": True},
                            },
                            {
                                "candidate_id": "privacy.beta",
                                "record_id": "privacy.beta",
                                "tweak_id": "privacy.beta",
                                "promotion_state": "promoted",
                                "apply_allowed": True,
                                "app_mapping_status": "matches-research",
                                "rollback_status": {"rollback_verified": True},
                            },
                        ]
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "tweaks" / "tweak-catalog.csv").write_text(
                "\n".join(
                    [
                        "id,name,description,risk,category,area,source,docs",
                        "system.alpha,System Alpha,Synthetic system tweak,Safe,System,Registry,app/Services/TweakProviders/SystemTweakProvider.cs#L10,research/records/system.alpha.json",
                        "privacy.beta,Privacy Beta,Synthetic privacy tweak,Safe,Privacy,Registry,app/Services/TweakProviders/PrivacyTweakProvider.cs#L10,research/records/privacy.beta.json",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json").write_text(
                json.dumps(
                    {
                        "categories": {
                            "system": {
                                "name": "System",
                                "entries": [
                                    {
                                        "id": "system.alpha",
                                        "name": "System Alpha",
                                        "description": "Synthetic surface entry for tests.",
                                        "documentation": "research/records/system.alpha.json",
                                        "verified": True,
                                    }
                                ],
                            },
                            "privacy": {
                                "name": "Privacy",
                                "entries": [
                                    {
                                        "id": "privacy.beta",
                                        "name": "Privacy Beta",
                                        "description": "Synthetic privacy entry for tests.",
                                        "documentation": "research/records/privacy.beta.json",
                                        "verified": True,
                                    }
                                ],
                            },
                        }
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            for record_id in ("system.alpha", "privacy.beta"):
                (repo_root / "research" / "records" / f"{record_id}.json").write_text(
                    json.dumps(
                        {
                            "record_id": record_id,
                            "tweak_id": record_id,
                            "record_status": "validated",
                            "summary": "Synthetic record for batch planning tests.",
                            "setting": {
                                "name": record_id,
                                "targets": [
                                    {
                                        "target_id": "value",
                                        "path": r"HKCU\\Software\\RegProbe",
                                        "value_name": "Value",
                                        "value_type": "REG_DWORD",
                                        "allowed_values": [{"value": 1, "label": "Enabled"}],
                                    }
                                ],
                            },
                            "app_current_implementation": {
                                "status": "matches-research",
                                "provider_source": "app/Services/TweakProviders/Test.cs",
                                "writes": [
                                    {
                                        "target_id": "value",
                                        "path": r"HKCU\\Software\\RegProbe",
                                        "value_name": "Value",
                                        "value_type": "REG_DWORD",
                                        "value": 1,
                                    }
                                ],
                            },
                            "decision": {
                                "apply_allowed": True,
                                "restore_default_supported": True,
                                "restore_previous_supported": True,
                            },
                            "validation_proof": {
                                "exact_quote_or_path": r"HKCU\\Software\\RegProbe\\Value = 1"
                            },
                            "evidence": [],
                        },
                        indent=2,
                    )
                    + "\n",
                    encoding="utf-8",
                )

            report = promoted_app_qa_batch.build_report(
                repo_root=repo_root,
                tweak_ids=["privacy.beta"],
                categories=[],
                limit_per_category=1,
                total_limit=4,
                run_live_kvm=False,
                wait_timeout=300,
            )
            report["run_results"] = [
                {
                    "tweak_id": "privacy.beta",
                    "qa_tweak_id": "privacy.beta",
                    "candidate_id": "privacy.beta",
                    "report_success": True,
                    "report_status": "ok",
                    "report_summary": "Synthetic success.",
                }
            ]
            report["summary"]["live_success_count"] = 1
            report["summary"]["live_failure_count"] = 0

            old_report_path = promoted_app_qa_batch.REPORT_PATH
            old_markdown_path = promoted_app_qa_batch.MARKDOWN_PATH
            old_history_path = promoted_app_qa_batch.HISTORY_PATH
            old_coverage_path = promoted_app_qa_batch.COVERAGE_PATH
            old_coverage_markdown_path = promoted_app_qa_batch.COVERAGE_MARKDOWN_PATH
            try:
                promoted_app_qa_batch.REPORT_PATH = audit_dir / "promoted-app-qa-batch-latest.json"
                promoted_app_qa_batch.MARKDOWN_PATH = audit_dir / "promoted-app-qa-batch-latest.md"
                promoted_app_qa_batch.HISTORY_PATH = audit_dir / "promoted-app-qa-batch-history.jsonl"
                promoted_app_qa_batch.COVERAGE_PATH = audit_dir / "promoted-app-qa-coverage-latest.json"
                promoted_app_qa_batch.COVERAGE_MARKDOWN_PATH = audit_dir / "promoted-app-qa-coverage-latest.md"
                promoted_app_qa_batch.write_artifacts(report, repo_root)
                promoted_app_qa_batch.write_artifacts(report, repo_root)
            finally:
                promoted_app_qa_batch.REPORT_PATH = old_report_path
                promoted_app_qa_batch.MARKDOWN_PATH = old_markdown_path
                promoted_app_qa_batch.HISTORY_PATH = old_history_path
                promoted_app_qa_batch.COVERAGE_PATH = old_coverage_path
                promoted_app_qa_batch.COVERAGE_MARKDOWN_PATH = old_coverage_markdown_path

            history_lines = (audit_dir / "promoted-app-qa-batch-history.jsonl").read_text(encoding="utf-8").splitlines()
            self.assertEqual(len(history_lines), 1)
            coverage = json.loads((audit_dir / "promoted-app-qa-coverage-latest.json").read_text(encoding="utf-8"))
            self.assertEqual(coverage["covered_count"], 1)
            self.assertEqual(coverage["uncovered_count"], 1)
            self.assertEqual(coverage["covered"][0]["tweak_id"], "privacy.beta")
            self.assertEqual(coverage["recommended_next_batches"][0]["category"], "System")
            self.assertEqual(coverage["recommended_next_batches"][0]["tweak_ids"], ["system.alpha"])
            self.assertIn("--id system.alpha", coverage["recommended_next_batches"][0]["command"])

            coverage_markdown = (audit_dir / "promoted-app-qa-coverage-latest.md").read_text(encoding="utf-8")
            self.assertIn("## Recommended Next Batches", coverage_markdown)
            self.assertIn("research qa-batch --id system.alpha", coverage_markdown)

    def test_coverage_markdown_has_placeholders_when_everything_is_covered(self) -> None:
        coverage = {
            "generated_utc": "2026-05-07T00:00:00Z",
            "history_entry_count": 1,
            "catalog_candidate_count": 1,
            "covered_count": 1,
            "uncovered_count": 0,
            "summary": {
                "coverage_percent": 100.0,
                "covered_categories": {"System": 1},
                "uncovered_categories": {},
            },
            "recommended_next_batches": [],
            "covered": [{"tweak_id": "system.alpha"}],
            "uncovered": [],
        }

        markdown = promoted_app_qa_batch.render_coverage_markdown(coverage)

        self.assertIn("## Uncovered Categories\n\n- No uncovered promoted app-QA candidates remain.", markdown)
        self.assertTrue(markdown.endswith("- No uncovered promoted app-QA candidates remain.\n"))
        self.assertNotIn("\n\n\n", markdown)


if __name__ == "__main__":
    unittest.main()
