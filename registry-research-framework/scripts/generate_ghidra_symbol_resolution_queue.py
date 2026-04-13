#!/usr/bin/env python3
from __future__ import annotations

import argparse
import importlib.util
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
FRAMEWORK_ROOT = REPO_ROOT / "registry-research-framework"
SEEDS_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-autotrigger-seeds.jsonl"
OUTPUT_PATH = FRAMEWORK_ROOT / "queue" / "ghidra-symbol-resolution-queue.json"

ACTIONABLE_FRAME_KINDS = {"module_offset", "raw_address", "plain_text"}


def load_local_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


autotrigger = load_local_module("symbol_resolution_queue_autotrigger", FRAMEWORK_ROOT / "scripts" / "generate_ghidra_autotrigger_seeds.py")


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def slugify(value: Any) -> str:
    return re.sub(r"[^a-z0-9]+", "-", str(value or "").lower()).strip("-") or "unnamed"


def normalize_hex(value: str) -> str:
    lowered = str(value or "").strip().lower()
    if lowered.startswith("0x"):
        lowered = lowered[2:]
    return f"0x{lowered}"


def parse_actionable_frame(frame: Any, *, target_binary: Any = None) -> tuple[dict[str, Any] | None, str | None]:
    text = str(frame or "").strip()
    resolution_kind = autotrigger.frame_resolution_kind(text)
    if resolution_kind not in ACTIONABLE_FRAME_KINDS:
        return None, resolution_kind

    binary_hint = str(target_binary or "").strip() or None
    module_name: str | None = None
    lookup_key: str
    offset_hex: str | None = None
    address: str | None = None
    symbol_hint: str | None = None

    if resolution_kind == "module_offset":
        module_name, raw_offset = text.split("+0x", 1)
        module_name = module_name.strip() or binary_hint
        offset_hex = normalize_hex(raw_offset)
        lookup_key = f"{module_name}+{offset_hex}" if module_name else offset_hex
    elif resolution_kind == "raw_address":
        address = normalize_hex(text)
        module_name = binary_hint
        lookup_key = f"{module_name}@{address}" if module_name else address
    else:
        symbol_hint = text
        module_name = binary_hint
        lookup_key = f"{module_name}!{symbol_hint}" if module_name else symbol_hint

    return (
        {
            "frame": text,
            "resolution_kind": resolution_kind,
            "lookup_key": lookup_key,
            "module_name": module_name,
            "target_binary": binary_hint,
            "offset_hex": offset_hex,
            "address": address,
            "symbol_hint": symbol_hint,
        },
        None,
    )


def build_next_action_hint(request: dict[str, Any]) -> str:
    lookup_key = str(request.get("lookup_key") or "")
    target_binary = str(request.get("target_binary") or "")
    candidate_ids = request.get("candidate_ids") or []
    candidate_hint = candidate_ids[0] if candidate_ids else "queued ghidra candidate"
    if target_binary:
        return f"Resolve {lookup_key} against {target_binary} symbols before running the next Ghidra pivot for {candidate_hint}."
    return f"Resolve {lookup_key} into a concrete symbol before running the next Ghidra pivot for {candidate_hint}."


