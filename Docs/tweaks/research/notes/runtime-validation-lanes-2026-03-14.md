## Runtime Validation Lanes

Date: 2026-03-14

This note groups the remaining review backlog into the next two investigation lanes:

- `Procmon-worthy`
  Use Procmon, reversible toggle-diff, and targeted runtime observation.
- `Ghidra-last-resort`
  Only escalate here after docs, ADMX, ADML, Procmon, and WPR or ETW are exhausted.

The goal is to keep the next phase focused on Windows-observed behavior instead of mixing runtime work with product-decision or split work.

## Procmon-worthy

These records are good candidates for runtime capture because they look like UI-backed, settings-backed, or registry-observed behaviors where Procmon can answer "what key does Windows really read or write?" without reverse engineering.

- `privacy.disable-resume`
  Official user policy exists, but the app writes a runtime HKCU setting instead. Procmon should determine which setting surface Windows actually reads when the feature is toggled.
- `audio.show-disconnected-devices`
  Sound Control Panel preference. Likely a straightforward UI-to-registry capture.
- `audio.show-hidden-devices`
  Same pattern as `audio.show-disconnected-devices`.
- `explorer.enable-explorer-compact-mode`
  Likely an Explorer UI preference or internal flag. Procmon can confirm the real runtime surface even without a primary Microsoft page.
- `performance.disable-animations`
  Old Win32 user preference surface. Procmon should settle whether `MinAnimate` is still the active runtime switch on this build.
- `performance.disable-menu-show-delay`
  Classic desktop preference. Procmon can confirm whether `MenuShowDelay` is still the active read path.
- `performance.disable-taskbar-animations`
  Explorer animation flag. Procmon can confirm the runtime path and read behavior.
- `peripheral.autoplay-take-no-action`
  User-choice mapping. Procmon can identify the exact handler surface written by the shell.
- `peripheral.disable-sticky-keys-prompt`
  Accessibility flag. Good candidate for UI-backed toggle-diff capture.
- `system.disable-auto-maintenance`
  Raw registry value with weak primary documentation. Procmon can confirm whether the maintenance stack reads the value.
- `system.disable-fullscreen-optimizations`
  App-compat and GameConfigStore behavior is often only observable through runtime reads and writes.
- `system.disable-jpeg-reduction`
  Desktop wallpaper import setting. Procmon can confirm the read path and whether the current value is live.
- `system.disable-restartable-apps`
  Feature setting versus observed registry path. Procmon should confirm the actual user-facing toggle surface.
- `system.disable-startup-delay`
  Explorer startup behavior is likely observable through Explorer reads after restart.
- `system.enable-game-mode`
  Settings-backed gaming feature. Procmon can confirm whether `AutoGameModeEnabled` is still the live path.
- `system.enable-hags`
  Settings-backed graphics scheduler feature. Procmon can confirm the runtime write path before deeper escalation.
- `visibility.hide-language-bar`
  User preference with no captured primary source. Procmon should identify whether the shell still honors `ShowStatus`.

## Ghidra-last-resort

These records should not move to Ghidra until all cheaper evidence layers fail. Most of them are undocumented low-level knobs, binary bundles, or enum-heavy values where Procmon alone will not explain the semantics.

- `audio.disable-spatial-audio`
  Internal audio engine flag with no captured primary Microsoft contract.
- `developer.terminal-dev-mode`
  Windows Terminal internal flags rather than a documented platform control.
- `power.optimize-cpu-boost`
  Raw undocumented bundle. Very likely needs ETW or binary analysis before it can be explained honestly.
- `security.disable-system-mitigations`
  Raw mitigation blobs under the kernel path. The official Microsoft surface is Exploit Guard XML, not raw registry bytes.
- `security.disable-wpbt`
  No primary Microsoft source captured for the Session Manager write.
- `system.disable-service-splitting`
  The exact `0xFFFFFFFF` no-splitting preset still lacks a strong primary Microsoft explanation.
- `system.dwm-disable-mpo`
  Overlay override with no primary Microsoft source captured.
- `system.graphics-disable-overlays`
  Same family as MPO and other internal graphics diagnostics.
- `system.graphics-page-fault-debug-mode`
  Graphics debug setting with no captured public contract.
- `system.kernel-adjust-dpc-threshold`
- `system.kernel-cache-aware-scheduling`
- `system.kernel-default-dynamic-hetero-cpu-policy`
- `system.kernel-disable-low-qos-timer-resolution`
- `system.kernel-dpc-queue-depth`
- `system.kernel-dpc-watchdog-period`
- `system.kernel-ideal-dpc-rate`
- `system.kernel-minimum-dpc-rate`
- `system.kernel-serialize-timer-expiration`
- `system.priority-control`
  These are all low-level scheduler, timer, kernel, or graphics controls where the remaining gap is value semantics rather than simple path discovery.

## Excluded from these lanes

These records are still real work, but they belong in other lanes:

- `rename`
  Product wording and scope work.
- `split-needed`
  Bundle decomposition work.
- Service records
  Service-proof methodology lane, not Procmon or Ghidra.
- `implementation-mismatch` records with clear surface redesigns
  Examples: `developer.wsl2-memory`, `security.disable-windows-firewall`, `security.disable-system-mitigations`.

## First runtime probe: SMB EncryptData

Attempted target: `network.smb-encrypt-data`

Reason it looked promising:

- strong existing Microsoft documentation for the `EncryptData` registry value
- app already writes the documented LanmanServer path
- seemed like a clean candidate for adding a Procmon-backed runtime proof

What was tested:

- current state checked with `Get-SmbServerConfiguration`
- toggled `Set-SmbServerConfiguration -EncryptData $true -Force`
- rolled back with `Set-SmbServerConfiguration -EncryptData $false -Force`
- confirmed the machine ended in the original state

Observed result:

- an unfiltered Procmon run produced an oversized log and was not useful
- a filtered Procmon run completed cleanly, but the filtered PML contained zero `EncryptData` events

Current conclusion:

- `Set-SmbServerConfiguration -EncryptData` is not a reliable Procmon anchor for proving the app's registry write surface
- this record should either be closed from documentation-only proof or revisited with an app-side or direct-registry capture, not with the official SMB cmdlet as the trigger

## Recommended next Procmon target

Start the runtime-diff lane with a user-preference or shell-backed record instead of another SMB server control.

Best candidates:

1. `audio.show-hidden-devices`
2. `audio.show-disconnected-devices`
3. `explorer.enable-explorer-compact-mode`
4. `privacy.disable-resume`
