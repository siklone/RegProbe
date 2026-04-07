from __future__ import annotations

import importlib.util
import json
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
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


external_import = load_module("external_evidence_import_lib", SCRIPTS_ROOT / "external_evidence_import_lib.py")


class ExternalEvidenceImportTests(unittest.TestCase):
    def test_osquery_json_import_materializes_candidate_queue_and_seed(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_root_path = Path(temp_root)
            input_path = temp_root_path / "osquery-registry.json"
            input_path.write_text(
                json.dumps(
                    [
                        {
                            "path": r"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                            "name": "HideRecommendedSection",
                            "type": "REG_DWORD",
                            "data": "1",
                        }
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            bundle = external_import.import_external_evidence(input_path, source_tool="osquery", run_id="external-osquery-test")
            outputs = external_import.materialize_external_research_artifacts(bundle, temp_root_path / "materialized")

            self.assertEqual(bundle["source_tool"], "osquery")
            self.assertEqual(bundle["observation_count"], 1)
            self.assertTrue(Path(outputs["bundle_path"]).exists())
            self.assertTrue(Path(outputs["normalized_bundle_path"]).exists())
            self.assertTrue(Path(outputs["candidate_queue"]).exists())

            queue_text = Path(outputs["candidate_queue"]).read_text(encoding="utf-8")
            self.assertIn("HideRecommendedSection", queue_text)
            self.assertIn("external-evidence-bundle.json", queue_text)
            self.assertIn("normalized-registry-bundle.json", queue_text)

            normalized_bundle = json.loads(Path(outputs["normalized_bundle_path"]).read_text(encoding="utf-8"))
            self.assertEqual(normalized_bundle["source_tool"], "imported")
            self.assertEqual(normalized_bundle["capture_phase"], "runtime")
            self.assertEqual(normalized_bundle["event_count"], 1)
            self.assertEqual(normalized_bundle["events"][0]["operation"], "imported-observation")
            self.assertEqual(normalized_bundle["events"][0]["hive"], "HKLM")
            self.assertEqual(normalized_bundle["events"][0]["key_path"], r"SOFTWARE\Policies\Microsoft\Windows\Explorer")
            self.assertEqual(normalized_bundle["events"][0]["value_name"], "HideRecommendedSection")

            seed_root = Path(outputs["record_seed_root"])
            seeds = list(seed_root.glob("*.json"))
            self.assertEqual(len(seeds), 1)
            seed_payload = json.loads(seeds[0].read_text(encoding="utf-8"))
            self.assertEqual(seed_payload["status"], "imported-seed")
            self.assertEqual(seed_payload["setting"]["targets"][0]["hive"], "HKLM")
            self.assertEqual(seed_payload["evidence"]["normalized_bundle_path"], outputs["normalized_bundle_path"])

    def test_materialize_external_research_artifacts_supports_split_bundle_root(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_root_path = Path(temp_root)
            bundle = {
                "$schema": "registry-research-framework/schemas/external-evidence-bundle.schema.json",
                "run_id": "external-split-root-test",
                "generated_utc": "2026-04-07T00:00:00Z",
                "source_tool": "osquery",
                "importer_name": "OsqueryRegistryImporter",
                "input_path": (temp_root_path / "input.json").as_posix(),
                "observation_count": 1,
                "observations": [
                    {
                        "candidate_id": "test-candidate",
                        "feature_area": "policy",
                        "source_tool": "osquery",
                        "key_path": r"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                        "value_name": "HideRecommendedSection",
                        "value_type": "REG_DWORD",
                        "observed_data": "1",
                        "recommended_value": None,
                        "rollback_value": None,
                        "trigger_action": None,
                        "required_privilege": None,
                        "confidence": "Probable",
                        "notes": None,
                        "evidence_refs": ["evidence/files/external/source.json"],
                    }
                ],
            }

            outputs = external_import.materialize_external_research_artifacts(
                bundle,
                temp_root_path / "imported",
                bundle_root=temp_root_path / "evidence-files",
            )

            self.assertTrue(Path(outputs["bundle_path"]).exists())
            self.assertTrue(Path(outputs["normalized_bundle_path"]).exists())
            self.assertTrue(Path(outputs["candidate_queue"]).exists())
            self.assertTrue(outputs["bundle_root"].endswith("/evidence-files"))
            self.assertTrue(outputs["artifact_root"].endswith("/imported"))

    def test_build_imported_candidate_backlog_groups_candidate_observations(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_root_path = Path(temp_root)

            bundle_one = {
                "$schema": "registry-research-framework/schemas/external-evidence-bundle.schema.json",
                "run_id": "external-osquery-one",
                "generated_utc": "2026-04-07T00:00:00Z",
                "source_tool": "osquery",
                "importer_name": "OsqueryRegistryImporter",
                "input_path": (temp_root_path / "input-one.json").as_posix(),
                "observation_count": 1,
                "observations": [
                    {
                        "candidate_id": "test-candidate",
                        "feature_area": "policy",
                        "source_tool": "osquery",
                        "key_path": r"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                        "value_name": "HideRecommendedSection",
                        "value_type": "REG_DWORD",
                        "observed_data": "1",
                        "recommended_value": None,
                        "rollback_value": None,
                        "trigger_action": None,
                        "required_privilege": None,
                        "confidence": "Probable",
                        "notes": None,
                        "evidence_refs": ["evidence/files/external/source-one.json"],
                    }
                ],
            }
            bundle_two = {
                "$schema": "registry-research-framework/schemas/external-evidence-bundle.schema.json",
                "run_id": "external-regshot-two",
                "generated_utc": "2026-04-07T00:10:00Z",
                "source_tool": "regshot",
                "importer_name": "RegshotImporter",
                "input_path": (temp_root_path / "input-two.txt").as_posix(),
                "observation_count": 1,
                "observations": [
                    {
                        "candidate_id": "test-candidate",
                        "feature_area": "policy",
                        "source_tool": "regshot",
                        "key_path": r"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                        "value_name": "HideRecommendedSection",
                        "value_type": "REG_DWORD",
                        "observed_data": "added line",
                        "recommended_value": None,
                        "rollback_value": None,
                        "trigger_action": None,
                        "required_privilege": None,
                        "confidence": "Weak Lead",
                        "notes": None,
                        "evidence_refs": ["evidence/files/external/source-two.txt"],
                    }
                ],
            }

            imported_root = temp_root_path / "imported"
            evidence_root = temp_root_path / "evidence"
            external_import.materialize_external_research_artifacts(
                bundle_one,
                imported_root / bundle_one["run_id"],
                bundle_root=evidence_root / bundle_one["run_id"],
            )
            external_import.materialize_external_research_artifacts(
                bundle_two,
                imported_root / bundle_two["run_id"],
                bundle_root=evidence_root / bundle_two["run_id"],
            )

            backlog = external_import.build_imported_candidate_backlog(imported_root)

            self.assertEqual(backlog["candidate_count"], 1)
            self.assertEqual(backlog["import_count"], 2)
            self.assertEqual(backlog["source_run_count"], 2)
            self.assertEqual(
                backlog["$schema"],
                "registry-research-framework/schemas/imported-candidate-backlog.schema.json",
            )
            self.assertEqual(backlog["counts_by_source_tool"]["osquery"], 1)
            self.assertEqual(backlog["counts_by_source_tool"]["regshot"], 1)
            self.assertEqual(backlog["entries"][0]["candidate_id"], "test-candidate")
            self.assertEqual(backlog["entries"][0]["highest_confidence"], "Probable")
            self.assertEqual(backlog["entries"][0]["import_count"], 2)
            self.assertEqual(len(backlog["entries"][0]["normalized_bundle_paths"]), 2)


if __name__ == "__main__":
    unittest.main()
