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
DEFAULT_PLAN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-plan.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.md"
DEFAULT_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "etw-stackwalk-hold-reopen-pack.zip"
REQUIRED_REPO_PATHS = [
    "scripts/vm-kvm/run-guest-etw-stackwalk-capture.py",
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


def sidecar_paths(plan_path: Path, plan: dict[str, Any]) -> list[Path]:
    candidates = [
        plan_path,
        plan_path.with_suffix(".md"),
        resolve_optional_repo_path(plan.get("source_batch_path")),
        resolve_optional_repo_path(plan.get("source_run_path")),
        FRAMEWORK_ROOT / "audit" / "etw-stackwalk-execution-manifest.json",
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
    return cleaned or "candidate"


def build_pack_plan(plan: dict[str, Any], *, plan_path: Path = DEFAULT_PLAN_PATH) -> dict[str, Any]:
    items = sorted(list(plan.get("items") or []), key=lambda item: str(item.get("candidate_id") or ""))
    pack_status = "ready" if items else "idle"
    operator_next_action = (
        "Review prerequisites, dry-run the include-holds plan command, then run the include-holds reopen command intentionally."
        if items
        else "No hold candidates are currently capture-ready."
    )
    return {
        "source_plan_path": portable_path(plan_path),
        "source_plan_markdown_path": portable_path(plan_path.with_suffix(".md")),
        "source_batch_path": plan.get("source_batch_path"),
        "source_run_path": plan.get("source_run_path"),
        "source_execution_manifest_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.json",
        "source_execution_manifest_markdown_path": "registry-research-framework/audit/etw-stackwalk-execution-manifest.md",
        "pack_status": pack_status,
        "default_run_mode": plan.get("default_run_mode"),
        "default_selected_job_count": int(plan.get("default_selected_job_count") or 0),
        "default_skipped_hold_count": int(plan.get("default_skipped_hold_count") or 0),
        "operator": {
            "next_action": operator_next_action,
            "intentional_reopen_required": bool(items),
        },
        "reopen_candidate_ids": [str(item.get("candidate_id") or "") for item in items],
        "required_repo_paths": list(REQUIRED_REPO_PATHS),
        "items": [
            {
                "candidate_id": item.get("candidate_id"),
                "feature_area": item.get("feature_area"),
                "next_missing_layer": item.get("next_missing_layer"),
                "promotion_blockers": list(item.get("promotion_blockers") or []),
                "reopen_prerequisites": list(item.get("reopen_prerequisites") or []),
                "default_dispatch_excluded": bool(item.get("default_dispatch_excluded")),
                "effective_config_command": item.get("effective_config_command"),
                "dispatch_command": item.get("dispatch_command"),
                "include_holds_plan_command": item.get("include_holds_plan_command"),
                "include_holds_run_command": item.get("include_holds_run_command"),
                "run_id": item.get("run_id"),
                "host_etl_repo_path": item.get("host_etl_repo_path"),
                "next_action_hint": item.get("next_action_hint"),
            }
            for item in items
        ],
    }


def write_command_files(items: list[dict[str, Any]], commands_root: Path) -> list[str]:
    commands_root.mkdir(parents=True, exist_ok=True)
    command_files: list[str] = []
    for index, item in enumerate(items, start=1):
        candidate_id = str(item.get("candidate_id") or f"candidate-{index}")
        filename = f"{index:02d}-{sanitize_filename(candidate_id)}.txt"
        path = commands_root / filename
        lines = [
            f"candidate_id: {candidate_id}",
            f"feature_area: {item.get('feature_area')}",
            f"next_missing_layer: {item.get('next_missing_layer')}",
            f"run_id: {item.get('run_id')}",
            f"host_etl_repo_path: {item.get('host_etl_repo_path')}",
            f"default_dispatch_excluded: {item.get('default_dispatch_excluded')}",
            f"next_action_hint: {item.get('next_action_hint')}",
            f"promotion_blockers: {', '.join(item.get('promotion_blockers') or [])}",
            "",
            "reopen_prerequisites:",
        ]
        prereqs = list(item.get("reopen_prerequisites") or [])
        if prereqs:
            lines.extend(f"- {prereq}" for prereq in prereqs)
        else:
            lines.append("- none")
        lines.extend(
            [
                "",
                "effective_config_command:",
                str(item.get("effective_config_command") or ""),
                "",
                "include_holds_plan_command:",
                str(item.get("include_holds_plan_command") or ""),
                "",
                "include_holds_run_command:",
                str(item.get("include_holds_run_command") or ""),
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
        "# ETW Stackwalk Hold Reopen Pack",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Default run mode: `{payload.get('default_run_mode')}`",
        f"- Default selected jobs: `{payload.get('default_selected_job_count')}`",
        f"- Default skipped hold jobs: `{payload.get('default_skipped_hold_count')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Reopen candidates: `{counts.get('reopen_candidates')}`",
        f"- Repo files copied: `{counts.get('repo_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Layout",
        "",
        "- `manifests/` hold reopen plan, execution manifest, batch, and run surfaces",
        "- `repo/` repo-side ETW scripts and config needed for an intentional reopen",
        "- `commands/` one reopen command file per intentional-hold candidate",
        "- `CHECKSUMS.json` SHA-256 manifest for the pack contents",
        "",
        "## Workflow",
        "",
        "Start by reading the prerequisites in each command file. Then dry-run the `include_holds` plan command for the candidate you want to reopen. Only run the `include_holds` execution command after we intentionally decide to reopen that lane.",
        "",
    ]
    write_text(pack_root / "README.md", "\n".join(lines) + "\n")


def materialize_hold_reopen_pack(
    plan: dict[str, Any],
    *,
    plan_path: Path = DEFAULT_PLAN_PATH,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    archive_path: Path = DEFAULT_ARCHIVE_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    pack_plan = build_pack_plan(plan, plan_path=plan_path)
    reset_dir(output_root)
    manifests_root = output_root / "manifests"
    repo_root = output_root / "repo"
    commands_root = output_root / "commands"
    manifests_root.mkdir(parents=True, exist_ok=True)

    manifest_files: list[str] = []
    for source_path in sidecar_paths(plan_path, plan):
        destination = manifests_root / source_path.name
        copy_file(source_path, destination)
        manifest_files.append(f"manifests/{source_path.name}")

    copied_repo_paths = copy_repo_paths(list(pack_plan.get("required_repo_paths") or []), repo_root)
    command_files = write_command_files(list(pack_plan.get("items") or []), commands_root)

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_plan_path": pack_plan.get("source_plan_path"),
        "source_plan_markdown_path": pack_plan.get("source_plan_markdown_path"),
        "source_batch_path": pack_plan.get("source_batch_path"),
        "source_run_path": pack_plan.get("source_run_path"),
        "source_execution_manifest_path": pack_plan.get("source_execution_manifest_path"),
        "source_execution_manifest_markdown_path": pack_plan.get("source_execution_manifest_markdown_path"),
        "pack_status": pack_plan.get("pack_status"),
        "default_run_mode": pack_plan.get("default_run_mode"),
        "default_selected_job_count": pack_plan.get("default_selected_job_count"),
        "default_skipped_hold_count": pack_plan.get("default_skipped_hold_count"),
        "operator": pack_plan.get("operator"),
        "output_root": portable_path(output_root),
        "archive_path": portable_path(archive_path),
        "counts": {
            "reopen_candidates": len(pack_plan.get("reopen_candidate_ids") or []),
            "repo_files_copied": len(copied_repo_paths),
            "command_files_written": len(command_files),
            "manifest_files_written": len(manifest_files),
            "pack_files_checksummed": 0,
        },
        "reopen_candidate_ids": pack_plan.get("reopen_candidate_ids") or [],
        "required_repo_paths": pack_plan.get("required_repo_paths") or [],
        "copied_repo_paths": copied_repo_paths,
        "command_files": command_files,
        "manifest_files": manifest_files,
        "items": pack_plan.get("items") or [],
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
        "# ETW Stackwalk Hold Reopen Pack",
        "",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Default run mode: `{payload.get('default_run_mode')}`",
        f"- Reopen candidates: `{(payload.get('counts') or {}).get('reopen_candidates')}`",
        f"- Repo files copied: `{(payload.get('counts') or {}).get('repo_files_copied')}`",
        f"- Command files written: `{(payload.get('counts') or {}).get('command_files_written')}`",
        f"- Pack files checksummed: `{(payload.get('counts') or {}).get('pack_files_checksummed')}`",
        f"- Archive SHA-256: `{(payload.get('archive') or {}).get('sha256')}`",
    ]
    write_text(markdown_path, "\n".join(markdown_lines) + "\n")
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Materialize a portable ETW stackwalk hold-reopen pack directory and zip archive.")
    parser.add_argument("--plan", type=Path, default=DEFAULT_PLAN_PATH)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--summary-output", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--archive-output", type=Path, default=DEFAULT_ARCHIVE_PATH)
    args = parser.parse_args()

    plan = load_json(args.plan)
    payload = materialize_hold_reopen_pack(
        plan,
        plan_path=args.plan,
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
                "reopen_candidates": (payload.get("counts") or {}).get("reopen_candidates"),
            },
            indent=2,
        )
    )
    return 0 if payload.get("pack_status") in {"ready", "idle"} else 1


if __name__ == "__main__":
    raise SystemExit(main())
