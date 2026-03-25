# High-Risk Dump Lane - 2026-03-24

This pass set up the first snapshot-first dump lane for the `Win25H2Clean` VM.

## Snapshot

- VM: `Win25H2Clean`
- Snapshot created before any risky lane work:
  - `baseline-20260324-high-risk-lane`

No risky values were changed in this pass. This was dump collection only.

## New VM Export Tooling

New host-side scripts:

- `scripts/vm/export-registry-key.ps1`
- `scripts/vm/export-high-risk-dumps.ps1`

The exporter runs inside the guest, captures:

- `reg query /s` text output
- `.reg` export
- metadata with guest boot time and exit codes

Host artifact root for this pass:

- [`research/evidence-files/vm-tooling-staging/registry-dumps/high-risk-dumps-summary.json`](../evidence-files/vm-tooling-staging/registry-dumps/high-risk-dumps-summary.json)

Main batch summary:

- `research/evidence-files/vm-tooling-staging/registry-dumps/high-risk-dumps-summary.json`

## Families Dumped

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`
- `HKLM/SYSTEM/CurrentControlSet/Control/Power`
- `HKLM/SYSTEM/CurrentControlSet/Services/stornvme/Parameters`
- `HKLM/SYSTEM/CurrentControlSet/Services/stornvme/Parameters/Device`
- `HKLM/SYSTEM/CurrentControlSet/Services/USBHUB3`
- `HKLM/SYSTEM/CurrentControlSet/Services/USBHUB3/Parameters`
- `HKLM/SYSTEM/CurrentControlSet/Services/USBHUB3/Parameters/Wdf`
- `HKLM/SOFTWARE/Microsoft/Windows NT/CurrentVersion/Windows`

## What A Clean 25H2 VM Actually Has

### Defender policy root

The clean VM currently has no configured Defender policy values under the root or `Policy Manager`.

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-policy-root-20260324-210024/defender-policy-root.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-policy-manager-20260324-210159/defender-policy-manager.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-reporting-20260324-211238/defender-reporting.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-spynet-20260324-211238/defender-spynet.txt`

This matters because the upstream dump lists are still useful, but they are not a live 25H2 policy baseline by themselves. They show the possible surface, not the currently configured state in this VM.

The first two narrow subkeys checked in this pass were also absent:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Reporting`
- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Spynet`

That gives us a clean absent baseline for `DisableEnhancedNotifications`, `SpyNetReporting`, and `SubmitSamplesConsent`.

### Control/Power

`HKLM/SYSTEM/CurrentControlSet/Control/Power` exported cleanly and is dense. The raw query has 3019 value lines in this VM.

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.reg`

This family is too large for blind testing. It needs a smaller queue built from `Power.txt`, current power records, and decompiled call sites.

### StorNVMe

The clean VM only exposed a small live set under `stornvme`:

- `BusType`
- `IoTimeoutValue`
- `StorageSupportedFeatures`
- `IoStripeAlignment`
- `DisableF0TimestampSync`

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/stornvme-parameters-20260324-210218/stornvme-parameters.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/stornvme-device-20260324-210225/stornvme-device.txt`

This looks hardware and controller specific. It does not look like a good first app surface.

### USBHUB3

The clean VM export is mostly service metadata plus logging/WDF values:

- service metadata like `ImagePath`, `Type`, `Start`, `ErrorControl`, `Group`, `Tag`
- logging values like `LogPages`, `WppRecorder_UseTimeStamp`
- WDF version/debug values like `WdfMajorVersion`, `WdfMinorVersion`

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/usbhub3-service-20260324-210231/usbhub3-service.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/usbhub3-parameters-20260324-210238/usbhub3-parameters.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/usbhub3-wdf-20260324-210245/usbhub3-wdf.txt`

This family is not ready for the main app surface. Most of what is present in the clean VM looks like driver metadata, tracing, or verifier state.

### CurrentVersion/Windows

The clean VM export has 39 live values. Some visible ones:

- `LoadAppInit_DLLs = 0`
- `DdeSendTimeout = 0`
- `DesktopHeapLogging = 1`
- `GDIProcessHandleQuota = 10000`
- `USERNestedWindowLimit = 50`
- `USERPostMessageLimit = 10000`
- `USERProcessHandleQuota = 10000`
- `RapidHpdTimeoutMs = 3000`
- `NaturalInputHandler = Ninput.dll`
- `IconServiceLib = IconCodecService.dll`

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/windows-nt-currentversion-windows-20260324-210251/windows-nt-currentversion-windows.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/windows-nt-currentversion-windows-20260324-210251/windows-nt-currentversion-windows.reg`

This family needs extra care. Some values are legacy, some are global Win32 limits, and some can affect shell/input/app behavior. It is easy to break Store-style or modern Windows app flows here.

## First Queue Split

### Defender lane

Best first candidates:

- `DisableEnhancedNotifications`
- `ThreatFileHashLogging`
- `HideExclusionsFromLocalAdmins`
- `SubmitSamplesConsent`
- `SpyNetReporting`

Why these first:

- narrower than core AV enforcement
- clearer ADMX or lead-note semantics
- safer to revert

Do not start with:

- `DisableAntiSpyware`
- `DisableAntiVirus`
- service state or tenant metadata values
- proxy cache/runtime values

### Storage lane

Start with observation only. Do not expose `stornvme` in the app yet.

Reason:

- live values are sparse
- several values are device-specific multi-strings
- power and queue behavior here can be hardware-specific

### USB lane

Keep `USBHUB3` out of the normal app surface for now.

Reason:

- clean VM values are mostly service metadata, logging, or verifier state
- better candidates sit in separate USB/input families already covered by `usbhub` and `SYSTEM/INPUT`

### CurrentVersion/Windows lane

Keep this research-only until each value is tied to a supported behavior or a strong code path.

This family gets a separate lane because it can affect shell, old Win32 behavior, input, and handle limits.

## Source Split

For these lanes, source roles stay separate:

- upstream dump or pseudocode: candidate discovery and lineage
- official docs or ADMX: supported semantics
- Procmon: runtime reads and writes
- Ghidra or decompiled pseudocode: code path when docs are weak
- WPR/WPA and bounded benchmarks: behavior under load when the value is performance sensitive

## Next Moves

1. Build a Defender-only queue from ADMX-backed keys first.
2. Keep `stornvme`, `USBHUB3`, and `CurrentVersion/Windows` in separate high-risk queues.
3. Use the new snapshot before any apply/reboot pass in those queues.
