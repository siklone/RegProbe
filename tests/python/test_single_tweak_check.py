from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "registry-research-framework" / "scripts"
if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


single_tweak_check = load_module(
    "check_single_tweak",
    SCRIPTS_ROOT / "check_single_tweak.py",
)


class SingleTweakCheckTests(unittest.TestCase):
    def test_real_repo_matches_system_responsiveness_and_expected_values(self) -> None:
        report = single_tweak_check.build_single_tweak_report(
            "SystemResponsiveness",
            expected_values=["10", "30000"],
            repo_root=REPO_ROOT,
        )

        self.assertEqual(report["status"], "ok")
        self.assertGreater(report["match_count"], 0)
        first_match = report["matches"][0]
        self.assertEqual(first_match["record_id"], "power.disable-network-power-saving.policy")
        self.assertEqual(first_match["promotion_state"], "promoted")
        self.assertTrue(first_match["app_surface_entry"]["present"])

        expected_checks = {item["expected_value"]: item for item in first_match["expected_value_checks"]}
        self.assertIn("10", expected_checks)
        self.assertIn("30000", expected_checks)
        self.assertTrue(expected_checks["10"]["found_any"])
        self.assertFalse(expected_checks["30000"]["found_any"])

    def test_temp_repo_matches_value_name_and_app_write(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            (repo_root / "research" / "records").mkdir(parents=True)
            (repo_root / "Docs" / "tweaks").mkdir(parents=True)
            (repo_root / "Docs" / "research" / "app-surface").mkdir(parents=True)

            (repo_root / "research" / "promotion-gates.json").write_text(
                """{
  "entries": [
    {
      "candidate_id": "system.test-setting",
      "record_id": "system.test-setting",
      "tweak_id": "system.test-setting",
      "promotion_state": "promoted",
      "record_promotion_allowed": true,
      "app_mapping_status": "matches-research"
    }
  ]
}
""",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "tweaks" / "tweak-catalog.csv").write_text(
                "\n".join(
                    [
                        "id,name,description,risk,category,area,source,docs",
                        "system.test-setting,System Test Setting,Tracks a synthetic registry value for tests.,Safe,System,Registry,app/Services/TweakProviders/SystemTweakProvider.cs#L10,Docs/system/system.md",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json").write_text(
                """{
  "categories": {
    "system": {
      "name": "System",
      "entries": [
        {
          "id": "system.test-setting",
          "name": "System Test Setting",
          "description": "Synthetic surface entry for tests.",
          "documentation": "research/records/system.test-setting.json",
          "verified": true,
          "batch_entries": [
            {
              "path": "HKLM\\\\Software\\\\RegProbe",
              "value_name": "SystemResponsiveness",
              "type": "REG_DWORD",
              "target_value": 10
            }
          ]
        }
      ]
    }
  }
}
""",
                encoding="utf-8",
            )
            (repo_root / "research" / "records" / "system.test-setting.json").write_text(
                """{
  "record_id": "system.test-setting",
  "tweak_id": "system.test-setting",
  "record_status": "validated",
  "summary": "Synthetic record for single tweak inspection tests.",
  "setting": {
    "name": "System Test Setting",
    "targets": [
      {
        "target_id": "systemresponsiveness",
        "path": "HKLM\\\\Software\\\\RegProbe",
        "value_name": "SystemResponsiveness",
        "value_type": "REG_DWORD",
        "allowed_values": [
          {
            "value": 10,
            "label": "Expected value"
          }
        ]
      }
    ]
  },
  "app_current_implementation": {
    "status": "matches-research",
    "provider_source": "app/Services/TweakProviders/SystemTweakProvider.cs",
    "writes": [
      {
        "target_id": "systemresponsiveness",
        "path": "HKLM\\\\Software\\\\RegProbe",
        "value_name": "SystemResponsiveness",
        "value_type": "REG_DWORD",
        "value": 10
      }
    ]
  },
  "decision": {
    "apply_allowed": true,
    "restore_default_supported": true,
    "restore_previous_supported": true
  },
  "validation_proof": {
    "exact_quote_or_path": "HKLM\\\\Software\\\\RegProbe\\\\SystemResponsiveness = 10"
  },
  "evidence": [
    {
      "evidence_id": "synthetic-runtime",
      "kind": "vm-test",
      "title": "Synthetic runtime signal",
      "location": "evidence/files/synthetic.txt",
      "summary": "Synthetic runtime hit for SystemResponsiveness."
    }
  ]
}
""",
                encoding="utf-8",
            )

            report = single_tweak_check.build_single_tweak_report(
                "SystemResponsiveness",
                expected_values=["10"],
                repo_root=repo_root,
            )

            self.assertEqual(report["status"], "ok")
            self.assertEqual(report["match_count"], 1)
            match = report["matches"][0]
            self.assertEqual(match["record_id"], "system.test-setting")
            self.assertTrue(match["app_surface_entry"]["present"])
            self.assertEqual(len(match["runtime_read_signals"]), 1)
            self.assertTrue(match["expected_value_checks"][0]["found_any"])

    def test_report_returns_no_match_for_unknown_query(self) -> None:
        report = single_tweak_check.build_single_tweak_report(
            "definitely-not-a-real-regprobe-token",
            repo_root=REPO_ROOT,
        )

        self.assertEqual(report["status"], "no-match")
        self.assertEqual(report["match_count"], 0)
        self.assertEqual(report["matches"], [])


if __name__ == "__main__":
    unittest.main()
