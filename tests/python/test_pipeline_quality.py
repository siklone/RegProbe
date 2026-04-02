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


artifact_metadata_lib = load_module("artifact_metadata_lib", SCRIPTS_ROOT / "artifact_metadata_lib.py")
v31_pipeline = load_module("v31_pipeline", REPO_ROOT / "registry-research-framework" / "pipeline" / "v31_pipeline.py")


class ArtifactMetadataTests(unittest.TestCase):
    def test_build_artifact_metadata_includes_hash_size_and_timestamp(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            artifact_path = Path(temp_root) / "artifact.json"
            artifact_path.write_text('{"ok":true}\n', encoding="utf-8")
            repo_ref = artifact_path.relative_to(REPO_ROOT).as_posix()

            payload = artifact_metadata_lib.build_artifact_metadata(REPO_ROOT, repo_ref)

            self.assertEqual(payload["path"], repo_ref)
            self.assertTrue(payload["exists"])
            self.assertEqual(payload["size"], artifact_path.stat().st_size)
            self.assertRegex(payload["sha256"], r"^[0-9a-f]{64}$")
            self.assertTrue(str(payload["collected_utc"]).endswith("Z"))


class PipelineCaptureStatusTests(unittest.TestCase):
    def test_lane_capture_status_marks_staged_without_capture(self) -> None:
        status = v31_pipeline.lane_capture_status({"status": "staged", "capture_status": "staged"})
        self.assertEqual(status, "staged-without-capture")

    def test_lane_capture_status_marks_placeholder_only_as_missing_capture(self) -> None:
        manifest = {
            "status": "runner-ok",
            "capture_artifacts": [
                {"path": "evidence/files/test-placeholder.etl.md", "exists": True, "placeholder": True}
            ],
        }
        self.assertEqual(v31_pipeline.lane_capture_status(manifest), "missing-capture")

    def test_lane_capture_status_marks_physical_json_as_runner_ok(self) -> None:
        evidence_root = REPO_ROOT / "evidence" / "files"
        with tempfile.TemporaryDirectory(dir=evidence_root) as temp_root:
            artifact_path = Path(temp_root) / "capture.json"
            artifact_path.write_text('{"captured":true}\n', encoding="utf-8")
            repo_ref = artifact_path.relative_to(REPO_ROOT).as_posix()

            manifest = {"status": "runner-ok", "capture_artifacts": [{"path": repo_ref}]}
            self.assertEqual(v31_pipeline.lane_capture_status(manifest), "runner-ok")

    def test_runner_required_for_kernel_lane(self) -> None:
        self.assertTrue(v31_pipeline.runner_required({"suspected_layer": "kernel", "boot_phase_relevant": False}))
        self.assertTrue(v31_pipeline.runner_required({"suspected_layer": "user-mode", "boot_phase_relevant": True}))
        self.assertFalse(v31_pipeline.runner_required({"suspected_layer": "user-mode", "boot_phase_relevant": False}))


if __name__ == "__main__":
    unittest.main()
