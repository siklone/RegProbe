from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "vm-kvm" / "run-power-request-override-reader-binding-reacquire.py"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


runner = load_module("power_request_override_reacquire_runner", SCRIPT_PATH)


class PowerRequestOverrideReacquireRunnerTests(unittest.TestCase):
    def test_load_kd_commands_filters_wrapper_lines(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            path = Path(temp_root) / "commands.txt"
            path.write_text(
                "\n".join(
                    [
                        ".echo REGPROBE_LOCALKD_BEGIN",
                        "x nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                        "uf nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                        ".echo REGPROBE_LOCALKD_END",
                        "q",
                        "",
                    ]
                ),
                encoding="utf-8",
            )

            commands = runner.load_kd_commands(path)

        self.assertEqual(
            commands,
            [
                "x nt!PopPowerRequestHandleRequestOverrideQueryResponse",
                "uf nt!PopPowerRequestHandleRequestOverrideQueryResponse",
            ],
        )


if __name__ == "__main__":
    unittest.main()
