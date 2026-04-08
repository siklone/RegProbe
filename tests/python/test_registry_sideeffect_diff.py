from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
TOOLS_ROOT = REPO_ROOT / "registry-research-framework" / "tools"


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


registry_sideeffect_diff = load_module(
    "registry_sideeffect_diff",
    TOOLS_ROOT / "registry_sideeffect_diff.py",
)


def write_reg(path: Path, body: str) -> None:
    text = "Windows Registry Editor Version 5.00\r\n\r\n" + body.strip().replace("\n", "\r\n") + "\r\n"
    path.write_text(text, encoding="utf-16")


class RegistrySideeffectDiffTests(unittest.TestCase):
    def test_reordered_values_do_not_report_changes(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.reg"
            after_path = temp_path / "after.reg"

            write_reg(
                before_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Beta"=dword:00000002
"Alpha"=dword:00000001
                """,
            )
            write_reg(
                after_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Alpha"=dword:00000001
"Beta"=dword:00000002
                """,
            )

            report = registry_sideeffect_diff.build_diff_report(before_path, after_path)

            self.assertIn("Detected format: semantic-registry (registry-export -> registry-export)", report)
            self.assertIn("- added_keys: 0", report)
            self.assertIn("- removed_keys: 0", report)
            self.assertIn("- modified_values: 0", report)

    def test_reports_key_add_remove_and_value_changes(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.reg"
            after_path = temp_path / "after.reg"

            write_reg(
                before_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Shared]
"Mode"=dword:00000000
"Removed"="gone"

[HKEY_LOCAL_MACHINE\\Software\\Old]
"Legacy"=dword:00000001
                """,
            )
            write_reg(
                after_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Shared]
"Added"="new"
"Mode"=dword:00000001

[HKEY_LOCAL_MACHINE\\Software\\New]
@="default"
"Enabled"=dword:00000001
                """,
            )

            report = registry_sideeffect_diff.build_diff_report(before_path, after_path)

            self.assertIn("- added_keys: 1", report)
            self.assertIn("- removed_keys: 1", report)
            self.assertIn("- added_values: 3", report)
            self.assertIn("- removed_values: 2", report)
            self.assertIn("- modified_values: 1", report)
            self.assertIn("- [HKEY_LOCAL_MACHINE\\Software\\New]", report)
            self.assertIn("- [HKEY_LOCAL_MACHINE\\Software\\Old]", report)
            self.assertIn('- [HKEY_LOCAL_MACHINE\\Software\\Shared] Added | string | new', report)
            self.assertIn('- [HKEY_LOCAL_MACHINE\\Software\\Shared] Removed | string | gone', report)
            self.assertIn(
                '- [HKEY_LOCAL_MACHINE\\Software\\Shared] Mode | dword:00000000 -> dword:00000001',
                report,
            )

    def test_wrapped_hex_payloads_compare_semantically(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.reg"
            after_path = temp_path / "after.reg"

            write_reg(
                before_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Path"=hex(2):25,00,50,00,41,00,54,00,48,00,25,00,00,00
                """,
            )
            write_reg(
                after_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Path"=hex(2):25,00,50,00,41,00,\
  54,00,48,00,25,00,00,00
                """,
            )

            report = registry_sideeffect_diff.build_diff_report(before_path, after_path)

            self.assertIn("- modified_values: 0", report)

    def test_main_writes_output_file(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.reg"
            after_path = temp_path / "after.reg"
            output_path = temp_path / "nested" / "diff.txt"
            output_json_path = temp_path / "nested" / "diff.json"

            write_reg(
                before_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Enabled"=dword:00000000
                """,
            )
            write_reg(
                after_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Enabled"=dword:00000001
                """,
            )

            exit_code = registry_sideeffect_diff.main(
                [
                    "--before",
                    str(before_path),
                    "--after",
                    str(after_path),
                    "--output",
                    str(output_path),
                    "--output-json",
                    str(output_json_path),
                ]
            )

            self.assertEqual(exit_code, 0)
            self.assertTrue(output_path.exists())
            self.assertTrue(output_json_path.exists())
            self.assertIn(
                '- [HKEY_LOCAL_MACHINE\\Software\\Example] Enabled | dword:00000000 -> dword:00000001',
                output_path.read_text(encoding="utf-8"),
            )
            payload = json.loads(output_json_path.read_text(encoding="utf-8"))
            self.assertEqual(payload["detected_format"], "semantic-registry")
            self.assertEqual(payload["summary_counts"]["modified_values"], 1)
            self.assertEqual(payload["summary_counts"]["added_values"], 0)

    def test_build_diff_payload_returns_machine_readable_counts(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.txt"
            after_path = temp_path / "after.txt"

            before_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Enabled    REG_DWORD    0x1",
                        "    Name    REG_SZ    Example",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            after_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Enabled    REG_DWORD    0x1",
                        "    Name    REG_SZ    Example updated",
                        "    Path    REG_EXPAND_SZ    @%SystemRoot%",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            payload = registry_sideeffect_diff.build_diff_payload(before_path, after_path)

            self.assertEqual(payload["detected_format"], "semantic-registry")
            self.assertEqual(payload["summary_counts"]["added_values"], 1)
            self.assertEqual(payload["summary_counts"]["modified_values"], 1)
            self.assertEqual(payload["summary_counts"]["removed_values"], 0)

    def test_registry_dump_text_compares_semantically(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.txt"
            after_path = temp_path / "after.txt"

            before_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Alpha    REG_DWORD    0x1",
                        "    Beta    REG_SZ    first",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            after_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Beta    REG_SZ    second",
                        "    Gamma    REG_DWORD    0x3",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = registry_sideeffect_diff.build_diff_report(before_path, after_path)

            self.assertIn("Detected format: semantic-registry (registry-dump-text -> registry-dump-text)", report)
            self.assertIn("- added_keys: 0", report)
            self.assertIn("- added_values: 1", report)
            self.assertIn("- removed_values: 1", report)
            self.assertIn("- modified_values: 1", report)
            self.assertIn('- [HKEY_LOCAL_MACHINE\\Software\\Example] Gamma | dword | 00000003', report)
            self.assertIn('- [HKEY_LOCAL_MACHINE\\Software\\Example] Alpha | dword | 00000001', report)
            self.assertIn('- [HKEY_LOCAL_MACHINE\\Software\\Example] Beta | string:first -> string:second', report)

    def test_export_and_dump_of_same_state_compare_cleanly(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            export_path = temp_path / "state.reg"
            dump_path = temp_path / "state.txt"

            write_reg(
                export_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Enabled"=dword:00000001
"Name"="Example"
                """,
            )
            dump_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Enabled    REG_DWORD    0x1",
                        "    Name    REG_SZ    Example",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = registry_sideeffect_diff.build_diff_report(export_path, dump_path)

            self.assertIn("Detected format: semantic-registry (registry-export -> registry-dump-text)", report)
            self.assertIn("- added_values: 0", report)
            self.assertIn("- removed_values: 0", report)
            self.assertIn("- modified_values: 0", report)
            self.assertIn("- unchanged_values: 2", report)

    def test_expand_sz_export_matches_dump_text(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            export_path = temp_path / "state.reg"
            dump_path = temp_path / "state.txt"

            write_reg(
                export_path,
                """
[HKEY_LOCAL_MACHINE\\Software\\Example]
"Description"=hex(2):40,00,25,00,53,00,79,00,73,00,74,00,65,00,6d,00,52,00,6f,00,6f,00,74,00,25,00,00,00
                """,
            )
            dump_path.write_text(
                "\n".join(
                    [
                        "HKEY_LOCAL_MACHINE\\Software\\Example",
                        "    Description    REG_EXPAND_SZ    @%SystemRoot%",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = registry_sideeffect_diff.build_diff_report(export_path, dump_path)

            self.assertIn("- added_values: 0", report)
            self.assertIn("- removed_values: 0", report)
            self.assertIn("- modified_values: 0", report)
            self.assertIn("- unchanged_values: 1", report)

    def test_generic_text_fallback_filters_noise(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            temp_path = Path(temp_root)
            before_path = temp_path / "before.txt"
            after_path = temp_path / "after.txt"

            before_path.write_text(
                "\n".join(
                    [
                        "NAME NOT FOUND",
                        "BUFFER OVERFLOW",
                        "Stable line",
                        "Removed line",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            after_path.write_text(
                "\n".join(
                    [
                        "NO MORE ENTRIES",
                        "Stable line",
                        "Added line",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = registry_sideeffect_diff.build_diff_report(before_path, after_path, max_entries_per_section=10)

            self.assertIn("Detected format: generic-text", report)
            self.assertIn("- ignored_before_noise_lines: 3", report)
            self.assertIn("- ignored_after_noise_lines: 2", report)
            self.assertIn("- added_lines: 1", report)
            self.assertIn("- removed_lines: 1", report)
            self.assertIn("- (1x) Added line", report)
            self.assertIn("- (1x) Removed line", report)


if __name__ == "__main__":
    unittest.main()
