#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
CURRENT_SNAPSHOT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.json"
TRANSITION_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive.md"
DEFAULT_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive.zip"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def resolve_repo_path(path_value: str) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else (REPO_ROOT / path)


def load_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"{path} JSON payload is not an object")
    return payload


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def reset_dir(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path)
    path.mkdir(parents=True, exist_ok=True)


def copy_file(src: Path, dst: Path) -> None:
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def pack_file_manifest(pack_root: Path) -> list[dict[str, Any]]:
    files: list[dict[str, Any]] = []
    for path in sorted(pack_root.rglob("*")):
        if not path.is_file():
            continue
        files.append(
            {
                "path": path.relative_to(pack_root).as_posix(),
                "size_bytes": path.stat().st_size,
                "sha256": sha256_file(path),
            }
        )
    return files


def build_archive(source_root: Path, archive_path: Path) -> None:
    archive_path.parent.mkdir(parents=True, exist_ok=True)
    if archive_path.exists():
        archive_path.unlink()
    with zipfile.ZipFile(archive_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(source_root.rglob("*")):
            if path.is_file():
                zf.write(path, arcname=path.relative_to(source_root).as_posix())


def sidecar_paths(snapshot_path: Path, transition_path: Path) -> list[Path]:
    candidates = [
        snapshot_path,
        snapshot_path.with_suffix(".md"),
        transition_path,
        transition_path.with_suffix(".md"),
    ]
    result: list[Path] = []
    seen: set[str] = set()
    for path in candidates:
        key = str(path.resolve()) if path.exists() else str(path)
        if key in seen or not path.exists():
            continue
        seen.add(key)
        result.append(path)
    return result


def build_archive_plan(
    snapshot: dict[str, Any],
    transition: dict[str, Any],
    *,
    snapshot_path: Path = CURRENT_SNAPSHOT_PATH,
    transition_path: Path = TRANSITION_SUMMARY_PATH,
) -> dict[str, Any]:
    snapshot_id = str(snapshot.get("snapshot_id") or "")
    transition_status = str(transition.get("transition_status") or "baseline")
    archive_status = "baseline-ready" if transition_status == "baseline" else "retained"
    operator_blocker = "retain-baseline-for-next-diff" if transition_status == "baseline" else "archive-current-snapshot"
    next_action = (
        "Retain this snapshot as the next previous baseline before expecting diff-driven transition summaries."
        if transition_status == "baseline"
        else "Refresh the retained baseline only after reviewing the current transition summary."
    )
    snapshot_target = "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"
    snapshot_md_target = "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md"
    retained_snapshot_json = "manifests/etw-stackwalk-reopen-snapshot.json"
    retained_snapshot_md = "manifests/etw-stackwalk-reopen-snapshot.md"
    return {
        "source_current_snapshot_path": portable_path(snapshot_path),
        "source_current_snapshot_markdown_path": portable_path(snapshot_path.with_suffix(".md")),
        "source_transition_summary_path": portable_path(transition_path),
        "source_transition_summary_markdown_path": portable_path(transition_path.with_suffix(".md")),
        "archive_status": archive_status,
        "transition_status": transition_status,
        "retained_snapshot_id": snapshot_id,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "promote_previous_snapshot_command": f"cp {portable_path(DEFAULT_OUTPUT_ROOT / retained_snapshot_json)} {snapshot_target}",
        "promote_previous_snapshot_markdown_command": f"cp {portable_path(DEFAULT_OUTPUT_ROOT / retained_snapshot_md)} {snapshot_md_target}",
        "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        "archive_candidate_count": int((snapshot.get('counts') or {}).get('candidate_count') or 0),
        "focus_snapshot_id": snapshot_id,
    }


def write_commands(commands_root: Path, plan: dict[str, Any]) -> list[str]:
    commands_root.mkdir(parents=True, exist_ok=True)
    command_specs = {
        "01-promote-previous-snapshot.txt": [
            "promote_previous_snapshot_command:",
            str(plan.get("promote_previous_snapshot_command") or ""),
            "",
            "promote_previous_snapshot_markdown_command:",
            str(plan.get("promote_previous_snapshot_markdown_command") or ""),
        ],
        "02-refresh-transition-summary.txt": [
            "refresh_transition_summary_command:",
            str(plan.get("refresh_transition_summary_command") or ""),
        ],
    }
    files: list[str] = []
    for filename, lines in command_specs.items():
        write_text(commands_root / filename, "\n".join(lines) + "\n")
        files.append(filename)
    return files


def write_readme(pack_root: Path, payload: dict[str, Any]) -> None:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# ETW Stackwalk Reopen Baseline Archive",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Archive status: `{payload.get('archive_status')}`",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Retained snapshot id: `{payload.get('retained_snapshot_id')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Manifest files copied: `{counts.get('manifest_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Commands",
        "",
        f"- `{payload.get('promote_previous_snapshot_command')}`",
        f"- `{payload.get('promote_previous_snapshot_markdown_command')}`",
        f"- `{payload.get('refresh_transition_summary_command')}`",
        "",
    ]
    write_text(pack_root / "README.md", "\n".join(lines))


