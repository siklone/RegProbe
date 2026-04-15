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

    def test_config_export_service_stays_split_into_use_case_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "ConfigExportService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "ConfigExportSnapshotBuilder.cs",
            REPO_ROOT / "app" / "Services" / "ConfigImportExecutor.cs",
            REPO_ROOT / "app" / "Services" / "ConfigExportModels.cs",
        ]

        self.assertLessEqual(len(service_lines), 120)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected config export split file: {path.relative_to(REPO_ROOT)}")

    def test_preset_service_stays_split_into_catalog_and_execution_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "PresetService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "PresetCatalog.cs",
            REPO_ROOT / "app" / "Services" / "PresetExecutionEngine.cs",
        ]

        self.assertLessEqual(len(service_lines), 80)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected preset split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_promotion_gate_service_stays_split_into_models_and_store_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogModels.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogStore.cs",
        ]

        self.assertLessEqual(len(service_lines), 280)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected promotion gate split file: {path.relative_to(REPO_ROOT)}")

    def test_application_layer_links_and_app_layer_removes_split_service_files(self) -> None:
        application_project = (REPO_ROOT / "application" / "application.csproj").read_text(encoding="utf-8")
        app_project = (REPO_ROOT / "app" / "app.csproj").read_text(encoding="utf-8")

        expected_linked_files = [
            "..\\\\app\\\\Services\\\\ConfigExportModels.cs",
            "..\\\\app\\\\Services\\\\ConfigExportSnapshotBuilder.cs",
            "..\\\\app\\\\Services\\\\ConfigImportExecutor.cs",
            "..\\\\app\\\\Services\\\\PresetCatalog.cs",
            "..\\\\app\\\\Services\\\\PresetExecutionEngine.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogModels.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogStore.cs",
        ]
        expected_removed_files = [
            "Services\\\\ConfigExportModels.cs",
            "Services\\\\ConfigExportSnapshotBuilder.cs",
            "Services\\\\ConfigImportExecutor.cs",
            "Services\\\\PresetCatalog.cs",
            "Services\\\\PresetExecutionEngine.cs",
            "Services\\\\TweakPromotionGateCatalogModels.cs",
            "Services\\\\TweakPromotionGateCatalogStore.cs",
        ]

        for value in expected_linked_files:
            self.assertIn(value, application_project)

        for value in expected_removed_files:
            self.assertIn(value, app_project)


if __name__ == "__main__":
    unittest.main()
