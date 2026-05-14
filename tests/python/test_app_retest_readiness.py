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


app_retest_readiness = load_module(
    "check_app_retest_readiness",
    FRAMEWORK_SCRIPTS / "check_app_retest_readiness.py",
)


class AppRetestReadinessTests(unittest.TestCase):
    def test_real_repo_retest_readiness_passes(self) -> None:
        report = app_retest_readiness.build_app_retest_readiness_report(REPO_ROOT)

        self.assertEqual(report["check_status"], "PASS")
        self.assertEqual(report["reports"]["kvm"]["summary"]["app_smoke_status"], "ok")
        self.assertEqual(report["reports"]["kvm"]["summary"]["contributor_lab_smoke_status"], "ok")
        self.assertIn("--contributor-lab", report["reports"]["kvm"]["summary"]["contributor_lab_app_args"])
        self.assertEqual(report["reports"]["kvm"]["summary"]["lane_health_status"], "ok")
        self.assertEqual(report["summary"]["app_only_tweak_count"], 0)
        self.assertEqual(report["summary"]["missing_rollback_story_count"], 0)

    def test_extract_markdown_table_parses_summary_rows(self) -> None:
        markdown = """# Title

## Summary

| Field | Value |
| --- | --- |
| Total records | 3 |
| Validated | 2 |

## Other
"""

        rows = app_retest_readiness.extract_markdown_table(markdown, "## Summary")

        self.assertEqual(
            rows,
            [
                {"Field": "Total records", "Value": "3"},
                {"Field": "Validated", "Value": "2"},
            ],
        )

    def test_evaluate_app_surface_flags_missing_gate_record_and_doc(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs" / "research" / "app-surface").mkdir(parents=True)
            (repo_root / "research" / "records").mkdir(parents=True)

            (repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json").write_text(
                json.dumps(
                    {
                        "categories": {
                            "system": {
                                "name": "System",
                                "entries": [
                                    {
                                        "id": "system.test-setting",
                                        "name": "System Test Setting",
                                        "documentation": "research/records/system.test-setting.review.json",
                                    }
                                ],
                            }
                        }
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "research" / "app-surface" / "app-only-catalog-tweaks.json").write_text(
                json.dumps({"tweaks": []}, indent=2) + "\n",
                encoding="utf-8",
            )

            report = app_retest_readiness.evaluate_app_surface(
                repo_root,
                record_by_id={},
                record_file_by_id={},
                gate_ids=set(),
            )

            self.assertEqual(report["status"], "FAIL")
            self.assertIn("system.test-setting", report["missing_gate_ids"])
            self.assertIn("system.test-setting", report["missing_record_ids"])
            self.assertIn("research/records/system.test-setting.review.json", report["missing_documentation_paths"])


if __name__ == "__main__":
    unittest.main()
