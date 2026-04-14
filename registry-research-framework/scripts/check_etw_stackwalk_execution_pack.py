#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_DIR = Path(__file__).resolve().parent
DEFAULT_MANIFEST_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.json"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from materialize_etw_stackwalk_execution_pack import build_pack_plan  # noqa: E402
from materialize_etw_stackwalk_execution_pack import load_json  # noqa: E402
from materialize_etw_stackwalk_execution_pack import portable_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def resolve_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    path = Path(cleaned)
    return path if path.is_absolute() else (REPO_ROOT / path)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def entry_map(payload: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("candidate_id") or ""): entry
        for entry in payload.get("entries") or []
        if str(entry.get("candidate_id") or "").strip()
    }


def validate_file_entry(pack_root: Path, entry: dict[str, Any]) -> list[str]:
    relative_path = str(entry.get("path") or "").strip()
    if not relative_path:
        return ["pack file entry is missing path"]
    path = pack_root / relative_path
    if not path.exists():
        return [f"pack file missing: {relative_path}"]
    errors: list[str] = []
    expected_size = int(entry.get("size_bytes") or 0)
    if path.stat().st_size != expected_size:
        errors.append(
            f"pack file size mismatch: {relative_path} expected={expected_size} actual={path.stat().st_size}"
        )
    expected_sha = str(entry.get("sha256") or "")
    if sha256_file(path) != expected_sha:
        errors.append(f"pack file sha256 mismatch: {relative_path}")
    return errors


def validate_zip_entry(zf: zipfile.ZipFile, entry: dict[str, Any]) -> list[str]:
    relative_path = str(entry.get("path") or "").strip()
    if not relative_path:
        return ["pack file entry is missing path"]
    try:
        data = zf.read(relative_path)
    except KeyError:
        return [f"archive entry missing: {relative_path}"]
    errors: list[str] = []
    expected_size = int(entry.get("size_bytes") or 0)
    if len(data) != expected_size:
        errors.append(f"archive entry size mismatch: {relative_path} expected={expected_size} actual={len(data)}")
    expected_sha = str(entry.get("sha256") or "")
    if sha256_bytes(data) != expected_sha:
        errors.append(f"archive entry sha256 mismatch: {relative_path}")
    return errors


