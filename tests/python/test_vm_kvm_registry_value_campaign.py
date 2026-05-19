import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = (
    Path(__file__).resolve().parents[2]
    / "scripts"
    / "vm-kvm"
    / "run-guest-registry-value-campaign.py"
)
VM_KVM_ROOT = SCRIPT_PATH.parent

if str(VM_KVM_ROOT) not in sys.path:
    sys.path.insert(0, str(VM_KVM_ROOT))


def load_module():
    spec = importlib.util.spec_from_file_location("run_guest_registry_value_campaign", SCRIPT_PATH)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class RegistryValueCampaignTests(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def test_planned_values_skips_observed_default_without_default_tests(self):
        row = {
            "requested_data": "0",
            "default_value": 0,
            "default_kind": "observed-present",
        }

        self.assertEqual(
            self.module.planned_values(row, include_default_tests=False, max_values=3),
            [1],
        )

    def test_planned_values_includes_default_when_requested(self):
        row = {
            "requested_data": "0",
            "default_value": 0,
            "default_kind": "observed-present",
        }

        self.assertEqual(
            self.module.planned_values(row, include_default_tests=True, max_values=3),
            [0, 1],
        )

    def test_load_campaign_rows_merges_default_matrix_and_key_missing_audit(self):
        report = {
            "default_value_matrix": [
                {
                    "index": 2,
                    "registry_path": "HKLM\\A",
                    "value_name": "PresentValue",
                    "requested_data": "1",
                    "default_kind": "observed-present",
                    "default_value": 0,
                }
            ],
            "key_missing_audit": [
                {
                    "index": 1,
                    "registry_path": "HKLM\\B\\MissingKey",
                    "value_name": "Policy",
                    "requested_data": "1",
                    "verdict": "no-authoritative-evidence-for-25h2",
                }
            ],
        }

        with tempfile.TemporaryDirectory() as tmpdir:
            path = Path(tmpdir) / "report.json"
            path.write_text(self.module.json.dumps(report), encoding="utf-8")
            rows = self.module.load_campaign_rows(path)

        self.assertEqual([row["index"] for row in rows], [1, 2])
        self.assertEqual(rows[0]["record_class"], "key-missing")
        self.assertEqual(rows[1]["record_class"], "value")

    def test_build_plan_honors_index_filters_and_value_limit(self):
        rows = [
            {
                "index": 1,
                "registry_path": "HKLM\\A",
                "value_name": "FirstValue",
                "requested_data": "0",
                "default_kind": "observed-absent",
                "default_value": None,
            },
            {
                "index": 2,
                "registry_path": "HKLM\\B",
                "value_name": "SecondValue",
                "requested_data": "5",
                "default_kind": "observed-absent",
                "default_value": None,
            },
        ]

        plan = self.module.build_plan(
            rows,
            include_default_tests=False,
            max_values_per_record=2,
            start_index=2,
            only_index=set(),
        )

        self.assertEqual(len(plan), 2)
        self.assertEqual({item["index"] for item in plan}, {2})
        self.assertEqual([item["value_data"] for item in plan], [5, 0])
        self.assertEqual(plan[0]["experiment_id"], "operator96-002-secondvalue-5")

    def test_read_artifact_observations_extracts_smoke_and_benchmark_deltas(self):
        artifact = {
            "status": "ok",
            "smoke": {"post_reboot_smoke_hard_success": True},
            "stages": {
                "apply": {
                    "result": {
                        "original": {"value_exists": False},
                        "after_apply": {"value_exists": True, "value": 1},
                        "baseline_smoke": {
                            "benchmarks": {
                                "cpu_single_seconds": 10,
                                "cpu_multi_seconds": 20,
                                "io_write_read_mib_per_second": 100,
                            }
                        },
                        "smoke": {
                            "hard_failure_count": 0,
                            "best_effort_failure_count": 1,
                            "interactive_user_smoke": {"status": "ok", "failure_count": 0, "user": "rai"},
                            "benchmarks": {
                                "cpu_single_seconds": 11,
                                "cpu_multi_seconds": 18,
                                "io_write_read_mib_per_second": 110,
                            },
                        },
                    }
                },
                "post_reboot_rollback": {
                    "result": {
                        "after_reboot": {"value_exists": True, "value": 1},
                        "restore_action": "removed-created-value",
                        "after_restore": {"value_exists": False},
                        "smoke": {
                            "hard_failure_count": 0,
                            "best_effort_failure_count": 1,
                            "interactive_user_smoke": {"status": "ok", "failure_count": 0, "user": "rai"},
                            "benchmarks": {
                                "cpu_single_seconds": 12,
                                "cpu_multi_seconds": 22,
                                "io_write_read_mib_per_second": 90,
                            },
                        },
                    }
                },
                "post_rollback": {
                    "result": {
                        "final": {"value_exists": False},
                        "smoke": {
                            "hard_failure_count": 0,
                            "best_effort_failure_count": 1,
                            "interactive_user_smoke": {"status": "ok", "failure_count": 0, "user": "rai"},
                        },
                    }
                },
            },
        }

        with tempfile.TemporaryDirectory() as tmpdir:
            path = Path(tmpdir) / "artifact.json"
            path.write_text(self.module.json.dumps(artifact), encoding="utf-8")
            observations = self.module.read_artifact_observations(path)

        self.assertEqual(observations["status"], "ok")
        self.assertEqual(observations["apply"]["interactive_user_smoke"]["status"], "ok")
        self.assertEqual(observations["post_reboot"]["restore_action"], "removed-created-value")
        self.assertEqual(observations["benchmark_delta_percent"]["apply_vs_baseline"]["cpu_single_seconds"], 10.0)
        self.assertEqual(observations["benchmark_delta_percent"]["apply_vs_baseline"]["cpu_multi_seconds"], -10.0)
        self.assertEqual(observations["benchmark_delta_percent"]["post_reboot_vs_baseline"]["io_write_read_mib_per_second"], -10.0)

    def test_run_experiment_forwards_host_noise_gate_options(self):
        captured = {}

        def fake_run(cmd, *, timeout=None):
            captured["cmd"] = cmd
            captured["timeout"] = timeout

            class Completed:
                returncode = 0
                stdout = '{"status":"ok"}'
                stderr = ""

            return Completed()

        original_run = self.module.run
        self.module.run = fake_run
        try:
            result = self.module.run_experiment(
                {
                    "registry_path": r"HKLM\\Software\\RegProbe",
                    "value_name": "EnableThing",
                    "value_data": 1,
                    "experiment_id": "operator96-001-enablething-1",
                },
                domain="vm",
                connect="qemu:///session",
                snapshot_name="clean",
                smoke_profile="gui",
                output_dir=Path("out"),
                stage_wait_timeout=10,
                reboot_wait_timeout=20,
                post_reboot_delay_seconds=30,
                host_noise_max_retries=18,
                host_noise_retry_interval_seconds=10.0,
                host_noise_busy_threshold_pct=12.5,
                host_noise_load1_per_cpu_threshold=0.5,
                host_noise_sample_interval_seconds=1.0,
                abort_on_noisy_host=True,
            )
        finally:
            self.module.run = original_run

        self.assertEqual(result["returncode"], 0)
        command = captured["cmd"]
        self.assertIn("--host-noise-max-retries", command)
        self.assertIn("18", command)
        self.assertIn("--host-noise-busy-threshold-pct", command)
        self.assertIn("12.5", command)
        self.assertIn("--host-noise-load1-per-cpu-threshold", command)
        self.assertIn("0.5", command)
        self.assertIn("--abort-on-noisy-host", command)

    def test_markdown_uses_custom_value_seed_language(self):
        payload = {
            "generated_utc": "2026-05-19T00:00:00Z",
            "status": "planned",
            "plan": [
                {
                    "index": 1,
                    "experiment_id": "operator96-001-enablething-1",
                    "registry_path": "HKLM\\Software\\Example",
                    "value_name": "EnableThing",
                    "value_data": 1,
                    "default_kind": "observed-absent",
                    "source_quality": "vm-observed",
                }
            ],
            "results": [],
        }

        with tempfile.TemporaryDirectory() as tmpdir:
            path = Path(tmpdir) / "campaign.md"
            self.module.write_plan_markdown(payload, path)
            text = path.read_text(encoding="utf-8")

        self.assertIn("# Custom Registry Value Seed Campaign", text)
        self.assertIn("historical filename context", text)
        self.assertNotIn("# Operator 96 Registry Value Campaign", text)


if __name__ == "__main__":
    unittest.main()
