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


result_ledger = load_module(
    "power_request_override_result_ledger_parsing_tests",
    FRAMEWORK_SCRIPTS / "generate_power_request_override_result_ledger.py",
)
promote_result_ledger = load_module(
    "power_request_override_promote_result_ledger_parsing_tests",
    FRAMEWORK_SCRIPTS / "promote_power_request_override_result_ledger.py",
)
handoff_bundle = load_module(
    "power_request_override_handoff_bundle_parsing_tests",
    FRAMEWORK_SCRIPTS / "verify_power_request_override_handoff_bundle.py",
)
runtime_trace_readiness = load_module(
    "runtime_trace_readiness_parsing_tests",
    FRAMEWORK_SCRIPTS / "audit_runtime_trace_runner_readiness.py",
)
kvm_guest_control_gap = load_module(
    "kvm_guest_control_gap_parsing_tests",
    FRAMEWORK_SCRIPTS / "audit_execution_required_kvm_guest_control_gap.py",
)
rollback_state = load_module(
    "rollback_state_parsing_tests",
    FRAMEWORK_SCRIPTS / "verify_rollback_state.py",
)
structured_state_diff = load_module(
    "structured_state_diff_parsing_tests",
    FRAMEWORK_SCRIPTS / "build_structured_state_diff.py",
)


class AuditJsonLoaderParsingTests(unittest.TestCase):
    def assert_rejects_non_object_json(self, module) -> None:  # noqa: ANN001
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "payload.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                module.load_json(path)

    def test_result_ledger_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(result_ledger)

    def test_promote_result_ledger_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(promote_result_ledger)

    def test_handoff_bundle_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(handoff_bundle)

    def test_runtime_trace_readiness_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(runtime_trace_readiness)

    def test_kvm_guest_control_gap_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(kvm_guest_control_gap)

    def test_rollback_state_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(rollback_state)

    def test_structured_state_diff_loader_rejects_non_object_payload(self) -> None:
        self.assert_rejects_non_object_json(structured_state_diff)


if __name__ == "__main__":
    unittest.main()
