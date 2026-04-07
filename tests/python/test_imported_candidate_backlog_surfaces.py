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


backlog_lib = load_module("imported_candidate_backlog_lib", SCRIPTS_ROOT / "imported_candidate_backlog_lib.py")
manifest_script = load_module("generate_evidence_manifest", SCRIPTS_ROOT / "generate_evidence_manifest.py")


class ImportedCandidateBacklogSurfaceTests(unittest.TestCase):
    def test_build_imported_candidate_backlog_summary_reads_canonical_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            backlog_path = Path(temp_root) / "imported-candidate-backlog.json"
            backlog_path.write_text(
                json.dumps(
                    {
                        "$schema": "registry-research-framework/schemas/imported-candidate-backlog.schema.json",
                        "schema_version": "1.0",
                        "generated_utc": "2026-04-07T14:36:19Z",
                        "backlog_type": "imported-candidates",
                        "source_import_root": "registry-research-framework/imported",
                        "source_queue_files": ["registry-research-framework/imported/run-a/candidate-queue.json"],
                        "source_run_count": 1,
                        "candidate_count": 2,
                        "import_count": 3,
                        "blocked_candidate_count": 2,
                        "counts_by_source_tool": {"osquery": 2, "regshot": 1},
                        "counts_by_confidence": {"Probable": 2, "Weak Lead": 1},
                        "counts_by_promotion_state": {"blocked": 3},
                        "entries": [],
                    }
                )
                + "\n",
                encoding="utf-8",
            )

            summary = backlog_lib.build_imported_candidate_backlog_summary(backlog_path)
            self.assertTrue(summary["exists"])
            self.assertEqual(summary["candidate_count"], 2)
            self.assertEqual(summary["import_count"], 3)
            self.assertEqual(summary["source_run_count"], 1)
            self.assertEqual(summary["blocked_candidate_count"], 2)
            self.assertEqual(summary["counts_by_source_tool"]["osquery"], 2)
            self.assertEqual(summary["counts_by_confidence"]["Probable"], 2)
            self.assertEqual(summary["counts_by_promotion_state"]["blocked"], 3)

    def test_render_md_emits_imported_candidate_backlog_rows(self) -> None:
        manifest = {
            "summary": {
                "total_records": 1,
                "validated": 1,
                "deprecated": 0,
                "review_required": 0,
                "records_with_evidence": 1,
                "records_without_evidence": 0,
                "records_missing_validation_proof": 0,
                "deprecated_missing_validation_proof": 0,
                "class_counts": {"A": 1},
                "imported_candidate_backlog": {
                    "exists": True,
                    "path": "research/imported-candidate-backlog.json",
                    "candidate_count": 4,
                    "import_count": 7,
                    "source_run_count": 3,
                    "blocked_candidate_count": 4,
                },
            },
            "records": [],
        }

        rendered = manifest_script.render_md(manifest)
        self.assertIn("Imported candidate backlog", rendered)
        self.assertIn("Imported candidate count", rendered)
        self.assertIn("Imported observation count", rendered)
        self.assertIn("Imported source run count", rendered)
        self.assertIn("Imported blocked candidate count", rendered)
        self.assertIn("research/imported-candidate-backlog.json", rendered)


if __name__ == "__main__":
    unittest.main()
