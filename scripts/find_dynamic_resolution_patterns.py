from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

PATTERN_GROUPS: dict[str, list[str]] = {
    "win32_loader": [
        "GetProcAddress",
        "GetModuleHandleA",
        "GetModuleHandleW",
        "LoadLibraryA",
        "LoadLibraryW",
    ],
    "nt_loader": [
        "LdrGetProcedureAddress",
        "LdrLoadDll",
        "RtlInitUnicodeString",
    ],
    "kernel_resolution": [
        "MmGetSystemRoutineAddress",
        "ZwQuerySystemInformation",
        "ZwSetValueKey",
    ],
}


def encode_ascii(pattern: str) -> bytes:
    return pattern.encode("ascii", errors="ignore")


def encode_wide(pattern: str) -> bytes:
    return pattern.encode("utf-16-le", errors="ignore")


def search_offsets(blob: bytes, needle: bytes) -> list[int]:
    offsets: list[int] = []
    start = 0
    while True:
        hit = blob.find(needle, start)
        if hit < 0:
            return offsets
        offsets.append(hit)
        start = hit + 1


def build_hits(blob: bytes, category: str, pattern: str) -> list[dict[str, Any]]:
    hits: list[dict[str, Any]] = []
    for encoding, needle in (("ascii", encode_ascii(pattern)), ("utf16", encode_wide(pattern))):
        for offset in search_offsets(blob, needle):
            hits.append(
                {
                    "category": category,
                    "pattern": pattern,
                    "encoding": encoding,
                    "offset": offset,
                }
            )
    return hits


def heuristic_score(unique_patterns: int, unique_categories: int) -> int:
    score = unique_patterns * 15 + unique_categories * 10
    if unique_patterns >= 5:
        score += 10
    return max(0, min(100, score))


def build_payload(binary_path: Path, label: str | None) -> dict[str, Any]:
    blob = binary_path.read_bytes()
    hits: list[dict[str, Any]] = []
    categories: list[dict[str, Any]] = []

    for category, patterns in PATTERN_GROUPS.items():
        category_hits: list[dict[str, Any]] = []
        seen_patterns: list[str] = []
        for pattern in patterns:
            pattern_hits = build_hits(blob, category, pattern)
            if pattern_hits:
                category_hits.extend(pattern_hits)
                if pattern not in seen_patterns:
                    seen_patterns.append(pattern)
        if category_hits:
            categories.append(
                {
                    "category": category,
                    "matched_patterns": seen_patterns,
                    "hit_count": len(category_hits),
                }
            )
            hits.extend(category_hits)

    unique_patterns = sorted({item["pattern"] for item in hits})
    unique_categories = sorted({item["category"] for item in hits})
    score = heuristic_score(len(unique_patterns), len(unique_categories))

    return {
        "binary": binary_path.name,
        "binary_path": str(binary_path),
        "label": label or binary_path.stem,
        "patterns_scanned": PATTERN_GROUPS,
        "summary": {
            "dynamic_resolution_signal": bool(hits),
            "unique_pattern_count": len(unique_patterns),
            "unique_category_count": len(unique_categories),
            "heuristic_score": score,
            "verdict": "signal-present" if hits else "no-signal",
        },
        "categories": categories,
        "hits": hits,
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Scan a binary for dynamic import resolution indicators.")
    parser.add_argument("--binary", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--label", type=str, default=None)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload = build_payload(args.binary, args.label)
    write_json(args.output, payload)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
