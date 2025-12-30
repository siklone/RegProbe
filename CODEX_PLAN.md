# Windows Optimizer - Comprehensive Development Plan

**Created:** December 28, 2025
**Purpose:** Complete roadmap for Codex/AI agents to continue development
**Branch:** `main`

---

## Quick Reference

| Document | Purpose |
|----------|---------|
| `CLAUDE.md` | Quick commands, guardrails, operational notes |
| `AGENTS.md` | Non-negotiable safety/architecture rules |
| `HANDOFF_REPORT.md` | Recent changes + incomplete work |
| `DEVELOPMENT_STATUS.md` | Known issues, fixes, testing checklist |
| `ARCHITECTURE.md` | System design, data flow, patterns |
| `ECOSYSTEM_FOUNDATIONS.md` | Future roadmap (cloud, plugins, scripts) |

---

## Current State Summary

### What's Working
- Monitor page (CPU, RAM, Processes, Temperature)
- Tweaks page (categories, detection, apply, rollback)
- Dashboard with health score (detected tweaks only)
- ElevatedHost discovery with fallbacks
- Platform detection (WSL2/Linux graceful degradation)
- Comprehensive debug logging (`%TEMP%\WindowsOptimizer_Debug.log`)
- Timeout protection (5s detection, 30s apply)
- Animation crash fix (no Freezable animations)
- **Phase 8: TweaksViewModel Refactoring (4500→1300 lines)**
- **10 TweakProvider implementations (modular architecture)**
- **Plugin SDK with HelloWorld example**

### What's NOT Working / Incomplete
- Network Adapters monitoring (may show empty)
- Disk Activity monitoring (may show empty)
- ElevatedHost packaging (needs CI/build step)
- CPU temp often shows N/A
- WiX MSI Installer (not started)

---

## Priority Matrix

### CRITICAL (Must Fix Before Release)

| Task | Files | Effort | Notes |
|------|-------|--------|-------|
| ElevatedHost packaging verification | `WindowsOptimizer.App.csproj`, build scripts | Medium | Add MSBuild/post-publish copy step |
| Windows 10/11 native testing | - | High | All features need verification on actual Windows |
| Durable rollback state | `TweakExecutionPipeline.cs`, `RegistryValueTweak.cs` | High | JSON/SQLite before Apply |

### HIGH (Important for UX)

| Task | Files | Effort | Notes |
|------|-------|--------|-------|
| Network monitoring fallback | `NetworkMonitor.cs` | Medium | Delta-based throughput implemented, needs testing |
| Disk monitoring fallback | `DiskMonitor.cs` | Medium | LogicalDisk counters added, needs testing |
| Monitor chart improvements | `MonitorView.xaml`, `MonitorViewModel.cs` | Medium | Add gridlines, min/max labels |
| CPU temp tooltip | `MonitorViewModel.cs` | Low | Explain sensor availability |

### MEDIUM (Stability Improvements)

| Task | Files | Effort | Notes |
|------|-------|--------|-------|
| Category detection cancellation | `CategoryGroupViewModel.cs` | Medium | Cancel pending detection on collapse |
| Memory leak testing | `MonitorViewModel.cs`, `*Monitor.cs` | Medium | 1hr stress test needed |
| Log rotation | `ElevatedHostClient.cs`, others with logging | Low | Prevent huge log files |
| Structured logging | Multiple files | Medium | JSON format with levels |

### LOW (Nice to Have)

| Task | Files | Effort | Notes |
|------|-------|--------|-------|
| Platform detection startup warning | `MainViewModel.cs` | Low | Show WSL2 warning at startup |
| Unit tests for timeout logic | `TweakExecutionPipeline.cs` | Medium | Critical path coverage |
| Retry logic for ElevatedHost | `ElevatedHostClient.cs` | Medium | Exponential backoff |

---

## Detailed Implementation Plan

### Phase 1: Stabilization (Immediate)

#### 1.1 ElevatedHost Packaging
**Goal:** Ensure ElevatedHost.exe is always present in publish output

**Steps:**
1. Add post-build target in `WindowsOptimizer.App.csproj`:
```xml
<Target Name="CopyElevatedHost" AfterTargets="Build">
  <Copy SourceFiles="$(SolutionDir)WindowsOptimizer.ElevatedHost\bin\$(Configuration)\$(TargetFramework)\$(RuntimeIdentifier)\WindowsOptimizer.ElevatedHost.exe"
        DestinationFolder="$(OutputPath)ElevatedHost\" />
</Target>
```
2. Add CI check to verify file exists after publish
3. Update README with packaging instructions

**Files to Modify:**
- `WindowsOptimizer.App/WindowsOptimizer.App.csproj`
- `.github/workflows/build.yml` (if exists)

---

#### 1.2 Durable Rollback State
**Goal:** Persist original values before Apply for recovery after crash/restart

