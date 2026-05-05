from __future__ import annotations

import importlib.util
import json
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
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


evidence_class_lib = load_module("evidence_class_lib_actionability", SCRIPTS_ROOT / "evidence_class_lib.py")


class EvidenceClassActionabilityTests(unittest.TestCase):
    def test_promoted_decision_gated_record_can_still_be_actionable(self) -> None:
        record = json.loads(
            (REPO_ROOT / "research" / "records" / "visibility.restore-classic-context-menu.review.json").read_text(encoding="utf-8-sig")
        )

        entry = evidence_class_lib.build_class_entry(record)

        self.assertEqual(entry["evidence_class"], "B")
        self.assertTrue(entry["is_actionable"])
        self.assertEqual(entry["action_state"], "actionable")
        self.assertIn("allows app apply and rollback", entry["gating_reason"])


if __name__ == "__main__":
    unittest.main()
