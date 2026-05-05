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


single_tweak_app_qa = load_module(
    "check_single_tweak_app_qa",
    FRAMEWORK_SCRIPTS / "check_single_tweak_app_qa.py",
)


class SingleTweakAppQaTests(unittest.TestCase):
    def test_real_repo_generates_app_qa_plan_for_system_responsiveness(self) -> None:
        report = single_tweak_app_qa.build_single_tweak_app_qa_report(
            "SystemResponsiveness",
            expected_values=["10", "30000"],
            repo_root=REPO_ROOT,
        )

        self.assertEqual(report["status"], "ok")
        self.assertGreater(report["qa_candidate_count"], 0)
        candidate = report["candidates"][0]
        self.assertIn("--qa-run-tweak", candidate["commands"]["direct_app"])
        self.assertIn(candidate["tweak_id"], candidate["commands"]["direct_app"])
        self.assertIn("--qa-skip-rollback", candidate["commands"]["direct_app_skip_rollback"])
        self.assertIn("research readiness", candidate["commands"]["readiness"])
        self.assertTrue(candidate["card_expectations"]["documentation"].endswith(".json"))
        self.assertIn("Success=", single_tweak_app_qa.render_single_tweak_app_qa_report(report))

    def test_temp_repo_generates_mutation_blocked_plan(self) -> None:
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
                                "candidate_id": "system.test-setting",
                                "record_id": "system.test-setting",
                                "tweak_id": "system.test-setting",
                                "promotion_state": "blocked",
                                "record_promotion_allowed": False,
                                "app_mapping_status": "matches-research",
                            }
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
                        "system.test-setting,System Test Setting,Tracks a synthetic registry value for tests.,Safe,System,Registry,app/Services/TweakProviders/SystemTweakProvider.cs#L10,Docs/system/system.md",
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
                                        "id": "system.test-setting",
                                        "name": "System Test Setting",
                                        "description": "Synthetic surface entry for tests.",
                                        "documentation": "research/records/system.test-setting.json",
                                        "verified": True,
                                        "batch_entries": [
                                            {
                                                "path": r"HKLM\\Software\\RegProbe",
                                                "value_name": "SystemResponsiveness",
                                                "type": "REG_DWORD",
                                                "target_value": 10,
                                            }
                                        ],
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
            (repo_root / "research" / "records" / "system.test-setting.json").write_text(
                json.dumps(
                    {
                        "record_id": "system.test-setting",
                        "tweak_id": "system.test-setting",
                        "record_status": "validated",
                        "summary": "Synthetic record for app QA planning tests.",
                        "setting": {
                            "name": "System Test Setting",
                            "targets": [
                                {
                                    "target_id": "systemresponsiveness",
                                    "path": r"HKLM\\Software\\RegProbe",
                                    "value_name": "SystemResponsiveness",
                                    "value_type": "REG_DWORD",
                                    "allowed_values": [{"value": 10, "label": "Expected value"}],
                                }
                            ],
                        },
                        "app_current_implementation": {
                            "status": "matches-research",
                            "provider_source": "app/Services/TweakProviders/SystemTweakProvider.cs",
                            "writes": [
                                {
                                    "target_id": "systemresponsiveness",
                                    "path": r"HKLM\\Software\\RegProbe",
                                    "value_name": "SystemResponsiveness",
                                    "value_type": "REG_DWORD",
                                    "value": 10,
                                }
                            ],
                        },
                        "decision": {
                            "apply_allowed": False,
                            "restore_default_supported": True,
                            "restore_previous_supported": False,
                        },
                        "validation_proof": {
                            "exact_quote_or_path": r"HKLM\\Software\\RegProbe\\SystemResponsiveness = 10"
                        },
                        "evidence": [],
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            report = single_tweak_app_qa.build_single_tweak_app_qa_report(
                "SystemResponsiveness",
                expected_values=["10"],
                repo_root=repo_root,
            )

            self.assertEqual(report["status"], "ok")
            candidate = report["candidates"][0]
            self.assertFalse(candidate["apply_allowed"])
            self.assertEqual(candidate["expected_report"]["status"], "mutation-blocked")
            self.assertFalse(candidate["expected_report"]["success"])
            self.assertEqual(candidate["expected_report"]["required_stages"], ["detect-before"])


if __name__ == "__main__":
    unittest.main()
