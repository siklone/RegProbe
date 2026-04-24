from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_ROOT = REPO_ROOT / "scripts"
FRAMEWORK_SCRIPTS = REPO_ROOT / "registry-research-framework" / "scripts"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


update_readme_progress = load_module(
    "update_readme_progress_parsing_tests",
    SCRIPTS_ROOT / "update_readme_progress.py",
)
docs_first_batches = load_module(
    "generate_docs_first_recovery_batches_parsing_tests",
    SCRIPTS_ROOT / "generate_docs_first_recovery_batches.py",
)


class ProgressLoaderParsingTests(unittest.TestCase):
    def test_update_readme_progress_config_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "config.json"
            path.write_text('["not","object"]', encoding="utf-8")
            original = update_readme_progress.CONFIG_PATH
            update_readme_progress.CONFIG_PATH = path
            self.addCleanup(setattr, update_readme_progress, "CONFIG_PATH", original)

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                update_readme_progress.load_config()

    def test_docs_first_entries_reject_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            path = Path(temp_root) / "docs-first.json"
            path.write_text('["not","object"]', encoding="utf-8-sig")
            original = docs_first_batches.DOCS_FIRST_BACKLOG
            docs_first_batches.DOCS_FIRST_BACKLOG = path
            self.addCleanup(setattr, docs_first_batches, "DOCS_FIRST_BACKLOG", original)

            with self.assertRaisesRegex(ValueError, "JSON payload is not an object"):
                docs_first_batches.load_docs_first_entries()

    def test_ghidra_job_queue_main_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            worklist = root / "blocked-worklist.json"
            output = root / "queue.jsonl"
            worklist.write_text('["not","object"]', encoding="utf-8")

            module_path = FRAMEWORK_SCRIPTS / "generate_ghidra_job_queue.py"
            script = (
                "import importlib.util, sys; "
                f"path=r'{module_path}'; "
                "spec=importlib.util.spec_from_file_location('ghidra_job_queue_test', path); "
                "mod=importlib.util.module_from_spec(spec); "
                "spec.loader.exec_module(mod); "
                f"mod.WORKLIST_PATH = __import__('pathlib').Path(r'{worklist}'); "
                f"mod.OUTPUT_PATH = __import__('pathlib').Path(r'{output}'); "
                "mod.main()"
            )
            completed = subprocess.run(
                [sys.executable, "-c", script],
                cwd=REPO_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertNotEqual(completed.returncode, 0)
            self.assertIn("JSON payload is not an object", completed.stderr)

    def test_check_capture_plan_main_rejects_non_object_payload(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            root = Path(temp_root)
            plan = root / "plan.json"
            output = root / "check.json"
            markdown = root / "check.md"
            plan.write_text('["not","object"]', encoding="utf-8")

            module_path = FRAMEWORK_SCRIPTS / "check_etw_stackwalk_capture_plan.py"
            completed = subprocess.run(
                [
                    sys.executable,
                    str(module_path),
                    "--plan",
                    str(plan),
                    "--output",
                    str(output),
                    "--markdown-output",
                    str(markdown),
                ],
                cwd=REPO_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertNotEqual(completed.returncode, 0)
            self.assertIn("JSON payload is not an object", completed.stderr)


if __name__ == "__main__":
    unittest.main()
