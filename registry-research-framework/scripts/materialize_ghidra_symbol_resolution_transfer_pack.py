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
DEFAULT_TRANSFER_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.md"
DEFAULT_ARCHIVE_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.zip"


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def resolve_optional_repo_path(path_value: str | None) -> Path | None:
    cleaned = str(path_value or "").strip()
    if not cleaned:
        return None
    return resolve_repo_path(cleaned)


def resolve_repo_path(path_value: str) -> Path:
    path = Path(path_value)
    return path if path.is_absolute() else (REPO_ROOT / path)


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


def write_command_files(jobs: list[dict[str, Any]], commands_root: Path) -> list[str]:
    commands_root.mkdir(parents=True, exist_ok=True)
    command_files: list[str] = []
    for index, job in enumerate(jobs, start=1):
        request_id = str(job.get("request_id") or f"job-{index}")
        filename = f"{index:02d}-{request_id}.txt"
        path = commands_root / filename
        lines = [
            f"request_id: {request_id}",
            f"target_binary: {job.get('target_binary')}",
            f"guest_binary_path: {job.get('guest_binary_path')}",
            f"candidate_ids: {', '.join(job.get('candidate_ids') or [])}",
            f"patterns: {', '.join(job.get('patterns') or [])}",
            "",
            str(job.get("suggested_command") or ""),
            "",
        ]
        write_text(path, "\n".join(lines))
        command_files.append(filename)
    return command_files


def write_pack_readme(pack_root: Path, payload: dict[str, Any]) -> None:
    counts = payload.get("counts") or {}
    operator = payload.get("operator") or {}
    lines = [
        "# Ghidra Symbol Resolution Transfer Pack",
        "",
        f"- Generated UTC: `{payload.get('generated_utc')}`",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Transfer status: `{payload.get('transfer_status')}`",
        f"- Operator blocker: `{operator.get('blocker')}`",
        f"- Next action: `{operator.get('next_action')}`",
        f"- Selected jobs: `{counts.get('selected_jobs')}`",
        f"- Repo files copied: `{counts.get('repo_files_copied')}`",
        f"- Command files written: `{counts.get('command_files_written')}`",
        f"- Pack files checksummed: `{counts.get('pack_files_checksummed')}`",
        "",
        "## Layout",
        "",
        "- `manifests/` copied JSON and markdown manifests",
        "- `repo/` repo-side scripts and guest helpers needed on the destination host",
        "- `commands/` one file per selected request with its suggested command",
        "- `CHECKSUMS.json` SHA-256 manifest for every file in this pack",
        "",
        "## Destination Workflow",
        "",
        "Use this pack from a full RegProbe checkout on the destination host. First validate the pack summary and archive, then unpack it, generate the imported-pack execution plan, dry-run that plan, and validate the dry-run surface before using `--execute`.",
        "",
        "```bash",
        "python3 registry-research-framework/scripts/check_ghidra_symbol_resolution_transfer_pack.py --summary /path/to/ghidra-symbol-resolution-transfer-pack.json",
        "python3 registry-research-framework/scripts/unpack_ghidra_symbol_resolution_transfer_pack.py --summary /path/to/ghidra-symbol-resolution-transfer-pack.json --output-root /path/to/ghidra-symbol-resolution-transfer-pack-import",
        "python3 registry-research-framework/scripts/generate_ghidra_transfer_pack_execution_plan.py --import /path/to/ghidra-symbol-resolution-transfer-pack-import.json",
        "python3 registry-research-framework/scripts/run_ghidra_transfer_pack_execution_plan.py --plan /path/to/ghidra-symbol-resolution-transfer-pack-execution-plan.json",
        "python3 registry-research-framework/scripts/check_ghidra_transfer_pack_execution_run.py --run /path/to/ghidra-symbol-resolution-transfer-pack-execution-run.json",
        "```",
        "",
    ]
    write_text(pack_root / "README.md", "\n".join(lines) + "\n")


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


def derive_pack_status(transfer: dict[str, Any]) -> str:
    selected_jobs = int((transfer.get("counts") or {}).get("selected_jobs") or 0)
    transfer_status = str(transfer.get("transfer_status") or "idle")
    if selected_jobs <= 0:
        return "idle"
    if transfer_status == "ready":
        return "ready"
    if transfer_status == "blocked":
        return "blocked"
    return transfer_status


