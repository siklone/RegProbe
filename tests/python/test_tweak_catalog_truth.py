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
        self.assertEqual(report["name_violations"], [])
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
                        "cleanup.eventlog-{logName.ToLowerInvariant()},Optimize Event Log,Use this to fix event-log issues.,Advanced,Cleanup,Command,engine/Tweaks/Commands/Cleanup/ClearEventLogsTweak.cs#L15,Docs/cleanup/cleanup.md",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = tweak_catalog_truth.build_tweak_catalog_truth_report(repo_root)

            self.assertEqual(report["check_status"], "FAIL")
            self.assertEqual(len(report["name_violations"]), 1)
            self.assertEqual(len(report["description_violations"]), 1)
            self.assertEqual(len(report["template_violations"]), 1)
            self.assertTrue(any("tweak name" in error for error in report["errors"]))
            self.assertTrue(any("description claim" in error for error in report["errors"]))
            self.assertTrue(any("template placeholder" in error for error in report["errors"]))

    def test_report_flags_prevention_and_resolution_language(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "Docs" / "tweaks").mkdir(parents=True)
            (repo_root / "Docs" / "tweaks" / "tweak-catalog.csv").write_text(
                "\n".join(
                    [
                        "id,name,description,risk,category,area,source,docs",
                        "power.disable-usb-selective-suspend,Disable USB Selective Suspend,Disables USB Selective Suspend to prevent USB devices from powering down unexpectedly. This can resolve issues with USB devices disconnecting.,Safe,Power,Command,engine/Tweaks/Commands/Power/DisableUsbSelectiveSuspendTweak.cs#L20,Docs/power/power.md",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = tweak_catalog_truth.build_tweak_catalog_truth_report(repo_root)

            self.assertEqual(report["check_status"], "FAIL")
            self.assertEqual(len(report["description_violations"]), 2)
            patterns = {entry["pattern"] for entry in report["description_violations"]}
            self.assertIn(r"\bcan resolve\b", patterns)
            self.assertIn(r"\bto prevent\b", patterns)


if __name__ == "__main__":
    unittest.main()
