#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_MANIFEST_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack.md"
DEFAULT_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-pack.zip"
REQUIRED_REPO_PATHS = [
    "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
    "registry-research-framework/scripts/generate_etw_stackwalk_dispatch_batch.py",
    "registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py",
    "registry-research-framework/scripts/generate_etw_stackwalk_hold_reopen_plan.py",
    "registry-research-framework/scripts/generate_etw_stackwalk_execution_manifest.py",
    "registry-research-framework/config/etw-stackwalk-profiles.json",
    "registry-research-framework/config/tweak-vm-runners.json",
]


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


def resolve_optional_repo_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    return resolve_repo_path(cleaned)


def load_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
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


def copy_repo_paths(relative_paths: list[str], destination_root: Path) -> list[str]:
    copied: list[str] = []
    for relative_path in relative_paths:
        src = resolve_repo_path(relative_path)
        if not src.exists():
            continue
        dst = destination_root / relative_path
        copy_file(src, dst)
        copied.append(relative_path)
    return copied


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


def manifest_sidecar_paths(manifest_path: Path, manifest: dict[str, Any]) -> list[Path]:
    candidates = [
        manifest_path,
        manifest_path.with_suffix(".md"),
        resolve_optional_repo_path(manifest.get("source_batch_path")),
        resolve_optional_repo_path(manifest.get("source_run_path")),
        resolve_optional_repo_path(manifest.get("source_hold_reopen_plan_path")),
    ]
    results: list[Path] = []
    seen: set[str] = set()
    for path in candidates:
        if path is None:
            continue
        for candidate in (path, path.with_suffix(".md")):
            key = str(candidate.resolve()) if candidate.exists() else str(candidate)
            if key in seen or not candidate.exists():
                continue
            seen.add(key)
            results.append(candidate)
    return results


def sanitize_filename(value: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9._-]+", "-", value).strip("-")
    return cleaned or "entry"


def selected_command(entry: dict[str, Any]) -> str:
    if entry.get("selection_reason") == "hold-reopen":
        return str(entry.get("include_holds_run_command") or "")
    return str(entry.get("dispatch_command") or "")


def build_pack_plan(manifest: dict[str, Any], *, manifest_path: Path = DEFAULT_MANIFEST_PATH) -> dict[str, Any]:
    entries = list(manifest.get("entries") or [])
    selected_entries = [entry for entry in entries if entry.get("selected")]
    excluded_entries = [entry for entry in entries if not entry.get("selected")]
    manifest_status = str(manifest.get("status") or "idle")
    if manifest_status == "blocked":
        pack_status = "blocked"
    elif selected_entries:
        pack_status = "ready"
    else:
        pack_status = "idle"

    return {
        "source_manifest_path": portable_path(manifest_path),
        "source_manifest_markdown_path": portable_path(manifest_path.with_suffix(".md")),
        "source_batch_path": manifest.get("source_batch_path"),
        "source_run_path": manifest.get("source_run_path"),
        "source_hold_reopen_plan_path": manifest.get("source_hold_reopen_plan_path"),
        "manifest_status": manifest_status,
        "pack_status": pack_status,
        "include_holds": bool(manifest.get("include_holds")),
        "operator": manifest.get("operator") or {},
        "requested_candidate_ids": list(manifest.get("requested_candidate_ids") or []),
        "selected_candidate_ids": [str(entry.get("candidate_id") or "") for entry in selected_entries],
        "excluded_candidate_ids": [str(entry.get("candidate_id") or "") for entry in excluded_entries],
        "required_repo_paths": list(REQUIRED_REPO_PATHS),
        "entries": [
            {
                "candidate_id": entry.get("candidate_id"),
                "selected": bool(entry.get("selected")),
                "selection_reason": entry.get("selection_reason"),
                "actionability": entry.get("actionability"),
                "profile_id": entry.get("profile_id"),
                "run_id": entry.get("run_id"),
                "host_etl_repo_path": entry.get("host_etl_repo_path"),
                "registry_path": entry.get("registry_path"),
                "value_name": entry.get("value_name"),
                "selected_command": selected_command(entry),
                "effective_config_command": entry.get("effective_config_command"),
                "dispatch_command": entry.get("dispatch_command"),
                "include_holds_run_command": entry.get("include_holds_run_command"),
                "next_action_hint": entry.get("next_action_hint"),
                "promotion_blockers": list(entry.get("promotion_blockers") or []),
                "reopen_prerequisites": list(entry.get("reopen_prerequisites") or []),
            }
            for entry in entries
        ],
    }


