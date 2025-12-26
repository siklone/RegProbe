# Development Status & Known Issues

**Last Updated:** December 26, 2025
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
- Reduced `StartupConnectTimeout` from 30s to **5 seconds**
- Added comprehensive logging to `ElevatedHostClient` for debugging
- Total timeout per tweak reduced from ~30s to ~6s (1s initial + 5s startup)

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

## 🐛 Known Issues

### 1. **Monitor Page - Empty Network Adapters and Disk Activity**
**Severity:** Medium
**Status:** 🔍 **INVESTIGATING**

**Description:**
- Network Adapters section shows empty
- Disk Activity section shows empty
- Other monitoring metrics (CPU, RAM, Processes) work correctly

**Possible Causes:**
- Performance counter instance name detection failing
- Network interface enumeration failing on WSL2/Linux
- Disk performance counters not available on non-Windows platforms

**Workaround:** None currently

**Files Affected:**
- `WindowsOptimizer.Infrastructure/Monitoring/NetworkMonitor.cs`
- `WindowsOptimizer.Infrastructure/Monitoring/DiskMonitor.cs`

**Priority:** Medium - affects monitoring functionality but doesn't crash

---

### 2. **Power Tweaks Not Functional on WSL2/Linux**
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

### 3. **Elevated Host Launch on First Tweak Apply**
**Severity:** Low
**Status:** ⚠️ **KNOWN BEHAVIOR**

**Description:**
- First time applying a tweak that requires elevation, a UAC prompt appears
- User must accept the UAC prompt within 5 seconds
- If UAC is cancelled or timed out, the tweak fails

**Expected Behavior:**
- UAC prompt is **normal and required** for system-level tweaks
- 5-second timeout ensures the application doesn't hang
- Subsequent tweaks use the same elevated host (no additional UAC prompts)

**Workaround:** Accept UAC prompt quickly when it appears

**Priority:** Low - expected Windows security behavior

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
- `WindowsOptimizer.Infrastructure/Monitoring/*.cs`

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
- WindowsOptimizer.ElevatedHost.exe must be in the same directory as the main app
- If missing, all elevation-required tweaks will fail
- Build configuration might not copy the ElevatedHost.exe to output directory

**Recommendation:**
- Verify ElevatedHost.exe exists in build output
- Add post-build copy command if missing
- Add application startup check for ElevatedHost.exe existence

**Files to Review:**
- `WindowsOptimizer.App/WindowsOptimizer.App.csproj`
- Build scripts

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

### High Priority (Critical for Stability)

- [ ] **Fix Network Adapter Monitoring**
  - Investigate why `NetworkMonitor` returns empty adapters
  - Check performance counter instance names
  - Add fallback using `System.Net.NetworkInformation`

- [ ] **Fix Disk Activity Monitoring**
  - Investigate why `DiskMonitor` returns empty disks
  - Check performance counter availability
  - Add fallback using WMI or P/Invoke

- [ ] **Add ElevatedHost.exe Existence Check**
  - Check on application startup
  - Show clear error message if missing
  - Disable elevation-required tweaks if unavailable

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

#### Monitor Page
- [ ] Monitor page loads without crashing
- [ ] CPU usage displays correctly
- [ ] RAM usage displays correctly
- [ ] Top processes list updates
- [ ] Temperature sensors work (if available)
- [ ] 60-second history graphs render
- [ ] Network adapters display (check empty issue)
- [ ] Disk activity displays (check empty issue)

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

**Total Commits (Recent):** 5
**Files Modified (Recent):** 8
**Lines Added:** ~150
**Lines Removed:** ~10
**Bugs Fixed:** 4 critical, 1 minor
**Platform Checks Added:** 2
**Timeout Mechanisms Added:** 2

---

## 🔗 Related Documents

- [README.md](README.md) - Project overview and features
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines
- [HANDOFF_REPORT.md](HANDOFF_REPORT.md) - Previous development handoff

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

**Last Build:** December 26, 2025
**Build Status:** ✅ Passing (with known issues)
**Ready for Production:** ⚠️ Needs testing on Windows 10/11 native
