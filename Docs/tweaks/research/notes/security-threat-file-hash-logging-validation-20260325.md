# Microsoft Defender Threat File Hash Logging

Record: `security.threat-file-hash-logging`

This pass started with a lab fix before the actual policy work.

## Lab fix

The original high-risk snapshot was not valid for Defender policy research.

Artifacts:

- `H:\Temp\vm-tooling-staging\defender-runtime-repair.json`

What it showed:

- `WinDefend` was present but `Disabled`
- `WdNisSvc` was present but `Disabled`
- `Get-MpComputerStatus` reported:
  - `AMRunningMode = Not running`
  - `AntivirusEnabled = false`
  - `RealTimeProtectionEnabled = false`

After re-enabling `WinDefend` and waiting for the service to settle, the same JSON showed:

- `AMRunningMode = Normal`
- `AntivirusEnabled = true`
- `RealTimeProtectionEnabled = true`
- `BehaviorMonitorEnabled = true`
- `IoavProtectionEnabled = true`

That repair was captured in a new snapshot:

- `baseline-20260325-defender-on`

Everything below uses that Defender-on snapshot, not the older disabled baseline.

## Source check

The source story is split.

Official Microsoft docs:

- `Create indicators for files`
  - path/feature: `MpEngine\EnableFileHashComputation`
  - key point: file-indicator hash support is for `PE` files only
- `Troubleshoot Microsoft Defender Antivirus settings`
  - key point: event `1120` is the file-hash-logging event and is tied to `ThreatFileHashLogging`

Local dump mirrors:

- `Docs/security/assets/Windows-Defender.txt`
- `Docs/tweaks/_source-mirrors/regkit/assets/traces/25H2.txt`

Those local mirrors show three related surfaces:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\ThreatFileHashLogging`
- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager\EnableFileHashComputation`
- `HKLM\SOFTWARE\Microsoft\Windows Defender\MpEngine\EnableFileHashComputation`

So the repo already had a naming split before the VM pass started.

## VM proof

Snapshot:

- `baseline-20260325-defender-on`

Artifacts:

- baseline:
  - `H:\Temp\vm-tooling-staging\defender-threat-file-hash-baseline-1-20260325-011024`
- documented MpEngine path:
  - `H:\Temp\vm-tooling-staging\defender-threat-file-hash-mpengine-1-20260325-011519`
- legacy root path:
  - `H:\Temp\vm-tooling-staging\defender-threat-file-hash-legacyroot-1-20260325-011845`
- Policy Manager alias:
  - `H:\Temp\vm-tooling-staging\defender-threat-file-hash-policymanager-1-20260325-012333`

### Baseline

The baseline run used the Defender-on snapshot with all known hash-logging policy surfaces unset.

Event summary:

```text
DETECTION_EVENT_COUNT=1
HASH_EVENT_COUNT=0
```

That means the EICAR probe was enough to trigger Defender detection event `1116`, but not enough to produce event `1120`.

That does not invalidate the pass. Microsoft's file-indicator doc says file hash computation support is for `PE` files only, and the EICAR probe here was a text file.

### Legacy root path

Setting:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender`
- `ThreatFileHashLogging = 1`

Direct runtime read:

```text
MsMpEng.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\ThreatFileHashLogging | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

The same trace also showed these sibling checks in the same window:

```text
MsMpEng.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager\EnableFileHashComputation | NAME NOT FOUND | Length: 16
MsMpEng.exe | RegQueryValue | HKLM\SOFTWARE\Microsoft\Windows Defender\MpEngine\EnableFileHashComputation | NAME NOT FOUND | Length: 16
```

So on this current live pass, `MsMpEng.exe` did read the legacy root value directly.

### Policy Manager alias

Setting:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager`
- `EnableFileHashComputation = 1`

Direct runtime read:

```text
MsMpEng.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Policy Manager\EnableFileHashComputation | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

The same pass also showed the root value missing:

```text
MsMpEng.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\ThreatFileHashLogging | NAME NOT FOUND | Length: 16
```

So the Policy Manager alias is also live on this 25H2 VM.

### Documented MpEngine path

Setting:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine`
- `EnableFileHashComputation = 1`

Result in this pass:

- event `1116` still happened
- event `1120` still did not happen
- no clean live `RegQueryValue` read for the documented MpEngine path was captured in the same non-rebooted probe window

That is the main gap left in this record.

## What this means

This record is not weak. It is just not clean enough for Class A.

What is solid:

- the feature name is documented by Microsoft
- the `0/1` model is documented
- the current 25H2 VM reads `ThreatFileHashLogging = 1` on the legacy root path
- the current 25H2 VM reads `EnableFileHashComputation = 1` on the Policy Manager alias
- the baseline Defender-on snapshot is now fixed and repeatable

What is still unresolved:

- the documented `MpEngine` path did not produce a live read in this non-rebooted pass
- the EICAR text-file probe is not enough to close the `1120` loop because Microsoft limits file-indicator hash support to `PE` files

## Current classification

Current state:

- validated
- not app-mapped
- research-gated
- `Class C`

That is the right class for now.

The key exists, the values are partly understood, and the live runtime story is much better than it was before this pass. But the active current-build control surface still needs one more tightening pass before this becomes a one-click app setting.
