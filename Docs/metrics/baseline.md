# Baseline Metrics

- Generated UTC: `2026-04-23T18:07:01Z`
- Source workflow run: `24087678150` on `main`
- Source commit: `fa50cbaa4fe1d619eab06cba50eda11412e066cf`
- Coverage artifact: `coverage-report` (`6308389518`)
- Coverage download path: `nightly-link`

## Coverage

- Total line coverage: `36.58%`
- Total branch coverage: `22.91%`

### Lowest Coverage Files
- `app/App.xaml` — line `0.00%`, branch `0.00%` (0/1 lines)
- `app/App.xaml.cs` — line `0.00%`, branch `0.00%` (0/50 lines)
- `app/Behaviors/NavigationTransitionBehavior.cs` — line `0.00%`, branch `0.00%` (0/105 lines)
- `app/Behaviors/WindowTitleBarDragBehavior.cs` — line `0.00%`, branch `0.00%` (0/19 lines)
- `app/Converters/ArcConverter.cs` — line `0.00%`, branch `0.00%` (0/39 lines)

## Highest Complexity Methods

- `app/Services/NohutoKeywordCategoryResolver.cs:5` `Resolve` — complexity `60`, lines `25`
- `app/ViewModels/WorkspaceFilterEvaluator.cs:44` `MatchesFilters` — complexity `34`, lines `94`
- `app/Services/TweakProviders/PrivacyTweakProvider.cs:29` `CreateTweaks` — complexity `29`, lines `1101`
- `app/Services/TweakDocumentationLinker.cs:9` `TweakDocumentationLinker` — complexity `27`, lines `141`
- `app/ViewModels/WinConfigCategoryCoverageMapper.cs:24` `MapLocalCategoryToWinConfigId` — complexity `22`, lines `36`

## Top Line-Count Hotspots (>300 lines)

- `app/ViewModels/TweakItemViewModel.cs` — `1900` lines
- `app/Services/TweakProviders/PrivacyTweakProvider.cs` — `1178` lines
- `tests/CommandTweakTests.cs` — `916` lines
- `app/ViewModels/TweaksViewModel.cs` — `914` lines
- `elevated-host/Program.cs` — `696` lines
- `engine/Tweaks/Misc/DisableVSCodeTelemetryTweak.cs` — `532` lines
- `app/Services/TweakProviders/SystemRegistryTweakProvider.cs` — `529` lines
- `app/Services/TweakProviders/VisibilityTweakProvider.cs` — `528` lines
- `app/Services/TweakProviders/JsonTweakLoader.cs` — `515` lines
- `engine/Tweaks/RegistryValuePresetBatchTweak.cs` — `469` lines
- `engine/TweakExecutionPipeline.cs` — `457` lines
- `infrastructure/Commands/CommandAllowlist.cs` — `430` lines
- `tests/CompositeTweakTests.cs` — `417` lines
- `tests/TweakItemViewModelTests.cs` — `412` lines
- `cli/Commands/Program.ResearchCommand.Blocked.cs` — `397` lines
- `engine/Tweaks/Commands/Registry/RegistryCommandBatchTweak.cs` — `387` lines
- `engine/Tweaks/ServiceStartModeBatchTweak.cs` — `383` lines
- `app/Services/TweakProviders/BaseTweakProvider.cs` — `382` lines
- `infrastructure/Services/ProfileManager.cs` — `377` lines
- `cli/Program.Validation.cs` — `372` lines
- `tests/TweakExecutionPipelineTests.cs` — `368` lines
- `app/Services/TweakProviders/NetworkTweakProvider.cs` — `367` lines
- `engine/Tweaks/RegistryValueTweak.cs` — `358` lines
- `engine/Tweaks/ScheduledTaskBatchTweak.cs` — `356` lines
- `app/ViewModels/ValueConverters.cs` — `352` lines
- `infrastructure/RollbackStateStore.cs` — `351` lines
- `engine/Tweaks/RegistryValueBatchTweak.cs` — `350` lines
- `tests/NohutoChangeAnalyzerTests.cs` — `349` lines
- `app/Services/TweakProviders/SystemTweakProvider.cs` — `347` lines
- `tests/TweakPromotionGateCatalogServiceTests.cs` — `347` lines
- `engine/Tweaks/RegistryValueSetTweak.cs` — `344` lines
- `engine/Tweaks/Developer/SetWsl2MemoryLimitTweak.cs` — `341` lines
- `app/ViewModels/TweakExecutionMessageParser.cs` — `328` lines
- `infrastructure/Elevation/ElevatedRegistryAccessor.cs` — `324` lines
- `engine/Tweaks/Commands/Cleanup/FileCleanupTweak.cs` — `319` lines
- `engine/Tweaks/Developer/EnableDockerWsl2BackendTweak.cs` — `318` lines
- `app/Services/TweakProviders/SecurityTweakProvider.cs` — `313` lines
- `tests/SecurityHardeningTests.cs` — `307` lines

## CLI Program.cs

- Method count: `5`
- Average method length: `14.4` lines
- Direct namespace dependencies: `System, System.Collections.Generic, System.CommandLine, System.Text.Json, RegProbe.Application.Services, RegProbe.Core, RegProbe.Engine, RegProbe.Infrastructure.Elevation`

### Command Hotspots
- `cli/Commands/Program.ResearchCommand.Blocked.cs` `RenderBlockedWorklist` — complexity `16`, lines `91`
- `cli/Commands/Program.ResearchCommand.Blocked.cs` `CreateResearchListBlockedCommand` — complexity `14`, lines `105`
- `cli/Commands/Program.TweakCommand.List.cs` `CreateTweakListCommand` — complexity `11`, lines `85`
- `cli/Commands/Program.ResearchCommand.Automation.cs` `CreateResearchGenerateRegressionPackCommand` — complexity `11`, lines `75`
- `cli/Commands/Program.DnsCommand.cs` `CreateDnsCommand` — complexity `10`, lines `119`

## UI Hotspots

- `app/Views/TweaksWorkspaceView.xaml` — `42` lines
- `app/Resources/TweaksWorkspaceResources.xaml` — `12` lines
- `app/ViewModels/TweaksViewModel.cs` — `914` lines
- `app/ViewModels/TweakItemViewModel.cs` — `1900` lines
- `app/MainWindow.xaml` — `531` lines
- `app/MainWindow.xaml.cs` — `23` lines

### MainWindow

- XAML lines: `531`
- Code-behind lines: `23`
- Event handlers in code-behind: `0`
- Mutable state fields in code-behind: `0`

## Post-Wave Hotspot Targets

### Active Review Targets

- `cli/Commands/Program.ResearchCommand.cs` — entrypoint to the `Program.ResearchCommand*.cs` partial family (`24` local lines, `826` aggregate family lines, complexity `90`); keep the research command surface in the active maintenance queue.
- `app/Services/TweakProviders/PrivacyTweakProvider.cs:CreateTweaks` — complexity `29`, lines `1101`; still the clearest large-method hotspot on the app side.

### Deferred Targets

- None. `app/ViewModels/TweakItemViewModel.cs` is currently at `1900/1980` budget and is no longer in the needs-refactor queue for this wave.

## Core Scripting Dependencies

- `core/core.csproj` package references: `none`
- `NLua` present in Core project: `False`
- `pythonnet` present in Core project: `False`
- Active runtime usage matches in repo scan: `0`
