# Windows Optimizer — Handoff (for Claude/Agents)

**Last Updated:** 2026-01-02
**Branch:** `main`

This doc is a practical handoff for the next agent (Claude or others): what changed recently, what is still broken/unfinished, and where to look.

## Quick Start

- Build: `dotnet build`
- Run: `dotnet run --project WindowsOptimizer.App`
- Logs:
  - Debug log: `%TEMP%\WindowsOptimizer_Debug.log`
  - Tweak CSV log: created under the app's log directory (see `AppPaths`)

## Recent High-Impact Changes (Good to Know)

- **Phase 8: TweaksViewModel Refactoring (2025-12-30)**
  - Reduced `TweaksViewModel.cs` from ~4500 lines to ~1300 lines
  - Created 10 specialized TweakProvider classes
  - All tweaks now loaded dynamically via `IEnumerable<ITweakProvider>` injection
  - Removed legacy helper methods (`CreateRegistryTweak`, etc.)
  - Files: `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`, `WindowsOptimizer.App/Services/TweakProviders/*.cs`

- **TweakProvider Implementations (2025-12-30)**
  - `PrivacyTweakProvider` - 25+ privacy tweaks
  - `SecurityTweakProvider` - 20+ security tweaks
  - `NetworkTweakProvider` - 15+ network tweaks
  - `SystemTweakProvider` - 15+ system tweaks
  - `PerformanceTweakProvider` - 10+ performance tweaks
  - `PowerTweakProvider` - 8+ power management tweaks
  - `VisibilityTweakProvider` - 12+ UI/UX tweaks
  - `PeripheralTweakProvider` - 6+ peripheral tweaks
  - `AudioTweakProvider` - 4+ audio tweaks
  - `MiscTweakProvider` - 8+ misc tweaks

- **Legacy tweak catalog restored (2025-12-30)**
  - Added `LegacyTweakProvider` to restore the 224-tweak catalog after the refactor.
  - Providers load first; legacy provider fills missing IDs (deduped).
  - Metadata enrichment now runs after all tweaks/plugins load.
  - Files: `WindowsOptimizer.App/Services/TweakProviders/LegacyTweakProvider.cs`,
    `WindowsOptimizer.App/ViewModels/MainViewModel.cs`,
    `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`

- **Tweak catalog + docs linking (2025-12-31)**
  - Added generator: `scripts/generate_tweak_catalog.py`
  - Outputs: `Docs/tweaks/tweak-catalog.md`, `Docs/tweaks/tweak-catalog.csv`, `Docs/tweaks/tweak-test-template.csv`
  - Added docs linker so tweaks show local doc links when `Docs/` is present
  - Added docs copy to build/publish output
  - New docs: `Docs/performance/performance.md`, `Docs/notifications/notifications.md`

- **Theme switching fix (runtime)**
  - Styles now use `DynamicResource` for theme-bound brushes so Light/Dark updates propagate.
  - Files: `WindowsOptimizer.App/Resources/Styles.xaml`

- **Light theme parity across main views (2025-12-31)**
  - Removed hard-coded colors from MainWindow/Dashboard/Tweaks/Monitor.
  - Added theme-aware brushes for caution/danger surfaces, terminal panels, and chart gradients.
  - Files: `WindowsOptimizer.App/MainWindow.xaml`, `WindowsOptimizer.App/Views/DashboardView.xaml`,
    `WindowsOptimizer.App/Views/TweaksView.xaml`, `WindowsOptimizer.App/Views/MonitorView.xaml`,
    `WindowsOptimizer.App/Resources/Colors.xaml`, `WindowsOptimizer.App/Resources/Colors.Light.xaml`

