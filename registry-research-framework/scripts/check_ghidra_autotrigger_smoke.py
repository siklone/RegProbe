#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_SMOKE_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke.json"
DEFAULT_OUTPUT_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke-check.json"
DEFAULT_MARKDOWN_PATH = FRAMEWORK_ROOT / "audit" / "ghidra-autotrigger-smoke-check.md"


REQUIRED_STATUS_FIELDS = {
    "smoke_status": "ok",
    "sync_status": "ok",
    "handoff_status": "ready",
    "transfer_status": "ready",
    "transfer_pack_status": "ready",
    "transfer_pack_check_status": "ok",
    "transfer_pack_import_status": "ok",
    "execution_plan_status": "ready",
    "execution_run_status": "ready",
    "execution_run_check_status": "ok",
}

REQUIRED_PATH_FIELDS = [
    "manifest_path",
    "seeds_path",
    "symbol_queue_path",
    "symbol_batch_path",
    "symbol_run_path",
    "dispatch_batch_path",
    "dispatch_run_path",
    "health_path",
    "sync_path",
    "handoff_path",
    "transfer_path",
    "transfer_pack_output_root",
    "transfer_pack_summary_path",
    "transfer_pack_archive_path",
    "transfer_pack_check_path",
    "transfer_pack_import_root",
    "transfer_pack_import_path",
    "execution_plan_path",
    "execution_run_path",
    "execution_run_check_path",
]


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


def child_status(path_value: str | None, key: str) -> Any:
    path = resolve_path(path_value)
    if not path or not path.exists() or path.is_dir():
        return None
    return load_json(path).get(key)


def validate_smoke(payload: dict[str, Any], *, smoke_path: Path = DEFAULT_SMOKE_PATH, generated_utc: str | None = None) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    errors: list[str] = []
    warnings: list[str] = []
    paths = payload.get("paths") or {}
    counts = payload.get("counts") or {}

    for field, expected in REQUIRED_STATUS_FIELDS.items():
        actual = payload.get(field)
        if actual != expected:
            errors.append(f"{field} expected {expected!r}, got {actual!r}")

    failed_assertions = payload.get("failed_assertions") or []
    if failed_assertions:
        errors.append(f"failed_assertions present: {', '.join(str(item) for item in failed_assertions)}")

    selected_ids = payload.get("selected_candidate_ids") or []
    selected_count = int(payload.get("selected_candidate_count") or 0)
    if selected_count <= 0:
        errors.append("selected_candidate_count must be positive")
    if selected_count != len(selected_ids):
        errors.append(f"selected_candidate_count mismatch: count={selected_count} ids={len(selected_ids)}")
    if int(counts.get("manifest_selected_count") or 0) != 1:
        errors.append("manifest_selected_count must be 1 for synthetic smoke")
    if int(counts.get("seed_count") or 0) != selected_count:
        errors.append("seed_count must match selected_candidate_count")
    for field in [
        "symbol_resolution_request_count",
        "symbol_resolution_batch_job_count",
        "dispatch_job_count",
        "dispatch_selected_job_count",
        "transfer_pack_selected_job_count",
    ]:
        if int(counts.get(field) or 0) <= 0:
            errors.append(f"{field} must be positive")

    operator = payload.get("operator") or {}
    if operator.get("blocker") != "symbol-resolution-ready":
        errors.append(f"operator blocker expected symbol-resolution-ready, got {operator.get('blocker')!r}")

    missing_paths: list[str] = []
    for field in REQUIRED_PATH_FIELDS:
        path = resolve_path(paths.get(field))
        if not path or not path.exists():
            missing_paths.append(field)
    if missing_paths:
        errors.append(f"missing smoke paths: {', '.join(missing_paths)}")

    child_expectations = [
        ("transfer_pack_check_path", "check_status", "ok"),
        ("transfer_pack_import_path", "import_status", "ok"),
        ("execution_plan_path", "execution_plan_status", "ready"),
        ("execution_run_path", "execution_run_status", "ready"),
        ("execution_run_check_path", "check_status", "ok"),
    ]
    for path_field, status_key, expected in child_expectations:
        actual = child_status(paths.get(path_field), status_key)
        if actual != expected:
            errors.append(f"{path_field} {status_key} expected {expected!r}, got {actual!r}")

    health_path = resolve_path(paths.get("health_path"))
    if health_path and health_path.exists():
        health = load_json(health_path)
        run_check = health.get("symbol_resolution_execution_run_check") or {}
        if run_check.get("status") != "ok":
            errors.append("health symbol_resolution_execution_run_check is not ok")
        if int(run_check.get("error_count") or 0) != 0:
            errors.append("health symbol_resolution_execution_run_check has errors")
    else:
        warnings.append("health path missing, child health cross-check skipped")

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "smoke_path": portable_path(smoke_path),
        "smoke_status": payload.get("smoke_status"),
        "check_status": "ok" if not errors else "error",
        "errors": errors,
        "warnings": warnings,
        "counts": {
            "selected_candidates": selected_count,
            "failed_assertions": len(failed_assertions),
            "missing_paths": len(missing_paths),
            "symbol_resolution_requests": int(counts.get("symbol_resolution_request_count") or 0),
            "execution_run_ready": 1 if payload.get("execution_run_status") == "ready" else 0,
            "execution_run_check_ok": 1 if payload.get("execution_run_check_status") == "ok" else 0,
        },
    }


def render_markdown(payload: dict[str, Any]) -> str:
    counts = payload.get("counts") or {}
    lines = [
        "# Ghidra Autotrigger Smoke Check",
        "",
        f"- Check status: `{payload.get('check_status')}`",
        f"- Smoke status: `{payload.get('smoke_status')}`",
        f"- Selected candidates: `{counts.get('selected_candidates')}`",
        f"- Failed assertions: `{counts.get('failed_assertions')}`",
        f"- Missing paths: `{counts.get('missing_paths')}`",
        f"- Symbol resolution requests: `{counts.get('symbol_resolution_requests')}`",
        f"- Execution run ready: `{counts.get('execution_run_ready')}`",
        f"- Execution run check ok: `{counts.get('execution_run_check_ok')}`",
        "",
        "## Errors",
        "",
    ]
    errors = payload.get("errors") or []
    if not errors:
        lines.append("- none")
    for error in errors:
        lines.append(f"- `{error}`")
    warnings = payload.get("warnings") or []
    lines.extend(["", "## Warnings", ""])
    if not warnings:
        lines.append("- none")
    for warning in warnings:
        lines.append(f"- `{warning}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the synthetic Ghidra autotrigger smoke summary and critical child surfaces.")
    parser.add_argument("--smoke", type=Path, default=DEFAULT_SMOKE_PATH)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    parser.add_argument("--markdown-output", type=Path, default=DEFAULT_MARKDOWN_PATH)
    args = parser.parse_args()

    payload = validate_smoke(load_json(args.smoke), smoke_path=args.smoke)
    write_json(args.output, payload)
    write_text(args.markdown_output, render_markdown(payload))
    print(json.dumps(payload, indent=2))
    return 0 if payload.get("check_status") == "ok" else 1


if __name__ == "__main__":
    raise SystemExit(main())
