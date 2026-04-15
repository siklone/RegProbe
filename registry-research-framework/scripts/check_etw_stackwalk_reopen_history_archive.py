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
DEFAULT_CURRENT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.json"
DEFAULT_PREVIOUS_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.previous.json"
DEFAULT_TRANSITION_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.json"
DEFAULT_BASELINE_ARCHIVE_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive.json"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive-check.md"

if str(CURRENT_DIR) not in sys.path:
    sys.path.insert(0, str(CURRENT_DIR))

from materialize_etw_stackwalk_reopen_history_archive import build_history_plan  # noqa: E402
from materialize_etw_stackwalk_reopen_history_archive import load_json  # noqa: E402
from materialize_etw_stackwalk_reopen_history_archive import load_json_if_exists  # noqa: E402
from materialize_etw_stackwalk_reopen_history_archive import portable_path  # noqa: E402
from materialize_etw_stackwalk_reopen_history_archive import resolve_repo_path  # noqa: E402


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


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


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def compare_history_summary(
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
        "source_current_snapshot_path",
        "source_previous_snapshot_path",
        "source_transition_summary_path",
        "source_transition_summary_markdown_path",
        "source_baseline_archive_summary_path",
        "source_baseline_archive_markdown_path",
        "history_status",
        "history_seed_source",
        "transition_status",
        "current_snapshot_id",
        "previous_snapshot_id",
        "retained_baseline_snapshot_id",
        "seed_previous_snapshot_command",
        "seed_previous_snapshot_markdown_command",
        "persist_current_snapshot_history_command",
        "refresh_transition_summary_command",
        "history_candidate_count",
        "focus_snapshot_id",
    ):
        if surface.get(key) != expected.get(key):
            errors.append(f"{key} mismatch: expected {expected.get(key)!r}, saw {surface.get(key)!r}.")
    for key in ("blocker", "next_action"):
        if (surface.get("operator") or {}).get(key) != (expected.get("operator") or {}).get(key):
            errors.append(f"operator.{key} mismatch.")
    if int((surface.get("counts") or {}).get("manifest_files_copied") or 0) != len(surface.get("manifest_files") or []):
        errors.append("manifest_files_copied does not match manifest_files length.")
    if int((surface.get("counts") or {}).get("seed_files_copied") or 0) != len(surface.get("seed_files") or []):
        errors.append("seed_files_copied does not match seed_files length.")
    if int((surface.get("counts") or {}).get("command_files_written") or 0) != len(surface.get("command_files") or []):
        errors.append("command_files_written does not match command_files length.")
    if int((surface.get("counts") or {}).get("pack_files_checksummed") or 0) != len(surface.get("pack_files") or []):
        errors.append("pack_files_checksummed does not match pack_files length.")
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "history_status": surface.get("history_status"),
        "current_snapshot_id": surface.get("current_snapshot_id"),
        "previous_snapshot_id": surface.get("previous_snapshot_id"),
    }


def validate_pack_assets(summary: dict[str, Any]) -> tuple[list[str], dict[str, int]]:
    errors: list[str] = []
    output_root = resolve_repo_path(summary.get("output_root"))
    archive_path = resolve_repo_path(((summary.get("archive") or {}).get("path")) or summary.get("archive_path"))
    pack_files = list(summary.get("pack_files") or [])
    checked_pack_files = 0
    checked_archive_files = 0

    if not output_root or not output_root.exists():
        errors.append("history archive output_root missing")
    else:
        for entry in pack_files:
            rel = str(entry.get("path") or "")
            path = output_root / rel
            if not path.exists():
                errors.append(f"pack file missing: {rel}")
                continue
            if path.stat().st_size != int(entry.get("size_bytes") or 0):
                errors.append(f"pack file size mismatch: {rel}")
            if sha256_file(path) != str(entry.get("sha256") or ""):
                errors.append(f"pack file sha256 mismatch: {rel}")
            checked_pack_files += 1

    if not archive_path or not archive_path.exists():
        errors.append("history archive zip missing")
    else:
        archive_meta = summary.get("archive") or {}
        if sha256_file(archive_path) != str(archive_meta.get("sha256") or ""):
            errors.append("history archive zip sha256 mismatch")
        with zipfile.ZipFile(archive_path) as zf:
            names = sorted(name for name in zf.namelist() if not name.endswith("/"))
            expected_names = sorted(str(entry.get("path") or "") for entry in pack_files if str(entry.get("path") or ""))
            if names != expected_names:
                errors.append("archive entries do not match pack_files")
            for entry in pack_files:
                rel = str(entry.get("path") or "")
                try:
                    data = zf.read(rel)
                except KeyError:
                    errors.append(f"archive entry missing: {rel}")
                    continue
                if len(data) != int(entry.get("size_bytes") or 0):
                    errors.append(f"archive entry size mismatch: {rel}")
                if sha256_bytes(data) != str(entry.get("sha256") or ""):
                    errors.append(f"archive entry sha256 mismatch: {rel}")
                checked_archive_files += 1

    return errors, {
        "checked_pack_files": checked_pack_files,
        "checked_archive_files": checked_archive_files,
    }


def render_markdown(payload: dict[str, Any]) -> str:
    lines = [
        "# ETW Stackwalk Reopen History Archive Check",
        "",
        f"- Status: `{payload.get('check_status')}`",
        f"- History status: `{payload.get('history_status')}`",
        f"- Current snapshot id: `{payload.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{payload.get('previous_snapshot_id')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    else:
        for error in errors:
            lines.append(f"- {error}")
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the ETW reopen history archive.")
    parser.add_argument("--current", type=Path, default=DEFAULT_CURRENT_PATH)
    parser.add_argument("--previous", type=Path, default=DEFAULT_PREVIOUS_PATH)
    parser.add_argument("--transition", type=Path, default=DEFAULT_TRANSITION_PATH)
    parser.add_argument("--baseline-archive-summary", type=Path, default=DEFAULT_BASELINE_ARCHIVE_SUMMARY_PATH)
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    current_snapshot = load_json(args.current)
    previous_snapshot = load_json_if_exists(args.previous)
    transition_summary = load_json(args.transition)
    baseline_archive_summary = load_json(args.baseline_archive_summary)
    surface = load_json(args.summary)
    expected = build_history_plan(
        current_snapshot,
        previous_snapshot,
        transition_summary,
        baseline_archive_summary,
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
        transition_summary_path=args.transition,
        baseline_archive_summary_path=args.baseline_archive_summary,
    )
    payload = compare_history_summary(surface, expected)
    asset_errors, asset_counts = validate_pack_assets(surface)
    payload["errors"].extend(asset_errors)
    payload["asset_counts"] = asset_counts
    if asset_errors:
        payload["check_status"] = "error"
    payload["current_path"] = portable_path(args.current)
    payload["previous_path"] = portable_path(args.previous)
    payload["transition_path"] = portable_path(args.transition)
    payload["summary_path"] = portable_path(args.summary)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps({"check": portable_path(args.output), "status": payload.get("check_status")}, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
