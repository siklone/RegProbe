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
PREVIOUS_SNAPSHOT_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-snapshot.previous.json"
TRANSITION_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-transition-summary.json"
BASELINE_ARCHIVE_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-baseline-archive.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.md"
DEFAULT_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-reopen-history-archive.zip"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path | None) -> str | None:
    if path is None:
        return None
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def resolve_repo_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    path = Path(cleaned)
    return path if path.is_absolute() else (REPO_ROOT / path)


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def load_json_if_exists(path: Path | None) -> dict[str, Any] | None:
    if path is None or not path.exists():
        return None
    return load_json(path)


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


def sidecar_paths(path: Path) -> list[Path]:
    candidates = [path, path.with_suffix(".md")]
    return [candidate for candidate in candidates if candidate.exists()]


def build_history_plan(
    current_snapshot: dict[str, Any],
    previous_snapshot: dict[str, Any] | None,
    transition_summary: dict[str, Any],
    baseline_archive_summary: dict[str, Any],
    *,
    current_snapshot_path: Path = CURRENT_SNAPSHOT_PATH,
    previous_snapshot_path: Path | None = PREVIOUS_SNAPSHOT_PATH,
    transition_summary_path: Path = TRANSITION_SUMMARY_PATH,
    baseline_archive_summary_path: Path = BASELINE_ARCHIVE_SUMMARY_PATH,
) -> dict[str, Any]:
    current_snapshot_id = str(current_snapshot.get("snapshot_id") or "")
    previous_snapshot_id = str((previous_snapshot or {}).get("snapshot_id") or "") or None
    retained_baseline_snapshot_id = (
        str(baseline_archive_summary.get("retained_snapshot_id") or "") or current_snapshot_id or None
    )
    baseline_root = resolve_repo_path(baseline_archive_summary.get("output_root")) or (
        baseline_archive_summary_path.parent / "etw-stackwalk-reopen-baseline-archive"
    )
    seed_snapshot_source = portable_path(baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.json")
    seed_snapshot_markdown_source = portable_path(baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.md")
    previous_target = "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json"
    previous_markdown_target = "registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md"
    history_store_dir = f"registry-research-framework/audit/etw-stackwalk-reopen-history-store/{current_snapshot_id or 'unknown'}"
    persist_command = (
        f"mkdir -p {history_store_dir} && "
        f"cp {portable_path(current_snapshot_path)} {history_store_dir}/etw-stackwalk-reopen-snapshot.json && "
        f"cp {portable_path(current_snapshot_path.with_suffix('.md'))} {history_store_dir}/etw-stackwalk-reopen-snapshot.md"
    )

    if previous_snapshot is None:
        history_status = "seed-required"
        history_seed_source = "baseline-archive"
        operator_blocker = "seed-previous-snapshot-from-baseline-archive"
        next_action = "Promote the retained baseline snapshot into snapshot.previous before expecting history-driven reopen diffs."
    elif previous_snapshot_id == current_snapshot_id:
        history_status = "stable"
        history_seed_source = "previous-snapshot"
        operator_blocker = "no-history-rotation-detected"
        next_action = "Current and previous snapshot ids match; wait for a new reopen snapshot before rotating history."
    else:
        history_status = "rotation-ready"
        history_seed_source = "previous-snapshot"
        operator_blocker = "persist-current-snapshot-before-rotation"
        next_action = "Persist the current snapshot into history storage, then rotate snapshot.previous after review."

    return {
        "source_current_snapshot_path": portable_path(current_snapshot_path),
        "source_previous_snapshot_path": portable_path(previous_snapshot_path) if previous_snapshot is not None else None,
        "source_transition_summary_path": portable_path(transition_summary_path),
        "source_transition_summary_markdown_path": portable_path(transition_summary_path.with_suffix(".md")),
        "source_baseline_archive_summary_path": portable_path(baseline_archive_summary_path),
        "source_baseline_archive_markdown_path": portable_path(baseline_archive_summary_path.with_suffix(".md")),
        "history_status": history_status,
        "history_seed_source": history_seed_source,
        "transition_status": transition_summary.get("transition_status"),
        "current_snapshot_id": current_snapshot_id,
        "previous_snapshot_id": previous_snapshot_id,
        "retained_baseline_snapshot_id": retained_baseline_snapshot_id,
        "operator": {
            "blocker": operator_blocker,
            "next_action": next_action,
        },
        "seed_previous_snapshot_command": f"cp {seed_snapshot_source} {previous_target}",
        "seed_previous_snapshot_markdown_command": f"cp {seed_snapshot_markdown_source} {previous_markdown_target}",
        "persist_current_snapshot_history_command": persist_command,
        "refresh_transition_summary_command": "python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py",
        "history_candidate_count": int((current_snapshot.get("counts") or {}).get("candidate_count") or 0),
        "focus_snapshot_id": current_snapshot_id,
    }


def write_commands(commands_root: Path, plan: dict[str, Any]) -> list[str]:
    commands_root.mkdir(parents=True, exist_ok=True)
    command_specs = {
        "01-seed-previous-snapshot.txt": [
            "seed_previous_snapshot_command:",
            str(plan.get("seed_previous_snapshot_command") or ""),
            "",
            "seed_previous_snapshot_markdown_command:",
            str(plan.get("seed_previous_snapshot_markdown_command") or ""),
        ],
        "02-persist-current-snapshot-history.txt": [
            "persist_current_snapshot_history_command:",
            str(plan.get("persist_current_snapshot_history_command") or ""),
        ],
        "03-refresh-transition-summary.txt": [
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
        "# ETW Stackwalk Reopen History Archive",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- History status: `{payload.get('history_status')}`",
        f"- History seed source: `{payload.get('history_seed_source')}`",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Current snapshot id: `{payload.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{payload.get('previous_snapshot_id')}`",
        f"- Retained baseline snapshot id: `{payload.get('retained_baseline_snapshot_id')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Manifest files copied: `{counts.get('manifest_files_copied')}`",
        f"- Seed files copied: `{counts.get('seed_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Commands",
        "",
        f"- `{payload.get('seed_previous_snapshot_command')}`",
        f"- `{payload.get('seed_previous_snapshot_markdown_command')}`",
        f"- `{payload.get('persist_current_snapshot_history_command')}`",
        f"- `{payload.get('refresh_transition_summary_command')}`",
        "",
    ]
    write_text(pack_root / "README.md", "\n".join(lines))


def materialize_history_archive(
    current_snapshot: dict[str, Any],
    previous_snapshot: dict[str, Any] | None,
    transition_summary: dict[str, Any],
    baseline_archive_summary: dict[str, Any],
    *,
    current_snapshot_path: Path = CURRENT_SNAPSHOT_PATH,
    previous_snapshot_path: Path | None = PREVIOUS_SNAPSHOT_PATH,
    transition_summary_path: Path = TRANSITION_SUMMARY_PATH,
    baseline_archive_summary_path: Path = BASELINE_ARCHIVE_SUMMARY_PATH,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    archive_path: Path = DEFAULT_ARCHIVE_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    plan = build_history_plan(
        current_snapshot,
        previous_snapshot,
        transition_summary,
        baseline_archive_summary,
        current_snapshot_path=current_snapshot_path,
        previous_snapshot_path=previous_snapshot_path,
        transition_summary_path=transition_summary_path,
        baseline_archive_summary_path=baseline_archive_summary_path,
    )

    reset_dir(output_root)
    manifest_files: list[str] = []
    seed_files: list[str] = []

    for src in sidecar_paths(current_snapshot_path):
        rel = f"manifests/current/{src.name}"
        copy_file(src, output_root / rel)
        manifest_files.append(rel)
    if previous_snapshot is not None and previous_snapshot_path is not None:
        for src in sidecar_paths(previous_snapshot_path):
            rel = f"manifests/previous/{src.name}"
            copy_file(src, output_root / rel)
            manifest_files.append(rel)
    for src in sidecar_paths(transition_summary_path):
        rel = f"manifests/transition/{src.name}"
        copy_file(src, output_root / rel)
        manifest_files.append(rel)
    for src in sidecar_paths(baseline_archive_summary_path):
        rel = f"manifests/baseline/{src.name}"
        copy_file(src, output_root / rel)
        manifest_files.append(rel)

    baseline_root = resolve_repo_path(baseline_archive_summary.get("output_root")) or (
        baseline_archive_summary_path.parent / "etw-stackwalk-reopen-baseline-archive"
    )
    for src in (
        baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.json",
        baseline_root / "manifests" / "etw-stackwalk-reopen-snapshot.md",
    ):
        if src.exists():
            rel = f"seed/retained-baseline/{src.name}"
            copy_file(src, output_root / rel)
            seed_files.append(rel)

    command_files = [f"commands/{name}" for name in write_commands(output_root / "commands", plan)]
    write_readme(
        output_root,
        {
            "generated_utc": generated_utc,
            **plan,
            "counts": {
                "manifest_files_copied": len(manifest_files),
                "seed_files_copied": len(seed_files),
                "command_files_written": len(command_files),
                "pack_files_checksummed": 0,
            },
        },
    )
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
            "seed_files_copied": len(seed_files),
            "command_files_written": len(command_files),
            "pack_files_checksummed": len(pack_files),
        },
        "manifest_files": manifest_files,
        "seed_files": seed_files,
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
        "# ETW Stackwalk Reopen History Archive",
        "",
        f"- History status: `{payload.get('history_status')}`",
        f"- History seed source: `{payload.get('history_seed_source')}`",
        f"- Transition status: `{payload.get('transition_status')}`",
        f"- Current snapshot id: `{payload.get('current_snapshot_id')}`",
        f"- Previous snapshot id: `{payload.get('previous_snapshot_id')}`",
        f"- Retained baseline snapshot id: `{payload.get('retained_baseline_snapshot_id')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Manifest files copied: `{counts.get('manifest_files_copied')}`",
        f"- Seed files copied: `{counts.get('seed_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Commands",
        "",
        f"- `{payload.get('seed_previous_snapshot_command')}`",
        f"- `{payload.get('seed_previous_snapshot_markdown_command')}`",
        f"- `{payload.get('persist_current_snapshot_history_command')}`",
        f"- `{payload.get('refresh_transition_summary_command')}`",
        "",
    ]
    return "\n".join(lines).rstrip() + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Materialize an ETW reopen history archive.")
    parser.add_argument("--current", type=Path, default=CURRENT_SNAPSHOT_PATH)
    parser.add_argument("--previous", type=Path, default=PREVIOUS_SNAPSHOT_PATH)
    parser.add_argument("--transition", type=Path, default=TRANSITION_SUMMARY_PATH)
    parser.add_argument("--baseline-archive-summary", type=Path, default=BASELINE_ARCHIVE_SUMMARY_PATH)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--summary-output", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--archive-output", type=Path, default=DEFAULT_ARCHIVE_PATH)
    args = parser.parse_args()

    current_snapshot = load_json(args.current)
    previous_snapshot = load_json_if_exists(args.previous)
    transition_summary = load_json(args.transition)
    baseline_archive_summary = load_json(args.baseline_archive_summary)
    payload = materialize_history_archive(
        current_snapshot,
        previous_snapshot,
        transition_summary,
        baseline_archive_summary,
        current_snapshot_path=args.current,
        previous_snapshot_path=args.previous,
        transition_summary_path=args.transition,
        baseline_archive_summary_path=args.baseline_archive_summary,
        output_root=args.output_root,
        summary_path=args.summary_output,
        markdown_path=args.markdown_output,
        archive_path=args.archive_output,
    )
    print(
        json.dumps(
            {
                "summary": portable_path(args.summary_output),
                "history_status": payload.get("history_status"),
                "current_snapshot_id": payload.get("current_snapshot_id"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
