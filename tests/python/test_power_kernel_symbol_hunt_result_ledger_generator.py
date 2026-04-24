from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "generate_power_kernel_symbol_hunt_result_ledger.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


generator = load_module("power_kernel_symbol_hunt_result_ledger_generator", SCRIPT_PATH)


class PowerKernelSymbolHuntResultLedgerGeneratorTests(unittest.TestCase):
    def test_infer_outcome_prefers_red_flags(self) -> None:
        outcome = generator.infer_outcome(
            {
                "execution_required_init_walker": {"required_markers_present": True, "strong_markers_seen": ["0x140C483EF"]},
                "execution_required_consumers": {"required_markers_present": True, "strong_markers_seen": []},
                "execution_required_setting_callback": {"required_markers_present": True, "strong_markers_seen": ["GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT"]},
                "global_timer_resolution_reader": {"required_markers_present": True, "strong_markers_seen": []},
            },
            ["missing stdout"],
        )
        self.assertEqual(outcome, "symbol-regression-or-wrapper-fog")

    def test_build_prefilled_payload_collects_markers(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            init_stdout = root / "init.stdout.txt"
            init_summary = root / "init.summary.json"
            consumers_stdout = root / "consumers.stdout.txt"
            consumers_summary = root / "consumers.summary.json"
            callback_stdout = root / "callback.stdout.txt"
            callback_summary = root / "callback.summary.json"
            timer_stdout = root / "timer.stdout.txt"
            timer_summary = root / "timer.summary.json"

            init_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "PopPowerRequestConvertSystemToExecution",
                        "PopPowerRequestActiveAudioEnablesExecutionRequired",
                        "0x140C48AB8",
                        "0x140C483EF",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            consumers_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "PopPowerRequestHandleExecutionEnablementUpdate",
                        "PopPowerRequestCallbackExecutionRequired",
                        "PopPowerRequestEvaluateExecutionRequiredStatus",
                        "PopExecutionRequiredTimeout",
                        "uf nt!PopPowerRequestHandleExecutionEnablementUpdate",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            callback_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "PopPowerRequestExecutionRequiredSettingCallback",
                        "GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT",
                        "PopExecutionRequiredTimeout",
                        "PopPowerRequestSetExecutionRequiredTimeoutTimer",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            timer_stdout.write_text(
                "\n".join(
                    [
                        "REGPROBE_LOCALKD_BEGIN",
                        "KiGlobalTimerResolutionRequests",
                        "TimerResolution",
                        "REGPROBE_LOCALKD_END",
                    ]
                ),
                encoding="utf-8",
            )
            for path in (init_summary, consumers_summary, callback_summary, timer_summary):
                path.write_text("{}", encoding="utf-8")

            payload = generator.build_prefilled_payload(
                run_id="test-run",
                init_stdout=init_stdout,
                init_summary=init_summary,
                consumers_stdout=consumers_stdout,
                consumers_summary=consumers_summary,
                callback_stdout=callback_stdout,
                callback_summary=callback_summary,
                timer_stdout=timer_stdout,
                timer_summary=timer_summary,
            )

        self.assertEqual(payload["fill_after_run"]["review_outcome"]["chosen_outcome"], "execution-required-seeding-retained")
        init_artifact = payload["fill_after_run"]["artifacts"]["execution_required_init_walker"]
        timer_artifact = payload["fill_after_run"]["artifacts"]["global_timer_resolution_reader"]
        self.assertFalse(init_artifact["stdout_path"].startswith("/"))
        self.assertFalse(timer_artifact["summary_path"].startswith("/"))
        self.assertEqual(init_artifact["strong_markers_seen"], ["0x140C483EF"])
        self.assertEqual(timer_artifact["strong_markers_seen"], [])
        self.assertEqual(timer_artifact["weak_markers_seen"], ["GlobalTimer", "TimerResolution"])

    def test_missing_external_artifact_red_flag_does_not_leak_absolute_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            missing_stdout = Path(temp_root) / "missing.stdout.txt"
            missing_summary = Path(temp_root) / "missing.summary.json"
            review, red_flags = generator.build_artifact_review(
                artifact_id="missing-test",
                stdout_path=missing_stdout,
                summary_path=missing_summary,
                command_file="command.txt",
                rubric_entry={"required_markers": ["REGPROBE_LOCALKD_BEGIN"]},
            )

        self.assertFalse(review["stdout_path"].startswith("/"))
        self.assertFalse(review["summary_path"].startswith("/"))
        self.assertFalse(any(str(temp_root) in flag for flag in red_flags))
        self.assertTrue(any("external-artifacts/" in flag for flag in red_flags))


if __name__ == "__main__":
    unittest.main()
