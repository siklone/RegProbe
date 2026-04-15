#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import dataclass
from pathlib import Path
from zipfile import ZipFile


@dataclass(frozen=True)
class ReleaseArtifactNames:
    portable_zip: str
    cli_zip: str
    checksum_file: str


def build_artifact_names(version_label: str, runtime: str) -> ReleaseArtifactNames:
    return ReleaseArtifactNames(
        portable_zip=f"RegProbe-Portable-{version_label}-{runtime}.zip",
        cli_zip=f"RegProbe-Cli-{version_label}-{runtime}.zip",
        checksum_file=f"RegProbe-{version_label}-{runtime}-sha256.txt",
    )


def compute_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_checksum_file(path: Path) -> dict[str, str]:
    checksums: dict[str, str] = {}
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line:
            continue
        parts = line.split()
        if len(parts) < 2:
            raise ValueError(f"Malformed checksum line: {raw_line!r}")
        checksums[parts[-1]] = parts[0].lower()
    return checksums


def zip_contains(zip_path: Path, expected_entries: list[str]) -> list[str]:
    with ZipFile(zip_path) as archive:
        entries = {name.replace("\\", "/").lower() for name in archive.namelist()}

    missing: list[str] = []
    for expected in expected_entries:
        normalized = expected.replace("\\", "/").lower()
        if normalized not in entries:
            missing.append(expected)
    return missing


def build_release_asset_report(asset_dir: Path, version_label: str, runtime: str) -> dict[str, object]:
    names = build_artifact_names(version_label, runtime)
    portable_path = asset_dir / names.portable_zip
    cli_path = asset_dir / names.cli_zip
    checksum_path = asset_dir / names.checksum_file

    errors: list[str] = []

    for path in (portable_path, cli_path, checksum_path):
        if not path.exists():
            errors.append(f"Missing expected release artifact: {path.name}")

    checksum_entries: dict[str, str] = {}
    if checksum_path.exists():
        try:
            checksum_entries = parse_checksum_file(checksum_path)
        except ValueError as exc:
            errors.append(str(exc))

    for artifact_path in (portable_path, cli_path):
        if not artifact_path.exists():
            continue

        if artifact_path.name not in checksum_entries:
            errors.append(f"Checksum file does not mention {artifact_path.name}")
            continue

        actual_hash = compute_sha256(artifact_path)
        expected_hash = checksum_entries[artifact_path.name]
        if actual_hash != expected_hash:
            errors.append(
                f"Checksum mismatch for {artifact_path.name}: expected {expected_hash}, got {actual_hash}"
            )

    if portable_path.exists():
        missing_portable = zip_contains(
            portable_path,
            [
                "RegProbe.App.exe",
                "ElevatedHost/RegProbe.ElevatedHost.exe",
                "Docs/product/user-guide.md",
            ],
        )
        errors.extend([f"Portable archive missing {item}" for item in missing_portable])

    if cli_path.exists():
        missing_cli = zip_contains(
            cli_path,
            [
                "RegProbe.CLI.exe",
                "README.md",
                "LICENSE",
                "Docs/product/cli.md",
            ],
        )
        errors.extend([f"CLI archive missing {item}" for item in missing_cli])

    return {
        "status": "PASS" if not errors else "FAIL",
        "asset_dir": asset_dir.as_posix(),
        "version_label": version_label,
        "runtime": runtime,
        "artifacts": {
            "portable_zip": names.portable_zip,
            "cli_zip": names.cli_zip,
            "checksum_file": names.checksum_file,
        },
        "errors": errors,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate packaged RegProbe release artifacts.")
    parser.add_argument("--asset-dir", required=True)
    parser.add_argument("--version-label", required=True)
    parser.add_argument("--runtime", default="win-x64")
    parser.add_argument("--write-report")
    args = parser.parse_args(argv)

    report = build_release_asset_report(
        asset_dir=Path(args.asset_dir),
        version_label=args.version_label,
        runtime=args.runtime,
    )
    if args.write_report:
        report_path = Path(args.write_report)
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if report["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