- **Docs linking + per-tweak anchors (2025-12-31)**
  - `tweak-catalog.html` generated with per-ID anchors.
  - Tweaks page now links directly to catalog entry anchors.
  - Files: `scripts/generate_tweak_catalog.py`, `Docs/tweaks/tweak-catalog.html`,
    `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`, `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

- **Startup scan + theme init ordering (2025-12-30)**
  - Theme is applied before any window shows (reduces dark→light flicker).
  - Splash renders first; scan runs before showing MainWindow.
  - Files: `WindowsOptimizer.App/App.xaml.cs`, `WindowsOptimizer.App/StartupWindow.xaml`

- **Detect runs off the UI thread (2025-12-30)**
  - `DetectStatusAsync` now uses a background task to avoid UI stalls.
  - File: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

- **Monitor card reveal/hover animation (safe transforms only) (2025-12-30)**
  - Added fade + lift on load; subtle hover scale without Freezable animation.
  - File: `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Tweak source links from catalog CSV (2025-12-30)**
  - UI surfaces `Source file` links from `Docs/tweaks/tweak-catalog.csv`.
  - File: `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`

- **Tweak source audit + coverage notes (2025-12-31)**
  - Added `scripts/audit_tweak_sources.py` to validate that each tweak’s registry/service tokens appear in the relevant docs folder.
  - Inserted small “App Coverage Notes” sections in category docs to document policy/value paths used by app-only tweaks.
  - Outputs: `Docs/tweaks/tweak-source-audit.md` + `.csv` (should show Missing documentation: 0 after updates).

- **Tweak docs report refresh (2026-01-02)**
  - `scripts/report_tweak_docs.py` validates per-tweak anchors across category docs, catalog, and details HTML.
  - Outputs: `Docs/tweaks/tweak-docs-report.md` + `.csv` + `.html` plus missing reports (should show 0 missing after updates).

- **Startup scan progress surfaced in splash (2025-12-30)**
  - Splash now updates with per‑tweak scan progress + current tweak name.
  - `DetectAllTweaksAsync` accepts progress + cancellation.
  - Files: `WindowsOptimizer.App/StartupWindow.xaml(.cs)`,
    `WindowsOptimizer.App/App.xaml.cs`,
    `WindowsOptimizer.App/ViewModels/MainViewModel.cs`,
    `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`,
    `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`,
    `WindowsOptimizer.App/ViewModels/StartupScanProgress.cs`

