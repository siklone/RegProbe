from __future__ import annotations

import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
APP_CSPROJ = REPO_ROOT / "app" / "app.csproj"


class AppRuntimeDocsTests(unittest.TestCase):
    def test_publish_includes_research_app_surface_manifest_tree(self) -> None:
        content = APP_CSPROJ.read_text(encoding="utf-8")
        self.assertIn(r"..\Docs\research\app-surface\**\*.*", content)
        self.assertIn(r"research\app-surface\%(RecursiveDir)%(Filename)%(Extension)", content)


if __name__ == "__main__":
    unittest.main()
