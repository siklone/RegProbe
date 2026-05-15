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

    def test_app_xaml_keeps_tweaks_workspace_resources_global(self) -> None:
        app_xaml = (REPO_ROOT / "app" / "App.xaml").read_text(encoding="utf-8")

        self.assertIn('ResourceDictionary Source="Resources/TweaksWorkspaceResources.xaml"', app_xaml)

    def test_tweak_filter_dropdowns_bind_selected_value_to_item_tags(self) -> None:
        secondary_panel = (
            REPO_ROOT / "app" / "Views" / "Tweaks" / "TweaksSecondaryPanel.xaml"
        ).read_text(encoding="utf-8")

        self.assertIn('SelectedValuePath="Tag"', secondary_panel)
        self.assertIn('SelectedValue="{Binding StatusFilter, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"', secondary_panel)
        self.assertIn('SelectedValue="{Binding ScopeFilter, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"', secondary_panel)

    def test_normal_tweak_surface_uses_end_user_card_gate_not_contributor_override(self) -> None:
        filter_evaluator = (
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceFilterEvaluator.cs"
        ).read_text(encoding="utf-8")

        self.assertIn("item.IsEndUserAppCardAllowed", filter_evaluator)
        self.assertNotIn("!item.IsMutationAllowed", filter_evaluator)

    def test_public_source_copy_treats_catalog_only_as_non_semantics_proof(self) -> None:
        policy = (
            REPO_ROOT / "app" / "Services" / "PublicEvidenceLinkPolicy.cs"
        ).read_text(encoding="utf-8")

        self.assertIn("Catalog-only source context is not a value-semantics proof", policy)
        self.assertIn("Docs, Runtime, and Rollback carry the app-safety proof", policy)

    def test_control_templates_do_not_bind_margin_from_padding(self) -> None:
        checked_paths = [
            REPO_ROOT / "app" / "MainWindow.xaml",
            REPO_ROOT / "app" / "Resources" / "Styles.xaml",
            REPO_ROOT / "app" / "Resources" / "Tweaks" / "Buttons.xaml",
            REPO_ROOT / "app" / "Resources" / "Tweaks" / "List.xaml",
        ]

        for path in checked_paths:
            text = path.read_text(encoding="utf-8")
            self.assertNotIn('ContentPresenter Margin="{TemplateBinding Padding}"', text)

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
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogBootstrap.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogStore.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateApplicator.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateMutationEvaluator.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateQueryService.cs",
        ]

        self.assertLessEqual(len(service_lines), 160)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected promotion gate split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_promotion_gate_catalog_store_stays_split_into_loader_index_and_audit_files(self) -> None:
        store_lines = (
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogStore.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateAuditLogWriter.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCatalogLoader.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateCloner.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGateIndexBuilder.cs",
            REPO_ROOT / "app" / "Services" / "TweakPromotionGatePathResolver.cs",
        ]

        self.assertLessEqual(len(store_lines), 70)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected promotion gate store split file: {path.relative_to(REPO_ROOT)}")

    def test_win_config_catalog_service_stays_split_into_focused_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "WinConfigCatalogService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "WinConfigCatalogModels.cs",
            REPO_ROOT / "app" / "Services" / "WinConfigCatalogParser.cs",
            REPO_ROOT / "app" / "Services" / "WinConfigCatalogStore.cs",
            REPO_ROOT / "app" / "Services" / "WinConfigCatalogClient.cs",
        ]

        self.assertLessEqual(len(service_lines), 120)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected win-config split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_provenance_service_stays_split_into_models_and_store_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "TweakProvenanceCatalogService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakProvenanceCatalogModels.cs",
            REPO_ROOT / "app" / "Services" / "TweakProvenanceCatalogStore.cs",
        ]

        self.assertLessEqual(len(service_lines), 160)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected tweak provenance split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_evidence_class_service_stays_split_into_models_and_store_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "TweakEvidenceClassCatalogService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakEvidenceClassCatalogModels.cs",
            REPO_ROOT / "app" / "Services" / "TweakEvidenceClassCatalogStore.cs",
        ]

        self.assertLessEqual(len(service_lines), 80)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected evidence class split file: {path.relative_to(REPO_ROOT)}")

    def test_dns_service_stays_split_into_provider_store_and_flusher_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "DnsService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "DnsProviderCatalog.cs",
            REPO_ROOT / "app" / "Services" / "DnsConfigurationStore.cs",
            REPO_ROOT / "app" / "Services" / "DnsCacheFlusher.cs",
        ]

        self.assertLessEqual(len(service_lines), 80)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected DNS split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_catalog_service_stays_split_into_bootstrap_and_provider_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "TweakCatalogService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakCatalogBootstrap.cs",
            REPO_ROOT / "app" / "Services" / "TweakCatalogIndexBuilder.cs",
            REPO_ROOT / "app" / "Services" / "TweakProviderCatalog.cs",
        ]

        self.assertLessEqual(len(service_lines), 120)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected tweak catalog split file: {path.relative_to(REPO_ROOT)}")

    def test_main_composition_coordinator_keeps_research_and_developer_providers_wired(self) -> None:
        coordinator = (
            REPO_ROOT / "app" / "ViewModels" / "MainCompositionCoordinator.cs"
        ).read_text(encoding="utf-8")

        self.assertIn("new ResearchAppSurfaceTweakProvider()", coordinator)
        self.assertIn("new DeveloperTweakProvider()", coordinator)
        self.assertNotIn("new VisibilityTweakProvider()", coordinator)

    def test_tweak_documentation_linker_stays_split_into_service_and_store_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "TweakDocumentationLinker.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakDocumentationCatalogStore.cs",
        ]

        self.assertLessEqual(len(service_lines), 180)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected documentation linker split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_documentation_catalog_store_stays_split_into_path_index_and_anchor_files(self) -> None:
        store_lines = (
            REPO_ROOT / "app" / "Services" / "TweakDocumentationCatalogStore.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "TweakDocumentationAnchorCache.cs",
            REPO_ROOT / "app" / "Services" / "TweakDocumentationCatalogIndex.cs",
            REPO_ROOT / "app" / "Services" / "TweakDocumentationCatalogModels.cs",
            REPO_ROOT / "app" / "Services" / "TweakDocumentationPathResolver.cs",
            REPO_ROOT / "app" / "Services" / "TweakDocumentationTextHelpers.cs",
        ]

        self.assertLessEqual(len(store_lines), 80)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected documentation catalog store split file: {path.relative_to(REPO_ROOT)}")

    def test_nohuto_repo_scan_service_stays_split_into_client_and_store_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanClient.cs",
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanStore.cs",
            REPO_ROOT / "app" / "Services" / "NohutoRepositoryScanner.cs",
        ]

        self.assertLessEqual(len(service_lines), 120)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected nohuto repo scan split file: {path.relative_to(REPO_ROOT)}")

    def test_nohuto_repo_scan_store_stays_split_into_cache_report_and_result_files(self) -> None:
        store_lines = (
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanStore.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanCache.cs",
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanReportWriter.cs",
            REPO_ROOT / "app" / "Services" / "NohutoRepoScanResultBuilder.cs",
            REPO_ROOT / "app" / "Services" / "RepositoryScanPayload.cs",
        ]

        self.assertLessEqual(len(store_lines), 90)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected nohuto repo scan store split file: {path.relative_to(REPO_ROOT)}")

    def test_nohuto_change_analyzer_stays_split_into_logic_and_models_files(self) -> None:
        service_path = REPO_ROOT / "app" / "Services" / "NohutoChangeAnalyzer.cs"
        service_text = service_path.read_text(encoding="utf-8")
        service_lines = service_text.splitlines()
        engine_lines = (
            REPO_ROOT / "app" / "Services" / "NohutoChangeEngine.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "NohutoChangeModels.cs",
            REPO_ROOT / "app" / "Services" / "NohutoChangeEngine.cs",
            REPO_ROOT / "app" / "Services" / "NohutoGitHubModels.cs",
            REPO_ROOT / "app" / "Services" / "NohutoChangeClassifier.cs",
        ]

        self.assertLessEqual(len(service_lines), 40)
        self.assertLessEqual(len(engine_lines), 120)
        self.assertIn("NohutoChangeEngine.Analyze", service_text)
        self.assertNotIn("NohutoChangeClassifier.", service_text)
        self.assertNotIn("scoreByCategory", service_text)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected nohuto analyzer split file: {path.relative_to(REPO_ROOT)}")

    def test_nohuto_change_classifier_stays_split_into_resolver_files(self) -> None:
        classifier_lines = (
            REPO_ROOT / "app" / "Services" / "NohutoChangeClassifier.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "NohutoChangeKind.cs",
            REPO_ROOT / "app" / "Services" / "NohutoChangeKindResolver.cs",
            REPO_ROOT / "app" / "Services" / "NohutoKeywordCategoryResolver.cs",
            REPO_ROOT / "app" / "Services" / "NohutoRepositoryCategoryResolver.cs",
        ]

        self.assertLessEqual(len(classifier_lines), 40)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected nohuto classifier split file: {path.relative_to(REPO_ROOT)}")

    def test_crash_report_service_stays_split_into_model_store_and_sender_files(self) -> None:
        service_lines = (
            REPO_ROOT / "app" / "Services" / "CrashReportService.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "CrashReport.cs",
            REPO_ROOT / "app" / "Services" / "CrashReportFactory.cs",
            REPO_ROOT / "app" / "Services" / "CrashReportSender.cs",
            REPO_ROOT / "app" / "Services" / "CrashReportStore.cs",
        ]

        self.assertLessEqual(len(service_lines), 90)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected crash report split file: {path.relative_to(REPO_ROOT)}")

    def test_os_detection_resolver_stays_split_into_readers_and_normalizer_files(self) -> None:
        resolver_lines = (
            REPO_ROOT / "app" / "Services" / "OsDetectionResolver.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "OsDetectionResult.cs",
            REPO_ROOT / "app" / "Services" / "OsDetectionState.cs",
            REPO_ROOT / "app" / "Services" / "OsRegistryInfoReader.cs",
            REPO_ROOT / "app" / "Services" / "OsWmiInfoReader.cs",
            REPO_ROOT / "app" / "Services" / "OsDisplayNameNormalizer.cs",
        ]

        self.assertLessEqual(len(resolver_lines), 90)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected OS detection split file: {path.relative_to(REPO_ROOT)}")

    def test_parallel_tweak_executor_stays_split_into_models_and_result_tracker_files(self) -> None:
        executor_lines = (
            REPO_ROOT / "app" / "Services" / "ParallelTweakExecutor.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "BatchExecutionModels.cs",
            REPO_ROOT / "app" / "Services" / "BatchExecutionResultTracker.cs",
        ]

        self.assertLessEqual(len(executor_lines), 180)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected batch execution split file: {path.relative_to(REPO_ROOT)}")

    def test_elevated_host_locator_stays_split_into_candidate_and_diagnostics_files(self) -> None:
        locator_lines = (
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostLocator.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostBuildInfo.cs",
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostBuildInfoExtractor.cs",
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostCandidateBuilder.cs",
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostCandidateValidator.cs",
            REPO_ROOT / "app" / "Utilities" / "ElevatedHostLocatorDiagnostics.cs",
        ]

        self.assertLessEqual(len(locator_lines), 80)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected elevated host locator split file: {path.relative_to(REPO_ROOT)}")

    def test_single_instance_manager_stays_split_into_ipc_and_ui_helpers(self) -> None:
        manager_lines = (
            REPO_ROOT / "app" / "Services" / "SingleInstanceManager.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "Services" / "SingleInstanceIpcClient.cs",
            REPO_ROOT / "app" / "Services" / "SingleInstanceIpcServer.cs",
            REPO_ROOT / "app" / "Services" / "SingleInstanceKeyProvider.cs",
            REPO_ROOT / "app" / "Services" / "SingleInstanceUserNotifier.cs",
            REPO_ROOT / "app" / "Services" / "SingleInstanceWindowActivator.cs",
        ]

        self.assertLessEqual(len(manager_lines), 140)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected single instance split file: {path.relative_to(REPO_ROOT)}")

    def test_workspace_browse_coordinator_stays_split_into_filter_search_and_group_builder_files(self) -> None:
        coordinator_lines = (
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceBrowseCoordinator.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceFilterEvaluator.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceCategoryGroupBuilder.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceSearchDebouncer.cs",
        ]

        self.assertLessEqual(len(coordinator_lines), 140)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected workspace browse split file: {path.relative_to(REPO_ROOT)}")

    def test_app_resource_dictionary_loads_converters_before_modern_styles(self) -> None:
        app_xaml = (REPO_ROOT / "app" / "App.xaml").read_text(encoding="utf-8")

        converters_index = app_xaml.find('Source="Resources/Converters.xaml"')
        modern_styles_index = app_xaml.find('Source="Resources/ModernStyles.xaml"')

        self.assertNotEqual(-1, converters_index)
        self.assertNotEqual(-1, modern_styles_index)
        self.assertLess(
            converters_index,
            modern_styles_index,
            "ModernStyles.xaml uses StaticResource converters, so Converters.xaml must load first.",
        )

    def test_workspace_catalog_coordinator_stays_split_into_loader_metadata_and_coverage_files(self) -> None:
        coordinator_lines = (
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceCatalogCoordinator.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceProviderTweakLoader.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspacePluginTweakLoader.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceTweakMetadataApplier.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceTweakIdSetBuilder.cs",
            REPO_ROOT / "app" / "ViewModels" / "WinConfigCategoryCoverageMapper.cs",
        ]

        self.assertLessEqual(len(coordinator_lines), 100)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected workspace catalog split file: {path.relative_to(REPO_ROOT)}")

    def test_workspace_command_coordinator_stays_split_into_command_set_file(self) -> None:
        coordinator_lines = (
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceCommandCoordinator.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceCommandSet.cs",
        ]

        self.assertLessEqual(len(coordinator_lines), 260)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected workspace command split file: {path.relative_to(REPO_ROOT)}")

    def test_tweaks_view_model_keeps_infrastructure_bootstrap_split_out(self) -> None:
        view_model_lines = (
            REPO_ROOT / "app" / "ViewModels" / "TweaksViewModel.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "ViewModels" / "TweaksWorkspaceInfrastructure.cs",
            REPO_ROOT / "app" / "ViewModels" / "WorkspaceSummaryPresentation.cs",
        ]

        self.assertLessEqual(len(view_model_lines), 920)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected TweaksViewModel split file: {path.relative_to(REPO_ROOT)}")

    def test_tweak_item_view_model_keeps_presentation_helpers_split_out(self) -> None:
        view_model_lines = (
            REPO_ROOT / "app" / "ViewModels" / "TweakItemViewModel.cs"
        ).read_text(encoding="utf-8").splitlines()
        expected_paths = [
            REPO_ROOT / "app" / "ViewModels" / "TweakItemPresentationFormatter.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakTechnicalInfoBuilder.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakExecutionMessageParser.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakExecutionLogFormatter.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakReferenceLinkNavigator.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakClipboardHelper.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakFileLogger.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakChoiceStateCoordinator.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakOptionModels.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakReferenceLinkModels.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakItemStateModels.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakCategoryPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakStatusPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakEvidenceClassPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakVerdictPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakProofSnapshotPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakRollbackPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakSurfacePresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakOutcomePresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakInventoryPresentation.cs",
            REPO_ROOT / "app" / "ViewModels" / "TweakProvenancePresentation.cs",
        ]

        self.assertLessEqual(len(view_model_lines), 1980)
        for path in expected_paths:
            self.assertTrue(path.exists(), f"Missing expected TweakItemViewModel split file: {path.relative_to(REPO_ROOT)}")

    def test_application_layer_links_and_app_layer_removes_split_service_files(self) -> None:
        application_project = (REPO_ROOT / "application" / "application.csproj").read_text(encoding="utf-8")
        app_project = (REPO_ROOT / "app" / "app.csproj").read_text(encoding="utf-8")

        expected_linked_files = [
            "..\\\\app\\\\Services\\\\ConfigExportModels.cs",
            "..\\\\app\\\\Services\\\\ConfigExportSnapshotBuilder.cs",
            "..\\\\app\\\\Services\\\\ConfigImportExecutor.cs",
            "..\\\\app\\\\Services\\\\DnsCacheFlusher.cs",
            "..\\\\app\\\\Services\\\\DnsConfigurationStore.cs",
            "..\\\\app\\\\Services\\\\DnsProviderCatalog.cs",
            "..\\\\app\\\\Services\\\\PresetCatalog.cs",
            "..\\\\app\\\\Services\\\\PresetExecutionEngine.cs",
            "..\\\\app\\\\Services\\\\TweakCatalogBootstrap.cs",
            "..\\\\app\\\\Services\\\\TweakCatalogIndexBuilder.cs",
            "..\\\\app\\\\Services\\\\TweakProviderCatalog.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogModels.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogBootstrap.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogStore.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateApplicator.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateMutationEvaluator.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateQueryService.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateAuditLogWriter.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCatalogLoader.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateCloner.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGateIndexBuilder.cs",
            "..\\\\app\\\\Services\\\\TweakPromotionGatePathResolver.cs",
            "..\\\\app\\\\Utilities\\\\ElevatedHostBuildInfo.cs",
            "..\\\\app\\\\Utilities\\\\ElevatedHostBuildInfoExtractor.cs",
            "..\\\\app\\\\Utilities\\\\ElevatedHostCandidateBuilder.cs",
            "..\\\\app\\\\Utilities\\\\ElevatedHostCandidateValidator.cs",
            "..\\\\app\\\\Utilities\\\\ElevatedHostLocatorDiagnostics.cs",
        ]
        expected_removed_files = [
            "Services\\\\ConfigExportModels.cs",
            "Services\\\\ConfigExportSnapshotBuilder.cs",
            "Services\\\\ConfigImportExecutor.cs",
            "Services\\\\DnsCacheFlusher.cs",
            "Services\\\\DnsConfigurationStore.cs",
            "Services\\\\DnsProviderCatalog.cs",
            "Services\\\\PresetCatalog.cs",
            "Services\\\\PresetExecutionEngine.cs",
            "Services\\\\TweakCatalogBootstrap.cs",
            "Services\\\\TweakCatalogIndexBuilder.cs",
            "Services\\\\TweakProviderCatalog.cs",
            "Services\\\\TweakPromotionGateCatalogModels.cs",
            "Services\\\\TweakPromotionGateCatalogBootstrap.cs",
            "Services\\\\TweakPromotionGateCatalogStore.cs",
            "Services\\\\TweakPromotionGateApplicator.cs",
            "Services\\\\TweakPromotionGateMutationEvaluator.cs",
            "Services\\\\TweakPromotionGateQueryService.cs",
            "Services\\\\TweakPromotionGateAuditLogWriter.cs",
            "Services\\\\TweakPromotionGateCatalogLoader.cs",
            "Services\\\\TweakPromotionGateCloner.cs",
            "Services\\\\TweakPromotionGateIndexBuilder.cs",
            "Services\\\\TweakPromotionGatePathResolver.cs",
            "Utilities\\\\ElevatedHostBuildInfo.cs",
            "Utilities\\\\ElevatedHostBuildInfoExtractor.cs",
            "Utilities\\\\ElevatedHostCandidateBuilder.cs",
            "Utilities\\\\ElevatedHostCandidateValidator.cs",
            "Utilities\\\\ElevatedHostLocatorDiagnostics.cs",
        ]

        for value in expected_linked_files:
            self.assertIn(value, application_project)

        for value in expected_removed_files:
            self.assertIn(value, app_project)


if __name__ == "__main__":
    unittest.main()
