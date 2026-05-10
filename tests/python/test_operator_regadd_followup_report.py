from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
REGISTRY_SCRIPTS = REPO_ROOT / "scripts" / "registry"
if str(REGISTRY_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(REGISTRY_SCRIPTS))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


followup_report = load_module(
    "build_operator_regadd_followup_report_for_tests",
    REGISTRY_SCRIPTS / "build_operator_regadd_followup_report.py",
)


class OperatorRegaddFollowupReportTests(unittest.TestCase):
    def test_normalize_registry_path_aliases_hklm(self) -> None:
        self.assertEqual(
            followup_report.normalize_registry_path(r"HKEY_LOCAL_MACHINE\System\CurrentControlSet"),
            r"hklm\system\currentcontrolset",
        )

    def test_parse_admx_policy_map_extracts_enabled_disabled_values(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            admx_path = Path(tmp) / "Power.admx"
            admx_path.write_text(
                """<?xml version="1.0" encoding="utf-8"?>
<policyDefinitions>
  <policies>
    <policy name="PowerThrottlingTurnOff" class="Machine" key="System\\CurrentControlSet\\Control\\Power\\PowerThrottling" valueName="PowerThrottlingOff">
      <enabledValue><decimal value="1" /></enabledValue>
      <disabledValue><decimal value="0" /></disabledValue>
    </policy>
  </policies>
</policyDefinitions>
""",
                encoding="utf-8",
            )

            mapping = followup_report.parse_admx_policy_map([admx_path])

        key = followup_report.target_key(
            r"HKLM\System\CurrentControlSet\Control\Power\PowerThrottling",
            "PowerThrottlingOff",
        )
        self.assertEqual(mapping[key][0]["policy_name"], "PowerThrottlingTurnOff")
        self.assertEqual(mapping[key][0]["enabled_value"], "1")
        self.assertEqual(mapping[key][0]["disabled_value"], "0")

    def test_value_missing_default_row_is_observed_absent(self) -> None:
        rows = followup_report.build_default_rows(
            [
                {
                    "index": 1,
                    "path": r"HKLM\System\CurrentControlSet\Control\Power",
                    "value_name": "Example",
                    "requested_data": "1",
                    "status": "value-missing",
                    "key_exists": True,
                    "value_exists": False,
                }
            ],
            {},
            {},
        )

        self.assertEqual(rows[0]["default_kind"], "observed-absent")
        self.assertIsNone(rows[0]["default_value"])
        self.assertEqual(rows[0]["recommended_test_values"], [1, 0])


if __name__ == "__main__":
    unittest.main()
