from __future__ import annotations

import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


class ArchitectureInvariantTests(unittest.TestCase):
    def test_cli_project_does_not_source_link_app_files(self) -> None:
        cli_project = (REPO_ROOT / "cli" / "cli.csproj").read_text(encoding="utf-8")

        self.assertNotIn("../app/", cli_project)
        self.assertNotIn("..\\app\\", cli_project)

    def test_core_project_does_not_reference_scripting_runtimes(self) -> None:
        core_project = (REPO_ROOT / "core" / "core.csproj").read_text(encoding="utf-8")

        self.assertNotIn("NLua", core_project)
        self.assertNotIn("pythonnet", core_project)

    def test_application_layer_does_not_reference_regprobe_app_namespaces(self) -> None:
        application_root = REPO_ROOT / "application"
        extracted_dirs = (
            REPO_ROOT / "app" / "Services",
            REPO_ROOT / "app" / "Models",
            REPO_ROOT / "app" / "Utilities",
        )

        source_files = list(application_root.rglob("*.cs"))
        for directory in extracted_dirs:
            source_files.extend(directory.rglob("*.cs"))

        leaked_refs: list[str] = []
        for path in source_files:
            text = path.read_text(encoding="utf-8")
            if "namespace RegProbe.Application" not in text and "using RegProbe.Application" not in text:
                continue

            if "RegProbe.App." in text:
                leaked_refs.append(str(path.relative_to(REPO_ROOT)))

        self.assertEqual(leaked_refs, [])


if __name__ == "__main__":
    unittest.main()