**Design:**
```
%AppData%\WindowsOptimizer\rollback-state.json
{
  "tweaks": [
    {
      "id": "privacy.disable-telemetry",
      "originalValue": 1,
      "appliedAt": "2025-12-28T10:00:00Z",
      "registryPath": "HKLM\\Software\\..."
    }
  ]
}
```

**Steps:**
1. Create `RollbackStateStore` class in Infrastructure
2. Before Apply, save original value from Detect result
3. On app startup, check for pending rollback states
4. Show recovery dialog if found

**Files to Create/Modify:**
- `WindowsOptimizer.Infrastructure/Logging/RollbackStateStore.cs` (new)
- `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`
- `WindowsOptimizer.Engine/Tweaks/RegistryValueTweak.cs`
- `WindowsOptimizer.App/ViewModels/MainViewModel.cs`

---

#### 1.3 Monitor Chart Improvements
**Goal:** Make CPU/RAM history charts readable

**Improvements:**
- Add horizontal gridlines (25%, 50%, 75%)
- Show min/max values as labels
- Add Y-axis scale indicator
- Tooltip on hover with exact values

**Files to Modify:**
- `WindowsOptimizer.App/Views/MonitorView.xaml`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

---

### Phase 2: Testing & Verification (Windows Native)

#### 2.1 Windows 10 Testing Checklist
- [ ] Clean install Windows 10 22H2
- [ ] Run application as non-admin
- [ ] Expand all tweak categories
- [ ] Apply one CurrentUser tweak (no UAC)
- [ ] Apply one LocalMachine tweak (UAC prompt)
- [ ] Verify rollback works
- [ ] Check Monitor page metrics accuracy
- [ ] Leave Monitor open for 1 hour (memory test)
- [ ] Export tweak log CSV
- [ ] Import/Export profile

#### 2.2 Windows 11 Testing Checklist
- Same as Windows 10
- Plus: Verify new Windows 11-specific tweaks

#### 2.3 Edge Case Testing
- [ ] Cancel UAC prompt → graceful failure
- [ ] Registry key doesn't exist → handled
- [ ] Network disconnected during operation → handled
- [ ] ElevatedHost.exe missing → warning banner shown
- [ ] 100+ rapid category expansions → no crash
- [ ] Apply during Detect → handled (queue or block)

---

### Phase 3: Performance Optimization

#### 3.1 Memory Optimization
**Goal:** Prevent memory leaks in long-running monitoring

**Steps:**
1. Implement `IDisposable` on all monitors
2. Dispose PerformanceCounter instances properly
3. Use weak references for cached data
4. Profile with VS Diagnostic Tools

