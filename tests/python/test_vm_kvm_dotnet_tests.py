from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile


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


guest_dotnet_tests = load_module(
    "run_guest_dotnet_tests_for_tests",
    VM_KVM_SCRIPTS / "run-guest-dotnet-tests.py",
)


class VmKvmGuestDotnetTests(unittest.TestCase):
    def test_create_stage_zip_preserves_repo_layout_required_by_csharp_tests(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            test_output = repo_root / "tests" / "bin" / "Release" / "net8.0-windows"
            test_output.mkdir(parents=True)
            (test_output / "RegProbe.Tests.dll").write_text("dll", encoding="utf-8")
            (repo_root / "Docs" / "research" / "app-surface").mkdir(parents=True)
            (repo_root / "Docs" / "research" / "app-surface" / "validated-registry-values.json").write_text(
                "{}",
                encoding="utf-8",
            )
            (repo_root / "research" / "records").mkdir(parents=True)
            (repo_root / "research" / "records" / "system.alpha.json").write_text("{}", encoding="utf-8")
            (repo_root / "research" / "promotion-gates.json").write_text("{}", encoding="utf-8")
            zip_path = repo_root / "stage.zip"

            payload = guest_dotnet_tests.create_stage_zip(
                repo_root,
                test_output_dir=test_output,
                stage_zip_path=zip_path,
            )

            self.assertEqual(payload["status"], "ok")
            with ZipFile(zip_path) as archive:
                names = set(archive.namelist())

        self.assertIn("tests/bin/Release/net8.0-windows/RegProbe.Tests.dll", names)
        self.assertIn("Docs/research/app-surface/validated-registry-values.json", names)
        self.assertIn("research/records/system.alpha.json", names)
        self.assertIn("research/promotion-gates.json", names)

    def test_create_stage_zip_reports_missing_inputs(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            repo_root = Path(temp_root)
            payload = guest_dotnet_tests.create_stage_zip(
                repo_root,
                test_output_dir=repo_root / "tests" / "bin" / "Release" / "net8.0-windows",
                stage_zip_path=repo_root / "stage.zip",
            )

        self.assertEqual(payload["status"], "error")
        self.assertEqual(payload["error_kind"], "missing-stage-input")
        self.assertTrue(payload["missing"])

    def test_parse_guest_test_result_extracts_nested_json_stdout(self) -> None:
        nested = {"Status": "PASS", "Counters": {"total": "401", "passed": "401", "failed": "0"}}
        payload = {
            "status": "completed",
            "execution": {
                "exitcode": 0,
                "stdout": json.dumps(nested),
            },
        }

        result = guest_dotnet_tests.parse_guest_test_result(payload)

        self.assertEqual(result["Status"], "PASS")
        self.assertEqual(result["Counters"]["passed"], "401")


if __name__ == "__main__":
    unittest.main()