def materialize_baseline_archive(
    snapshot: dict[str, Any],
    transition: dict[str, Any],
    *,
    snapshot_path: Path = CURRENT_SNAPSHOT_PATH,
    transition_path: Path = TRANSITION_SUMMARY_PATH,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    archive_path: Path = DEFAULT_ARCHIVE_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    plan = build_archive_plan(snapshot, transition, snapshot_path=snapshot_path, transition_path=transition_path)

    reset_dir(output_root)
    manifests_root = output_root / "manifests"
    commands_root = output_root / "commands"

    manifest_files: list[str] = []
    for src in sidecar_paths(snapshot_path, transition_path):
        rel = f"manifests/{src.name}"
        copy_file(src, output_root / rel)
        manifest_files.append(rel)

    command_files = [f"commands/{name}" for name in write_commands(commands_root, plan)]
    write_readme(output_root, {"generated_utc": generated_utc, **plan, "counts": {}})
    pack_files_pre = pack_file_manifest(output_root)
    write_json(output_root / "CHECKSUMS.json", {"schema_version": "1.0", "file_count": len(pack_files_pre), "files": pack_files_pre})
    pack_files = pack_file_manifest(output_root)
    build_archive(output_root, archive_path)

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        **plan,
        "output_root": portable_path(output_root),
        "archive_path": portable_path(archive_path),
        "archive": {
            "path": portable_path(archive_path),
            "size_bytes": archive_path.stat().st_size if archive_path.exists() else 0,
            "sha256": sha256_file(archive_path) if archive_path.exists() else None,
        },
        "counts": {
            "manifest_files_copied": len(manifest_files),
            "command_files_written": len(command_files),
            "pack_files_checksummed": len(pack_files),
        },
        "manifest_files": manifest_files,
        "command_files": command_files,
        "pack_files": pack_files,
    }
    write_json(summary_path, payload)
    write_text(markdown_path, render_markdown(payload))
    return payload


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# ETW Stackwalk Reopen Baseline Archive",
        "",
        f"- Archive status: `{payload.get('archive_status')}`",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Retained snapshot id: `{payload.get('retained_snapshot_id')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Manifest files copied: `{counts.get('manifest_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Commands",
        "",
        f"- `{payload.get('promote_previous_snapshot_command')}`",
        f"- `{payload.get('promote_previous_snapshot_markdown_command')}`",
        f"- `{payload.get('refresh_transition_summary_command')}`",
        "",
    ]
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Materialize a baseline archive for ETW reopen snapshots.")
    parser.add_argument("--snapshot", type=Path, default=CURRENT_SNAPSHOT_PATH)
    parser.add_argument("--transition", type=Path, default=TRANSITION_SUMMARY_PATH)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--summary-output", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--archive-output", type=Path, default=DEFAULT_ARCHIVE_PATH)
    args = parser.parse_args()

    payload = materialize_baseline_archive(
        load_json(args.snapshot),
        load_json(args.transition),
        snapshot_path=args.snapshot,
        transition_path=args.transition,
        output_root=args.output_root,
        summary_path=args.summary_output,
        markdown_path=args.markdown_output,
        archive_path=args.archive_output,
    )
    print(
        json.dumps(
            {
                "summary": portable_path(args.summary_output),
                "archive_status": payload.get("archive_status"),
                "retained_snapshot_id": payload.get("retained_snapshot_id"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
