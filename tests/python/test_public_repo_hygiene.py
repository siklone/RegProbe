from __future__ import annotations

import importlib.util
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


public_repo_hygiene = load_module(
    "check_public_repo_hygiene",
    FRAMEWORK_SCRIPTS / "check_public_repo_hygiene.py",
)


class PublicRepoHygieneTests(unittest.TestCase):
    def test_clean_public_repo_surface_passes(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs").mkdir(parents=True)
            (repo_root / ".github" / "workflows").mkdir(parents=True)
            (repo_root / ".github" / "ISSUE_TEMPLATE").mkdir(parents=True)
            (repo_root / "Docs" / "product").mkdir(parents=True)
            (repo_root / "Docs" / "research").mkdir(parents=True)
            (repo_root / "README.md").write_text(
                "# Repo\n\n## What RegProbe Does\n\n## Start Here\n\n`Tweaks` `Recovery` `Diagnostics`\n\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\n\n## Entry Points\n",
                encoding="utf-8",
            )
            (repo_root / "CONTRIBUTING.md").write_text(
                "# Contributing\n\nDetect -> Apply -> Verify -> Rollback\nintegration coverage\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\nDocs/product/media.md\nDocs/product/cli.md\nDocs/product/support-matrix.md\n",
                encoding="utf-8",
            )
            (repo_root / "SECURITY.md").write_text("# Security\n", encoding="utf-8")
            (repo_root / "Docs" / "guide.md").write_text("# Guide\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "user-guide.md").write_text(
                "# User Guide\n\n`Tweaks` `Recovery` `Diagnostics`\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "product" / "cli.md").write_text("# CLI\n\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "support-matrix.md").write_text("# Support Matrix\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "media.md").write_text(
                "# Product Media\n\n## When To Refresh\n\ndo not merge a UI rename\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "SETTINGS_EXPANSION_REPORT_2026-03-09.md").write_text("# Report\n\nPossible additions only.\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_CONFIGURATION_AUDIT_2026-03-09.md").write_text("# Audit\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_CONFIGURATION_SOURCES.md").write_text("# Sources\n\n## Expansion Order\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_TRANCHE_EVALUATION_2026-03-09.md").write_text("# Tranche\n\n## 7. Immediate Backlog\n", encoding="utf-8")
            (repo_root / "Docs" / "research" / "how-to-read-a-record.md").write_text("# Read\n\n## Reading Order\n", encoding="utf-8")
            (repo_root / "Docs" / "security").mkdir(parents=True, exist_ok=True)
            (repo_root / "Docs" / "visibility").mkdir(parents=True, exist_ok=True)
            (repo_root / "Docs" / "security" / "use-case-guide.md").write_text("# Security guide\n\n| Value | Scenario value | Notes |\n", encoding="utf-8")
            (repo_root / "Docs" / "visibility" / "use-case-guide.md").write_text("# Visibility guide\n\nExample fonts:\n", encoding="utf-8")
            (repo_root / ".github" / "CODEOWNERS").write_text("* @owner\n", encoding="utf-8")
            (repo_root / ".github" / "PULL_REQUEST_TEMPLATE.md").write_text(
                "## Summary\n\n## SAFE Flow Impact\n\nintegration coverage should be updated\nScreenshot or media lane updated if UI changed\nSupport matrix or release docs updated if package contract changed\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "bug-report.yml").write_text(
                "Tweaks\nRecovery\nDiagnostics\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "feature-request.yml").write_text(
                "Tweaks\nRecovery\nDiagnostics\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "research-finding.yml").write_text("name: test\n", encoding="utf-8")
            (repo_root / ".github" / "workflows" / "dotnet.yml").write_text(
                "on:\n  push:\n    branches: [main]\n",
                encoding="utf-8",
            )

            report = public_repo_hygiene.build_public_repo_hygiene_report(repo_root)

            self.assertEqual(report["check_status"], "PASS")
            self.assertFalse(report["errors"])
            self.assertEqual(report["comparative_prose_violations"], [])

    def test_report_flags_missing_security_absolute_paths_and_placeholder_test(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs").mkdir(parents=True)
            (repo_root / ".github" / "workflows").mkdir(parents=True)
            (repo_root / "tests").mkdir(parents=True)
            (repo_root / "README.md").write_text("# Repo\n", encoding="utf-8")
            (repo_root / "CONTRIBUTING.md").write_text(
                "# Contributing\n\nSee [bad](H:/D/Dev/RegProbe/research/file.json)\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "guide.md").write_text("# Guide\n", encoding="utf-8")
            (repo_root / ".github" / "workflows" / "dotnet.yml").write_text(
                "on:\n  push:\n    branches: [main, develop]\n",
                encoding="utf-8",
            )
            (repo_root / "tests" / "UnitTest1.cs").write_text("// placeholder\n", encoding="utf-8")

            report = public_repo_hygiene.build_public_repo_hygiene_report(repo_root)

            self.assertEqual(report["check_status"], "FAIL")
            self.assertTrue(any("SECURITY.md is missing" in error for error in report["errors"]))
            self.assertTrue(any("product-first entry sections" in error for error in report["errors"]))
            self.assertTrue(any("main-only policy" in error for error in report["errors"]))
            self.assertTrue(any("UnitTest1.cs" in error for error in report["errors"]))
            self.assertTrue(any("issue templates" in error for error in report["errors"]))
            self.assertTrue(any("CODEOWNERS" in error for error in report["errors"]))
            self.assertTrue(any("Docs/product/cli.md" in error for error in report["errors"]))
            self.assertTrue(any("support-matrix.md" in error for error in report["errors"]))
            self.assertTrue(any("media.md is missing" in error for error in report["errors"]))
            self.assertTrue(any("README.md is missing the research inspect or research readiness examples" in error for error in report["errors"]))
            self.assertTrue(any("CONTRIBUTING.md is missing the research inspect or research readiness workflow" in error for error in report["errors"]))
            self.assertTrue(any("Docs/product/cli.md is missing the research inspect or research readiness command coverage" in error for error in report["errors"]))
            self.assertTrue(any("README.md is missing the research qa-plan workflow" in error for error in report["errors"]))
            self.assertTrue(any("CONTRIBUTING.md is missing the research qa-plan workflow" in error for error in report["errors"]))
            self.assertTrue(any("Docs/product/cli.md is missing the research qa-plan command coverage" in error for error in report["errors"]))
            self.assertTrue(any("README.md is missing the research qa-batch workflow" in error for error in report["errors"]))
            self.assertTrue(any("CONTRIBUTING.md is missing the research qa-batch workflow" in error for error in report["errors"]))
            self.assertTrue(any("Docs/product/cli.md is missing the research qa-batch command coverage" in error for error in report["errors"]))
            self.assertTrue(any("SAFE flow integration expectation" in error for error in report["errors"]))
            self.assertTrue(any("product media lane expectation" in error for error in report["errors"]))
            self.assertTrue(any("CLI docs update expectation" in error for error in report["errors"]))
            self.assertTrue(any("release/support-matrix update expectation" in error for error in report["errors"]))
            self.assertTrue(any("SAFE flow integration reminder" in error for error in report["errors"]))
            self.assertTrue(any("media or release contract checklist items" in error for error in report["errors"]))
            self.assertTrue(any("Issue templates drifted" in error for error in report["errors"]))
            self.assertTrue(any("media refresh or rename-drift rules" in error for error in report["errors"]))
            self.assertEqual(len(report["absolute_local_path_violations"]), 1)

    def test_finds_unpinned_github_action_references(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            workflow_dir = repo_root / ".github" / "workflows"
            workflow_dir.mkdir(parents=True)
            workflow_path = workflow_dir / "dotnet.yml"
            workflow_path.write_text(
                "\n".join(
                    [
                        "name: test",
                        "jobs:",
                        "  check:",
                        "    steps:",
                        "      - uses: actions/checkout@v6",
                        "      - uses: actions/setup-dotnet@c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7 # v5",
                        "      - uses: ./local-action",
                    ]
                ),
                encoding="utf-8",
            )

            violations = public_repo_hygiene.find_unpinned_workflow_actions([workflow_path], repo_root)

            self.assertEqual(len(violations), 1)
            self.assertEqual(violations[0]["action"], "actions/checkout@v6")
            self.assertEqual(violations[0]["reason"], "ref_is_not_full_commit_sha")

    def test_report_flags_comparative_public_prose_drift(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs").mkdir(parents=True)
            (repo_root / ".github" / "workflows").mkdir(parents=True)
            (repo_root / ".github" / "ISSUE_TEMPLATE").mkdir(parents=True)
            (repo_root / "Docs" / "product").mkdir(parents=True)
            (repo_root / "Docs" / "research").mkdir(parents=True)
            (repo_root / "README.md").write_text(
                "# Repo\n\n## What RegProbe Does\n\n## Start Here\n\n`Tweaks` `Recovery` `Diagnostics`\n\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\n\n## Useful Entry Points\n",
                encoding="utf-8",
            )
            (repo_root / "CONTRIBUTING.md").write_text(
                "# Contributing\n\nDetect -> Apply -> Verify -> Rollback\nintegration coverage\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\nDocs/product/media.md\nDocs/product/cli.md\nDocs/product/support-matrix.md\n",
                encoding="utf-8",
            )
            (repo_root / "SECURITY.md").write_text("# Security\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "user-guide.md").write_text(
                "# User Guide\n\n`Tweaks` `Recovery` `Diagnostics`\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "product" / "cli.md").write_text("# CLI\n\nresearch inspect\nresearch readiness\nresearch qa-plan\nresearch qa-batch\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "support-matrix.md").write_text("# Support Matrix\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "media.md").write_text(
                "# Product Media\n\n## When To Refresh\n\ndo not merge a UI rename\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "SETTINGS_EXPANSION_REPORT_2026-03-09.md").write_text("# Report\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_CONFIGURATION_AUDIT_2026-03-09.md").write_text("# Audit\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_CONFIGURATION_SOURCES.md").write_text("# Sources\n", encoding="utf-8")
            (repo_root / "Docs" / "UPSTREAM_TRANCHE_EVALUATION_2026-03-09.md").write_text("# Tranche\n", encoding="utf-8")
            (repo_root / "Docs" / "research" / "how-to-read-a-record.md").write_text("# Read\n", encoding="utf-8")
            (repo_root / "Docs" / "security").mkdir(parents=True, exist_ok=True)
            (repo_root / "Docs" / "visibility").mkdir(parents=True, exist_ok=True)
            (repo_root / "Docs" / "security" / "use-case-guide.md").write_text("# Security guide\n", encoding="utf-8")
            (repo_root / "Docs" / "visibility" / "use-case-guide.md").write_text("# Visibility guide\n", encoding="utf-8")
            (repo_root / ".github" / "CODEOWNERS").write_text("* @owner\n", encoding="utf-8")
            (repo_root / ".github" / "PULL_REQUEST_TEMPLATE.md").write_text(
                "## Summary\n\n## SAFE Flow Impact\n\nintegration coverage should be updated\nScreenshot or media lane updated if UI changed\nSupport matrix or release docs updated if package contract changed\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "bug-report.yml").write_text(
                "Tweaks\nRecovery\nDiagnostics\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "feature-request.yml").write_text(
                "Tweaks\nRecovery\nDiagnostics\n",
                encoding="utf-8",
            )
            (repo_root / ".github" / "ISSUE_TEMPLATE" / "research-finding.yml").write_text("name: test\n", encoding="utf-8")
            (repo_root / ".github" / "workflows" / "dotnet.yml").write_text(
                "on:\n  push:\n    branches: [main]\n",
                encoding="utf-8",
            )

            report = public_repo_hygiene.build_public_repo_hygiene_report(repo_root)

            self.assertEqual(report["check_status"], "FAIL")
            self.assertTrue(any("comparative repo-authored prose" in error for error in report["errors"]))
            self.assertEqual(len(report["comparative_prose_violations"]), 1)


if __name__ == "__main__":
    unittest.main()
