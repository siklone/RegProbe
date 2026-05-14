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
    def app_backed_record(self, status: str = "ok-gated-override", summary: str | None = None) -> dict:
        return {
            "record_id": "example.app-qa",
            "tweak_id": "example.app-qa",
            "record_status": "validated",
            "setting": {
                "area": "Example",
                "targets": [
                    {
                        "path": "HKCU\\Software\\Example",
                        "value_name": "Enabled",
                        "value_type": "REG_DWORD",
                    }
                ],
            },
            "decision": {
                "apply_allowed": True,
                "confidence": "high",
                "restore_previous_supported": True,
                "needs_vm_validation": False,
                "blocking_issues": [],
            },
            "app_current_implementation": {
                "status": "matches-research",
            },
            "validation_proof": {
                "source_url": "Docs/example.md",
                "exact_quote_or_path": "Docs/example.md:1",
                "key_found_on_page": True,
            },
            "evidence": [
                {
                    "evidence_id": "example-app-qa-20260507",
                    "kind": "vm-test",
                    "title": "RegProbe app QA for example.app-qa",
                    "location": "evidence/captures/example-app-qa-20260507.json",
                    "summary": summary
                    or f"Status {status}: QA-only gated mutation override used; apply/verify path completed and rollback restored the tweak.",
                    "supports": ["value", "behavior", "side-effects", "ui-mapping"],
                    "strength": "high",
                },
                {
                    "evidence_id": "repo-code",
                    "kind": "repo-code",
                    "title": "Current engine implementation",
                    "location": "engine/Tweaks/Example.cs",
                    "summary": "The app provider and engine write the documented value.",
                    "supports": ["value", "behavior", "ui-mapping"],
                    "strength": "high",
                },
            ],
        }

    def test_promoted_decision_gated_record_can_still_be_actionable(self) -> None:
        record = json.loads(
            (REPO_ROOT / "research" / "records" / "visibility.restore-classic-context-menu.review.json").read_text(encoding="utf-8-sig")
        )

        entry = evidence_class_lib.build_class_entry(record)

        self.assertEqual(entry["evidence_class"], "B")
        self.assertTrue(entry["is_actionable"])
        self.assertEqual(entry["action_state"], "actionable")
        self.assertIn("allows app apply and rollback", entry["gating_reason"])

    def test_app_qa_apply_verify_rollback_contract_closes_missing_layer(self) -> None:
        record = self.app_backed_record()

        self.assertTrue(evidence_class_lib.has_app_backed_runtime_contract(record))
        self.assertTrue(evidence_class_lib.has_converged_vm_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "none")

        entry = evidence_class_lib.build_class_entry(record)

        self.assertEqual(entry["evidence_class"], "A")
        self.assertTrue(entry["is_actionable"])
        self.assertEqual(entry["runtime_proof"]["links"][0]["kind"], "vm-test")

    def test_app_qa_already_applied_does_not_close_mutation_contract(self) -> None:
        record = self.app_backed_record(
            "already-applied",
            "Status already-applied: the tweak already matched the desired state; the app verified it and skipped rollback because no mutation was performed.",
        )

        self.assertFalse(evidence_class_lib.has_app_backed_runtime_contract(record))
        self.assertFalse(evidence_class_lib.has_converged_vm_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "runtime-trace")

    def test_app_qa_failure_does_not_close_mutation_contract(self) -> None:
        record = self.app_backed_record(
            "check-failed",
            "Status check-failed: reg.exe failed (1): ERROR: Access is denied.",
        )

        self.assertFalse(evidence_class_lib.has_app_backed_runtime_contract(record))
        self.assertFalse(evidence_class_lib.has_converged_vm_evidence(record))
        self.assertEqual(evidence_class_lib.next_missing_layer(record), "runtime-trace")


if __name__ == "__main__":
    unittest.main()
