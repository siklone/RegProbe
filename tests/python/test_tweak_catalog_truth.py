from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


tweak_catalog_truth = load_module(
    "check_tweak_catalog_truth",
    SCRIPTS_ROOT / "check_tweak_catalog_truth.py",
)


class TweakCatalogTruthTests(unittest.TestCase):
    def test_real_catalog_passes(self) -> None:
        report = tweak_catalog_truth.build_tweak_catalog_truth_report(REPO_ROOT)

        self.assertEqual(report["check_status"], "PASS")
        self.assertEqual(report["description_violations"], [])
        self.assertEqual(report["template_violations"], [])

    def test_report_flags_claim_language_and_template_placeholders(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs" / "tweaks").mkdir(parents=True)
            (repo_root / "Docs" / "tweaks" / "tweak-catalog.csv").write_text(
                "\n".join(
                    [
                        "id,name,description,risk,category,area,source,docs",
                        "cleanup.eventlog-{logName.ToLowerInvariant()},Clear {logName} Event Log,Use this to fix event-log issues.,Advanced,Cleanup,Command,engine/Tweaks/Commands/Cleanup/ClearEventLogsTweak.cs#L15,Docs/cleanup/cleanup.md",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = tweak_catalog_truth.build_tweak_catalog_truth_report(repo_root)

            self.assertEqual(report["check_status"], "FAIL")
            self.assertEqual(len(report["description_violations"]), 1)
            self.assertEqual(len(report["template_violations"]), 2)
            self.assertTrue(any("description claim" in error for error in report["errors"]))
            self.assertTrue(any("template placeholder" in error for error in report["errors"]))


if __name__ == "__main__":
    unittest.main()
