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
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


v31_pipeline = load_module("v31_pipeline", REPO_ROOT / "registry-research-framework" / "pipeline" / "v31_pipeline.py")


class NormalizedPipelineFieldTests(unittest.TestCase):
    def test_lane_capture_status_marks_normalization_error(self) -> None:
        manifest = {
            "status": "runner-ok",
            "normalization_status": "error",
            "normalized_result_ref": "evidence/files/test/normalized.json",
        }
        self.assertEqual(v31_pipeline.lane_capture_status(manifest), "normalization-error")

    def test_lane_capture_status_marks_capture_without_normalization(self) -> None:
        evidence_root = REPO_ROOT / "evidence" / "files"
        with tempfile.TemporaryDirectory(dir=evidence_root) as temp_root:
            artifact_path = Path(temp_root) / "capture.csv"
            artifact_path.write_text("ok\n", encoding="utf-8")
            repo_ref = artifact_path.relative_to(REPO_ROOT).as_posix()
            manifest = {
                "status": "runner-ok",
                "normalization_status": "missing",
                "capture_artifacts": [{"path": repo_ref}],
            }
            self.assertEqual(v31_pipeline.lane_capture_status(manifest), "captured-without-normalization")

    def test_lane_capture_status_accepts_existing_normalized_bundle(self) -> None:
        evidence_root = REPO_ROOT / "evidence" / "files"
        with tempfile.TemporaryDirectory(dir=evidence_root) as temp_root:
            bundle_path = Path(temp_root) / "normalized.json"
            bundle_path.write_text('{"status":"ok"}\n', encoding="utf-8")
            repo_ref = bundle_path.relative_to(REPO_ROOT).as_posix()
            manifest = {
                "status": "runner-ok",
                "normalization_status": "ok",
                "normalized_result_ref": repo_ref,
            }
            self.assertEqual(v31_pipeline.lane_capture_status(manifest), "runner-ok")


if __name__ == "__main__":
    unittest.main()
