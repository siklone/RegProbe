#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
TARGETS = [
    REPO_ROOT / "research" / "README.md",
    REPO_ROOT / "research" / "evidence-atlas.md",
    REPO_ROOT / "research" / "evidence-audit.json",
    REPO_ROOT / "research" / "evidence-classes.json",
    REPO_ROOT / "research" / "evidence-index.json",
    REPO_ROOT / "research" / "evidence-manifest.json",
    REPO_ROOT / "research" / "evidence-manifest.md",
    REPO_ROOT / "research" / "validated-proof-audit.json",
]

REPLACEMENTS = [
    ("httpresearch/evidence-files/", "evidence/files/"),
    ("research/evidence-files/", "evidence/files/"),
    ("httpresearch/evidence-files", "evidence/files"),
    ("research/evidence-files", "evidence/files"),
]


def normalize_file(path: Path) -> bool:
    if not path.exists():
        return False

    original = path.read_text(encoding="utf-8")
    updated = original
    for old, new in REPLACEMENTS:
        updated = updated.replace(old, new)

    if updated == original:
        return False

    path.write_text(updated, encoding="utf-8", newline="\n")
    return True


def main() -> int:
    changed = 0
    for path in TARGETS:
        if normalize_file(path):
            changed += 1
            print(f"Normalized {path.relative_to(REPO_ROOT)}")

    print(f"Updated {changed} published file(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