def write_command_files(entries: list[dict[str, Any]], commands_root: Path) -> list[str]:
    commands_root.mkdir(parents=True, exist_ok=True)
    command_files: list[str] = []
    selected_entries = [entry for entry in entries if entry.get("selected")]
    for index, entry in enumerate(selected_entries, start=1):
        candidate_id = str(entry.get("candidate_id") or f"candidate-{index}")
        filename = f"{index:02d}-{sanitize_filename(candidate_id)}.txt"
        path = commands_root / filename
        lines = [
            f"candidate_id: {candidate_id}",
            f"selection_reason: {entry.get('selection_reason')}",
            f"profile_id: {entry.get('profile_id')}",
            f"run_id: {entry.get('run_id')}",
            f"registry_path: {entry.get('registry_path')}",
            f"value_name: {entry.get('value_name')}",
            f"host_etl_repo_path: {entry.get('host_etl_repo_path')}",
            f"next_action_hint: {entry.get('next_action_hint')}",
            f"promotion_blockers: {', '.join(entry.get('promotion_blockers') or [])}",
        ]
        prereqs = list(entry.get("reopen_prerequisites") or [])
        if prereqs:
            lines.extend(["", "reopen_prerequisites:"])
            lines.extend(f"- {prereq}" for prereq in prereqs)
        lines.extend(
            [
                "",
                "effective_config_command:",
                str(entry.get("effective_config_command") or ""),
                "",
                "selected_command:",
                str(entry.get("selected_command") or ""),
                "",
            ]
        )
        write_text(path, "\n".join(lines))
        command_files.append(filename)
    return command_files


def write_pack_readme(pack_root: Path, payload: dict[str, Any]) -> None:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# ETW Stackwalk Execution Pack",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Manifest status: `{payload.get('manifest_status')}`",
        f"- Include holds: `{payload.get('include_holds')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Requested candidates: `{counts.get('requested_candidates')}`",
        f"- Selected candidates: `{counts.get('selected_candidates')}`",
        f"- Excluded candidates: `{counts.get('excluded_candidates')}`",
        f"- Repo files copied: `{counts.get('repo_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Layout",
        "",
        "- `manifests/` current ETW execution surfaces copied into the pack",
        "- `repo/` repo-side runner and config files for inspection on another host",
        "- `commands/` one command file per selected ETW execution candidate",
        "- `CHECKSUMS.json` SHA-256 manifest for the pack contents",
        "",
        "## Workflow",
        "",
        "Use this pack from a full RegProbe checkout. Inspect the copied manifest and command files first. When the pack status is `ready`, run the selected commands from the repo checkout. When the pack status is `idle`, use the hold-reopen plan in the manifest set before reopening execution lanes.",
        "",
    ]
    write_text(pack_root / "README.md", "\n".join(lines) + "\n")


