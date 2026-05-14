from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest import mock


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


registry_value_experiment = load_module(
    "run_guest_registry_value_experiment_for_tests",
    VM_KVM_SCRIPTS / "run-guest-registry-value-experiment.py",
)


class VmKvmRegistryValueExperimentTests(unittest.TestCase):
    def test_recover_from_snapshot_reverts_starts_and_waits_for_qga(self) -> None:
        calls: list[list[str]] = []

        def fake_run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
            calls.append(cmd)
            return subprocess.CompletedProcess(cmd, 0, stdout=f"ok:{cmd[-1]}", stderr="")

        with mock.patch.object(registry_value_experiment, "run", side_effect=fake_run), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
            return_value={"status": "ok", "guest_health": "stable"},
        ) as wait_for_qga:
            recovery = registry_value_experiment.recover_from_snapshot(
                domain="regprobe-win11-25h2-session",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
                wait_timeout=600,
            )

        self.assertEqual(recovery["status"], "ok")
        self.assertEqual([step["action"] for step in recovery["steps"]], ["destroy-runtime", "snapshot-revert", "start-domain"])
        self.assertEqual(calls[0], ["virsh", "-c", "qemu:///session", "destroy", "regprobe-win11-25h2-session"])
        self.assertEqual(
            calls[1],
            [
                "virsh",
                "-c",
                "qemu:///session",
                "snapshot-revert",
                "regprobe-win11-25h2-session",
                "clean-25h2-qga",
                "--force",
            ],
        )
        self.assertEqual(calls[2], ["virsh", "-c", "qemu:///session", "start", "regprobe-win11-25h2-session"])
        wait_for_qga.assert_called_once_with("regprobe-win11-25h2-session", "qemu:///session", 600)

    def test_recover_from_snapshot_reports_revert_failure_without_starting(self) -> None:
        calls: list[list[str]] = []

        def fake_run(cmd: list[str], *, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
            calls.append(cmd)
            if "snapshot-revert" in cmd:
                return subprocess.CompletedProcess(cmd, 1, stdout="", stderr="snapshot failed")
            return subprocess.CompletedProcess(cmd, 0, stdout="", stderr="")

        with mock.patch.object(registry_value_experiment, "run", side_effect=fake_run), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
        ) as wait_for_qga:
            recovery = registry_value_experiment.recover_from_snapshot(
                domain="regprobe-win11-25h2-session",
                connect="qemu:///session",
                snapshot_name="clean-25h2-qga",
                wait_timeout=600,
            )

        self.assertEqual(recovery["status"], "error")
        self.assertEqual(recovery["error"], "snapshot-revert-failed")
        self.assertEqual(len(calls), 2)
        wait_for_qga.assert_not_called()

    def test_stage_script_removes_empty_key_created_for_missing_original(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            script_path = Path(tmp) / "stage.ps1"
            registry_value_experiment.write_guest_stage_script(script_path)
            script = script_path.read_text(encoding="utf-8")

        self.assertIn("GetSubKeyNames", script)
        self.assertIn("removed-created-key", script)
        self.assertIn("removed-created-value-key-retained", script)

    def test_stage_script_records_micro_benchmarks_for_core_and_gui_smoke(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            script_path = Path(tmp) / "stage.ps1"
            registry_value_experiment.write_guest_stage_script(script_path)
            script = script_path.read_text(encoding="utf-8")

        self.assertIn("function Invoke-MicroBenchmarks", script)
        self.assertIn("RegProbeMicroBench", script)
        self.assertIn("cpu_single_iterations_per_second", script)
        self.assertIn("sample_count", script)
        self.assertIn("spread_pct", script)
        self.assertIn("io_write_read_mib_per_second", script)

    def test_host_noise_gate_reports_quiet_host_from_proc_snapshots(self) -> None:
        reads = {
            "/proc/stat": [
                "cpu  100 0 100 800 0 0 0 0 0 0\n",
                "cpu  110 0 110 980 0 0 0 0 0 0\n",
            ],
            "/proc/loadavg": ["0.10 0.20 0.30 1/100 1\n"],
            "/proc/cpuinfo": ["processor\t: 0\nprocessor\t: 1\n"],
        }

        def fake_read_text(self: Path, encoding: str = "utf-8") -> str:
            values = reads[str(self)]
            return values.pop(0) if len(values) > 1 else values[0]

        with mock.patch.object(registry_value_experiment.time, "sleep"), mock.patch.object(Path, "read_text", fake_read_text):
            result = registry_value_experiment.wait_for_quiet_host(max_retries=0, sample_interval_seconds=0.01)

        self.assertEqual(result["noise_status"], "ok")
        self.assertEqual(result["host_cpu_count"], 2)
        self.assertLess(result["host_cpu_busy_pct"], 20)

    def test_run_experiment_aborts_before_apply_when_host_stays_noisy(self) -> None:
        args = SimpleNamespace(
            output_name="operator96-027-longdpcqueuethreshold-2",
            value_name="LongDpcQueueThreshold",
            value_data=2,
            registry_path="HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel",
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            smoke_profile="gui",
            auto_revert_snapshot_on_boot_failure=True,
            revert_snapshot_name="clean-25h2-qga",
            reboot_wait_timeout=420,
            stage_wait_timeout=420,
            post_reboot_delay_seconds=90,
            no_host_noise_gate=False,
            host_noise_max_retries=0,
            host_noise_retry_interval_seconds=0,
            host_noise_busy_threshold_pct=12.5,
            host_noise_load1_per_cpu_threshold=0.5,
            host_noise_sample_interval_seconds=0,
            abort_on_noisy_host=True,
        )

        with mock.patch.object(
            registry_value_experiment,
            "list_domain_snapshots",
            return_value={"snapshots": ["clean-25h2-qga"], "returncode": 0, "stderr": ""},
        ), mock.patch.object(
            registry_value_experiment, "write_guest_stage_script"
        ), mock.patch.object(
            registry_value_experiment,
            "wait_for_quiet_host",
            return_value={"noise_status": "noisy", "noise_reason": "cpu_busy"},
        ), mock.patch.object(
            registry_value_experiment,
            "run_guest_stage",
        ) as run_guest_stage:
            result = registry_value_experiment.run_experiment(args)

        self.assertEqual(result["status"], "error")
        self.assertEqual(result["error"], "host-noise-preflight-failed")
        self.assertEqual(result["outcome"], "aborted-before-apply")
        self.assertFalse(result["safety"]["mutation_started"])
        self.assertEqual(result["preflight"]["host_noise_meta"]["noise_status"], "noisy")
        run_guest_stage.assert_not_called()

    def test_stage_script_records_baseline_and_interactive_smoke(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            script_path = Path(tmp) / "stage.ps1"
            registry_value_experiment.write_guest_stage_script(script_path)
            script = script_path.read_text(encoding="utf-8")

        self.assertIn("baseline_smoke", script)
        self.assertIn("function Invoke-InteractiveUserSmoke", script)
        self.assertIn("interactive-store-uri", script)
        self.assertIn("schtasks.exe", script)
        self.assertIn("hard_failure_count", script)
        self.assertIn("best_effort_failure_count", script)

    def test_reboot_guest_returns_timeout_contract(self) -> None:
        timeout = subprocess.TimeoutExpired(
            ["virsh", "-c", "qemu:///session", "reboot", "domain"],
            timeout=30,
            output=b"partial stdout",
            stderr=b"partial stderr",
        )

        with mock.patch.object(registry_value_experiment, "run", side_effect=timeout):
            result = registry_value_experiment.reboot_guest("domain", "qemu:///session")

        self.assertEqual(result["status"], "timeout")
        self.assertEqual(result["error"], "guest-reboot-command-timeout")
        self.assertEqual(result["timeout_seconds"], 30)
        self.assertEqual(result["stdout"], "partial stdout")
        self.assertEqual(result["stderr"], "partial stderr")

    def test_run_experiment_marks_recovered_apply_boot_failure_as_controlled_result(self) -> None:
        args = SimpleNamespace(
            output_name="operator96-059-ttmenabled-1",
            value_name="TtmEnabled",
            value_data=1,
            registry_path="HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            smoke_profile="gui",
            auto_revert_snapshot_on_boot_failure=True,
            revert_snapshot_name="clean-25h2-qga",
            reboot_wait_timeout=420,
            stage_wait_timeout=420,
            post_reboot_delay_seconds=90,
            no_host_noise_gate=False,
            host_noise_max_retries=0,
            host_noise_retry_interval_seconds=0,
            host_noise_busy_threshold_pct=100,
            host_noise_load1_per_cpu_threshold=100,
            host_noise_sample_interval_seconds=0,
        )
        stage_payload = {"status": "ok", "smoke": {"items": []}}

        with mock.patch.object(
            registry_value_experiment,
            "list_domain_snapshots",
            return_value={"snapshots": ["clean-25h2-qga"], "returncode": 0, "stderr": ""},
        ), mock.patch.object(
            registry_value_experiment, "write_guest_stage_script"
        ), mock.patch.object(
            registry_value_experiment,
            "wait_for_quiet_host",
            return_value={"noise_status": "ok"},
        ), mock.patch.object(
            registry_value_experiment,
            "run_guest_stage",
            return_value=(0, {"status": "ok"}, stage_payload),
        ), mock.patch.object(
            registry_value_experiment,
            "reboot_guest",
            return_value={"status": "ok"},
        ), mock.patch.object(
            registry_value_experiment.time,
            "sleep",
        ), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
            return_value={"status": "error", "error_kind": "qga-preflight-failed"},
        ), mock.patch.object(
            registry_value_experiment,
            "recover_from_snapshot",
            return_value={"status": "ok", "snapshot": "clean-25h2-qga"},
        ):
            result = registry_value_experiment.run_experiment(args)

        self.assertEqual(result["status"], "ok")
        self.assertEqual(result["outcome"], "boot-failure-recovered")
        self.assertTrue(result["controlled_failure"])
        self.assertEqual(result["error"], "guest-did-not-return-after-apply-reboot")
        self.assertEqual(result["recovery"]["status"], "ok")
        self.assertFalse(result["smoke"]["post_reboot_smoke_hard_success"])

    def test_run_experiment_recovers_post_reboot_rollback_stage_failure(self) -> None:
        args = SimpleNamespace(
            output_name="operator96-072-heteromulticlassparkingenabled-1",
            value_name="HeteroMultiClassParkingEnabled",
            value_data=1,
            registry_path="HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power",
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            smoke_profile="gui",
            auto_revert_snapshot_on_boot_failure=True,
            revert_snapshot_name="clean-25h2-qga",
            reboot_wait_timeout=420,
            stage_wait_timeout=420,
            post_reboot_delay_seconds=90,
            no_host_noise_gate=False,
            host_noise_max_retries=0,
            host_noise_retry_interval_seconds=0,
            host_noise_busy_threshold_pct=100,
            host_noise_load1_per_cpu_threshold=100,
            host_noise_sample_interval_seconds=0,
        )
        apply_payload = {"status": "ok", "smoke": {"items": []}}

        with mock.patch.object(
            registry_value_experiment,
            "list_domain_snapshots",
            return_value={"snapshots": ["clean-25h2-qga"], "returncode": 0, "stderr": ""},
        ), mock.patch.object(
            registry_value_experiment, "write_guest_stage_script"
        ), mock.patch.object(
            registry_value_experiment,
            "wait_for_quiet_host",
            return_value={"noise_status": "ok"},
        ), mock.patch.object(
            registry_value_experiment,
            "run_guest_stage",
            side_effect=[
                (0, {"status": "ok"}, apply_payload),
                (1, {"status": "error"}, {"status": "error", "error": "qga-stage-failed"}),
            ],
        ), mock.patch.object(
            registry_value_experiment,
            "reboot_guest",
            return_value={"status": "ok"},
        ), mock.patch.object(
            registry_value_experiment.time,
            "sleep",
        ), mock.patch.object(
            registry_value_experiment,
            "wait_for_qga",
            return_value={"status": "ok"},
        ), mock.patch.object(
            registry_value_experiment,
            "recover_from_snapshot",
            return_value={"status": "ok", "snapshot": "clean-25h2-qga"},
        ):
            result = registry_value_experiment.run_experiment(args)

        self.assertEqual(result["status"], "ok")
        self.assertEqual(result["outcome"], "rollback-stage-failure-recovered")
        self.assertTrue(result["controlled_failure"])
        self.assertEqual(result["error"], "post-reboot-rollback-stage-failed")
        self.assertEqual(result["recovery"]["status"], "ok")


if __name__ == "__main__":
    unittest.main()
