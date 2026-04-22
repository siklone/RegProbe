from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
RUNNER_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-kernel-symbol-hunt.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


power_kernel_hunt = load_module("power_kernel_symbol_hunt_runner_tests", RUNNER_PATH)


class PowerKernelSymbolHuntRunnerTests(unittest.TestCase):
    def test_dry_run_plan_contains_three_expected_passes(self) -> None:
        args = power_kernel_hunt.argparse.Namespace(
            repo_root=str(REPO_ROOT),
            domain="regprobe-win11-25h2-session",
            connect="qemu:///session",
            bridge_base_url="http://10.0.2.2:8766",
            upload_dir="/tmp/regprobe-bridge",
            guest_scripts_root=r"C:\RegProbe-Diag\bootstrap",
            delay_ms="18",
            wake_key="KEY_ENTER",
            timeout_seconds=240,
            smoke_timeout_seconds=180,
            init_walker_output_name="init-walker-test",
            consumers_output_name="consumers-test",
            global_timer_output_name="global-timer-test",
            dry_run=True,
        )

        payload = power_kernel_hunt.build_plan_payload(args, REPO_ROOT)

        self.assertEqual(payload["mode"], "dry-run")
        self.assertEqual(len(payload["passes"]), 3)
        self.assertEqual(
            [item["name"] for item in payload["passes"]],
            [
                "execution-required-init-walker",
                "execution-required-consumers",
                "global-timer-resolution-reader",
            ],
        )
        for item in payload["passes"]:
            self.assertGreater(item["kd_command_count"], 0)
            self.assertTrue(item["command"])
            self.assertIn("--output-name", item["command"])

    def test_command_files_strip_markers_and_quit(self) -> None:
        commands = power_kernel_hunt.load_kd_commands(
            REPO_ROOT / "registry-research-framework" / "audit" / "execution-required-init-walker-reacquire-local-kd-20260422.txt"
        )
        self.assertNotIn(".echo REGPROBE_LOCALKD_BEGIN", commands)
        self.assertNotIn(".echo REGPROBE_LOCALKD_END", commands)
        self.assertNotIn("q", commands)
        self.assertIn("u 0x140C48AB8 L0x120", commands)


if __name__ == "__main__":
    unittest.main()
