from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


research_queue = load_module(
    "research_queue_parsing_tests",
    FRAMEWORK_SCRIPTS / "build_research_queue.py",
)


class ResearchQueueParsingTests(unittest.TestCase):
    def test_runtime_discovery_jsonl_rejects_non_object_line(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            discovery_events = root / "discovery-events.jsonl"
            discovery_events.write_text('["not","object"]\n', encoding="utf-8")

            original_events = research_queue.DISCOVERY_EVENTS_PATH
            original_snapshot = research_queue.ETL_REGISTRY_DISCOVERY_PATH
            research_queue.DISCOVERY_EVENTS_PATH = discovery_events
            research_queue.ETL_REGISTRY_DISCOVERY_PATH = root / "missing-snapshot.json"
            self.addCleanup(setattr, research_queue, "DISCOVERY_EVENTS_PATH", original_events)
            self.addCleanup(setattr, research_queue, "ETL_REGISTRY_DISCOVERY_PATH", original_snapshot)

            with self.assertRaisesRegex(ValueError, "JSONL payload is not an object"):
                research_queue.load_runtime_discovery_candidates()


if __name__ == "__main__":
    unittest.main()
