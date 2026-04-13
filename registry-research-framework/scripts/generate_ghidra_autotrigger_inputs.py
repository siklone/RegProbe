#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
DEFAULT_SEARCH_ROOT = REPO_ROOT / "evidence"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-inputs.json"


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


autotrigger = load_local_module("autotrigger_inputs_seeds", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_seeds.py")


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def iso_from_timestamp(timestamp: float) -> str:
    return datetime.fromtimestamp(timestamp, tz=timezone.utc).isoformat().replace("+00:00", "Z")


def discover_input_entries(
    bundle_roots: list[Path],
    *,
    require_caller_stack: bool = True,
    limit: int | None = None,
) -> list[dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    seen: set[Path] = set()
    for root in bundle_roots:
        for path in autotrigger.collect_bundle_paths(bundle_root=root):
            if path in seen:
                continue
            seen.add(path)
            bundle = autotrigger.load_json(path)
            caller_stack_event_count = autotrigger.bundle_caller_stack_event_count(bundle)
            if require_caller_stack and caller_stack_event_count <= 0:
                continue
            stat = path.stat()
            entries.append(
                {
                    "path": autotrigger.portable_path(path),
                    "run_id": bundle.get("run_id"),
                    "source_tool": bundle.get("source_tool"),
                    "capture_phase": bundle.get("capture_phase"),
                    "normalizer_name": bundle.get("normalizer_name"),
                    "status": bundle.get("status"),
                    "event_count": int(bundle.get("event_count") or 0),
                    "caller_stack_event_count": caller_stack_event_count,
                    "generated_utc": bundle.get("generated_utc"),
                    "modified_utc": iso_from_timestamp(stat.st_mtime),
                    "_sort_mtime": stat.st_mtime,
                }
            )
    entries.sort(
        key=lambda item: (
            -int(item.get("caller_stack_event_count") or 0),
            -float(item.get("_sort_mtime") or 0),
            str(item.get("path") or ""),
        )
    )
    if limit is not None:
        entries = entries[:limit]
    for index, entry in enumerate(entries, start=1):
        entry["priority_rank"] = index
        entry.pop("_sort_mtime", None)
    return entries


def input_manifest(
    bundle_roots: list[Path],
    *,
    require_caller_stack: bool = True,
    limit: int | None = None,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    entries = discover_input_entries(bundle_roots, require_caller_stack=require_caller_stack, limit=limit)
    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "search_roots": [autotrigger.portable_path(root) for root in bundle_roots],
        "require_caller_stack": require_caller_stack,
        "selection_limit": limit,
        "selected_count": len(entries),
        "entries": entries,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Discover normalized bundles that can feed the Ghidra autotrigger lane.")
    parser.add_argument("--bundle-root", type=Path, action="append", default=[])
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--include-no-stack", action="store_true", help="Keep normalized bundles even when they contain no caller_stack events.")
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    args = parser.parse_args()

    roots = args.bundle_root or [DEFAULT_SEARCH_ROOT]
    payload = input_manifest(
        roots,
        require_caller_stack=not args.include_no_stack,
        limit=args.limit,
    )
    write_json(args.output, payload)
    print(
        json.dumps(
            {
                "output": autotrigger.portable_path(args.output),
                "selected_count": payload["selected_count"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