def symbol_resolution_queue_from_seeds(
    seed_rows: list[dict[str, Any]],
    *,
    generated_utc: str | None = None,
) -> dict[str, Any]:
    generated_utc = generated_utc or now_utc()
    grouped: dict[str, dict[str, Any]] = {}
    skipped_frame_counts: dict[str, int] = {}
    actionable_frame_count = 0

    for seed in seed_rows:
        candidate_id = str(seed.get("candidate_id") or "")
        source_bundle_path = str(seed.get("source_bundle_path") or "")
        source_run_id = str(seed.get("source_run_id") or "")
        event_index = int(seed.get("event_index") or 0)
        target_binary = seed.get("target_binary")

        for frame in seed.get("unresolved_frames") or []:
            parsed, skipped_reason = parse_actionable_frame(frame, target_binary=target_binary)
            if not parsed:
                skipped_frame_counts[skipped_reason or "unknown"] = skipped_frame_counts.get(skipped_reason or "unknown", 0) + 1
                continue

            actionable_frame_count += 1
            lookup_key = str(parsed["lookup_key"])
            request = grouped.setdefault(
                lookup_key,
                {
                    "lookup_key": lookup_key,
                    "resolution_kind": parsed["resolution_kind"],
                    "module_name": parsed["module_name"],
                    "target_binary": parsed["target_binary"],
                    "offset_hex": parsed["offset_hex"],
                    "address": parsed["address"],
                    "symbol_hint": parsed["symbol_hint"],
                    "_candidate_ids": set(),
                    "_source_bundle_paths": set(),
                    "_source_run_ids": set(),
                    "_source_event_indices": set(),
                    "_frame_variants": set(),
                    "_suggested_patterns": set(),
                    "_occurrence_count": 0,
                },
            )
            request["_candidate_ids"].add(candidate_id)
            if source_bundle_path:
                request["_source_bundle_paths"].add(source_bundle_path)
            if source_run_id:
                request["_source_run_ids"].add(source_run_id)
            if event_index > 0:
                request["_source_event_indices"].add(event_index)
            request["_frame_variants"].add(str(frame).strip())
            for pattern in seed.get("suggested_patterns") or []:
                cleaned = str(pattern or "").strip()
                if cleaned:
                    request["_suggested_patterns"].add(cleaned)
            request["_occurrence_count"] += 1

    requests: list[dict[str, Any]] = []
    grouped_rows = sorted(
        grouped.values(),
        key=lambda item: (
            -len(item["_candidate_ids"]),
            -int(item["_occurrence_count"]),
            str(item.get("module_name") or ""),
            str(item.get("lookup_key") or ""),
        ),
    )
    for index, row in enumerate(grouped_rows, start=1):
        request = {
            "request_id": f"ghidra-symbol-{index:02d}-{slugify(row.get('lookup_key'))}",
            "status": "queued",
            "created_utc": generated_utc,
            "priority_rank": index,
            "lookup_key": row.get("lookup_key"),
            "resolution_kind": row.get("resolution_kind"),
            "module_name": row.get("module_name"),
            "target_binary": row.get("target_binary"),
            "offset_hex": row.get("offset_hex"),
            "address": row.get("address"),
            "symbol_hint": row.get("symbol_hint"),
            "occurrence_count": int(row["_occurrence_count"]),
            "candidate_count": len(row["_candidate_ids"]),
            "candidate_ids": sorted(item for item in row["_candidate_ids"] if item),
            "source_bundle_paths": sorted(row["_source_bundle_paths"]),
            "source_run_ids": sorted(row["_source_run_ids"]),
            "source_event_indices": sorted(row["_source_event_indices"]),
            "frame_variants": sorted(row["_frame_variants"]),
            "suggested_patterns": sorted(row["_suggested_patterns"]),
            "suggested_symbol_sources": [
                "microsoft-public-symbol-server",
                "local-pdb-cache",
                "ghidra-project-symbol-tree",
            ],
        }
        request["next_action_hint"] = build_next_action_hint(request)
        requests.append(request)

    return {
        "schema_version": "1.0",
        "generated_utc": generated_utc,
        "source_seeds_path": autotrigger.portable_path(SEEDS_PATH),
        "request_count": len(requests),
        "diagnostics": {
            "actionable_frame_count": actionable_frame_count,
            "skipped_frame_counts": skipped_frame_counts,
        },
        "requests": requests,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a symbol-resolution queue from unresolved Ghidra autotrigger frames.")
    parser.add_argument("--seeds", type=Path, default=SEEDS_PATH)
    parser.add_argument("--output", type=Path, default=OUTPUT_PATH)
    args = parser.parse_args()

    seeds = autotrigger.load_jsonl(args.seeds)
    payload = symbol_resolution_queue_from_seeds(seeds)
    write_json(args.output, payload)
    print(
        json.dumps(
            {
                "output": autotrigger.portable_path(args.output),
                "request_count": payload["request_count"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
