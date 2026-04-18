from __future__ import annotations

import argparse
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


registry_policy_probe = load_module(
    "run_guest_registry_policy_probe_for_tests",
    VM_KVM_SCRIPTS / "run-guest-registry-policy-probe.py",
)


class VmKvmRegistryPolicyProbeTests(unittest.TestCase):
    def test_try_probe_stage_fallback_preserves_contract_fields(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_dir = Path(temp_root)
            summary_path = temp_dir / "probe-summary.json"
            probe_stage_path = temp_dir / "probe-stage.json"
            result_path = temp_dir / "probe-result.txt"

            probe_stage_path.write_text(
                json.dumps(
                    {
                        "generated_utc": "2026-04-18T00:00:00Z",
                        "stage": "collect",
                        "status": "error",
                        "message": "collect stage failed",
                    },
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )
            result_path.write_text("ERROR=saveas timed out\n", encoding="utf-8")

            args = argparse.Namespace(
                registry_path=r"HKLM\SOFTWARE\RegProbe",
                value_name="Enabled",
                output_name="policy-probe-test",
                trigger_profile="default",
            )

            summary, payload = registry_policy_probe.try_probe_stage_fallback(
                summary_path=summary_path,
                probe_stage_path=probe_stage_path,
                result_path=result_path,
                args=args,
            )

        self.assertEqual(summary["status"], "error")
        self.assertEqual(summary["error_kind"], "probe-stage-error")
        self.assertEqual(summary["recovery_action"], "inspect-probe-stage")
        self.assertEqual(summary["transport_blocker"], "probe-stage-error")
        self.assertEqual(summary["guest_health"], "degraded")
        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "probe-stage-error")
        self.assertEqual(payload["recovery_action"], "inspect-probe-stage")
        self.assertEqual(payload["transport_blocker"], "probe-stage-error")
        self.assertEqual(payload["guest_health"], "degraded")
        self.assertEqual(payload["summary_source"], "probe-stage-fallback")


if __name__ == "__main__":
    unittest.main()
