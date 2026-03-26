#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Any

from research_path_lib import REPO_ROOT, normalize_reference_text

TARGET_ROOTS = [
    REPO_ROOT / "research" / "records",
    REPO_ROOT / "evidence" / "records",
]

PATH_HINT_RE = re.compile(
    r"(?:"
    r"research[\\/]+evidence-files|"
    r"httpresearch[\\/]+evidence-files|"
    r"evidence[\\/]+files|"
    r"evidence[\\/]+records|"
    r"evidence[\\/]+[A-Za-z0-9._-]+[\\/]|"
    r"Docs[\\/]|"
    r"registry-research-framework[\\/]|"
    r"[A-Za-z]:[\\/]|"
    r"\\\\vmware-host\\"
    r")",
    re.IGNORECASE,
)


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def write_json(path: Path, payload: Any) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def maybe_normalize_string(value: str, title: str) -> str:
    if not value:
        return value
    if not PATH_HINT_RE.search(value):
        return value
    return normalize_reference_text(value, title=title)


def normalize_payload(payload: Any, title: str) -> Any:
    if isinstance(payload, dict):
        return {key: normalize_payload(value, title=title) for key, value in payload.items()}
    if isinstance(payload, list):
        return [normalize_payload(item, title=title) for item in payload]
    if isinstance(payload, str):
        return maybe_normalize_string(payload, title=title)
    return payload


def main() -> int:
    changed = 0
    for root in TARGET_ROOTS:
        if not root.exists():
            continue
        for path in sorted(root.rglob("*.json")):
            payload = load_json(path)
            title = path.stem
            if isinstance(payload, dict):
                title = str(payload.get("tweak_id") or payload.get("record_id") or title)
            normalized = normalize_payload(payload, title=title)
            if normalized != payload:
                write_json(path, normalized)
                changed += 1
                print(f"normalized {path.relative_to(REPO_ROOT)}")
    print(f"changed {changed} json files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
