from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"

if str(SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_ROOT))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


audit_static = load_module(
    "audit_static_evidence_v32_parsing_tests",
    SCRIPTS_ROOT / "audit_static_evidence_v32.py",
)
evidence_index = load_module(
    "generate_evidence_index_parsing_tests",
    SCRIPTS_ROOT / "generate_evidence_index.py",
)
blocked_worklist = load_module(
    "check_blocked_worklist_parsing_tests",
    FRAMEWORK_SCRIPTS / "check_blocked_worklist.py",
)


class AuditResilienceParsingTests(unittest.TestCase):
    def test_link_review_overrides_non_object_payload_returns_empty_map(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "overrides.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")
            original = audit_static.LINK_REVIEW_OVERRIDES_PATH
            audit_static.LINK_REVIEW_OVERRIDES_PATH = path
            self.addCleanup(setattr, audit_static, "LINK_REVIEW_OVERRIDES_PATH", original)

            self.assertEqual(audit_static.load_link_review_overrides(), {})

    def test_extract_url_refs_non_object_payload_returns_empty_list(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "record.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")

            self.assertEqual(audit_static.extract_url_refs_from_json(path), [])

    def test_priority_record_non_object_payload_is_skipped(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            record_path = root / "example.json"
            record_path.write_text('["not","object"]', encoding="utf-8-sig")
            original_root = audit_static.RECORDS_ROOT
            audit_static.RECORDS_ROOT = root
            self.addCleanup(setattr, audit_static, "RECORDS_ROOT", original_root)

            self.assertEqual(audit_static.load_priority_record("example"), {})

    def test_v31_companion_non_object_payload_returns_none(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            record_root = root / "example"
            record_root.mkdir(parents=True)
            (record_root / "full-evidence.json").write_text('["not","object"]', encoding="utf-8-sig")
            original_root = evidence_index.V31_EVIDENCE_ROOT
            evidence_index.V31_EVIDENCE_ROOT = root
            self.addCleanup(setattr, evidence_index, "V31_EVIDENCE_ROOT", original_root)

            self.assertIsNone(evidence_index.load_v31_companion("example"))

    def test_blocked_worklist_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "blocked-worklist.json"
            path.write_text('["not","object"]', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                blocked_worklist.build_result(path)


if __name__ == "__main__":
    unittest.main()
