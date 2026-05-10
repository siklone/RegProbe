import importlib.util
import sys
import unittest
from pathlib import Path


SCRIPT_PATH = (
    Path(__file__).resolve().parents[2]
    / "registry-research-framework"
    / "scripts"
    / "registry_value_verdict.py"
)


def load_module():
    spec = importlib.util.spec_from_file_location("registry_value_verdict_for_tests", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def artifact_with_bench(baseline: dict, applied: dict, post_reboot: dict | None = None, *, noise_status: str | None = None):
    host_noise_meta = {"noise_status": noise_status} if noise_status else None
    apply_stage = {
        "result": {
            "original": {"key_exists": True, "value_exists": True, "value": 0, "value_kind": "System.Int32", "status": "value-present"},
            "after_apply": {"key_exists": True, "value_exists": True, "value": 1, "value_kind": "System.Int32", "status": "value-present"},
            "baseline_smoke": {"hard_failure_count": 0, "interactive_user_smoke": {"failure_count": 0}, "benchmarks": baseline},
            "smoke": {"hard_failure_count": 0, "interactive_user_smoke": {"failure_count": 0}, "benchmarks": applied},
        }
    }
    post_reboot_stage = {
        "result": {
            "after_reboot": {"key_exists": True, "value_exists": True, "value": 1, "value_kind": "System.Int32", "status": "value-present"},
            "restore_action": "restored-original-value",
            "after_restore": {"key_exists": True, "value_exists": True, "value": 0, "value_kind": "System.Int32", "status": "value-present"},
            "smoke": {"hard_failure_count": 0, "interactive_user_smoke": {"failure_count": 0}, "benchmarks": post_reboot or applied},
        }
    }
    post_rollback_stage = {
        "result": {
            "final": {"key_exists": True, "value_exists": True, "value": 0, "value_kind": "System.Int32", "status": "value-present"},
            "smoke": {"hard_failure_count": 0, "interactive_user_smoke": {"failure_count": 0}, "benchmarks": post_reboot or applied},
        }
    }
    if host_noise_meta:
        apply_stage["host_noise_meta"] = host_noise_meta
        post_reboot_stage["host_noise_meta"] = host_noise_meta
        post_rollback_stage["host_noise_meta"] = host_noise_meta
    return {"status": "ok", "stages": {"apply": apply_stage, "post_reboot_rollback": post_reboot_stage, "post_rollback": post_rollback_stage}}


class RegistryValueVerdictTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_legacy_cpu_seconds_lower_is_better(self):
        payload = artifact_with_bench(
            {"status": "ok", "cpu_multi_seconds": 10.0, "io_write_read_mib_per_second": 100.0},
            {"status": "ok", "cpu_multi_seconds": 9.0, "io_write_read_mib_per_second": 101.0},
        )

        verdict = self.module.compute_registry_value_verdict(payload)

        self.assertEqual(verdict["overall"], "low_confidence")
        self.assertEqual(verdict["delta_pct"], 10.0)
        self.assertEqual(verdict["host_noise"], "unknown")
        self.assertEqual(verdict["confidence"], "low")

    def test_io_throughput_higher_is_better(self):
        payload = artifact_with_bench(
            {"status": "ok", "io_write_read_mib_per_second": 100.0},
            {"status": "ok", "io_write_read_mib_per_second": 112.0},
            noise_status="ok",
        )

        verdict = self.module.compute_registry_value_verdict(payload)

        self.assertEqual(verdict["overall"], "io_gain")
        self.assertEqual(verdict["delta_pct"], 12.0)

    def test_hard_smoke_breakage_wins_over_perf_gain(self):
        payload = artifact_with_bench(
            {"status": "ok", "cpu_multi_seconds": 10.0},
            {"status": "ok", "cpu_multi_seconds": 8.0},
        )
        payload["stages"]["post_reboot_rollback"]["result"]["smoke"]["hard_failure_count"] = 1

        verdict = self.module.compute_registry_value_verdict(payload)

        self.assertEqual(verdict["overall"], "app_breakage")
        self.assertTrue(verdict["safety_findings"])

    def test_noisy_host_withholds_perf_verdict(self):
        payload = artifact_with_bench(
            {"status": "ok", "io_write_read_mib_per_second": 100.0},
            {"status": "ok", "io_write_read_mib_per_second": 130.0},
            noise_status="noisy",
        )

        verdict = self.module.compute_registry_value_verdict(payload)

        self.assertEqual(verdict["overall"], "noisy")
        self.assertEqual(verdict["confidence"], "low")

    def test_rollback_failure_wins_over_no_effect(self):
        payload = artifact_with_bench(
            {"status": "ok", "io_write_read_mib_per_second": 100.0},
            {"status": "ok", "io_write_read_mib_per_second": 101.0},
        )
        payload["stages"]["post_rollback"]["result"]["final"]["value"] = 1

        verdict = self.module.compute_registry_value_verdict(payload)

        self.assertEqual(verdict["overall"], "rollback_failure")


if __name__ == "__main__":
    unittest.main()
