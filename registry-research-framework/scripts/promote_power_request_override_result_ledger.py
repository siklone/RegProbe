#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
AUDIT_ROOT = REPO_ROOT / "registry-research-framework" / "audit"
DEFAULT_SOURCE_JSON = AUDIT_ROOT / "power-request-override-reader-binding-result-ledger-autofill.json"
DEFAULT_SOURCE_MD = AUDIT_ROOT / "power-request-override-reader-binding-result-ledger-autofill.md"


def portable_path(path: Path, *, repo_root: Path = REPO_ROOT) -> str:
    try:
        return str(path.resolve().relative_to(repo_root)).replace("\\", "/")
    except ValueError:
        return str(path.resolve()).replace("\\", "/")


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def slugify_fragment(value: str) -> str:
    slug = re.sub(r"[^A-Za-z0-9._-]+", "-", value.strip()).strip("-._").lower()
    return slug


def resolve_run_id(*, payload: dict[str, Any], explicit_run_id: str | None) -> str:
    if explicit_run_id:
        return explicit_run_id
    fill = payload.get("fill_after_run") or {}
    run_id = str(fill.get("run_id") or "").strip()
    if not run_id or run_id.startswith("<replace-with-"):
        raise ValueError("run_id is missing or still a template placeholder; pass --run-id explicitly")
    return run_id


def target_paths(run_id: str, *, audit_root: Path | None = None) -> tuple[Path, Path]:
    resolved_audit_root = audit_root or AUDIT_ROOT
    suffix = slugify_fragment(run_id)
    if not suffix:
        raise ValueError("run_id did not produce a usable output suffix")
    stem = f"power-request-override-reader-binding-result-ledger-{suffix}"
    return resolved_audit_root / f"{stem}.json", resolved_audit_root / f"{stem}.md"


def resolve_audit_root(*, source_json: Path, source_md: Path) -> Path:
    default_json = DEFAULT_SOURCE_JSON.resolve()
    default_md = DEFAULT_SOURCE_MD.resolve()
    resolved_source_json = source_json.resolve()
    resolved_source_md = source_md.resolve()
    if resolved_source_json == default_json and resolved_source_md == default_md:
        return AUDIT_ROOT
    if resolved_source_json.parent != resolved_source_md.parent:
        raise ValueError("source_json and source_md must live under the same audit directory when using custom source paths")
    return resolved_source_json.parent


def promote(
    *,
    source_json: Path,
    source_md: Path,
    run_id: str,
    audit_root: Path | None = None,
    force: bool = False,
) -> dict[str, str]:
    if not source_json.exists():
        raise FileNotFoundError(f"source JSON not found: {source_json}")
    if not source_md.exists():
        raise FileNotFoundError(f"source markdown not found: {source_md}")

    payload = load_json(source_json)
    fill = payload.get("fill_after_run") or {}
    payload_run_id = str(fill.get("run_id") or "").strip()
    if payload_run_id and not payload_run_id.startswith("<replace-with-") and payload_run_id != run_id:
        raise ValueError(f"payload run_id {payload_run_id!r} does not match requested run_id {run_id!r}")

    target_json, target_md = target_paths(run_id, audit_root=audit_root)
    if not force and (target_json.exists() or target_md.exists()):
        raise FileExistsError(
            f"target already exists: {portable_path(target_json) if target_json.exists() else portable_path(target_md)}"
        )

    source_json.replace(target_json)
    source_md.replace(target_md)
    return {
        "run_id": run_id,
        "target_json": portable_path(target_json),
        "target_md": portable_path(target_md),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Promote ignored PowerRequestOverride autofill ledgers into dated audit artifacts.")
    parser.add_argument("--source-json", type=Path, default=DEFAULT_SOURCE_JSON)
    parser.add_argument("--source-md", type=Path, default=DEFAULT_SOURCE_MD)
    parser.add_argument("--run-id")
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    payload = load_json(args.source_json)
    run_id = resolve_run_id(payload=payload, explicit_run_id=args.run_id)
    audit_root = resolve_audit_root(source_json=args.source_json, source_md=args.source_md)
    target_json, target_md = target_paths(run_id, audit_root=audit_root)
    if args.dry_run:
        print(
            json.dumps(
                {
                    "mode": "dry-run",
                    "source_json": portable_path(args.source_json.resolve()),
                    "source_md": portable_path(args.source_md.resolve()),
                    "target_json": portable_path(target_json),
                    "target_md": portable_path(target_md),
                    "force": args.force,
                },
                indent=2,
            )
        )
        return 0
    result = promote(
        source_json=args.source_json.resolve(),
        source_md=args.source_md.resolve(),
        run_id=run_id,
        audit_root=audit_root,
        force=args.force,
    )
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
