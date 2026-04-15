# Baseline Metrics

- Generated UTC: `2026-04-15T11:36:01Z`
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

- `cli/Commands/Program.ResearchCommand.cs:15` `CreateResearchCommand` — complexity `63`, lines `537`
- `app/Services/NohutoChangeAnalyzer.cs:305` `ResolveKeywordCategory` — complexity `60`, lines `50`
- `app/ViewModels/WorkspaceBrowseCoordinator.cs:105` `MatchesFilters` — complexity `34`, lines `94`
- `app/Services/OsDetectionResolver.cs:28` `Resolve` — complexity `31`, lines `184`
- `app/Services/TweakProviders/PrivacyTweakProvider.cs:29` `CreateTweaks` — complexity `29`, lines `1101`

## CLI Program.cs

- Method count: `9`
- Average method length: `23.0` lines
- Direct namespace dependencies: `System, System.Collections.Generic, System.CommandLine, System.Diagnostics, System.IO, System.Linq, System.Text.Json, RegProbe.Application.Services, RegProbe.Application.Services.TweakProviders, RegProbe.Application.Utilities, RegProbe.Core, RegProbe.Engine, RegProbe.Infrastructure.Elevation`

### Command Hotspots
- `cli/Commands/Program.ResearchCommand.cs` `CreateResearchCommand` — complexity `63`, lines `537`
- `cli/Commands/Program.TweakCommand.cs` `CreateTweakCommand` — complexity `21`, lines `198`
- `cli/Commands/Program.DnsCommand.cs` `CreateDnsCommand` — complexity `7`, lines `96`
- `cli/Commands/Program.PresetCommand.cs` `CreatePresetCommand` — complexity `5`, lines `78`
- `cli/Commands/Program.ExportCommand.cs` `CreateExportCommand` — complexity `1`, lines `59`

## UI Hotspots

- `app/Views/TweaksWorkspaceView.xaml` — `744` lines
- `app/Resources/TweaksWorkspaceResources.xaml` — `806` lines
- `app/ViewModels/TweaksViewModel.cs` — `1180` lines
- `app/ViewModels/TweakItemViewModel.cs` — `3530` lines
- `app/MainWindow.xaml` — `530` lines
- `app/MainWindow.xaml.cs` — `27` lines

### MainWindow

- XAML lines: `530`
- Code-behind lines: `27`
- Event handlers in code-behind: `0`
- Mutable state fields in code-behind: `0`

## Core Scripting Dependencies

- `core/core.csproj` package references: `none`
- `NLua` present in Core project: `False`
- `pythonnet` present in Core project: `False`
- Active runtime usage matches in repo scan: `0`
