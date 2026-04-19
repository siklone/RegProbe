from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-pipeline.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


pipeline = load_module("power_request_override_pipeline_runner", SCRIPT_PATH)


class PowerRequestOverridePipelineRunnerTests(unittest.TestCase):
    def test_artifact_paths_follow_bridge_naming(self) -> None:
        upload_dir = Path("/tmp/regprobe-bridge")
        paths = pipeline.artifact_paths(upload_dir, "local-kd-powerrequest-response-reacquire-20260419a")

        self.assertEqual(
            paths["stdout"].as_posix(),
            "/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a.stdout.txt",
        )
        self.assertEqual(
            paths["summary"].as_posix(),
            "/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a-summary.json",
        )


if __name__ == "__main__":
    unittest.main()