**Files to Review:**
- `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`
- `WindowsOptimizer.Infrastructure/Metrics/ProcessMonitor.cs`
- `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`
- `WindowsOptimizer.Infrastructure/Metrics/DiskMonitor.cs`
- `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

#### 3.2 Startup Performance
**Goal:** Reduce time to first UI render

**Optimizations:**
- Lazy-load tweak providers
- Background hardware discovery
- Cache hardware profile to disk
- Defer plugin loading

---

### Phase 4: Future Features (From ECOSYSTEM_FOUNDATIONS.md)

#### 4.1 Backend Services (Not Implemented)
- [ ] ASP.NET Core API for preset repository
- [ ] PostgreSQL/MySQL database
- [ ] JWT authentication
- [ ] WebSocket for remote management
- [ ] Web dashboard (React/Blazor)

#### 4.2 UI Enhancements
- [ ] Plugin marketplace page
- [ ] Telemetry opt-in/out settings
- [ ] Audit log viewer
- [ ] Script editor with syntax highlighting
- [ ] Dark/Light theme toggle

#### 4.3 Advanced Features
- [ ] WiX MSI installer
- [ ] Auto-update mechanism
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] i18n (multi-language support)

---

## Architecture Quick Reference

```
WindowsOptimizer.App (WPF UI)
    │
    ├── Views/*.xaml (XAML UI)
    ├── ViewModels/*.cs (MVVM logic)
    └── Services/TweakProviders/*.cs (tweak creation)

WindowsOptimizer.Engine (Business Logic)
    │
    ├── TweakExecutionPipeline.cs (orchestration)
    └── Tweaks/*.cs (concrete implementations)

WindowsOptimizer.Infrastructure (External Services)
    │
    ├── Elevation/ (ElevatedHost communication)
    ├── Metrics/ (CPU, RAM, Disk, Network monitors)
    ├── Registry/ (local + elevated registry access)
    └── Logging/ (tweak logs, debug logs)

WindowsOptimizer.Core (Contracts)
    │
    ├── ITweak.cs (Detect, Apply, Verify, Rollback)
    ├── ITweakProvider.cs (category-based providers)
    └── IRegistryAccessor.cs (registry abstraction)

WindowsOptimizer.ElevatedHost (Separate Process)
    │
    └── Program.cs (named pipe server, admin operations)
```

---

## Guardrails (DO NOT BREAK)

### Safety Rules
1. **SAFE tweaks MUST be reversible**: Detect → Apply → Verify → Rollback
2. **Default is Preview/DryRun**: No system changes until explicit Apply
3. **NO "disable Defender/Firewall/SmartScreen" under SAFE**
4. **Admin operations via ElevatedHost**: App is NOT always-admin
5. **Always log actions**: Logs MUST be exportable

### WPF Animation Rules
1. **DO NOT animate Freezables** created in templates/resources
2. **PREFER** animating named transforms (`TranslateTransform`, `ScaleTransform`)
3. **PREFER** overlay `Opacity` animations
4. If using shared resources for transforms, set `x:Shared="False"`

### Code Quality
1. Prefer small, composable services
2. Add unit tests for engine contracts
3. Use nullable reference types (`string?`)
4. Follow Microsoft C# coding conventions

---

## Common Commands

```powershell
# Build
dotnet build

# Run (normal)
dotnet run --project WindowsOptimizer.App

# Run with ElevatedHost override
$env:WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH="C:\path\to\WindowsOptimizer.ElevatedHost.exe"
dotnet run --project WindowsOptimizer.App

# Build Release
dotnet publish WindowsOptimizer.App -c Release -r win-x64 --self-contained

# Run Tests
dotnet test

# Check debug log
Get-Content "$env:TEMP\WindowsOptimizer_Debug.log" -Tail 100
```

---

## File Locations (Common Entry Points)

| Purpose | File |
|---------|------|
| Main entry | `WindowsOptimizer.App/App.xaml.cs` |
| Navigation | `WindowsOptimizer.App/ViewModels/MainViewModel.cs` |
| Tweaks UI | `WindowsOptimizer.App/Views/TweaksView.xaml` |
| Tweaks logic | `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` (4200+ lines) |
| Individual tweak VM | `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs` |
| Category expand | `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs` |
| Monitor UI | `WindowsOptimizer.App/Views/MonitorView.xaml` |
| Monitor logic | `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs` |
| Dashboard | `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs` |
| Pipeline | `WindowsOptimizer.Engine/TweakExecutionPipeline.cs` |
| Registry tweak | `WindowsOptimizer.Engine/Tweaks/RegistryValueTweak.cs` |
| ElevatedHost client | `WindowsOptimizer.Infrastructure/Elevation/ElevatedHostClient.cs` |
| ElevatedHost locator | `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs` |
| CPU/RAM metrics | `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs` |
| Network monitor | `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs` |
| Disk monitor | `WindowsOptimizer.Infrastructure/Metrics/DiskMonitor.cs` |

---

## Recent Commits (Context)

| Commit | Description |
|--------|-------------|
| `32ef7f2` | Platform detection + reduced timeout |
| `6323209` | DEVELOPMENT_STATUS.md documentation |
| `fc21306` | Tweaks animation crash fix |
| `969700f` | ElevatedHost missing warning |
| `7c66e13` | Dashboard health measured state |
| `46abd80` | ElevatedHost discovery improvements |
| `e81a462` | 30s timeout per tweak step |
| `1e302fa` | 5s timeout for category detection |

---

## Suggested PR Order

1. **ElevatedHost packaging** - Ensure build/publish copies it reliably
2. **Monitor charts** - Add gridlines, labels, CPU temp tooltip
3. **Durable rollback** - JSON state persistence before Apply
4. **Category detection cancellation** - Cancel on collapse
5. **Windows 10/11 native testing** - Verify all features
6. **Memory optimization** - Profile and fix leaks
7. **Unit tests** - Critical path coverage

---

## Questions for Product Owner

1. **Rollback persistence**: SQLite or JSON file?
2. **Telemetry**: Opt-in by default? What to collect?
3. **License**: MIT or other?
4. **Target audience**: Power users or enterprise?
5. **Plugin security**: Require code signing?
6. **Localization**: Which languages first?

---

## Notes for Next Agent

1. **Start by reading**: `HANDOFF_REPORT.md` first, then `DEVELOPMENT_STATUS.md`
2. **Build and test on Windows**: Not WSL2 - full functionality requires Windows native
3. **Check debug log**: `%TEMP%\WindowsOptimizer_Debug.log` has detailed traces
4. **Don't break guardrails**: Safety rules are non-negotiable
5. **Keep docs updated**: Update HANDOFF_REPORT.md with your changes
6. **Push frequently**: Commit working changes often

---

**Last Updated:** December 28, 2025
**Status:** Ready for Windows 10/11 native testing
**Next Priority:** ElevatedHost packaging verification
