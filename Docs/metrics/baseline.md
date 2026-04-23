# Baseline Metrics

- Generated UTC: `2026-04-23T12:34:11Z`
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

## Top Line-Count Hotspots

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

## CLI Program.cs

- Method count: `5`
- Average method length: `14.2` lines
- Direct namespace dependencies: `System, System.Collections.Generic, System.CommandLine, System.Text.Json, RegProbe.Application.Services, RegProbe.Core, RegProbe.Engine, RegProbe.Infrastructure.Elevation`

### Command Hotspots
- `cli/Commands/Program.ResearchCommand.Blocked.cs` `RenderBlockedWorklist` — complexity `16`, lines `141`
- `cli/Commands/Program.ResearchCommand.Blocked.cs` `CreateResearchListBlockedCommand` — complexity `14`, lines `83`
- `cli/Commands/Program.ResearchCommand.Automation.cs` `CreateResearchGenerateRegressionPackCommand` — complexity `11`, lines `58`
- `cli/Commands/Program.TweakCommand.List.cs` `CreateTweakListCommand` — complexity `10`, lines `74`
- `cli/Commands/Program.TweakCommand.Revert.cs` `CreateTweakRevertCommand` — complexity `8`, lines `66`

## UI Hotspots

- `app/Views/TweaksWorkspaceView.xaml` — `42` lines
- `app/Resources/TweaksWorkspaceResources.xaml` — `12` lines
- `app/ViewModels/TweaksViewModel.cs` — `914` lines
- `app/ViewModels/TweakItemViewModel.cs` — `1900` lines
- `app/MainWindow.xaml` — `531` lines
- `app/MainWindow.xaml.cs` — `27` lines

### MainWindow

- XAML lines: `531`
- Code-behind lines: `27`
- Event handlers in code-behind: `0`
- Mutable state fields in code-behind: `0`

## Post-Wave Hotspot Targets

### Active Review Targets

- `cli/Commands/Program.ResearchCommand*.cs` — partial-family aggregate of `826` physical lines and complexity `90` across six files; keep this command family in the active maintenance queue even though the root partial file stays thin.
- `app/Services/TweakProviders/PrivacyTweakProvider.cs:29` `CreateTweaks` — complexity `29`, lines `1101`; still the clearest large-method hotspot on the app side.

### Deferred Targets

- `app/ViewModels/TweakItemViewModel.cs` — `1900` lines; keep it out of the active hotspot queue for now because it is already within the current `1900/1980` budget and should not be reopened in this wave.

## Core Scripting Dependencies

- `core/core.csproj` package references: `none`
- `NLua` present in Core project: `False`
- `pythonnet` present in Core project: `False`
- Active runtime usage matches in repo scan: `0`