def compare_pack_summary(
    surface: dict[str, Any],
    expected: dict[str, Any],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    if surface.get("schema_version") != "1.0":
        errors.append("schema_version must be 1.0.")

    for key in (
        "source_manifest_path",
        "source_manifest_markdown_path",
        "source_batch_path",
        "source_run_path",
        "source_hold_reopen_plan_path",
        "manifest_status",
        "pack_status",
        "include_holds",
        "requested_candidate_ids",
        "selected_candidate_ids",
        "excluded_candidate_ids",
        "required_repo_paths",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")

    if (surface.get("operator") or {}).get("next_action") != (expected.get("operator") or {}).get("next_action"):
        errors.append("operator.next_action mismatch.")
    if int((surface.get("counts") or {}).get("requested_candidates") or 0) != len(expected.get("requested_candidate_ids") or []):
        errors.append("requested_candidates count does not match requested_candidate_ids.")
    if int((surface.get("counts") or {}).get("selected_candidates") or 0) != len(expected.get("selected_candidate_ids") or []):
        errors.append("selected_candidates count does not match selected_candidate_ids.")
    if int((surface.get("counts") or {}).get("excluded_candidates") or 0) != len(expected.get("excluded_candidate_ids") or []):
        errors.append("excluded_candidates count does not match excluded_candidate_ids.")
    if int((surface.get("counts") or {}).get("repo_files_copied") or 0) != len(surface.get("copied_repo_paths") or []):
        errors.append("repo_files_copied does not match copied_repo_paths length.")
    if int((surface.get("counts") or {}).get("command_files_written") or 0) != len(surface.get("command_files") or []):
        errors.append("command_files_written does not match command_files length.")
    if int((surface.get("counts") or {}).get("manifest_files_written") or 0) != len(surface.get("manifest_files") or []):
        errors.append("manifest_files_written does not match manifest_files length.")
    if int((surface.get("counts") or {}).get("pack_files_checksummed") or 0) != len(surface.get("pack_files") or []):
        errors.append("pack_files_checksummed does not match pack_files length.")

    surface_entries = entry_map(surface)
    expected_entries = entry_map(expected)
    if sorted(surface_entries) != sorted(expected_entries):
        errors.append("candidate set does not match current execution manifest.")
    for candidate_id, expected_entry in expected_entries.items():
        entry = surface_entries.get(candidate_id)
        if not entry:
            continue
        for key in (
            "selected",
            "selection_reason",
            "actionability",
            "profile_id",
            "run_id",
            "host_etl_repo_path",
            "registry_path",
            "value_name",
            "selected_command",
            "effective_config_command",
            "dispatch_command",
            "include_holds_run_command",
            "next_action_hint",
            "promotion_blockers",
            "reopen_prerequisites",
        ):
            if entry.get(key) != expected_entry.get(key):
                errors.append(f"{candidate_id}: {key} mismatch.")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "pack_status": surface.get("pack_status"),
        "manifest_status": surface.get("manifest_status"),
        "selected_candidates": int((surface.get("counts") or {}).get("selected_candidates") or 0),
    }


def validate_pack_assets(summary: dict[str, Any]) -> tuple[list[str], dict[str, int]]:
    errors: list[str] = []
    pack_root = resolve_path(summary.get("output_root"))
    archive_path = resolve_path(((summary.get("archive") or {}).get("path")) or summary.get("archive_path"))
    pack_files = list(summary.get("pack_files") or [])

    checked_pack_files = 0
    checked_archive_files = 0
    checksum_manifest_files = 0
    archive_entries = 0

    checksum_payload: dict[str, Any] = {}
    if not pack_root:
        errors.append("summary missing output_root")
    elif not pack_root.exists():
        errors.append(f"pack root missing: {portable_path(pack_root)}")
    else:
        for entry in pack_files:
            errors.extend(validate_file_entry(pack_root, entry))
            checked_pack_files += 1
        checksum_path = pack_root / "CHECKSUMS.json"
        if checksum_path.exists():
            checksum_payload = load_json(checksum_path)
            checksum_manifest_files = len(checksum_payload.get("files") or [])
            checksum_paths = {str(item.get("path") or "") for item in (checksum_payload.get("files") or [])}
            expected_paths = {str(item.get("path") or "") for item in pack_files if str(item.get("path") or "") != "CHECKSUMS.json"}
            if checksum_paths != expected_paths:
                errors.append("CHECKSUMS.json file list does not match summary pack_files")
            if int(checksum_payload.get("file_count") or 0) != len(checksum_paths):
                errors.append("CHECKSUMS.json file_count does not match files length")
        else:
            errors.append("CHECKSUMS.json missing from pack root")

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
            archive_entries = len([name for name in zf.namelist() if not name.endswith("/")])
            for entry in pack_files:
                errors.extend(validate_zip_entry(zf, entry))
                checked_archive_files += 1
            expected_archive_entries = sorted(str(item.get("path") or "") for item in pack_files if str(item.get("path") or ""))
            actual_archive_entries = sorted(name for name in zf.namelist() if not name.endswith("/"))
            if actual_archive_entries != expected_archive_entries:
                errors.append("archive entries do not match summary pack_files")

    return errors, {
        "checked_pack_files": checked_pack_files,
        "checked_archive_files": checked_archive_files,
        "checksum_manifest_files": checksum_manifest_files,
        "archive_entries": archive_entries,
        "command_files": len(summary.get("command_files") or []),
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# ETW Stackwalk Execution Pack Check",
        "",
        f"- Check status: `{payload.get('check_status')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Manifest status: `{payload.get('manifest_status')}`",
        f"- Selected candidates: `{payload.get('selected_candidates')}`",
        f"- Checked pack files: `{counts.get('checked_pack_files')}`",
        f"- Archive entries: `{counts.get('archive_entries')}`",
        f"- Command files: `{counts.get('command_files')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    else:
        for error in errors:
            lines.append(f"- `{error}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate a materialized ETW stackwalk execution pack.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST_PATH)
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    manifest = load_json(args.manifest)
    summary = load_json(args.summary)
    expected = build_pack_plan(manifest, manifest_path=args.manifest)
    payload = compare_pack_summary(summary, expected)
    asset_errors, counts = validate_pack_assets(summary)
    payload["errors"].extend(asset_errors)
    payload["check_status"] = "ok" if not payload["errors"] else "error"
    payload["summary_path"] = portable_path(args.summary)
    payload["manifest_path"] = portable_path(args.manifest)
    payload["counts"] = counts
    payload["archive"] = summary.get("archive") or {}
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
