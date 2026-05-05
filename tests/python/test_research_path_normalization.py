from __future__ import annotations

import importlib.util
import sys
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


research_path_lib = load_module(
    "research_path_lib_for_tests",
    SCRIPTS_ROOT / "research_path_lib.py",
)


class ResearchPathNormalizationTests(unittest.TestCase):
    def test_evidence_raw_path_stays_under_raw_root(self) -> None:
        self.assertEqual(
            "evidence/raw/ghidra/allowremotedasd-kvm-20260406b/evidence.json",
            research_path_lib.normalize_repo_relative_path(
                "evidence/raw/ghidra/allowremotedasd-kvm-20260406b/evidence.json"
            ),
        )

    def test_evidence_capture_path_stays_under_capture_root(self) -> None:
        self.assertEqual(
            "evidence/captures/security-disable-vbs-etw-stackwalk-attempt-20260427.json",
            research_path_lib.normalize_repo_relative_path(
                "evidence/captures/security-disable-vbs-etw-stackwalk-attempt-20260427.json"
            ),
        )

    def test_record_local_file_still_routes_to_records_root(self) -> None:
        self.assertEqual(
            "evidence/records/system.io-allow-remote-dasd/full-evidence.json",
            research_path_lib.normalize_repo_relative_path(
                "evidence/system.io-allow-remote-dasd/full-evidence.json"
            ),
        )


if __name__ == "__main__":
    unittest.main()
