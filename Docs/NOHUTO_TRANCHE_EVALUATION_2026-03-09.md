# Nohuto Tranche Evaluation (2026-03-09)

This note reviews the latest nohuto-derived registry/configuration tranche against the current Windows Optimizer configuration surface.

The goal is not to mirror every upstream key 1:1. The goal is to decide:

- what is already covered by the app,
- what can be added safely,
- what should become expert-only or hardware-aware,
- and what should remain documentation or inspection only.

## Decision Summary

| Decision | Meaning |
| --- | --- |
| Already covered | The app already ships a tweak that maps to the same key family or behavior. |
| Add next | Good candidate for a new tweak or value-backed setting. |
| Expert-only | Real and useful, but too low-level, too device-specific, or too easy to misuse for the default SAFE surface. |
| Read-only first | Better exposed as detected state and documentation before any one-click action exists. |
| Do not add as SAFE | Too risky, too speculative, or too hardware/vendor-specific for the main curated tweak list. |

## 1. Already Covered Well

These upstream findings are already represented in the app and do not need a second parallel tweak.

| Upstream area | Example registry/value family | App coverage | Status |
| --- | --- | --- | --- |
| Session Manager service splitting | `SvcHostSplitThresholdInKB` | `system.disable-service-splitting` | Already covered |
| Kernel DPC family | `AdjustDpcThreshold`, `DpcQueueDepth`, `MinimumDpcRate`, `IdealDpcRate`, `SerializeTimerExpiration` | `system.kernel-*` tweaks in `SystemRegistryTweakProvider` | Already covered |
| DWM / MPO | `OverlayTestMode` | `system.dwm-disable-mpo` | Already covered |
| NTFS behavior | `NtfsDisableLastAccessUpdate`, `NtfsDisable8dot3NameCreation`, `NtfsMemoryUsage` | `system.ntfs-*` | Already covered |
| Fast Startup | `HiberbootEnabled` | `power.disable-fast-startup` | Already covered |
| Power throttling | `PowerThrottlingOff` | `power.disable-power-throttling` | Already covered |
| USB selective suspend behavior | `IdleInWorkingState`, `SelectiveSuspendOn`, global selective suspend families | `power.disable-usb-selective-suspend` | Already covered at curated level |
| Mouse acceleration | `MouseThreshold1`, `MouseThreshold2`, `MouseSpeed` | `peripheral.mouse-disable-acceleration` | Already covered |
| Raw mouse throttling | `RawMouseThrottleEnabled`, `RawMouseThrottleDuration`, `RawMouseThrottleLeeway` | `peripheral.mouse-disable-throttle` | Already covered |
| Keyboard language switch hotkeys | `HKCU\Keyboard Layout\Toggle\*` | `KeyboardTweaks.CreateDisableLanguageSwitchHotkeyTweak` | Already covered |
| Diagnostic telemetry master policy | `AllowTelemetry` | `privacy.disable-diagnostic-data` | Already covered |
| Application telemetry | `AITEnable` | `privacy.disable-application-telemetry` | Already covered |
| Device name telemetry | `AllowDeviceNameInTelemetry` | `privacy.disable-device-name-telemetry` | Already covered |
| Diagnostic Data Viewer | `DisableDiagnosticDataViewer` | `privacy.disable-diagnostic-data-viewer` | Already covered |
| Diagnostic data delete button | `DisableDeviceDelete` | `privacy.disable-diagnostic-data-delete` | Already covered |
| Telemetry opt-in UI and notifications | `DisableTelemetryOptInSettingsUx`, `DisableTelemetryOptInChangeNotification` | matching privacy tweaks | Already covered |
| Diagnostic log / dump limiting | `LimitDiagnosticLogCollection`, `LimitDumpCollection` | matching privacy tweaks | Already covered |
| OneSettings downloads | `DisableOneSettingsDownloads` | `privacy.disable-onesettings-downloads` | Already covered |
| Biometrics | `Biometrics\\Enabled`, `Credential Provider\\Enabled`, `Domain Accounts` | matching privacy tweaks | Already covered |
| Cross-device master switch | `EnableCdp` policy family | `privacy.disable-cross-device-experiences` | Already covered |
| Sign-in privacy | `DontDisplayLastUserName`, `DontDisplayUserName` | matching privacy tweaks | Already covered |
| CEIP | `CEIPEnable` | `privacy.disable-ceip` | Already covered |

## 2. Good Candidates To Add Next

These are reasonable additions because they are upstream-backed, user-comprehensible, and can be modeled without breaking the SAFE contract.

| Candidate | Why it is a good fit | Recommendation |
| --- | --- | --- |
| Find My Device policy | Clear user-facing behavior, policy-backed, low ambiguity | Add as `privacy.disable-find-my-device` or `privacy.find-my-device` |
| Cross-device sharing level picker | Upstream now exposes a more granular model than the current on/off CDP switch | Add as enum-backed advanced setting with `Off / My devices / Everyone nearby` |
| Raw mouse throttle advanced values | The app already has the master toggle; upstream also gives duration/leeway semantics | Add as expert inline value editor behind the existing mouse throttle setting |
| Keyboard layout switch policy visibility | Already partly covered by a disable-hotkey tweak, but the app does not expose the exact value states clearly | Add state detail and explicit value mapping instead of a second tweak |

