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
            (repo_root / "README.md").write_text(
                "# Repo\n\n## What RegProbe Does\n\n## Start Here\n\n`Tweaks` `Recovery` `Diagnostics`\n",
                encoding="utf-8",
            )
            (repo_root / "CONTRIBUTING.md").write_text("# Contributing\n", encoding="utf-8")
            (repo_root / "SECURITY.md").write_text("# Security\n", encoding="utf-8")
            (repo_root / "Docs" / "guide.md").write_text("# Guide\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "user-guide.md").write_text(
                "# User Guide\n\n`Tweaks` `Recovery` `Diagnostics`\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "product" / "cli.md").write_text("# CLI\n", encoding="utf-8")
            (repo_root / "Docs" / "product" / "support-matrix.md").write_text("# Support Matrix\n", encoding="utf-8")
            (repo_root / ".github" / "CODEOWNERS").write_text("* @owner\n", encoding="utf-8")
            (repo_root / ".github" / "PULL_REQUEST_TEMPLATE.md").write_text("## Summary\n", encoding="utf-8")
            for name in ("bug-report.yml", "feature-request.yml", "research-finding.yml"):
                (repo_root / ".github" / "ISSUE_TEMPLATE" / name).write_text("name: test\n", encoding="utf-8")
            (repo_root / ".github" / "workflows" / "dotnet.yml").write_text(
                "on:\n  push:\n    branches: [main]\n",
                encoding="utf-8",
            )

            report = public_repo_hygiene.build_public_repo_hygiene_report(repo_root)

            self.assertEqual(report["check_status"], "PASS")
            self.assertFalse(report["errors"])

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
            self.assertEqual(len(report["absolute_local_path_violations"]), 1)


if __name__ == "__main__":
    unittest.main()
