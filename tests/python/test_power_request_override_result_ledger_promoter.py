from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = (
    REPO_ROOT
    / "registry-research-framework"
    / "scripts"
    / "promote_power_request_override_result_ledger.py"
)


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


promoter = load_module("power_request_override_result_ledger_promoter", SCRIPT_PATH)


class PowerRequestOverrideResultLedgerPromoterTests(unittest.TestCase):
    def test_resolve_run_id_rejects_template_placeholder(self) -> None:
        with self.assertRaises(ValueError):
            promoter.resolve_run_id(payload={"fill_after_run": {"run_id": "<replace-with-run-id>"}}, explicit_run_id=None)

    def test_target_paths_slugify_run_id(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            target_json, target_md = promoter.target_paths("Run 19/A", audit_root=root)
        self.assertEqual(target_json.name, "power-request-override-reader-binding-result-ledger-run-19-a.json")
        self.assertEqual(target_md.name, "power-request-override-reader-binding-result-ledger-run-19-a.md")

    def test_cli_dry_run_prints_target_paths_without_moving_files(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            source_json = root / "autofill.json"
            source_md = root / "autofill.md"
            source_json.write_text(json.dumps({"fill_after_run": {"run_id": "reader-pass-a"}}), encoding="utf-8")
            source_md.write_text("# draft\n", encoding="utf-8")

            proc = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT_PATH),
                    "--source-json",
                    str(source_json),
                    "--source-md",
                    str(source_md),
                    "--dry-run",
                ],
                cwd=str(REPO_ROOT),
                capture_output=True,
                text=True,
                check=True,
            )
            payload = json.loads(proc.stdout)
            self.assertEqual(payload["mode"], "dry-run")
            self.assertTrue(payload["target_json"].endswith("power-request-override-reader-binding-result-ledger-reader-pass-a.json"))
            self.assertTrue(source_json.exists())
            self.assertTrue(source_md.exists())

    def test_promote_moves_autofill_outputs_to_dated_targets(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            source_json = root / "autofill.json"
            source_md = root / "autofill.md"
            source_json.write_text(json.dumps({"fill_after_run": {"run_id": "reader-pass-a"}}), encoding="utf-8")
            source_md.write_text("# draft\n", encoding="utf-8")
            result = promoter.promote(
                source_json=source_json,
                source_md=source_md,
                run_id="reader-pass-a",
                audit_root=root,
            )

        self.assertFalse(source_json.exists())
        self.assertFalse(source_md.exists())
        self.assertTrue(result["target_json"].endswith("power-request-override-reader-binding-result-ledger-reader-pass-a.json"))
        self.assertTrue(result["target_md"].endswith("power-request-override-reader-binding-result-ledger-reader-pass-a.md"))

    def test_promote_rejects_mismatched_payload_run_id(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            source_json = root / "autofill.json"
            source_md = root / "autofill.md"
            source_json.write_text(json.dumps({"fill_after_run": {"run_id": "actual-run"}}), encoding="utf-8")
            source_md.write_text("# draft\n", encoding="utf-8")
            with self.assertRaises(ValueError):
                promoter.promote(source_json=source_json, source_md=source_md, run_id="different-run")

    def test_promote_refuses_to_overwrite_existing_target_without_force(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            source_json = root / "autofill.json"
            source_md = root / "autofill.md"
            source_json.write_text(json.dumps({"fill_after_run": {"run_id": "reader-pass-a"}}), encoding="utf-8")
            source_md.write_text("# draft\n", encoding="utf-8")
            (root / "power-request-override-reader-binding-result-ledger-reader-pass-a.json").write_text("{}", encoding="utf-8")
            with self.assertRaises(FileExistsError):
                promoter.promote(
                    source_json=source_json,
                    source_md=source_md,
                    run_id="reader-pass-a",
                    audit_root=root,
                )

    def test_promote_force_allows_overwrite(self) -> None:
        with tempfile.TemporaryDirectory() as temp_root:
            root = Path(temp_root)
            source_json = root / "autofill.json"
            source_md = root / "autofill.md"
            source_json.write_text(json.dumps({"fill_after_run": {"run_id": "reader-pass-a"}}), encoding="utf-8")
            source_md.write_text("# draft\n", encoding="utf-8")
            target_json = root / "power-request-override-reader-binding-result-ledger-reader-pass-a.json"
            target_md = root / "power-request-override-reader-binding-result-ledger-reader-pass-a.md"
            target_json.write_text("{\"old\": true}\n", encoding="utf-8")
            target_md.write_text("# old\n", encoding="utf-8")

            promoter.promote(
                source_json=source_json,
                source_md=source_md,
                run_id="reader-pass-a",
                audit_root=root,
                force=True,
            )

        self.assertFalse(source_json.exists())
        self.assertFalse(source_md.exists())


if __name__ == "__main__":
    unittest.main()