## 3. Better As Expert-Only Or Hardware-Aware

These are real settings, but they should not be dropped into the normal SAFE toggle list.

| Area | Examples | Why not default SAFE | Recommendation |
| --- | --- | --- | --- |
| NIC advanced tuning | `ITR`, `*InterruptModeration`, `*RssProfile`, `*NumRssQueues`, `*RssBaseProcNumber`, `*RssMaxProcNumber` | Driver-specific, adapter-specific, and strongly workload-dependent | Build a future NIC advanced workspace, gated by adapter/driver detection |
| NVIDIA PhysX selection | `NvCplPhysxAuto`, `physxGpuId` | Vendor-specific and requires hardware-aware detection and enum handling | Add only in an NVIDIA-only advanced section |
| 25H2 graphics scheduler knobs | `AudioDgAutoBoostPriority`, `DebugLargeSmoothenedDuration`, `EnableDirectSubmission`, `FrameServerAutoBoostPriority` | Upstream evidence is real, but semantics are partly experimental and not yet safe for one-click shipping | Expose read-only detection first; only add write actions after stronger validation |
| Kernel scheduler internals | `CacheAwareScheduling`, `DefaultDynamicHeteroCpuPolicy` | Too low-level, not user-comprehensible, and hard to validate across hardware | Keep out of SAFE; consider an expert diagnostics page only |
| Memory management internals | `LargeSystemCache`, `PagedPoolSize`, `NonPagedPoolSize`, `RegistryQuota` | Can materially affect memory behavior and are not broadly recommended as generic desktop tweaks | Expert-only at most; not a default configuration item |
| StorPort / NVMe power tuning | `EnableIdlePowerManagement`, `EnableHIPM`, `EnableDIPM` | Storage-path and hardware-specific, with unclear upside on many systems | Hardware-aware advanced disk page, not global SAFE toggle |
| Per-device USB power / usbflags | `IgnoreHWSerNum`, `ResetOnResume`, `DisableLpm`, `Usb20HardwareLpmOverride`, `Usb20HardwareLpmTimeout` | Per-device hack flags with fragile semantics and strong hardware variance | Do not ship as normal toggles; use documentation or troubleshooting flow only |

## 4. Read-Only First

These are valuable to surface in the UI, but should start as evidence and context before they become one-click actions.

| Area | Why read-only first |
| --- | --- |
| 25H2 graphics scheduler values | They are useful for “this system is using these scheduler knobs” diagnostics, but not yet good one-click candidates. |
| NVIDIA PhysX mode | Useful to show current GPU/CPU PhysX routing, even before adding change actions. |
| NIC RSS / interrupt moderation state | Useful to inspect current adapter behavior and explain latency vs CPU tradeoffs. |
| WHEA logging state | Good as a diagnostics readout; poor as a default tweak recommendation. |

## 5. Do Not Add As Normal SAFE Toggles

These do not fit the main curated configuration list even if upstream documents them.

| Area | Why |
| --- | --- |
| WHEA logging disable paths | Disabling or suppressing hardware error reporting is not aligned with a safe optimization surface. |
| Per-device USB hack flags | Too fragile, too driver-dependent, and too hard to explain safely. |
| Generic “kernel memory pool” sizing tweaks | Too easy to cargo-cult and too easy to misapply without clear user benefit. |
| Experimental 25H2 scheduler write knobs | Evidence exists, but the repo itself treats parts of this area as still being worked out. |

## 6. Practical Product Decision

The tranche should not be treated as “add all upstream knobs.”

The right product move is:

1. Keep the existing curated SAFE set as the default experience.
2. Add a small second wave of high-confidence user-facing settings:
   - Find My Device policy
   - Cross-device sharing level
   - richer raw mouse throttle value detail
3. Add read-only advanced inspection surfaces for:
   - NVIDIA state
   - graphics scheduler state
   - NIC RSS / interrupt moderation state
   - WHEA state
4. Keep per-device USB flags, kernel pool sizing, and speculative scheduler writes out of the normal one-click configuration list.

## 7. Recommended Immediate Backlog

| Priority | Work item | Why |
| --- | --- | --- |
| High | Add `Find My Device` policy-backed tweak | Clear behavior, low ambiguity, upstream-backed |
| High | Upgrade cross-device setting from on/off to enum-backed | Better matches upstream evidence |
| Medium | Add read-only graphics scheduler inspector | Valuable on 25H2 systems without overpromising tweak safety |
| Medium | Add NVIDIA advanced state card (PhysX mode only) | Good hardware-aware improvement |
| Medium | Add NIC advanced inspector for RSS / IM / ITR | Better than generic “optimize NIC” toggles |
| Low | Add raw mouse throttle advanced value editor | Good expert feature, lower general value |

## 8. Bottom Line

Yes, the tranche is useful.

But it should be split into three buckets:

- **already represented in the app**,  
- **safe next additions**,  
- **upstream research that belongs in expert or read-only tooling rather than the main SAFE tweak list**.

That keeps the app aligned with the repo’s strongest findings without turning the configuration surface into a raw registry browser.
