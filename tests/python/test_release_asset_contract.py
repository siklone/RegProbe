from __future__ import annotations

import hashlib
import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPTS_DIR = REPO_ROOT / "scripts"
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


release_assets = load_module("check_release_assets", SCRIPTS_DIR / "check_release_assets.py")


class ReleaseAssetContractTests(unittest.TestCase):
    def test_release_asset_report_passes_for_matching_archives_and_checksums(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            asset_dir = Path(temp_root)
            names = release_assets.build_artifact_names("v1.2.3", "win-x64")
            portable_zip = asset_dir / names.portable_zip
            cli_zip = asset_dir / names.cli_zip
            checksum_file = asset_dir / names.checksum_file

            self._write_zip(
                portable_zip,
                {
                    "RegProbe.App.exe": b"app",
                    "ElevatedHost/RegProbe.ElevatedHost.exe": b"host",
                    "Docs/product/user-guide.md": b"# guide",
                },
            )
            self._write_zip(
                cli_zip,
                {
                    "RegProbe.CLI.exe": b"cli",
                    "README.md": b"# readme",
                    "LICENSE": b"MIT",
                    "Docs/product/cli.md": b"# cli",
                },
            )

            checksum_file.write_text(
                "\n".join(
                    [
                        f"{self._sha256(portable_zip)}  {portable_zip.name}",
                        f"{self._sha256(cli_zip)}  {cli_zip.name}",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            report = release_assets.build_release_asset_report(asset_dir, "v1.2.3", "win-x64")

            self.assertEqual(report["status"], "PASS")
            self.assertEqual(report["errors"], [])

    def test_release_asset_report_flags_checksum_and_missing_entry_drift(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            asset_dir = Path(temp_root)
            names = release_assets.build_artifact_names("v9.9.9", "win-x64")
            portable_zip = asset_dir / names.portable_zip
            cli_zip = asset_dir / names.cli_zip
            checksum_file = asset_dir / names.checksum_file

            self._write_zip(
                portable_zip,
                {
                    "RegProbe.App.exe": b"app",
                    "ElevatedHost/RegProbe.ElevatedHost.exe": b"host",
                },
            )
            self._write_zip(
                cli_zip,
                {
                    "RegProbe.CLI.exe": b"cli",
                    "README.md": b"# readme",
                },
            )

            checksum_file.write_text(
                f"{'0' * 64}  {portable_zip.name}\n",
                encoding="utf-8",
            )

            report = release_assets.build_release_asset_report(asset_dir, "v9.9.9", "win-x64")

            self.assertEqual(report["status"], "FAIL")
            errors = report["errors"]
            self.assertTrue(any("Checksum mismatch" in error for error in errors))
            self.assertTrue(any("does not mention" in error for error in errors))
            self.assertTrue(any("Portable archive missing Docs/product/user-guide.md" in error for error in errors))
            self.assertTrue(any("CLI archive missing LICENSE" in error for error in errors))
            self.assertTrue(any("CLI archive missing Docs/product/cli.md" in error for error in errors))

    def test_main_writes_json_report_when_requested(self) -> None:
        with tempfile.TemporaryDirectory(dir=REPO_ROOT) as temp_root:
            asset_dir = Path(temp_root)
            names = release_assets.build_artifact_names("v2.0.0", "win-x64")
            portable_zip = asset_dir / names.portable_zip
            cli_zip = asset_dir / names.cli_zip
            checksum_file = asset_dir / names.checksum_file
            report_path = asset_dir / "release-asset-check.json"

            self._write_zip(
                portable_zip,
                {
                    "RegProbe.App.exe": b"app",
                    "ElevatedHost/RegProbe.ElevatedHost.exe": b"host",
                    "Docs/product/user-guide.md": b"# guide",
                },
            )
            self._write_zip(
                cli_zip,
                {
                    "RegProbe.CLI.exe": b"cli",
                    "README.md": b"# readme",
                    "LICENSE": b"MIT",
                    "Docs/product/cli.md": b"# cli",
                },
            )

            checksum_file.write_text(
                "\n".join(
                    [
                        f"{self._sha256(portable_zip)}  {portable_zip.name}",
                        f"{self._sha256(cli_zip)}  {cli_zip.name}",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            exit_code = release_assets.main(
                [
                    "--asset-dir",
                    asset_dir.as_posix(),
                    "--version-label",
                    "v2.0.0",
                    "--runtime",
                    "win-x64",
                    "--write-report",
                    report_path.as_posix(),
                ]
            )

            self.assertEqual(exit_code, 0)
            self.assertTrue(report_path.exists())
            written_report = report_path.read_text(encoding="utf-8")
            self.assertIn('"status": "PASS"', written_report)

    @staticmethod
    def _write_zip(path: Path, entries: dict[str, bytes]) -> None:
        with ZipFile(path, "w") as archive:
            for name, payload in entries.items():
                archive.writestr(name, payload)

    @staticmethod
    def _sha256(path: Path) -> str:
        digest = hashlib.sha256()
        digest.update(path.read_bytes())
        return digest.hexdigest()


if __name__ == "__main__":
    unittest.main()
