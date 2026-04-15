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

    def test_cli_bootstrap_and_research_root_stay_compact(self) -> None:
        cli_program_lines = (REPO_ROOT / "cli" / "Program.cs").read_text(encoding="utf-8").splitlines()
        research_root_lines = (
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.cs"
        ).read_text(encoding="utf-8").splitlines()
        tweak_root_lines = (
            REPO_ROOT / "cli" / "Commands" / "Program.TweakCommand.cs"
        ).read_text(encoding="utf-8").splitlines()

        self.assertLessEqual(len(cli_program_lines), 120)
        self.assertLessEqual(len(research_root_lines), 40)
        self.assertLessEqual(len(tweak_root_lines), 30)

    def test_cli_research_command_is_split_into_targeted_files(self) -> None:
        expected_paths = [
            REPO_ROOT / "cli" / "Program.ResearchHelpers.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.Promotion.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.Blocked.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.Automation.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.Trace.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.ResearchCommand.Json.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.TweakCommand.List.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.TweakCommand.Apply.cs",
            REPO_ROOT / "cli" / "Commands" / "Program.TweakCommand.Revert.cs",
        ]

        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected CLI split file: {path.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    unittest.main()
