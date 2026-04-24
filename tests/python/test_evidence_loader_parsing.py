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


evidence_manifest = load_module(
    "evidence_manifest_parsing_tests",
    SCRIPTS_ROOT / "generate_evidence_manifest.py",
)
evidence_atlas = load_module(
    "evidence_atlas_parsing_tests",
    SCRIPTS_ROOT / "generate_evidence_atlas.py",
)
source_enrichment = load_module(
    "source_enrichment_parsing_tests",
    SCRIPTS_ROOT / "source_enrichment_scan.py",
)


class EvidenceLoaderParsingTests(unittest.TestCase):
    def non_object_json_path(self) -> Path:
        temp_root = tempfile.TemporaryDirectory(dir=REPO_ROOT)
        self.addCleanup(temp_root.cleanup)
        path = Path(temp_root.name) / "payload.json"
        path.write_text('["not","object"]', encoding="utf-8")
        return path

    def test_manifest_index_loader_rejects_non_object_payload(self) -> None:
        path = self.non_object_json_path()
        original_path = evidence_manifest.INDEX_PATH
        evidence_manifest.INDEX_PATH = path
        self.addCleanup(setattr, evidence_manifest, "INDEX_PATH", original_path)

        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            evidence_manifest.load_index()

    def test_atlas_index_loader_rejects_non_object_payload(self) -> None:
        path = self.non_object_json_path()
        original_path = evidence_atlas.EVIDENCE_INDEX_PATH
        evidence_atlas.EVIDENCE_INDEX_PATH = path
        self.addCleanup(setattr, evidence_atlas, "EVIDENCE_INDEX_PATH", original_path)

        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            evidence_atlas.load_index()

    def test_source_enrichment_loader_rejects_non_object_payload(self) -> None:
        with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
            source_enrichment.load_json(self.non_object_json_path())


if __name__ == "__main__":
    unittest.main()
