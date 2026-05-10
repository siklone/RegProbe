import importlib.util
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[2] / "scripts" / "registry" / "parse_reg_add_batch.py"
SPEC = importlib.util.spec_from_file_location("parse_reg_add_batch", SCRIPT_PATH)
parse_reg_add_batch = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(parse_reg_add_batch)


class ParseRegAddBatchTests(unittest.TestCase):
    def test_parses_wrapped_commands_with_missing_slashes(self):
        raw = """
        reg add "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power" /v "LidReliabilityState" /t REG_DWORD
        d "1" /f
        reg add "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel" /v
        "MaxDynamicTickDuration" /t REG_DWORD /d "1" /freg add
        "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power" /v "TtmEnabled" /t REG_DWORD /d "0"
        f
        """

        payload = parse_reg_add_batch.parse_batch(raw)

        self.assertEqual(payload["status"], "ok")
        self.assertEqual(payload["input_command_count"], 3)
        names = [entry["value_name"] for entry in payload["entries"]]
        self.assertEqual(names, ["LidReliabilityState", "MaxDynamicTickDuration", "TtmEnabled"])
        self.assertEqual(payload["entries"][0]["requested_data"], 1)
        self.assertTrue(payload["entries"][1]["requires_snapshot_or_overlay"])

    def test_marks_security_and_kernel_values_as_critical(self):
        raw = """
        reg add "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel" /v
        "DisableExceptionChainValidation" /t REG_DWORD /d "1" /f
        """

        payload = parse_reg_add_batch.parse_batch(raw)
        entry = payload["entries"][0]

        self.assertEqual(entry["risk"], "critical")
        self.assertIn("security-sensitive", entry["tags"])
        self.assertIn("boot-critical", entry["tags"])


if __name__ == "__main__":
    unittest.main()