def materialize_execution_pack(
    manifest: dict[str, Any],
    *,
    manifest_path: Path = DEFAULT_MANIFEST_PATH,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    archive_path: Path = DEFAULT_ARCHIVE_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    plan = build_pack_plan(manifest, manifest_path=manifest_path)
    reset_dir(output_root)
    manifests_root = output_root / "manifests"
    repo_root = output_root / "repo"
    commands_root = output_root / "commands"
    manifests_root.mkdir(parents=True, exist_ok=True)

    manifest_files: list[str] = []
    for source_path in manifest_sidecar_paths(manifest_path, manifest):
        destination = manifests_root / source_path.name
        copy_file(source_path, destination)
        manifest_files.append(f"manifests/{source_path.name}")

    copied_repo_paths = copy_repo_paths(list(plan.get("required_repo_paths") or []), repo_root)
    command_files = write_command_files(list(plan.get("entries") or []), commands_root)

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_manifest_path": plan.get("source_manifest_path"),
        "source_manifest_markdown_path": plan.get("source_manifest_markdown_path"),
        "source_batch_path": plan.get("source_batch_path"),
        "source_run_path": plan.get("source_run_path"),
        "source_hold_reopen_plan_path": plan.get("source_hold_reopen_plan_path"),
        "manifest_status": plan.get("manifest_status"),
        "pack_status": plan.get("pack_status"),
        "include_holds": plan.get("include_holds"),
        "operator": plan.get("operator"),
        "output_root": portable_path(output_root),
        "archive_path": portable_path(archive_path),
        "counts": {
            "requested_candidates": len(plan.get("requested_candidate_ids") or []),
            "selected_candidates": len(plan.get("selected_candidate_ids") or []),
            "excluded_candidates": len(plan.get("excluded_candidate_ids") or []),
            "repo_files_copied": len(copied_repo_paths),
            "command_files_written": len(command_files),
            "manifest_files_written": len(manifest_files),
            "pack_files_checksummed": 0,
        },
        "requested_candidate_ids": plan.get("requested_candidate_ids") or [],
        "selected_candidate_ids": plan.get("selected_candidate_ids") or [],
        "excluded_candidate_ids": plan.get("excluded_candidate_ids") or [],
        "required_repo_paths": plan.get("required_repo_paths") or [],
        "copied_repo_paths": copied_repo_paths,
        "command_files": command_files,
        "manifest_files": manifest_files,
        "entries": plan.get("entries") or [],
        "pack_files": [],
        "archive": {},
    }
    write_pack_readme(output_root, payload)
    files_before_checksums = pack_file_manifest(output_root)
    checksum_payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "file_count": len(files_before_checksums),
        "files": files_before_checksums,
    }
    write_json(output_root / "CHECKSUMS.json", checksum_payload)
    pack_files = pack_file_manifest(output_root)
    build_archive(output_root, archive_path)
    payload["counts"]["pack_files_checksummed"] = len(pack_files)
    payload["pack_files"] = pack_files
    payload["archive"] = {
        "path": portable_path(archive_path),
        "size_bytes": archive_path.stat().st_size if archive_path.exists() else 0,
        "sha256": sha256_file(archive_path) if archive_path.exists() else None,
    }
    write_json(summary_path, payload)
    markdown_lines = [
        "# ETW Stackwalk Execution Pack",
        "",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Manifest status: `{payload.get('manifest_status')}`",
        f"- Include holds: `{payload.get('include_holds')}`",
        f"- Output root: `{payload.get('output_root')}`",
        f"- Archive path: `{payload.get('archive_path')}`",
        f"- Requested candidates: `{(payload.get('counts') or {}).get('requested_candidates')}`",
        f"- Selected candidates: `{(payload.get('counts') or {}).get('selected_candidates')}`",
        f"- Excluded candidates: `{(payload.get('counts') or {}).get('excluded_candidates')}`",
        f"- Repo files copied: `{(payload.get('counts') or {}).get('repo_files_copied')}`",
        f"- Command files written: `{(payload.get('counts') or {}).get('command_files_written')}`",
        f"- Pack files checksummed: `{(payload.get('counts') or {}).get('pack_files_checksummed')}`",
        f"- Archive SHA-256: `{(payload.get('archive') or {}).get('sha256')}`",
    ]
    write_text(markdown_path, "\n".join(markdown_lines) + "\n")
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Materialize a portable ETW stackwalk execution pack directory and zip archive.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST_PATH)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--summary-output", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--archive-output", type=Path, default=DEFAULT_ARCHIVE_PATH)
    args = parser.parse_args()

    manifest = load_json(args.manifest)
    payload = materialize_execution_pack(
        manifest,
        manifest_path=args.manifest,
        output_root=args.output_root,
        summary_path=args.summary_output,
        markdown_path=args.markdown_output,
        archive_path=args.archive_output,
    )
    print(
        json.dumps(
            {
                "output_root": portable_path(args.output_root),
                "archive_path": portable_path(args.archive_output),
                "pack_status": payload.get("pack_status"),
                "selected_candidates": (payload.get("counts") or {}).get("selected_candidates"),
            },
            indent=2,
        )
    )
    return 0 if payload.get("pack_status") in {"ready", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
