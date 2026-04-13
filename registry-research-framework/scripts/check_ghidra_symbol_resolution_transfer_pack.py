#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-check.md"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def resolve_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    path = Path(cleaned)
    return path if path.is_absolute() else (REPO_ROOT / path)


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_file_entry(pack_root: Path, entry: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    relative_path = str(entry.get("path") or "").strip()
    if not relative_path:
        return ["pack file entry is missing path"]
    path = pack_root / relative_path
    if not path.exists():
        return [f"pack file missing: {relative_path}"]
    expected_size = int(entry.get("size_bytes") or 0)
    actual_size = path.stat().st_size
    if actual_size != expected_size:
        errors.append(f"pack file size mismatch: {relative_path} expected={expected_size} actual={actual_size}")
    expected_sha = str(entry.get("sha256") or "")
    actual_sha = sha256_file(path)
    if actual_sha != expected_sha:
        errors.append(f"pack file sha256 mismatch: {relative_path}")
    return errors


def validate_transfer_pack(summary: dict[str, Any], *, summary_path: Path = DEFAULT_SUMMARY_PATH, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    pack_root = resolve_path(summary.get("output_root"))
    archive_path = resolve_path(((summary.get("archive") or {}).get("path")) or summary.get("archive_path"))
    pack_files = list(summary.get("pack_files") or [])
    command_files = list(summary.get("command_files") or [])
    request_ids = list(summary.get("request_ids") or [])

    if not pack_root:
        errors.append("summary missing output_root")
    elif not pack_root.exists():
        errors.append(f"pack root missing: {portable_path(pack_root)}")

    checked_files = 0
    checksum_payload: dict[str, Any] = {}
    if pack_root and pack_root.exists():
        for entry in pack_files:
            errors.extend(validate_file_entry(pack_root, entry))
            checked_files += 1
        checksum_path = pack_root / "CHECKSUMS.json"
        if checksum_path.exists():
            checksum_payload = load_json(checksum_path)
            checksum_paths = {str(item.get("path") or "") for item in (checksum_payload.get("files") or [])}
            expected_paths = {str(item.get("path") or "") for item in pack_files if str(item.get("path") or "") != "CHECKSUMS.json"}
            if checksum_paths != expected_paths:
                errors.append("CHECKSUMS.json file list does not match summary pack_files")
            if int(checksum_payload.get("file_count") or 0) != len(checksum_paths):
                errors.append("CHECKSUMS.json file_count does not match files length")
        else:
            errors.append("CHECKSUMS.json missing from pack root")

    archive_entries: list[str] = []
    if not archive_path:
        errors.append("summary missing archive path")
    elif not archive_path.exists():
        errors.append(f"archive missing: {portable_path(archive_path)}")
    else:
        archive = summary.get("archive") or {}
        expected_archive_sha = str(archive.get("sha256") or "")
        if expected_archive_sha and sha256_file(archive_path) != expected_archive_sha:
            errors.append("archive sha256 mismatch")
        expected_archive_size = int(archive.get("size_bytes") or 0)
        if expected_archive_size and archive_path.stat().st_size != expected_archive_size:
            errors.append("archive size mismatch")
        with zipfile.ZipFile(archive_path) as zf:
            archive_entries = sorted(name for name in zf.namelist() if not name.endswith("/"))
        expected_archive_entries = sorted(str(item.get("path") or "") for item in pack_files if str(item.get("path") or ""))
        if archive_entries != expected_archive_entries:
            errors.append("archive entries do not match summary pack_files")

    if len(command_files) != len(request_ids):
        errors.append("command_files count does not match request_ids count")
    if int((summary.get("counts") or {}).get("command_files_written") or 0) != len(command_files):
        errors.append("command_files_written does not match command_files length")
    if int((summary.get("counts") or {}).get("pack_files_checksummed") or 0) != len(pack_files):
        errors.append("pack_files_checksummed does not match pack_files length")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "summary_path": portable_path(summary_path),
        "pack_status": summary.get("pack_status"),
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "counts": {
            "checked_pack_files": checked_files,
            "checksum_manifest_files": len((checksum_payload.get("files") or [])),
            "archive_entries": len(archive_entries),
            "command_files": len(command_files),
            "request_ids": len(request_ids),
        },
        "archive": summary.get("archive") or {},
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# Ghidra Symbol Resolution Transfer Pack Check",
        "",
        f"- Check status: `{payload.get('check_status')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Checked pack files: `{counts.get('checked_pack_files')}`",
        f"- Archive entries: `{counts.get('archive_entries')}`",
        f"- Command files: `{counts.get('command_files')}`",
        f"- Archive SHA-256: `{(payload.get('archive') or {}).get('sha256')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    for error in errors:
        lines.append(f"- `{error}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate a materialized Ghidra symbol-resolution transfer pack.")
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    summary = load_json(args.summary)
    payload = validate_transfer_pack(summary, summary_path=args.summary)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