- **Tweak card compact summary (2025-12-30)**
  - Collapsed cards now show an area badge (Registry/Service/Task/etc.)
    plus `Current → Target` on a single line.
  - Files: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`,
    `WindowsOptimizer.App/Views/TweaksView.xaml`

- **Tweak catalog now includes Changes + Risk (2025-12-30)**
  - Catalog generator now extracts description + risk from tweak definitions.
  - CSV/MD/HTML include new columns for quick per‑tweak summaries.
  - Files: `scripts/generate_tweak_catalog.py`,
    `Docs/tweaks/tweak-catalog.md`,
    `Docs/tweaks/tweak-catalog.csv`,
    `Docs/tweaks/tweak-catalog.html`

- **Category docs now include per‑tweak anchors (2025-12-30)**
  - Each category doc gets a generated “Tweak Index” with anchors per tweak.
  - UI doc links now jump to the specific tweak section (file URI anchor support).
  - Files: `scripts/generate_tweak_catalog.py`,
    `Docs/*/*.md`,
    `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`,
    `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

- **Monitor charts now show Now/Peak/Low for network/disk (2025-12-30)**
  - Added live stats in the chart headers for better readability.
  - Files: `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`,
    `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor layout tightened for denser cards (2025-12-31)**
  - Reduced padding/margins for stat + section cards.
  - Files: `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor Network/Disk charts share scale + current dots (2025-12-31)**
  - MultiBinding converters use a common max across series for accurate comparison.
  - Added last-value markers for download/upload and read/write lines.
  - Files: `WindowsOptimizer.App/Views/MonitorView.xaml`,
    `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`,
    `WindowsOptimizer.App/ViewModels/ValueConverters.cs`

- **Monitor Network/Disk scale hints (2025-12-31)**
  - Added peak scale labels under chart headers for quick context.
  - File: `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor header toolbar modernized (2025-12-31)**
  - Added Live (1s) pill + icon Save button with hover states.
  - File: `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor top process lists compacted (2025-12-31)**
  - Added column headers and compact icon actions for CPU/RAM/Network lists.
  - File: `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor chart axis labels (2025-12-31)**
  - Added left-axis labels (max/mid/0) for Network/Disk charts.
  - Files: `WindowsOptimizer.App/Views/MonitorView.xaml`,
    `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

- **Monitor chart readability pass (2025-12-31)**
  - CPU/RAM now use dynamic axis labels (max/75/50/25/0).
  - Increased chart contrast via stronger area fills, glow, and gridlines.
  - Files: `WindowsOptimizer.App/Views/MonitorView.xaml`,
    `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`,
    `WindowsOptimizer.App/Resources/Colors.xaml`,
    `WindowsOptimizer.App/Resources/Colors.Light.xaml`

- **Monitor per-process network via ETW + TCP EStats (2025-12-31)**
  - Uses ETW (TCP + UDP) when available; falls back to TCP EStats, then IO approx.
  - Network section title/subtitle updates based on mode.
  - Files: `WindowsOptimizer.Infrastructure/Metrics/NetworkEtwSampler.cs`,
    `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`,
    `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`,
    `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Monitor disk I/O process list + fixed CPU/RAM axis scaling (2025-12-31)**
  - Added Top 10 processes by disk I/O (read + write bytes) with MB/s column.
  - CPU/RAM charts now scale to 0-100% for clearer axis labels.
  - Files: `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`,
    `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`,
    `WindowsOptimizer.App/Views/MonitorView.xaml`

- **Tweak status badges clarified (2025-12-31)**
  - Status text badge added near tweak names (compact + expanded).
  - Mixed state detected from current value and surfaced with icon/color.
  - Files: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`,
    `WindowsOptimizer.App/Views/TweaksView.xaml`

- **Batch tweak breakdown in Technical Info (2025-12-31)**
  - Detect messages with `Services:` or `Tasks:` now populate a per-item list in Technical Info.
  - Helps explain Mixed states (which items are missing/enabled).
  - Files: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`,
    `WindowsOptimizer.App/Views/TweaksView.xaml`

- **Registry batch breakdown + compact summary (2025-12-31)**
  - Registry batch tweaks now include per-entry details (`Entries:`) with current → target values.
  - Collapsed cards show a compact "matched / missing" summary when batch details exist.
  - Files: `WindowsOptimizer.Engine/Tweaks/RegistryValueBatchTweak.cs`,
    `WindowsOptimizer.Engine/Tweaks/RegistryValueSetTweak.cs`,
    `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`,
    `WindowsOptimizer.App/Views/TweaksView.xaml`

- **Non-essential services list expanded (2025-12-31)**
  - Added Bluetooth + print-related services to the batch list so Mixed state is explicit.
  - File: `WindowsOptimizer.App/Services/TweakProviders/SystemTweakProvider.cs`

- **Startup scan + rolled-back filter fix (2025-12-31)**
  - Auto Detect on app launch with a blocking overlay.
  - Rolled-back filter now uses `WasRolledBack` flag.
  - Files: `WindowsOptimizer.App/ViewModels/MainViewModel.cs`, `WindowsOptimizer.App/MainWindow.xaml`,
    `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`, `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

- **Durable rollback state persistence**
  - `RollbackStateStore` persists original values to `%AppData%\WindowsOptimizerSuite\rollback-state.json`

- **Crash recovery banner (Recover/Dismiss)**
  - Startup checks for pending rollbacks and surfaces a banner in `MainWindow` with recovery actions.

- **Category detection cancellation**
  - Categories now use CancellationTokenSource to cancel pending detection when collapsed

- **Monitor chart improvements**
  - Added gridlines (25%, 50%, 75%, 100%) to CPU/RAM history charts
  - Added Min/Max value indicators to chart headers

## Current Known Issues / Incomplete Work (Prioritized)

1) **Windows 10/11 native testing required**
   - All recent changes need verification on actual Windows (not WSL2).
   - Test checklist in `CODEX_PLAN.md` Phase 2.
   - Priority: HIGH
   - Include: light theme parity, docs anchor links, startup scan overlay

2) **WiX MSI Installer not implemented**
   - Professional installer needed for distribution
   - Priority: HIGH

3) **Network/Disk monitoring may show empty**
   - Delta-based throughput and LogicalDisk counters implemented but need real testing.
   - Relevant: `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`, `DiskMonitor.cs`

4) **Crash recovery UX could be improved (optional)**
   - Banner exists, but a modal dialog + detailed list of affected tweaks might be clearer.

5) **Memory leak testing**
   - Monitor page should be stress-tested (1hr) to verify no memory leaks.

6) **Legacy provider cleanup**
   - `LegacyTweakProvider` is a bridge to restore missing tweaks.
   - Remaining work: migrate leftover tweaks into category providers and remove legacy provider once parity is reached.

7) **Startup scan + theme flicker verification**
   - Theme now applies before splash, but needs Windows verification.
   - Verify no main-window flicker and no UI freeze during scan.

8) **Tweak docs depth**
   - Docs are linked, but per-tweak “what it changes / risk / source” content is still sparse.
   - Add anchored sections per tweak or per subcategory where practical.

9) **Monitor UX modernization**
   - Animations added, but overall layout and charts still need a compact + modern pass.

## Guardrails (Do Not Break)

- **SAFE tweaks must be reversible**: Detect → Apply → Verify → Rollback
- **Default is Preview/DryRun**: no automatic system changes
- **Do not add "disable Defender/Firewall/SmartScreen" under SAFE**
- **Admin operations must run via ElevatedHost** (separate process); app is not always-admin
- **Always log actions**; logs must be exportable
- **WPF animation rule**: do not animate Freezables created in templates/resources

## Operational Notes

- ElevatedHost path override: `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`
- Recommended publish layout: include `ElevatedHost/WindowsOptimizer.ElevatedHost.exe` beside the main app output.

## Where to Look (Common Entry Points)

- Tweaks UI: `WindowsOptimizer.App/Views/TweaksView.xaml`
- Tweak VM logic: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- Tweaks aggregation/filtering: `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` (~1300 lines)
- **TweakProviders: `WindowsOptimizer.App/Services/TweakProviders/*.cs` (modular tweak definitions)**
- Dashboard health: `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`
- ElevatedHost discovery: `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs`
- Monitor UI/VM: `WindowsOptimizer.App/Views/MonitorView.xaml`
- Metrics sources: `WindowsOptimizer.Infrastructure/Metrics/*.cs`
- Pipeline orchestration: `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`
- Rollback state: `WindowsOptimizer.Infrastructure/RollbackStateStore.cs`

## Suggested Next PRs (Order)

1. ~~TweaksViewModel Refactoring~~ ✅ DONE (Phase 8)
2. ~~TweakProvider implementations~~ ✅ DONE
3. **Windows 10/11 native testing** (PRIORITY)
4. **Startup scan/theme flicker verification** (PRIORITY)
5. **Tweak card summary (Current → Target + Area)**
6. **Docs depth pass (per-tweak summary + anchors)**
7. **Monitor UX modernization (compact + smooth)**
8. **WiX MSI Installer setup** (PRIORITY)
9. Memory optimization / leak testing
10. Optional: crash recovery modal + per-tweak selection

## Agent Verification Checklist (Please Validate)
- Confirm theme loads before splash (no dark→light flash).
- Confirm startup scan doesn’t stall UI (splash stays responsive).
- Confirm `Source file` links open the correct local file.
- Confirm Monitor animations don’t trigger Freezable animation errors.

## Files Changed This Session (2025-12-30)

- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` - Major refactoring (4500→1300 lines)
- `WindowsOptimizer.App/Services/TweakProviders/PrivacyTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/SecurityTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/NetworkTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/SystemTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PowerTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/VisibilityTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PeripheralTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/AudioTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/MiscTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/LegacyTweakProvider.cs` - Restored legacy tweak catalog
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs` - Added legacy provider to load order
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` - Apply tweak metadata after load
- `WindowsOptimizer.App/Resources/Styles.xaml` - Theme-bound resources switched to DynamicResource

## Files Changed This Session (2025-12-30)

- `WindowsOptimizer.App/App.xaml.cs` - Theme init before splash + render yield
- `WindowsOptimizer.App/StartupWindow.xaml` - Splash copy updated
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` - Detect runs off UI thread
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Monitor card reveal/hover animation
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs` - Source file links from CSV
- `Docs/tweaks/tweaks.md` - Docs note for catalog + source linking
- `HANDOFF_REPORT.md`, `CODEX_PLAN.md`, `CODEX_TODO.md`, `DEVELOPMENT_STATUS.md`, `CLAUDE.md` - Roadmap + agent checklist updates
- `WindowsOptimizer.App/StartupWindow.xaml` - Progress text placeholders
- `WindowsOptimizer.App/StartupWindow.xaml.cs` - Progress update method
- `WindowsOptimizer.App/ViewModels/StartupScanProgress.cs` - Progress model
- `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs` - Scan progress pipeline
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs` - Startup scan progress wiring
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` - Progress reporting + cancellation
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs` - CSV header mapping for docs links
- `scripts/generate_tweak_catalog.py` - Catalog now includes description + risk
- `Docs/tweaks/tweak-catalog.md` - Changes/Risk columns
- `Docs/tweaks/tweak-catalog.csv` - Description/Risk columns
- `Docs/tweaks/tweak-catalog.html` - Changes/Risk columns
- `Docs/*/*.md` - Generated Tweak Index sections with anchors
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Network/Disk header stats
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Network/Disk Now/Peak/Low properties

## Files Changed This Session (2025-12-31)

- `WindowsOptimizer.App/MainWindow.xaml` - Theme parity + startup scan overlay
- `WindowsOptimizer.App/Views/DashboardView.xaml` - Theme parity + system info panel updates
- `WindowsOptimizer.App/Views/TweaksView.xaml` - Theme parity, caution/danger badges, terminal styling
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Theme-aware chart gradients and button text colors
- `WindowsOptimizer.App/Resources/Colors.xaml` / `WindowsOptimizer.App/Resources/Colors.Light.xaml` - New theme tokens
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs` - Catalog entry anchor links
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` - Rollback flag + docs anchor open
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Network/Disk Now/Peak/Low stats
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Network/Disk header stats + tighter spacing
- `WindowsOptimizer.App/ViewModels/ValueConverters.cs` - Shared-scale sparklines + last-value dots
- `CODEX_TODO.md`, `DEVELOPMENT_STATUS.md`, `HANDOFF_REPORT.md` - Monitor UI status updates
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` - Mixed status detection + status badges
- `WindowsOptimizer.App/Views/TweaksView.xaml` - Status text badges near tweak names
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` - Batch detail extraction for services/tasks
- `WindowsOptimizer.App/Views/TweaksView.xaml` - Batch breakdown section in Technical Info
- `WindowsOptimizer.Engine/Tweaks/RegistryValueBatchTweak.cs` - Registry batch detail lines (Entries)
- `WindowsOptimizer.Engine/Tweaks/RegistryValueSetTweak.cs` - Registry batch detail lines (Entries)
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` - Batch summary line for collapsed cards
- `WindowsOptimizer.App/Views/TweaksView.xaml` - Collapsed summary line + registry breakdown
- `WindowsOptimizer.App/Services/TweakProviders/SystemTweakProvider.cs` - Expanded non-essential services list
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Header toolbar modernization (Live pill + Save)
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Compact top process tables (headers + icon actions)
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Network/Disk mid-scale values
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Network/Disk y-axis labels
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - CPU/RAM axis scale values
- `WindowsOptimizer.App/Views/MonitorView.xaml` - CPU/RAM axis labels + gridline contrast
- `WindowsOptimizer.App/Resources/Colors.xaml` - Added 20/40 alpha chart colors
- `WindowsOptimizer.App/Resources/Colors.Light.xaml` - Added 20/40 alpha chart colors
- `WindowsOptimizer.Infrastructure/Metrics/NetworkEtwSampler.cs` - ETW per-process network sampling
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs` - Network mode selection + TCP EStats fallback
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Network process mode title/subtitle
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Network section binds to mode labels
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs` - Disk I/O process list support (read/write MB/s)
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Disk process collection + export
- `WindowsOptimizer.App/Views/MonitorView.xaml` - Disk process list + CPU/RAM fixed axis labels
