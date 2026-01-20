# Development Status & Known Issues

**Last Updated:** January 19, 2026
**Project:** Windows Optimizer - WPF .NET 8
**Branch:** main

---

## 📋 Table of Contents
- [Recent Fixes & Changes](#recent-fixes--changes)
- [Known Issues](#known-issues)
- [Potential Issues](#potential-issues)
- [Platform Compatibility](#platform-compatibility)
- [Todo & Roadmap](#todo--roadmap)
- [Testing Checklist](#testing-checklist)

---

## ✅ Recent Fixes & Changes

### 0. Sprint 1: Single Instance + Threading Architecture (Commit: pending)
**Improvement:** Added core infrastructure for single instance enforcement and multi-threaded metric collection.

**New Files:**
- `WindowsOptimizer.App/Services/SingleInstanceManager.cs` - Named Mutex + Named Pipe IPC
- `WindowsOptimizer.Infrastructure/Threading/MetricDataBus.cs` - Channel-based lock-free messaging
- `WindowsOptimizer.Infrastructure/Threading/MetricWorkerPool.cs` - Dedicated worker thread pool
- `WindowsOptimizer.Infrastructure/Threading/ThreadingDiagnostics.cs` - Performance tracking

**Modified Files:**
- `WindowsOptimizer.App/App.xaml.cs` - Single instance integration

**Features:**
- Only one app instance can run at a time
- Second instance forwards args to first via Named Pipes
- Abandoned mutex recovery (handles crashed previous instance)
- 60fps debounced UI updates via Channel-based messaging
- Dedicated worker threads for metric collection
- Performance diagnostics for threading subsystem

**Status:** 🧪 **IMPLEMENTED** - Needs Windows testing

---

### 0.1 Sprint 2: Fallback Data Provider + Hardware Identification (Commit: pending)
**Improvement:** Added robust data fetching with multiple sources and hardware identification.

**New Files:**
- `WindowsOptimizer.Infrastructure/Data/FallbackDataProvider.cs` - Priority-based fallback with retry + circuit breaker
- `WindowsOptimizer.Infrastructure/Data/RetryPolicy.cs` - Exponential backoff retry configuration
- `WindowsOptimizer.Infrastructure/Data/DataResult.cs` - Result types with error tracking
- `WindowsOptimizer.Infrastructure/Hardware/HardwareIdentifier.cs` - CPU, GPU, Motherboard, RAM identification

**Features:**
- FallbackDataProvider tries sources in priority order until one succeeds
- Retry with exponential backoff (configurable)
- Circuit breaker pattern disables failing sources temporarily
- Hardware identification via WMI and Registry
- CPU: Name, cores, threads, clock speed, architecture
- GPU: PCI ID, vendor, driver, VRAM
- Motherboard: Manufacturer, product, serial
- RAM: Modules, capacity, speed, type

**Status:** 🧪 **IMPLEMENTED** - Needs Windows testing

---

### 0.2 Sprint 3: Splash Screen Preloading (Commit: pending)
**Improvement:** Added PreloadManager for structured startup task management.

**New Files:**
- `WindowsOptimizer.App/Services/PreloadManager.cs` - Preload task orchestration

**Features:**
- Critical tasks run sequentially (must succeed for app to start)
- Non-critical tasks run in parallel (can fail gracefully)
- Progress reporting for UI updates
- Semaphore-based throttling for parallel tasks
- PreloadException for critical failures

**Usage:**
```csharp
var preloader = new PreloadManager(progress);
preloader.RegisterTask("Settings", LoadSettings, isCritical: true, priority: 100);
preloader.RegisterTask("Hardware", ScanHardware, isCritical: false, priority: 50);
var result = await preloader.RunAllAsync(ct);
```

**Status:** 🧪 **IMPLEMENTED** - Ready for integration with StartupWindow

---

### 0.3 Sprint 4: Hardware Card ViewModels (Commit: pending)
**Improvement:** Added reusable hardware card ViewModels for Monitor view redesign.

**New Files:**
- `WindowsOptimizer.App/ViewModels/Hardware/HardwareCardViewModelBase.cs` - Base class with common properties
- `WindowsOptimizer.App/ViewModels/Hardware/CpuCardViewModel.cs` - CPU card with identification and metrics
- `WindowsOptimizer.App/ViewModels/Hardware/GpuCardViewModel.cs` - GPU card with VRAM and vendor info
- `WindowsOptimizer.App/ViewModels/Hardware/RamCardViewModel.cs` - RAM card with modules and speed

**Features:**
- Base class with Icon, Title, Subtitle, PrimaryValue, SecondaryMetrics
- Automatic hardware identification via HardwareIdentifier
- MetricDataBus integration for real-time updates
- Color-coded status (green/yellow/red) based on thresholds
- INotifyPropertyChanged for WPF binding

**Status:** 🧪 **IMPLEMENTED** - Ready for XAML integration

---

### 0.4 Sprint 5: Process Management (Commit: pending)
**Improvement:** Added process priority, affinity, and memory management utilities.

**New Files:**
- `WindowsOptimizer.Infrastructure/Process/ProcessPriorityManager.cs` - Priority control (Idle to Realtime)
- `WindowsOptimizer.Infrastructure/Process/ProcessAffinityManager.cs` - CPU affinity with P/E-core presets
- `WindowsOptimizer.Infrastructure/Process/ProcessMemoryManager.cs` - Working set trimming

**Features:**
- Set/Get process priority with admin detection
- CPU affinity masks with presets (PerformanceCores, EfficiencyCores, AllCores)
- Working set trimming (single process or all)
- Memory info retrieval (working set, private, virtual)

**Status:** 🧪 **IMPLEMENTED** - Ready for UI integration

---

### 0.5 Quick Wins: Toast Notifications + Keyboard Shortcuts (Commit: pending)
**Improvement:** Added toast notification system and keyboard shortcuts for better UX.

**New Files:**
- `WindowsOptimizer.App/Services/INotificationService.cs` - Interface
- `WindowsOptimizer.App/Services/NotificationService.cs` - Implementation
- `WindowsOptimizer.App/Views/Controls/NotificationHost.xaml` - XAML overlay
- `WindowsOptimizer.App/Converters/CommonConverters.cs` - NullToCollapsed etc.

**Modified Files:**
- `WindowsOptimizer.App/MainWindow.xaml` - Added NotificationHost, controls namespace
- `WindowsOptimizer.App/MainWindow.xaml.cs` - NotificationService integration

**Features:**
- Toast notifications: Success/Warning/Error/Info
- Auto-dismiss with configurable duration
- Slide-in animation
- Global access via MainWindow.Instance.Notifications
- Keyboard shortcuts: Ctrl+1-5 tabs, Ctrl+F search, Escape clear

**Status:** ✅ **IMPLEMENTED** - Ready to use

---

### 0.6 Performance & Stability Pack (Commit: pending)
**Improvement:** Implemented Async/Await best practices and verified UI virtualization.

**New Files:**
- `WindowsOptimizer.App/ViewModels/AsyncRelayCommand.cs` - Safe async command implementation
- `WindowsOptimizer.App/Utilities/TaskExtensions.cs` - SafeFireAndForget extension

**Modified Files:**
- `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs` - Replaced `async void` with safe commands
- `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs` - Stabilized expand/collapse logic

**Features:**
- **Crash Prevention:** Replaced dangerous `async void` patterns with `AsyncRelayCommand` that handles exceptions safely.
- **UI Responsiveness:** Verified `VirtualizingStackPanel` and Recycling mode in `TweaksView` to handle large lists efficiently.
- **Safe Fire-and-Forget:** Added robust error logging for background tasks.

**Status:** ✅ **OPTIMIZED** - Stability improved

---
### 0.7 Startup Preload + Hardware Detail Live Metrics (Commit: pending)
**Improvement:** Integrated PreloadManager and metric threading into startup; hardware detail windows now load async with live metrics.

**New Files:**
- `WindowsOptimizer.App/Services/AppServices.cs` - Shared MetricWorkerPool + MetricDataBus initialization
- `WindowsOptimizer.App/ViewModels/Hardware/HardwareDetailViewModel.cs` - Async hardware detail loader with live metric updates

**Modified Files:**
- `WindowsOptimizer.App/App.xaml.cs` - PreloadManager tasks + threading initialization before MainWindow
- `WindowsOptimizer.App/StartupWindow.xaml.cs` - Splash progress updates for preload tasks
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs` - Pass MetricWorkerPool into MonitorViewModel
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` - Publish missing metric keys, disk bytes/sec, worker pool sampling
- `WindowsOptimizer.App/ViewModels/Hardware/RamCardViewModel.cs` - RAM GB unit fix for live values
- `WindowsOptimizer.App/Views/HardwareDetailWindow.xaml.cs` - Dispose view model on close

**Features:**
- Startup preloads threading + hardware identifiers on splash (non-blocking)
- MetricWorkerPool backs monitor sampling for core/io/process/aux metrics
- Hardware detail window updates CPU/GPU/RAM/Disk stats live
- Hardware detail quick stats now update with live clock/power, RAM used/free, and disk read/write speeds
- MetricDataBus now emits CPU clock/power, GPU clock/power/memory, RAM available, disk read/write, disk health
- RAM + disk card units aligned (GB + bytes/sec)

**Status:** ?. **IMPLEMENTED** - Needs Windows verification

---

### 0.8 Hardware Database Scaffolding (Commit: pending)
**Improvement:** Added SQLite-backed hardware specs database with fallback resolver for CPU/GPU cards.

**New Files:**
- `WindowsOptimizer.Infrastructure/Hardware/HardwareDatabase.cs` - SQLite schema + lookup helpers
- `WindowsOptimizer.Infrastructure/Hardware/HardwareSpecs.cs` - Specs models
- `WindowsOptimizer.Infrastructure/Hardware/HardwareSpecsService.cs` - Fallback resolver using `FallbackDataProvider`

**Modified Files:**
- `WindowsOptimizer.Infrastructure/AppPaths.cs` - Hardware DB path
- `WindowsOptimizer.Infrastructure/WindowsOptimizer.Infrastructure.csproj` - Microsoft.Data.Sqlite reference
- `WindowsOptimizer.App/App.xaml.cs` - Preload database init
- `WindowsOptimizer.App/ViewModels/Hardware/CpuCardViewModel.cs` - Use specs resolver
- `WindowsOptimizer.App/ViewModels/Hardware/GpuCardViewModel.cs` - Use specs resolver

**Features:**
- Hardware DB schema created on startup under app data
- Fallback specs resolution (DB -> identity) for CPU/GPU cards
- Cards show DB-backed subtitles and mark `HasSpecs` when matched

**Status:** ?. **IMPLEMENTED** - DB population/update pending

---


### 1. Monitor Page Crash Fix (Commit: `0082b11`, `158b5b8`)
**Problem:** Application crashed when clicking the Monitor tab.

**Root Cause:**
- Field initializers (`private readonly ProcessMonitor _processMonitor = new();`) execute **before** constructor
- Exceptions in field initializers are NOT caught by constructor try-catch blocks
- If any monitoring service (CPU, RAM, Network, Disk) failed to initialize, the entire app crashed

**Solution:**
- Made all monitor service fields **nullable** (`MetricProvider?`, `ProcessMonitor?`, etc.)
- Removed field initializers
- Moved initialization to constructor with **individual try-catch blocks** for each service
- Added null checks throughout MonitorViewModel (`if (_processMonitor != null)`)
- Application now starts with partial monitoring if some services fail

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

**Status:** ✅ **FIXED** - Monitor page loads gracefully even if some services fail

---

### 2. Tweaks Category Expansion Crash Fix (Commit: `1e302fa`)
**Problem:** Application crashed when rapidly opening/closing tweak categories or clicking between categories.

**Root Cause:**
- Fire-and-forget async pattern: `_ = DetectAllTweaksAsync();`
- Exceptions in background tasks were not caught
- If any tweak's `DetectAsync()` hung or threw an exception, the entire category expansion crashed

**Solution:**
- Changed `ToggleExpand()` from `void` to `async void`
- Properly awaited `DetectAllTweaksAsync()` with try-catch
- Added **5-second timeout** per tweak detection using `Task.WhenAny()`
- Added nested try-catch for individual tweak detection to continue with other tweaks if one fails

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs`

**Status:** ✅ **FIXED** - Category expansion is now stable and timeouts are logged

---

### 3. Tweak Apply Operation Hanging (Commit: `e81a462`)
**Problem:** Clicking "Apply" on a tweak (especially Power tweaks) caused the application to hang indefinitely with a spinning cursor. No progress, no error message.

**Root Cause:**
- `TweakExecutionPipeline.RunStepAsync()` had **no timeout mechanism**
- If a tweak's `DetectAsync()`, `ApplyAsync()`, `VerifyAsync()`, or `RollbackAsync()` hung, it would wait forever
- Power tweaks were timing out trying to connect to the Elevated Host

**Solution:**
- Added **30-second timeout** to `TweakExecutionPipeline.RunStepAsync()`
- Used `CancellationTokenSource.CreateLinkedTokenSource()` with `CancelAfter(30s)`
- Used `Task.WhenAny()` to race between the operation and a timeout task
- Returns `TweakStatus.Failed` with clear timeout message

**Files Changed:**
- `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`

**Status:** ✅ **FIXED** - Tweaks now timeout after 30 seconds and show clear error message

---

### 4. Power Tweaks Timeout on Non-Windows Platforms (Commit: `32ef7f2`)
**Problem:** Power tweaks (and all `LocalMachine` registry tweaks) timed out after 30 seconds when running on Linux/WSL2.

**Root Cause:**
- Application was running on **Linux WSL2** (`uname -r` shows `6.6.87.2-microsoft-standard-WSL2`)
- Power tweaks require:
  1. **Elevated Host** (WindowsOptimizer.ElevatedHost.exe) - Windows executable
  2. **Windows Registry** (`HKEY_LOCAL_MACHINE`) - doesn't exist on Linux
  3. **Windows Named Pipes** - not compatible with Linux
- `ElevatedHostClient` tried to connect for 30 seconds before giving up
- Each Power tweak waited the full 30 seconds, causing 5-7 minute delays

**Solution:**
- Added **platform detection** to `ElevatedHostClient` and `LocalRegistryAccessor`
- Immediately throws `PlatformNotSupportedException` on non-Windows platforms
- `StartupConnectTimeout` remains **30 seconds on Windows** (allows time for UAC + host startup)
- Added comprehensive logging to `ElevatedHostClient` for debugging
- Total time on non-Windows reduced to near-immediate failure (no connection attempts)

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClient.cs`
- `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClientOptions.cs`
- `WindowsOptimizer.Infrastructure/Registry/LocalRegistryAccessor.cs`

**Status:** ✅ **FIXED** - Platform issues now fail immediately with clear error messages

---

### 5. File Logging for WPF Debugging (Commits: Multiple)
**Problem:** WPF applications don't show console output, making debugging impossible.

**Solution:**
- Added `LogToFile()` helper methods to multiple ViewModels
- Logs written to `%TEMP%\WindowsOptimizer_Debug.log`
- Tracks:
  - Navigation between pages
  - Category expansion/collapse
  - Tweak detection and apply operations
  - Elevated host connection attempts
  - Errors and exceptions with stack traces

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml.cs`
- `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClient.cs`

**Status:** ✅ **IMPLEMENTED** - Detailed logging available for debugging

---

### 6. Monitor Network/Disk Monitoring Fallback Improvements (Commits: `b047e4c`, `c2e8520`)
**Problem:** Network adapters and disk activity could appear empty (or stuck at 0) when performance counters are unavailable or when instance mapping fails.

**Solution:**
- `NetworkMonitor` now falls back to delta-based throughput using `System.Net.NetworkInformation` totals when performance counters are unavailable/unmatched
- `DiskMonitor` uses `LogicalDisk` performance counters (drive-letter instances) for more reliable per-drive I/O rates
- Both monitors avoid returning an empty list solely because performance counters are unavailable

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`
- `WindowsOptimizer.Infrastructure/Metrics/DiskMonitor.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs verification on Windows 10/11 native

---

### 7. Dashboard Health - Measured (Detected) Scoring (Commit: `7c66e13`)
**Problem:** Dashboard health score could show `0%` even on a healthy system because no tweaks were detected yet.

**Root Cause:**
- Health was computed from all scorable tweaks, but most tweaks start in an `Unknown` state until Detect runs.

**Solution:**
- Health score is now computed from *detected* scorable tweaks only.
- Dashboard shows `—` until at least one tweak has a detected state.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`
- `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/Views/DashboardView.xaml`

**Status:** ✅ **IMPLEMENTED**

---

### 8. Tweaks UI - Hover Animation Stability + Tooltips (Commit: `fc21306`)
**Problem:** Tweaks page could intermittently crash with animation errors like:
- `Cannot animate '(0).(1)' on an immutable object instance.`
- `'BorderThickness' property does not point to a DependencyObject...`

**Root Cause:**
- Animations targeting **Freezable** objects created in templates (e.g., `DropShadowEffect`, `SolidColorBrush`) can hit frozen/shared instances and throw on the UI thread.

**Solution:**
- Removed risky Freezable animations (shadow blur/depth/color) on Tweaks cards and headers.
- Kept hover feedback using safe animations only (overlay `Opacity`, `TranslateTransform`, `ScaleTransform` on named transforms).
- Added/kept short hover tooltips for batch actions (Detect/Preview/Apply/Verify/Rollback) and filters.

**Files Changed:**
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** ✅ **IMPLEMENTED** - Needs user verification on Windows 10/11

---

### 9. Tweaks Count Mismatch (Dashboard vs Tweaks) (Commits: `41839dc`, `5fe0757`, `6dbc736`)
**Problem:** Dashboard could show a higher tweak count than the Tweaks page (e.g., 313 vs 224).

**Root Cause:**
- Provider/plugin tweaks were added after the initial category/group build, so the UI tree and summary were not rebuilt.

**Solution:**
- Rebuild filter summary and category groups after providers/plugins load.
- De-duplicate provider/plugin tweaks by ID.
- Make Visibility category dense layout persistent during rebuilds.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`

**Status:** ✅ **IMPLEMENTED** - Needs user verification on Windows

---

### 10. Scheduled Tasks Batch - Detect Robustness (Commit: `5fe0757`)
**Problem:** Some environments don't have every scheduled task listed; Detect could fail on missing tasks instead of treating them as "not present".

**Solution:**
- Treat task "not found" errors as missing tasks (non-fatal) and continue.
- Added unit tests covering not-found detection and apply/verify behavior.

**Files Changed:**
- `WindowsOptimizer.Engine/Tweaks/ScheduledTaskBatchTweak.cs`
- `WindowsOptimizer.Tests/ScheduledTaskBatchTweakTests.cs`

**Status:** ✅ **IMPLEMENTED** - Needs user verification on Windows

---

### 11. Monitor Disk I/O Process List + Fixed CPU/RAM Axis (Commit: TBD)
**Problem:** Monitor lacked disk I/O per-process visibility, and CPU/RAM charts scaled to peak values (e.g., 68%) instead of a fixed 100% axis, confusing users.

**Solution:**
- Added Top 10 processes by disk I/O (read + write bytes) with MB/s display.
- CPU/RAM chart axes now use a fixed 0-100% scale while keeping Peak/Low/Now for accuracy.

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Status:** ✅ **IMPLEMENTED**

---

### 12. Tweak Documentation Coverage Audit (Commit: TBD)
**Problem:** Several tweaks had no direct documentation of the underlying registry/value paths in the category docs, making it hard to validate whether a tweak was legitimate.

**Solution:**
- Added `scripts/audit_tweak_sources.py` to scan tweak definitions and confirm registry/service tokens exist in the relevant docs folder.
- Inserted small "App Coverage Notes" sections into category docs so app-only registry values are explicitly documented.
- Generated audit output in `Docs/tweaks/tweak-source-audit.md` + `.csv` (expected Missing documentation: 0 after updates).

**Files Changed:**
- `scripts/audit_tweak_sources.py`
- `Docs/peripheral/peripheral.md`
- `Docs/misc/misc.md`
- `Docs/notifications/notifications.md`
- `Docs/performance/performance.md`
- `Docs/privacy/privacy.md`
- `Docs/security/security.md`
- `Docs/system/system.md`

**Status:** ✅ **IMPLEMENTED** - Re-run audit after adding new tweaks

---

### 12. Batch Tweak Breakdown in Technical Info (Commit: TBD)
**Problem:** Mixed state was hard to interpret because batch tweaks only showed a summary without per-item status.

**Solution:**
- Parse Detect messages for `Services:` and `Tasks:` sections.
- Surface per-item breakdown in the Technical Info tab for quick inspection.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** ✅ **IMPLEMENTED**

---

### 13. Registry Batch Breakdown + Compact Summary (Commit: TBD)
**Problem:** Registry batch tweaks did not expose per-entry status, and collapsed cards lacked a quick "matched/missing" hint.

**Solution:**
- Registry batch detect messages now include per-entry details with current → target values.
- Collapsed tweak cards show a compact "matched / missing" summary when batch details are available.

**Files Changed:**
- `WindowsOptimizer.Engine/Tweaks/RegistryValueBatchTweak.cs`
- `WindowsOptimizer.Engine/Tweaks/RegistryValueSetTweak.cs`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** ✅ **IMPLEMENTED**

---

### 14. Non-Essential Services List Expansion (Commit: TBD)
**Problem:** The "Disable Non-Essential Services" tweak mentioned Print Spooler/Bluetooth but did not include those services, leading to confusing Mixed states.

**Solution:**
- Added Bluetooth and print-related service names (including per-user patterns) to the batch list.

**Files Changed:**
- `WindowsOptimizer.App/Services/TweakProviders/SystemTweakProvider.cs`

**Status:** ✅ **IMPLEMENTED**

---

### 15. ElevatedHost Discovery + Missing Host Warning (Commits: `46abd80`, `969700f`)
**Problem:** When running via `dotnet run`, the app could fail to find the ElevatedHost executable and all admin-required tweaks would fail.

**Solution:**
- ElevatedHost path discovery now checks common publish layouts, RID folders, and dev bin outputs.
- Env var override supported: `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH`.
- Tweaks page shows a clear warning banner if the host executable is missing.

**Files Changed:**
- `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`
- `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClient.cs`

**Status:** ✅ **IMPLEMENTED** - Packaging still needs verification

---

### 16. Legacy Tweak Catalog Restore + Metadata Ordering (Commit: pending)
**Problem:** Total tweak count dropped after provider refactor; several tweaks disappeared from the UI.

**Solution:**
- Added `LegacyTweakProvider` to restore the original 224-tweak catalog.
- Providers load first; legacy provider fills missing IDs (deduped).
- Tweak metadata enrichment (registry paths, references, sub-options) now runs **after** all tweaks/plugins load.
- Theme switching fix: Styles now use `DynamicResource` for theme-bound brushes.

**Files Changed:**
- `WindowsOptimizer.App/Services/TweakProviders/LegacyTweakProvider.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`
- `WindowsOptimizer.App/Resources/Styles.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification (tweak count + theme switch)

---

### 17. Startup Theme Flicker + Splash Render (Commit: `244f99d`)
**Problem:** Splash could appear in dark theme then switch to light, and scanning could stall first render.

**Solution:**
- Apply theme before any window shows.
- Yield to render before starting startup scan.
- Keep splash visible while scan runs.

**Files Changed:**
- `WindowsOptimizer.App/App.xaml.cs`
- `WindowsOptimizer.App/StartupWindow.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 18. Detect Runs Off UI Thread (Commit: `244f99d`)
**Problem:** Detect could stall the UI when many tweaks are scanned.

**Solution:**
- `DetectStatusAsync` executes pipeline work on a background task.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 19. Monitor Card Reveal Animation + Source Links (Commits: `4b4a451`, `ce6def0`)
**Problem:** Monitor visuals felt static and tweak sources were hard to find.

**Solution:**
- Monitor cards now animate with safe transform-based reveal + hover.
- Tweak docs linker reads `tweak-catalog.csv` and adds `Source file` links.

**Files Changed:**
- `WindowsOptimizer.App/Views/MonitorView.xaml`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 16. Startup Scan Progress Wiring (Commit: pending)
**Problem:** Splash didn’t convey scan progress; user perceived freezes.

**Solution:**
- Splash updates with `Scanning tweaks X/Y` and current tweak name.
- Startup scan pipeline accepts progress + cancellation.

**Files Changed:**
- `WindowsOptimizer.App/StartupWindow.xaml`
- `WindowsOptimizer.App/StartupWindow.xaml.cs`
- `WindowsOptimizer.App/ViewModels/StartupScanProgress.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 17. Tweak Card Compact Summary (Commit: pending)
**Problem:** Collapsed tweak cards did not surface `Current → Target` + impact area.

**Solution:**
- Added an impact area badge (Registry/Service/Task/etc.).
- One-line `Current → Target` summary shown on the card.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 18. Tweak Catalog Changes/Risk Columns (Commit: pending)
**Problem:** Docs did not provide per‑tweak “what changes” and risk at a glance.

**Solution:**
- Catalog generator now extracts description + risk for each tweak.
- CSV/MD/HTML catalogs include `Changes` and `Risk` columns.
- Docs linker now reads CSV headers instead of fixed indexes.

**Files Changed:**
- `scripts/generate_tweak_catalog.py`
- `Docs/tweaks/tweak-catalog.md`
- `Docs/tweaks/tweak-catalog.csv`
- `Docs/tweaks/tweak-catalog.html`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`
- `Docs/tweaks/tweaks.md`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 19. Category Docs Tweak Index Anchors (Commit: pending)
**Problem:** Category docs were long and lacked per‑tweak anchors.

**Solution:**
- Generator now injects a `Tweak Index (Generated)` section per doc.
- Anchors use tweak IDs so UI links can jump directly.

**Files Changed:**
- `scripts/generate_tweak_catalog.py`
- `Docs/*/*.md`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

### 13. Light Theme Parity Across Main Views (Commit: pending)
**Problem:** Light theme still showed dark surfaces in several views (hard-coded colors).

**Solution:**
- Replaced hard-coded colors with theme-aware `DynamicResource` brushes.
- Added caution/danger surface brushes and terminal surface brush.
- Added chart gradient color tokens for Monitor charts.

**Files Changed:**
- `WindowsOptimizer.App/MainWindow.xaml`
- `WindowsOptimizer.App/Views/DashboardView.xaml`
- `WindowsOptimizer.App/Views/TweaksView.xaml`
- `WindowsOptimizer.App/Views/MonitorView.xaml`
- `WindowsOptimizer.App/Resources/Colors.xaml`
- `WindowsOptimizer.App/Resources/Colors.Light.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification (Light theme)

---

### 14. Tweak Docs Anchor Links (Commit: pending)
**Problem:** Tweak docs links opened large markdown files at the top; users had to search manually.

**Solution:**
- Generated HTML catalog with per-tweak anchors.
- Tweaks now include a direct “Catalog entry” link pointing to the anchor.

**Files Changed:**
- `scripts/generate_tweak_catalog.py`
- `Docs/tweaks/tweak-catalog.html`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

**Status:** ✅ **IMPLEMENTED** - Needs Windows verification

---

### 15. Startup Scan + RolledBack Filter Fix (Commit: pending)
**Problem:** Users had to manually scan after launch; rolled-back filter was unreliable.

**Solution:**
- Auto Detect on app startup with a blocking overlay.
- Rolled-back filter now uses explicit `WasRolledBack` state.
- Detect-all preserves category expansion state.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/MainWindow.xaml`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`

**Status:** ✅ **IMPLEMENTED** - Needs Windows verification

---

### 16. Monitor Network/Disk Header Stats (Commit: pending)
**Problem:** Network/Disk charts lacked quick context for current vs peak/low values.

**Solution:**
- Added live Now/Peak/Low metrics in Network I/O and Disk I/O chart headers (download/upload + read/write).
- Tightened card padding/margins to keep the Monitor layout denser without losing readability.
- Network/Disk lines now share a common scale and show current-value dots for easier comparison.
- Added scale hints for Network/Disk charts (peak value label).
- Added left-axis labels for Network/Disk charts (max/mid/0).
- Added dynamic axis labels for CPU/RAM charts (max/75/50/25/0).
- Increased chart contrast (area fills + glow + gridlines).
- Modernized Monitor header toolbar with Live pill and icon Save button.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 17. Tweak Status Badge Clarity (Commit: pending)
**Problem:** Status icons were ambiguous for Applied/Mixed/Error states.

**Solution:**
- Added a status text badge next to tweak names in both compact and expanded cards.
- Added Mixed status detection (based on current value) with its own icon/color.
- Status tooltips now reflect Mixed state.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 18. Monitor Top Processes Compaction (Commit: pending)
**Problem:** Top process lists were visually heavy and hard to scan.

**Solution:**
- Added column headers for CPU/RAM/IO/PID.
- Switched action buttons to compact icon buttons with tooltips.
- Reduced row padding for denser layout.

**Files Changed:**
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 19. Monitor Per-Process Network (ETW + TCP EStats) (Commit: pending)
**Problem:** Network process list used total I/O bytes (disk + network).

**Solution:**
- Added ETW sampling (TCP + UDP) per PID when available.
- Falls back to TCP EStats (IPv4/IPv6), then total process I/O if unavailable.
- UI now switches title/description based on measurement mode.

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`
- `WindowsOptimizer.Infrastructure/Metrics/NetworkEtwSampler.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification

---

### 20. Monitor Latency + Storage Health Improvements (Commit: pending)
**Problem:** Single-target latency wasn't representative, and storage health often reported N/A for NVMe.

**Solution:**
- Latency card now measures gateway + Cloudflare (1.1.1.1) + Google (8.8.8.8).
- Disk health now uses WMI storage reliability counters with MSFT_PhysicalDisk fallback.
- CPU fan detection now walks sub-hardware sensors for better coverage.
- CSV export includes the new latency targets.

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification (NVMe + OEM drivers)

---

### 21. Windows Packaging Script (Commit: pending)
**Problem:** Sharing builds with testers required manual publish steps.

**Solution:**
- Added `scripts/package_windows.cmd` + `scripts/package_windows.ps1` to build a self-contained zip.
- Ensures `Docs/` and `ElevatedHost/` are copied into the publish output.

**Files Changed:**
- `scripts/package_windows.cmd`
- `scripts/package_windows.ps1`

**Status:** ✅ **IMPLEMENTED**

---

### 22. Monitor Sensor Coverage + Scroll Smoothing (Commit: pending)
**Problem:** Some systems report missing CPU temp/fan, and disk health shows N/A. Scrolling could feel laggy.

**Solution:**
- CPU temp now scans CPU + motherboard + SuperIO sensors and falls back to WMI thermal zones.
- CPU fan now includes SuperIO sensors, and SMART/health checks now use multiple WMI fallbacks.
- Tweaks list cards are bitmap cached to reduce scroll jank.

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/TweaksView.xaml`

**Status:** 🧪 **IMPLEMENTED** - Needs Windows verification (varied hardware)

---

### 23. Monitor Disk Health List + Sensor Diagnostics Export + Optional Shadows (Commit: pending)
**Problem:** Disk health and fan sensors still report `N/A` on some hardware, and heavy shadow effects contributed to scroll jank.

**Solution:**
- Monitor now shows **per-physical-disk** health rows with SMART/WMI matching and source labels.
- Added **Sensor Diagnostics** export to dump LHM + WMI storage data for hardware debugging.
- Added **Card Shadows** toggle in Settings (default OFF) to reduce GPU load during scrolling.

**Files Changed:**
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.App/Views/MonitorView.xaml`
- `WindowsOptimizer.App/Views/TweaksView.xaml`
- `WindowsOptimizer.App/Views/DashboardView.xaml`
- `WindowsOptimizer.App/Resources/Styles.xaml`
- `WindowsOptimizer.App/ViewModels/SettingsViewModel.cs`
- `WindowsOptimizer.Infrastructure/AppSettings.cs`
- `WindowsOptimizer.App/Services/UiPreferences.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs hardware verification on SATA/NVMe systems

---

### 20. Monitor Collections + CPU/GPU Metrics Reliability (Commit: `1a16a95`)
**Problem:** Startup Apps / Services / Processes lists sometimes showed 0-1 items with errors like
`Cannot change or check the contents or Current position of CollectionView while Refresh is being deferred.`.
CPU speed and GPU memory totals were also inaccurate on some systems.

**Root Cause:**
- UI updates were applied while the CollectionView was in a deferred refresh state, leading to re-entrancy issues.
- CPU speed fell back to `Win32_Processor.CurrentClockSpeed` (often base speed).
- GPU total memory fell back to `Win32_VideoController.AdapterRAM` (can cap at 4 GB).

**Solution:**
- Dispatch list updates and selection updates at `DispatcherPriority.ContextIdle`.
- Simplified UpdateCollection logic and added retry on `InvalidOperationException` / `NotSupportedException`.
- Ensure loading flags are set on the UI thread.
- CPU speed now reads `Win32_PerfFormattedData_Counters_ProcessorInformation.ProcessorFrequency` first.
- GPU total memory now uses `GPU Adapter Memory` perf counters when available.
- DirectX registry value is trimmed before mapping.

**Files Changed:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`

**Status:** 🧪 **IMPLEMENTED** - Needs verification on Windows 10/11

---

## 🐛 Known Issues

### 1. **Monitor Page - Empty Network Adapters and Disk Activity**
**Severity:** Medium
**Status:** 🧪 **NEEDS TESTING**

**Description:**
- Network Adapters and/or Disk Activity may show empty (or show 0 I/O) on some environments
- Other monitoring metrics (CPU, RAM, Processes) work correctly
- Startup Apps / Services / Processes lists may show 0-1 entries after tab switches or refreshes
- Status banner may show: `CollectionView ... Refresh is being deferred`

**Possible Causes:**
- Performance counter availability / mapping differences across environments
- Platform limitations (WSL2/Linux)
- UI updates applied while CollectionView is deferring refresh (re-entrancy)

**Notes:**
- Fallbacks were added (see commits `b047e4c`, `c2e8520`) but still need broad validation on Windows 10/11.
- A UI update deferral fix was added in `1a16a95` (ContextIdle updates + retry), still needs verification on native Windows.

**Files Affected:**
- `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`
- `WindowsOptimizer.Infrastructure/Metrics/DiskMonitor.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

**Priority:** Medium - affects monitoring functionality but doesn't crash

---

### 2. **Disk Health / Fan Sensors Missing on Some Hardware**
**Severity:** Medium
**Status:** 🧪 **NEEDS HARDWARE VERIFICATION**

**Description:**
- Some NVMe/SATA devices still report `N/A` for disk health (SMART/WMI coverage varies by vendor).
- CPU fan RPM may be missing on boards without exposed SuperIO sensors.
- GPU fan RPM, driver version, and memory totals can be `N/A` depending on vendor/WMI coverage.
- CPU current speed can still show base speed if perf counters or WMI perf classes are unavailable.

**Notes:**
- Use the **Sensor Diagnostics** export (Monitor header) to capture LHM + WMI data.
- Check if LHM reports storage sensors and if WMI storage classes are accessible.
- New CPU/GPU fallbacks were added in `1a16a95` (perf WMI + GPU adapter memory counters) and need hardware validation.

**Files Affected:**
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

**Priority:** Medium - visible in UI but does not crash

---

### 3. **Power Tweaks Not Functional on WSL2/Linux**
**Severity:** High (if running on WSL2), Low (if running on Windows)
**Status:** ✅ **BY DESIGN**

**Description:**
- All Power category tweaks fail immediately on non-Windows platforms
- Error: "Elevated operations are only supported on Windows"

**Root Cause:**
- Power tweaks require Windows-specific features:
  - Windows Registry (`HKEY_LOCAL_MACHINE`)
  - Elevated Host process (Windows executable)
  - UAC elevation (Windows-only)

**Solution:**
- This is **expected behavior** when running on WSL2/Linux
- The application must be run on **native Windows** for full functionality
- Platform detection prevents hanging and provides clear error messages

**Recommendation:** Run on Windows 10/11 for full tweak support

**Priority:** Low - working as designed

---

### 4. **Elevated Host Launch on First Tweak Apply**
**Severity:** Low
**Status:** ⚠️ **KNOWN BEHAVIOR**

**Description:**
- First time applying a tweak that requires elevation, a UAC prompt appears
- User must accept the UAC prompt within ~30 seconds
- If UAC is cancelled or timed out, the tweak fails

**Expected Behavior:**
- UAC prompt is **normal and required** for system-level tweaks
- Startup connect timeout prevents indefinite hangs
- Subsequent tweaks use the same elevated host (no additional UAC prompts)

**Workaround:** Accept UAC prompt quickly when it appears

**Priority:** Low - expected Windows security behavior

---

### 5. **Legacy Provider Migration Needed**
**Severity:** Medium
**Status:** ⚠️ **PENDING**

**Description:**
- `LegacyTweakProvider` restores missing tweaks but duplicates the refactored provider catalog.
- Long-term plan: migrate remaining tweaks into category providers and remove legacy provider.

**Recommendation:**
- Audit provider parity vs legacy list.
- Move remaining tweaks into category providers.
- Remove legacy provider once parity is verified.

---

## ⚠️ Potential Issues

### 1. **Memory Leak in Monitoring Services**
**Severity:** Medium
**Status:** 🔍 **NEEDS TESTING**

**Description:**
- DispatcherTimer runs every 1 second in MonitorViewModel
- Multiple PerformanceCounter instances created
- Potential memory leak if counters/monitors are not disposed properly

**Recommendation:**
- Long-running stress test (leave Monitor page open for 1+ hour)
- Monitor memory usage in Task Manager
- Ensure all IDisposable resources are properly disposed

**Files to Review:**
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- `WindowsOptimizer.Infrastructure/Metrics/*.cs`

---

### 2. **Race Condition in Category Detection**
**Severity:** Low
**Status:** ⚠️ **POSSIBLE**

**Description:**
- User could rapidly open/close categories before detection completes
- Multiple detection operations could run simultaneously for the same tweak
- 5-second timeout mitigates but doesn't eliminate the issue

**Potential Fix:**
- Add cancellation token to `DetectAllTweaksAsync()`
- Cancel pending detection when category is collapsed
- Add `_isDetecting` flag to prevent concurrent detection

**Files Affected:**
- `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs`

---

### 3. **ElevatedHost.exe Missing After Build**
**Severity:** High
**Status:** ⚠️ **BUILD CONFIGURATION**

**Description:**
- ElevatedHost must be discoverable for elevation-required tweaks.
- If missing, admin-required tweaks will fail (the Tweaks page now shows a warning banner with the expected path).
- Build/publish configuration might still fail to copy the ElevatedHost into the app output directory.

**Recommendation:**
- Verify ElevatedHost exists in the publish output (`ElevatedHost/WindowsOptimizer.ElevatedHost.exe`)
- Add a post-build/publish copy step if missing (and/or CI check)
- Consider a startup health check in addition to the Tweaks page banner

**Files to Review:**
- `WindowsOptimizer.App/WindowsOptimizer.App.csproj`
- Build scripts

---

### 4. **Startup Scan Still Feels Blocking**
**Severity:** Medium
**Status:** 🧪 **NEEDS VERIFICATION**

**Description:**
- Scan runs before MainWindow shows (by design).
- If scan is heavy, splash may still feel “frozen”.

**Recommendation:**
- Verify on Windows 10/11 with full tweak catalog.
- If still slow: move DetectAllTweaksAsync to background with progress + cancel.

**Files to Review:**
- `WindowsOptimizer.App/App.xaml.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`

---

### 5. **Docs Linking: Template IDs**
**Severity:** Low
**Status:** ⚠️ **POSSIBLE**

**Description:**
- Some catalog entries use templated IDs (e.g., `cleanup.eventlog-{logName}`).
- UI links may not match runtime IDs if templates aren’t expanded in CSV.

**Recommendation:**
- Ensure catalog generator expands template IDs or add runtime mapping.

**Files to Review:**
- `Docs/tweaks/tweak-catalog.csv`
- `scripts/generate_tweak_catalog.py`
- `WindowsOptimizer.App/Services/TweakDocumentationLinker.cs`

---

### 6. **Monitor Animations Regression Risk**
**Severity:** Low
**Status:** 🧪 **NEEDS VERIFICATION**

**Description:**
- New reveal/hover animations must avoid Freezable animation errors.
- WPF can throw when animating shared/freezed objects.

**Recommendation:**
- Verify on Windows (dark/light themes).
- Keep transforms `x:Shared="False"` and avoid animating brushes/effects.

**Files to Review:**
- `WindowsOptimizer.App/Views/MonitorView.xaml`

---

### 4. **Tweak State Persistence After Crash**
**Severity:** Medium
**Status:** 🔍 **NEEDS TESTING**

**Description:**
- If application crashes during tweak apply, the original state might not be saved
- Rollback functionality depends on `_hasDetected` flag and stored `_detectedValue`
- Crash could leave system in inconsistent state

**Recommendation:**
- Implement durable tweak log (SQLite database or JSON file)
- Store original values **before** applying changes
- Add recovery mechanism on application startup

**Files Affected:**
- `WindowsOptimizer.Engine/Tweaks/RegistryValueTweak.cs`
- `WindowsOptimizer.Infrastructure/Logging/TweakLogStore.cs`

---

## 🖥️ Platform Compatibility

### Windows 10/11 (Native)
- ✅ Full functionality
- ✅ All monitoring features
- ✅ All tweak categories
- ✅ Elevated Host support
- ✅ Registry operations

### WSL2 (Windows Subsystem for Linux)
- ⚠️ **Limited functionality**
- ✅ Application launches
- ✅ UI works
- ✅ CurrentUser registry tweaks (might work)
- ❌ LocalMachine registry tweaks (fail immediately)
- ❌ Power tweaks (not supported)
- ❌ Network/Disk monitoring (empty)
- ❌ Elevated Host (Windows executable)

### Linux (Native)
- ❌ **Not supported**
- The application is WPF-based and requires Windows
- Might run under Wine but not recommended

**Recommendation:** Use **Windows 10/11 native** for full functionality

---

## 📝 Todo & Roadmap

> **Note:** A comprehensive development roadmap (v2.1) has been approved and documented in [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md). The roadmap covers major architectural improvements including Single Instance enforcement, Multi-threaded architecture, Hardware Database, Monitor View redesign, and Process Management enhancements.

### High Priority (Critical for Stability)

- [ ] **Fix Network Adapter Monitoring**
  - Investigate why `NetworkMonitor` returns empty adapters
  - Check performance counter instance names
  - Add fallback using `System.Net.NetworkInformation`

- [ ] **Fix Disk Activity Monitoring**
  - Investigate why `DiskMonitor` returns empty disks
  - Check performance counter availability
  - Add fallback using WMI or P/Invoke

- [x] **Warn when ElevatedHost is missing**
  - Tweaks page shows a warning banner when the host executable isn't found
  - Remaining work: ensure publish output copies `ElevatedHost/WindowsOptimizer.ElevatedHost.exe` reliably (CI/build step)

- [ ] **Implement Tweak State Persistence**
  - Save original values to durable storage before applying
  - Add recovery mechanism on startup
  - Prevent inconsistent system state after crashes

### Medium Priority (Improvements)

- [ ] **Add Cancellation to Category Detection**
  - Cancel pending detection when category collapses
  - Prevent race conditions
  - Improve responsiveness

- [ ] **Optimize Monitoring Performance**
  - Reduce PerformanceCounter overhead
  - Implement caching for static data
  - Profile memory usage over time

- [ ] **Add Retry Logic to Elevated Host Connection**
  - Retry failed connections with exponential backoff
  - Better handling of transient failures
  - User-friendly error messages

- [ ] **Improve Logging**
  - Structured logging (JSON format)
  - Log rotation (prevent huge log files)
  - Configurable log levels (Info, Debug, Error)

### Low Priority (Nice to Have)

- [ ] **Platform Detection at Startup**
  - Show warning if running on WSL2/Linux
  - Inform user about limited functionality
  - Suggest running on native Windows

- [ ] **Add Unit Tests for Critical Paths**
  - TweakExecutionPipeline timeout logic
  - Platform detection
  - Registry operations

- [ ] **Telemetry & Crash Reporting**
  - Anonymous crash reporting (opt-in)
  - Help identify common issues
  - Improve stability over time

---

## ✅ Testing Checklist

### Before Release

#### UI Theme
- [ ] Light/Dark theme swap updates Dashboard, Tweaks, Monitor, MainWindow

#### Monitor Page
- [ ] Monitor page loads without crashing
- [ ] CPU usage displays correctly
- [ ] RAM usage displays correctly
- [ ] Top processes list updates
- [ ] Processes tab shows Top 10 for CPU/RAM/Network/Disk (not empty)
- [ ] Temperature sensors work (if available)
- [ ] 60-second history graphs render
- [ ] Network adapters display (check empty issue)
- [ ] Disk activity displays (check empty issue)
- [ ] Startup Apps list populates and refreshes without CollectionView errors
- [ ] Services list populates and refreshes without CollectionView errors
- [ ] CPU current speed shows boosted value under load (not always base speed)
- [ ] GPU memory total + DirectX version display correctly
- [ ] GPU fan RPM appears when supported

#### Tweaks Page
- [ ] All categories expand/collapse without crashing
- [ ] Tweak detection completes within 5 seconds
- [ ] Detection shows correct current state (Applied/Not Applied)
- [ ] Apply operation works for CurrentUser tweaks
- [ ] Apply operation works for LocalMachine tweaks (requires elevation)
- [ ] UAC prompt appears for elevated tweaks
- [ ] Timeout occurs after 30 seconds if hung
- [ ] Rollback restores original state
- [ ] Batch operations work correctly
- [ ] Use `Docs/tweaks/tweak-catalog.md` to validate every tweak ID at least once

#### Platform-Specific
- [ ] Test on **Windows 10** (native)
- [ ] Test on **Windows 11** (native)
- [ ] Verify graceful degradation on WSL2
- [ ] Check error messages on non-Windows platforms

#### Performance
- [ ] Monitor memory usage over 1 hour
- [ ] Check for memory leaks in monitoring
- [ ] Verify CPU usage is reasonable (<10% idle)
- [ ] Test with 100+ rapid category expansions

#### Edge Cases
- [ ] What happens if ElevatedHost.exe is missing?
- [ ] What happens if UAC is cancelled?
- [ ] What happens if registry key doesn't exist?
- [ ] What happens if tweak hangs indefinitely?
- [ ] What happens if network is disconnected during operation?

---

## 📊 Statistics

Key recent commits (not exhaustive):
- `46abd80` ElevatedHost discovery for `dotnet run`
- `7c66e13` Dashboard health shows measured state
- `7a5ed0c` Tweaks link opening + compact search
- `fc21306` Tweaks crash fix (avoid Freezable animations)
- `969700f` Tweaks warning when ElevatedHost missing
- `1a16a95` Monitor collection update stability + CPU/GPU metric fallbacks

---

## 🔗 Related Documents

- [README.md](README.md) - Project overview and features
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md) - **NEW** Approved v2.1 roadmap for major features
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines
- [HANDOFF_REPORT.md](HANDOFF_REPORT.md) - Previous development handoff
- [Docs/tweaks/tweak-catalog.md](Docs/tweaks/tweak-catalog.md) - Generated tweak catalog with source/doc links
- [Docs/tweaks/tweak-catalog.html](Docs/tweaks/tweak-catalog.html) - Anchor-friendly catalog for per-tweak links
- [Docs/tweaks/tweak-test-template.csv](Docs/tweaks/tweak-test-template.csv) - Test checklist template for manual validation

---

## 📞 Support

If you encounter issues:

1. Check `%TEMP%\WindowsOptimizer_Debug.log` for detailed error messages
2. Ensure you're running on **Windows 10/11 native** (not WSL2)
3. Check if `WindowsOptimizer.ElevatedHost.exe` exists in the application directory
4. Verify UAC is enabled and you can accept elevation prompts
5. Open an issue on GitHub with:
   - OS version (`winver` output)
   - Error message from log file
   - Steps to reproduce
   - Screenshot if applicable

---

**Last Build:** Unverified in this environment (run `dotnet build` on Windows 10/11)
**Build Status:** ⚠️ Requires Windows verification
**Ready for Production:** ⚠️ Needs testing on Windows 10/11 native
