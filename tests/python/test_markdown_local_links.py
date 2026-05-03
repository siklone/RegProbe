from __future__ import annotations

import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
INLINE_CODE_RE = re.compile(r"`[^`]*`")
MARKDOWN_LINK_RE = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
WINDOWS_ABSOLUTE_RE = re.compile(r"^[A-Za-z]:[\\/]")

SCAN_ROOTS = (
    REPO_ROOT / "README.md",
    REPO_ROOT / "Docs",
    REPO_ROOT / "research" / "README.md",
    REPO_ROOT / "research" / "notes",
    REPO_ROOT / "registry-research-framework" / "docs",
)

EXCLUDED_FILES = {
    REPO_ROOT / "research" / "evidence-atlas.md",
    REPO_ROOT / "research" / "evidence-manifest.md",
}


def iter_markdown_files() -> list[Path]:
    paths: list[Path] = []
    for root in SCAN_ROOTS:
        if not root.exists():
            continue
        if root.is_file():
            paths.append(root)
            continue
        paths.extend(sorted(root.rglob("*.md")))
    return [path for path in paths if path not in EXCLUDED_FILES]


def find_broken_links(path: Path) -> list[str]:
    failures: list[str] = []
    in_fence = False
    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), start=1):
        stripped = raw_line.lstrip()
        if stripped.startswith("```"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue

        line = INLINE_CODE_RE.sub("", raw_line)
        for match in MARKDOWN_LINK_RE.finditer(line):
            target = match.group(1).strip()
            if target.startswith(("http://", "https://", "mailto:", "#")):
                continue

            target_no_fragment = target.split("#", 1)[0]
            if not target_no_fragment:
                continue

            if WINDOWS_ABSOLUTE_RE.match(target_no_fragment):
                failures.append(f"{path}:{line_number}: absolute Windows path {target}")
                continue

            if target_no_fragment.startswith("/"):
                resolved = Path(target_no_fragment)
            else:
                resolved = (path.parent / target_no_fragment).resolve()

            if not resolved.exists():
                failures.append(f"{path}:{line_number}: missing target {target}")
    return failures


class MarkdownLocalLinkTests(unittest.TestCase):
    def test_repo_markdown_links_resolve(self) -> None:
        failures: list[str] = []
        for path in iter_markdown_files():
            failures.extend(find_broken_links(path))

        self.assertFalse(
            failures,
            "Broken local markdown links found:\n" + "\n".join(failures[:50]),
        )


if __name__ == "__main__":
    unittest.main()