def materialize_transfer_pack(
    transfer: dict[str, Any],
    *,
    transfer_path: Path = DEFAULT_TRANSFER_PATH,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    archive_path: Path = DEFAULT_ARCHIVE_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    reset_dir(output_root)
    manifests_root = output_root / "manifests"
    repo_root = output_root / "repo"
    commands_root = output_root / "commands"
    manifests_root.mkdir(parents=True, exist_ok=True)

    source_transfer_markdown = transfer_path.with_suffix(".md")
    source_handoff_path = resolve_optional_repo_path(transfer.get("source_handoff_path"))
    source_handoff_markdown = source_handoff_path.with_suffix(".md") if source_handoff_path else None

    manifest_files: list[str] = []
    if transfer_path.exists():
        copy_file(transfer_path, manifests_root / transfer_path.name)
        manifest_files.append(f"manifests/{transfer_path.name}")
    if source_transfer_markdown.exists():
        copy_file(source_transfer_markdown, manifests_root / source_transfer_markdown.name)
        manifest_files.append(f"manifests/{source_transfer_markdown.name}")
    if source_handoff_path and source_handoff_path.exists():
        copy_file(source_handoff_path, manifests_root / source_handoff_path.name)
        manifest_files.append(f"manifests/{source_handoff_path.name}")
    if source_handoff_markdown and source_handoff_markdown.exists():
        copy_file(source_handoff_markdown, manifests_root / source_handoff_markdown.name)
        manifest_files.append(f"manifests/{source_handoff_markdown.name}")

    copied_repo_paths = copy_repo_paths(list(transfer.get("required_repo_paths") or []), repo_root)
    command_files = write_command_files(list(transfer.get("jobs") or []), commands_root)
    request_ids = [
        str(job.get("request_id") or "")
        for job in (transfer.get("jobs") or [])
        if str(job.get("request_id") or "")
    ]

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_transfer_path": portable_path(transfer_path),
        "pack_status": derive_pack_status(transfer),
        "transfer_status": transfer.get("transfer_status"),
        "operator": transfer.get("operator") or {},
        "output_root": portable_path(output_root),
        "archive_path": portable_path(archive_path),
        "counts": {
            "selected_jobs": int((transfer.get("counts") or {}).get("selected_jobs") or 0),
            "repo_files_copied": len(copied_repo_paths),
            "command_files_written": len(command_files),
            "manifest_files_written": len(manifest_files),
            "pack_files_checksummed": 0,
        },
        "candidate_ids": transfer.get("candidate_ids") or [],
        "request_ids": request_ids,
        "copied_repo_paths": copied_repo_paths,
        "command_files": command_files,
        "manifest_files": manifest_files,
        "pack_files": [],
        "archive": {},
        "jobs": transfer.get("jobs") or [],
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
        "# Ghidra Symbol Resolution Transfer Pack",
        "",
        f"- Pack status: `{payload.get('pack_status')}`",
        f"- Transfer status: `{payload.get('transfer_status')}`",
        f"- Output root: `{payload.get('output_root')}`",
        f"- Archive path: `{payload.get('archive_path')}`",
        f"- Selected jobs: `{(payload.get('counts') or {}).get('selected_jobs')}`",
        f"- Repo files copied: `{(payload.get('counts') or {}).get('repo_files_copied')}`",
        f"- Command files written: `{(payload.get('counts') or {}).get('command_files_written')}`",
        f"- Pack files checksummed: `{(payload.get('counts') or {}).get('pack_files_checksummed')}`",
        f"- Archive SHA-256: `{(payload.get('archive') or {}).get('sha256')}`",
    ]
    write_text(markdown_path, "\n".join(markdown_lines) + "\n")
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description="Materialize a portable Ghidra symbol-resolution transfer pack directory and zip archive.")
    parser.add_argument("--transfer", type=Path, default=DEFAULT_TRANSFER_PATH)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--summary-output", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    parser.add_argument("--archive-output", type=Path, default=DEFAULT_ARCHIVE_PATH)
    args = parser.parse_args()

    transfer = load_json(args.transfer)
    payload = materialize_transfer_pack(
        transfer,
        transfer_path=args.transfer,
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
                "selected_jobs": (payload.get("counts") or {}).get("selected_jobs"),
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
