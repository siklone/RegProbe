from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
VM_KVM_SCRIPTS = REPO_ROOT / "scripts" / "vm-kvm"
if str(VM_KVM_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(VM_KVM_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


summary_contract = load_module("summary_contract_lib", VM_KVM_SCRIPTS / "summary_contract_lib.py")


class VmKvmSummaryContractTests(unittest.TestCase):
    def test_read_json_object_rejects_non_object_json(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            payload_path = Path(temp_root) / "payload.json"
            payload_path.write_text('["not","object"]\n', encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "payload JSON payload is not an object"):
                summary_contract.read_json_object(payload_path, context="payload")

    def test_apply_summary_contract_fills_ok_defaults(self) -> None:
        payload = summary_contract.apply_summary_contract({"status": "ok", "normalization_status": "ok"})
        self.assertIsNone(payload["error_kind"])
        self.assertEqual(payload["recovery_action"], "none")
        self.assertEqual(payload["transport_blocker"], "none")
        self.assertEqual(payload["guest_health"], "stable")

    def test_apply_summary_contract_fills_timeout_defaults(self) -> None:
        payload = summary_contract.apply_summary_contract({"status": "timeout"})
        self.assertEqual(payload["error_kind"], "runner-timeout")
        self.assertEqual(payload["recovery_action"], "rerun-runner")
        self.assertEqual(payload["transport_blocker"], "timeout")
        self.assertEqual(payload["guest_health"], "unknown")

    def test_apply_summary_contract_preserves_existing_error_fields(self) -> None:
        payload = summary_contract.apply_summary_contract(
            {
                "status": "error",
                "error_kind": "probe-stage-error",
                "recovery_action": "inspect-probe-stage",
                "transport_blocker": "probe-stage-error",
                "guest_health": "degraded",
            },
            default_error_kind="runner-error",
        )
        self.assertEqual(payload["error_kind"], "probe-stage-error")
        self.assertEqual(payload["recovery_action"], "inspect-probe-stage")
        self.assertEqual(payload["transport_blocker"], "probe-stage-error")
        self.assertEqual(payload["guest_health"], "degraded")

    def test_write_summary_contract_writes_json_file(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            output_path = Path(temp_root) / "summary.json"
            payload = summary_contract.write_summary_contract(
                output_path,
                {"status": "timeout", "output_name": "test-output"},
                default_recovery_action="rerun-probe",
            )
            self.assertTrue(output_path.exists())
            written = json.loads(output_path.read_text(encoding="utf-8"))
            self.assertEqual(written["status"], "timeout")
            self.assertEqual(written["recovery_action"], "rerun-probe")
            self.assertEqual(payload["transport_blocker"], "timeout")


if __name__ == "__main__":
    unittest.main()
