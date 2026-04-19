from __future__ import annotations

import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"

HANDOFF_INDEX_JSON = AUDIT_ROOT / "power-request-override-handoff-index-20260419.json"
EXECUTION_MANIFEST_JSON = AUDIT_ROOT / "power-request-override-reader-binding-execution-manifest-20260419.json"
REACQUIRE_PLAN_JSON = AUDIT_ROOT / "power-request-override-reader-binding-reacquire-plan-20260419.json"
REVIEW_RUBRIC_JSON = AUDIT_ROOT / "power-request-override-reader-binding-review-rubric-20260419.json"
RESULT_LEDGER_TEMPLATE_JSON = AUDIT_ROOT / "power-request-override-reader-binding-result-ledger-template-20260419.json"
RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-reacquire.py"
PIPELINE_RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-pipeline.py"
LEDGER_GENERATOR_PATH = (
    REPO_ROOT / "registry-research-framework" / "scripts" / "generate_power_request_override_result_ledger.py"
)
LEDGER_PROMOTER_PATH = (
    REPO_ROOT / "registry-research-framework" / "scripts" / "promote_power_request_override_result_ledger.py"
)
SCRIPT_CATALOG_MD = REPO_ROOT / "Docs" / "research" / "script-catalog.md"
GITIGNORE = REPO_ROOT / ".gitignore"


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


class PowerRequestOverrideHandoffBundleTests(unittest.TestCase):
    def test_handoff_index_paths_exist(self) -> None:
        payload = load_json(HANDOFF_INDEX_JSON)

        self.assertEqual(payload["record_id"], "power.control.power-request-override-subtree")
        read_order = payload.get("read_order") or []
        self.assertGreaterEqual(len(read_order), 10)

        for entry in read_order:
            rel = entry["path"]
            target = REPO_ROOT / rel
            self.assertTrue(target.exists(), rel)

    def test_execution_manifest_entries_match_command_files(self) -> None:
        manifest = load_json(EXECUTION_MANIFEST_JSON)

        self.assertEqual(manifest["status"], "ready")
        self.assertEqual(int(manifest["selected_count"]), 2)
        self.assertEqual(manifest["pipeline_runner"]["path"], "scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py")
        self.assertEqual(
            manifest["pipeline_runner"]["dry_run_example"],
            "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run",
        )
        self.assertTrue(PIPELINE_RUNNER_PATH.exists())
        self.assertEqual(manifest["runner"]["path"], "scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py")
        self.assertTrue(RUNNER_PATH.exists())
        self.assertEqual(
            manifest["promotion"]["promote_script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            manifest["promotion"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertIn(
            "--run-id power-request-override-reader-binding-reacquire",
            manifest["promotion"]["current_run_example"],
        )
        self.assertIn("gitignored", manifest["promotion"]["scratch_policy"])
        self.assertTrue(manifest["promotion"]["promote_dry_run_example"].endswith("--dry-run"))
        self.assertTrue(manifest["promotion"]["current_run_dry_run_example"].endswith("--dry-run"))
        self.assertTrue(
            manifest["promotion"]["preview_targets"]["target_json"].endswith(
                "power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json"
            )
        )
        self.assertIn("refuses to overwrite", manifest["promotion"]["overwrite_policy"])
        self.assertTrue(LEDGER_PROMOTER_PATH.exists())
        entries = manifest.get("entries") or []
        self.assertEqual(len(entries), 2)

        for entry in entries:
            command_file = REPO_ROOT / entry["command_file"]
            self.assertTrue(command_file.exists(), entry["command_file"])
            content = command_file.read_text(encoding="utf-8")
            for marker in entry.get("success_markers") or []:
                self.assertIn(marker, content)

    def test_reacquire_plan_references_existing_files(self) -> None:
        payload = load_json(REACQUIRE_PLAN_JSON)

        artifacts = payload.get("required_reacquire_artifacts") or []
        self.assertEqual(len(artifacts), 2)
        for artifact in artifacts:
            command_file = REPO_ROOT / artifact["command_file"]
            self.assertTrue(command_file.exists(), artifact["command_file"])
            self.assertEqual(artifact["must_include"], ["stdout.txt", "summary.json", "local-kd.txt"])

        review_inputs = (load_json(EXECUTION_MANIFEST_JSON).get("review_inputs") or [])
        for rel in review_inputs:
            self.assertTrue((REPO_ROOT / rel).exists(), rel)

    def test_handoff_index_includes_runner(self) -> None:
        payload = load_json(HANDOFF_INDEX_JSON)

        self.assertEqual(payload["pipeline_runner"]["path"], "scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py")
        self.assertEqual(
            payload["pipeline_runner"]["dry_run_example"],
            "python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run",
        )
        self.assertTrue(PIPELINE_RUNNER_PATH.exists())
        self.assertEqual(payload["runner"]["path"], "scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py")
        self.assertTrue(RUNNER_PATH.exists())
        self.assertEqual(
            payload["promotion"]["promote_script"],
            "registry-research-framework/scripts/promote_power_request_override_result_ledger.py",
        )
        self.assertEqual(
            payload["promotion"]["current_run_id"],
            "power-request-override-reader-binding-reacquire",
        )
        self.assertIn(
            "--run-id power-request-override-reader-binding-reacquire",
            payload["promotion"]["current_run_example"],
        )
        self.assertTrue(payload["promotion"]["promote_dry_run_example"].endswith("--dry-run"))
        self.assertTrue(payload["promotion"]["current_run_dry_run_example"].endswith("--dry-run"))
        self.assertTrue(
            payload["promotion"]["preview_targets"]["target_md"].endswith(
                "power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.md"
            )
        )
        self.assertIn("local-only", payload["promotion"]["note"])
        self.assertIn("refuses to overwrite", payload["promotion"]["overwrite_policy"])

    def test_script_catalog_mentions_power_request_override_handoff_scripts(self) -> None:
        content = SCRIPT_CATALOG_MD.read_text(encoding="utf-8")

        self.assertIn("scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py", content)
        self.assertIn("scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py", content)
        self.assertIn("registry-research-framework/scripts/generate_power_request_override_result_ledger.py", content)
        self.assertIn("registry-research-framework/scripts/promote_power_request_override_result_ledger.py", content)
        self.assertTrue(LEDGER_GENERATOR_PATH.exists())
        self.assertTrue(LEDGER_PROMOTER_PATH.exists())

    def test_pipeline_autofill_outputs_are_local_only(self) -> None:
        content = GITIGNORE.read_text(encoding="utf-8")

        self.assertIn(
            "registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.json",
            content,
        )
        self.assertIn(
            "registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.md",
            content,
        )

    def test_review_rubric_and_result_template_stay_aligned(self) -> None:
        rubric = load_json(REVIEW_RUBRIC_JSON)
        ledger = load_json(RESULT_LEDGER_TEMPLATE_JSON)

        rubric_outcomes = {entry["outcome"] for entry in (rubric.get("outcome_mapping") or [])}
        ledger_outcome = ledger["fill_after_run"]["review_outcome"]["chosen_outcome"]

        self.assertIn("direct-registry-read", rubric_outcomes)
        self.assertIn("consumer-semantics-without-read", rubric_outcomes)
        self.assertIn("umpo-boundary-is-best-signal", rubric_outcomes)
        self.assertIn("wrapper-only-path", rubric_outcomes)
        self.assertEqual(
            ledger_outcome,
            "<direct-registry-read|consumer-semantics-without-read|umpo-boundary-is-best-signal|wrapper-only-path>",
        )


if __name__ == "__main__":
    unittest.main()
