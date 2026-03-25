# Defender Validation Queue - 2026-03-24

This queue starts from a clean `Win25H2Clean` baseline and the new snapshot-first dump lane.

Snapshot used for this lane:

- `baseline-20260324-high-risk-lane`
- `baseline-20260325-defender-on`

## Clean 25H2 baseline

The clean VM does not currently have these policy subkeys:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Reporting`
- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Spynet`

Artifacts:

- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-reporting-20260324-211238/defender-reporting.txt`
- `research/evidence-files/vm-tooling-staging/registry-dumps/defender-spynet-20260324-211238/defender-spynet.txt`

That gives us a clean absent baseline for the first ADMX-backed candidates.

## Defender-on repair baseline

The original high-risk snapshot was not valid for Defender policy work because the Defender engine was disabled.

Artifact:

- `research/evidence-files/vm-tooling-staging/defender-runtime-repair.json`

What changed:

- `WinDefend` was re-enabled
- `WdNisSvc` came back with Defender
- `Get-MpComputerStatus` moved from `Not running` to `Normal`

That repair was saved as:

- `baseline-20260325-defender-on`

All later Defender-specific probes should use that snapshot.

## Tier 1 - start here

These have the best mix of documented semantics and low blast radius.

### DisableEnhancedNotifications

- Path: `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Reporting`
- Value: `DisableEnhancedNotifications`
- Type: `REG_DWORD`
- Documented meaning:
  - `0 = notifications enabled`
  - `1 = notifications disabled`
- Source:
  - `Docs/system/system.md` lines `1404-1417`
- Clean 25H2 baseline:
  - subkey absent
- Why first:
  - notification-only
  - direct ADMX semantics already captured

### SpyNetReporting

- Path: `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Spynet`
- Value: `SpyNetReporting`
- Type: `REG_DWORD`
- Reported meaning in current repo notes:
  - `0 = MAPS disabled`
  - `1 = Basic membership`
  - `2 = Advanced membership`
- Source:
  - `research/notes/windows-11-settings-and-privacy-leads.md` lines `258-264`
- Clean 25H2 baseline:
  - subkey absent
- Why first:
  - narrow policy
  - low risk compared with protection or service toggles

### SubmitSamplesConsent

- Path: `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Spynet`
- Value: `SubmitSamplesConsent`
- Type: `REG_DWORD`
- Reported meaning in current repo notes:
  - `0 = Always prompt`
  - `2 = Never send`
  - `3 = Send all automatically`
- Source:
  - `research/notes/windows-11-settings-and-privacy-leads.md` lines `266-273`
- Clean 25H2 baseline:
  - subkey absent
- Why first:
  - narrow data-sharing policy
  - reversible
  - safer than toggling real-time scanning

## Tier 2 - dump-visible, semantics still need work

### ThreatFileHashLogging

- Path from dump:
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
- Source:
  - `Docs/security/assets/Windows-Defender.txt`
- What we know:
  - the value exists in upstream dump/mirror material
  - the name strongly suggests a logging-only setting
- What is still missing:
  - a clean final answer on whether the documented `MpEngine` path or the live root / Policy Manager paths should be treated as canonical on 25H2
  - a clean final answer on what extra condition is needed for event `1120` on 25H2, because the official Microsoft PE demo sample still did not produce it

### HideExclusionsFromLocalAdmins

- Paths from dump:
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`
- Source:
  - `Docs/security/assets/Windows-Defender.txt`
- What we know:
  - the value exists in both root and `Policy Manager` dump surfaces
  - the name suggests UI or access scoping, not scanning behavior
- What is still missing:
  - which path is the supported control surface on current builds
  - official semantics
  - live runtime proof in the VM

## Completed in this lane

### HideExclusionsFromLocalAdmins

- Official behavior now comes from Microsoft Defender docs.
- Live 25H2 VM proof now exists for:
  - root path `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
  - Policy Manager alias `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`
- Result:
  - both paths hide managed exclusions from `Get-MpPreference`
  - the managed exclusions branch stays populated
- Current classification:
  - `Class B`
  - app-mapped
  - research-gated
- Record:
  - `research/records/security.hide-defender-exclusions-from-local-admins.review.json`

### ThreatFileHashLogging

- Official docs now link the feature to `EnableFileHashComputation` and tie event `1120` to file-hash logging.
- Live 25H2 VM proof now exists for:
  - root path `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/ThreatFileHashLogging`
  - Policy Manager alias `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager/EnableFileHashComputation`
- Result:
  - baseline Defender-on EICAR probe produced event `1116` and no `1120`
  - `MsMpEng.exe` read the root legacy value directly when it was set to `1`
  - `MsMpEng.exe` read the Policy Manager alias directly when it was set to `1`
  - the documented `MpEngine` path did not produce a direct live read in the non-rebooted pass
- a same-window `WinDefend` restart attempt was blocked in the guest
- a rebooted capture pass still did not produce a direct read from the documented policy path
- a PE overlay follow-up that copied a built-in Windows PE and appended the EICAR string did not trigger Defender at all, so it is not a usable `1120` sample
- an official Microsoft `BaFS Sample` PE file later produced event `1116` on the baseline and on live root / Policy Manager passes, but still produced no event `1120`
- Current classification:
  - `Class C`
  - not app-mapped
  - research-gated
- Record:
  - `research/records/security.threat-file-hash-logging.review.json`

## Not first-pass candidates

Do not start with these:

- `DisableAntiSpyware`
- `DisableAntiVirus`
- service state values like `IsServiceRunning`
- timestamps and tenant IDs like `InstallTime`, `OOBEInstallTime`, `OrgID`, `PartnerGUID`
- proxy cache/runtime values like `CachedProxy*`, `LastKnownGoodProxy`

These are either overridden on modern builds, state-like, tenant-specific, or too risky for the first pass.

## Next validation steps

1. `DisableEnhancedNotifications`
   - dump baseline already done
   - verify path/value with current repo docs
   - set `1`
   - reboot if needed
   - capture Procmon around Windows Security UI
   - restore snapshot
2. `SpyNetReporting`
   - same pattern
3. `SubmitSamplesConsent`
   - same pattern
4. `ThreatFileHashLogging`
   - only revisit if a better explanation for missing event `1120` appears
   - current PE follow-up is no longer missing; it is negative evidence
5. `HideExclusionsFromLocalAdmins`
   - done

