#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import shutil
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_SUMMARY_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack.json"
DEFAULT_OUTPUT_ROOT = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-symbol-resolution-transfer-pack-import.md"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


pack_check_mod = load_local_module(
    "transfer_pack_import_check",
    FRAMEWORK_ROOT / "scripts" / "check_ghidra_symbol_resolution_transfer_pack.py",
)


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


def safe_extract_zip(archive_path: Path, output_root: Path) -> list[str]:
    extracted: list[str] = []
    root = output_root.resolve()
    with zipfile.ZipFile(archive_path) as zf:
        for info in zf.infolist():
            if info.is_dir():
                continue
            destination = (output_root / info.filename).resolve()
            if root not in [destination, *destination.parents]:
                raise ValueError(f"unsafe archive entry outside output root: {info.filename}")
            destination.parent.mkdir(parents=True, exist_ok=True)
            with zf.open(info) as src, destination.open("wb") as dst:
                shutil.copyfileobj(src, dst)
            extracted.append(info.filename)
    return sorted(extracted)


def unpack_transfer_pack(
    summary: dict[str, Any],
    *,
    summary_path: Path = DEFAULT_SUMMARY_PATH,
    archive_path: Path | None = None,
    output_root: Path = DEFAULT_OUTPUT_ROOT,
    output_path: Path = DEFAULT_OUTPUT_PATH,
    markdown_path: Path = DEFAULT_MARKDOWN_PATH,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    effective_archive_path = archive_path or resolve_path(((summary.get("archive") or {}).get("path")) or summary.get("archive_path"))
    errors: list[str] = []

    if not effective_archive_path:
        errors.append("archive path missing")
    elif not effective_archive_path.exists():
        errors.append(f"archive path missing: {portable_path(effective_archive_path)}")

    check_payload = pack_check_mod.validate_transfer_pack(summary, summary_path=summary_path, generated_utc=generated_utc)
    errors.extend(check_payload.get("errors") or [])

    extracted_files: list[str] = []
    if not errors and effective_archive_path:
        reset_dir(output_root)
        extracted_files = safe_extract_zip(effective_archive_path, output_root)

    expected_files = sorted(str(item.get("path") or "") for item in (summary.get("pack_files") or []) if str(item.get("path") or ""))
    if not errors and extracted_files != expected_files:
        errors.append("extracted file list does not match summary pack_files")

    payload = {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "summary_path": portable_path(summary_path),
        "archive_path": portable_path(effective_archive_path) if effective_archive_path else None,
        "output_root": portable_path(output_root),
        "import_status": "ok" if not errors else "error",
        "errors": errors,
        "counts": {
            "expected_files": len(expected_files),
            "extracted_files": len(extracted_files),
            "command_files": len(summary.get("command_files") or []),
            "request_ids": len(summary.get("request_ids") or []),
        },
        "check": {
            "status": check_payload.get("check_status"),
            "checked_archive_files": (check_payload.get("counts") or {}).get("checked_archive_files"),
        },
        "extracted_files": extracted_files,
    }
    write_json(output_path, payload)
    write_text(markdown_path, render_markdown(payload))
    return payload


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# Ghidra Symbol Resolution Transfer Pack Import",
        "",
        f"- Import status: `{payload.get('import_status')}`",
        f"- Archive path: `{payload.get('archive_path')}`",
        f"- Output root: `{payload.get('output_root')}`",
        f"- Expected files: `{counts.get('expected_files')}`",
        f"- Extracted files: `{counts.get('extracted_files')}`",
        f"- Command files: `{counts.get('command_files')}`",
        f"- Request ids: `{counts.get('request_ids')}`",
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
    parser = argparse.ArgumentParser(description="Validate and unpack a Ghidra symbol-resolution transfer pack archive.")
    parser.add_argument("--summary", type=Path, default=DEFAULT_SUMMARY_PATH)
    parser.add_argument("--archive", type=Path, default=None)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    summary = load_json(args.summary)
    payload = unpack_transfer_pack(
        summary,
        summary_path=args.summary,
        archive_path=args.archive,
        output_root=args.output_root,
        output_path=args.output,
        markdown_path=args.markdown_output,
    )
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("import_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
