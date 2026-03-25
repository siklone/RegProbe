# Microsoft Defender Threat File Hash Logging

Record: `security.threat-file-hash-logging`

This pass started with a lab fix before the actual policy work.

## Lab fix

The original high-risk snapshot was not valid for Defender policy research.

Artifacts:

- `research/evidence-files/vm-tooling-staging/defender-runtime-repair.json`

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
  - path/feature: `MpEngine/EnableFileHashComputation`
  - key point: file-indicator hash support is for `PE` files only
- `Demonstrate cloud-delivered protection`
  - key point: Microsoft provides a password-protected `BaFS Sample` PE file for Defender demos
- `Troubleshoot Microsoft Defender Antivirus settings`
  - key point: event `1120` is the file-hash-logging event and is tied to `ThreatFileHashLogging`

Local dump mirrors:

- `Docs/security/assets/Windows-Defender.txt`
- `research/_source-mirrors/regkit/assets/traces/25H2.txt`

Those local mirrors show three related surfaces:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/ThreatFileHashLogging`
- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager/EnableFileHashComputation`
- `HKLM/SOFTWARE/Microsoft/Windows Defender/MpEngine/EnableFileHashComputation`

So the repo already had a naming split before the VM pass started.

## VM proof

Snapshot:

- `baseline-20260325-defender-on`

Artifacts:

- baseline:
  - [`defender-threat-file-hash-baseline-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-baseline-1-20260325-011024/defender-threat-file-hash-baseline-events.json)
- documented MpEngine path:
  - [`defender-threat-file-hash-mpengine-1-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-011519/defender-threat-file-hash-mpengine-1-events.json)
- legacy root path:
  - [`defender-threat-file-hash-legacyroot-1.txt`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-20260325-011845/defender-threat-file-hash-legacyroot-1.txt)
- Policy Manager alias:
  - [`defender-threat-file-hash-policymanager-1.txt`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-20260325-012333/defender-threat-file-hash-policymanager-1.txt)
- service-restart follow-up:
  - [`defender-threat-file-hash-mpengine-1-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-095038/defender-threat-file-hash-mpengine-1-events.json)
- rebooted documented MpEngine path:
  - [`defender-threat-file-hash-mpengine-1.txt`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-20260325-100039/defender-threat-file-hash-mpengine-1.txt)
- official Microsoft PE sample baseline:
  - [`defender-threat-file-hash-baseline-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-baseline-1-custom-20260325-131902/defender-threat-file-hash-baseline-custom-events.json)
- official Microsoft PE sample with legacy root:
  - [`defender-threat-file-hash-legacyroot-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-custom-20260325-133409/defender-threat-file-hash-legacyroot-1-custom-events.json)
- official Microsoft PE sample with Policy Manager:
  - [`defender-threat-file-hash-policymanager-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-custom-20260325-135316/defender-threat-file-hash-policymanager-1-custom-events.json)
- official Microsoft PE sample with rebooted MpEngine path:
  - [`defender-threat-file-hash-mpengine-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-custom-20260325-140816/defender-threat-file-hash-mpengine-1-custom-events.json)

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

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
- `ThreatFileHashLogging = 1`

Direct runtime read:

```text
MsMpEng.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/ThreatFileHashLogging | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

The same trace also showed these sibling checks in the same window:

```text
MsMpEng.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager/EnableFileHashComputation | NAME NOT FOUND | Length: 16
MsMpEng.exe | RegQueryValue | HKLM/SOFTWARE/Microsoft/Windows Defender/MpEngine/EnableFileHashComputation | NAME NOT FOUND | Length: 16
```

So on this current live pass, `MsMpEng.exe` did read the legacy root value directly.

### Policy Manager alias

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`
- `EnableFileHashComputation = 1`

Direct runtime read:

```text
MsMpEng.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager/EnableFileHashComputation | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

The same pass also showed the root value missing:

```text
MsMpEng.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/ThreatFileHashLogging | NAME NOT FOUND | Length: 16
```

So the Policy Manager alias is also live on this 25H2 VM.

### Documented MpEngine path

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/MpEngine`
- `EnableFileHashComputation = 1`

Result in this pass:

- event `1116` still happened
- event `1120` still did not happen
- no clean live `RegQueryValue` read for the documented MpEngine path was captured in the same non-rebooted probe window

That was the main gap left in this record, so I ran two more follow-ups.

### Service restart follow-up

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/MpEngine`
- `EnableFileHashComputation = 1`

Result in this pass:

- the helper requested a `WinDefend` restart before the EICAR probe
- Defender blocked the service stop/start from the guest
- the event log still showed `1116`
- the pass still showed no clean live `RegQueryValue` read for the documented policy path

Recorded service result:

```text
SERVICE_RESTART={"requested":true,"attempted":true,"succeeded":false,"error":"Service 'Microsoft Defender Antivirus Service (WinDefend)' cannot be stopped ..."}
```

So this pass did not unlock any extra MpEngine evidence. It only proved that a same-window guest-side service restart is not available in this lab setup.

### Rebooted MpEngine follow-up

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/MpEngine`
- `EnableFileHashComputation = 1`

Method:

- revert to `baseline-20260325-defender-on`
- write the documented policy value
- perform a real guest reboot
- run a capture-only Procmon pass plus the same EICAR activity

Result in this pass:

- `MATCH_COUNT=0` for the tracked `EnableFileHashComputation` and `ThreatFileHashLogging` fragments
- event `1116` still happened
- event `1120` still did not happen
- `MsMpEng.exe` did read the non-policy `HKLM/SOFTWARE/Microsoft/Windows Defender/MpEngine` branch again, but only for existing ring values like `MpEngineRing`, `MpCampRing`, and `MpSignatureRing`

That is stronger than the first negative result because it removes the "maybe it only gets read after reboot" explanation.

### PE overlay follow-up

I also tried a bounded PE-only follow-up to avoid relying on the text-file EICAR path.

Method:

- revert to `baseline-20260325-defender-on`
- copy a built-in Windows PE (`choice.exe` or `notepad.exe`)
- append the EICAR string to the copied PE as an overlay
- run the Defender activity pass with a hard `MpCmdRun` timeout

Artifact:

- `research/evidence-files/vm-tooling-staging/defender-pe-baseline-20260325/defender-pe-baseline.json`

Result:

```text
SAMPLE_KIND=pe
MPCMDRUN_EXIT_CODE=0
MPCMDRUN_TIMED_OUT=False
DETECTION_EVENT_COUNT=0
HASH_EVENT_COUNT=0
```

So this PE overlay sample did not trigger Defender at all. That means it is not a usable PE test sample for closing the `1120` gap.

## Official Microsoft PE sample follow-up

I then switched to the official Microsoft cloud-protection demo sample instead of the text-file EICAR probe or the synthetic PE overlay.

Host extraction proof:

- zip source: `https://go.microsoft.com/fwlink/?linkid=2298135`
- extracted file:
  - `research/evidence-files/vm-tooling-staging/defender-cloud-demo-extracted/microsoft-defender-cloud-demo.exe.md`
- file metadata:
  - `FileDescription = BaFS Sample`
  - `OriginalFilename = BaFS Sample.exe`
  - `SHA256 = 670b00e90a7c9eb7ac6674441551e7764a8364c26e44dcc92474a9abcfac4c04`

### Official PE baseline

Artifacts:

  - [`defender-threat-file-hash-baseline-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-baseline-1-custom-20260325-131902/defender-threat-file-hash-baseline-custom-events.json)

Result:

```text
DETECTION_EVENT_COUNT=1
HASH_EVENT_COUNT=0
Name: Trojan:Win32/Clengtst.A!plock
Path: file:_research/evidence-files/missing/defender-custom-sample-exe.md
```

So the official Microsoft PE sample was enough to trigger a real Defender detection on this VM, but event `1120` still did not appear on the unset baseline.

### Official PE with legacy root

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
- `ThreatFileHashLogging = 1`

Artifacts:

  - [`defender-threat-file-hash-legacyroot-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-legacyroot-1-custom-20260325-133409/defender-threat-file-hash-legacyroot-1-custom-events.json)

Result:

```text
DETECTION_EVENT_COUNT=1
HASH_EVENT_COUNT=0
```

The same PE sample still produced only event `1116`. In this PE detection window, the filtered Procmon summary did not show a fresh direct read of `ThreatFileHashLogging`.

### Official PE with Policy Manager

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`
- `EnableFileHashComputation = 1`

Artifacts:

  - [`defender-threat-file-hash-policymanager-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-policymanager-1-custom-20260325-135316/defender-threat-file-hash-policymanager-1-custom-events.json)

Result:

```text
DETECTION_EVENT_COUNT=1
HASH_EVENT_COUNT=0
```

Again, the official PE sample produced event `1116` and no `1120`. In this PE detection window, the filtered Procmon summary did not show a fresh direct read of `Policy Manager/EnableFileHashComputation`.

### Official PE with rebooted MpEngine path

Setting:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/MpEngine`
- `EnableFileHashComputation = 1`

Method:

- revert to `baseline-20260325-defender-on`
- write the documented policy value
- perform a real guest reboot
- run a capture-only Procmon pass plus the official Microsoft PE sample

Artifacts:

  - [`defender-threat-file-hash-mpengine-1-custom-events.json`](../evidence-files/vm-tooling-staging/defender-threat-file-hash-mpengine-1-custom-20260325-140816/defender-threat-file-hash-mpengine-1-custom-events.json)

Result:

```text
MATCH_COUNT=0
DETECTION_EVENT_COUNT=0
HASH_EVENT_COUNT=0
FILE_EXISTS_AFTER=False
MPCMDRUN_EXIT_CODE=2
```

This is a strong negative follow-up:

- the documented policy path still did not produce a direct read after reboot
- event `1120` still did not appear
- the PE sample had already been removed by the time the activity collector checked for it

## What this means

This record is not weak. It is just not clean enough for Class A.

What is solid:

- the feature name is documented by Microsoft
- the `0/1` model is documented
- the current 25H2 VM reads `ThreatFileHashLogging = 1` on the legacy root path
- the current 25H2 VM reads `EnableFileHashComputation = 1` on the Policy Manager alias
- the baseline Defender-on snapshot is now fixed and repeatable
- the documented policy `MpEngine` path still did not produce a direct live read even after a full rebooted follow-up

What is still unresolved:

- the documented policy `MpEngine` path did not produce a direct live read in either the non-rebooted or rebooted pass
- a guest-side `WinDefend` restart was blocked, so there is still no service-restart trace window
- even the official Microsoft `BaFS Sample` PE file produced event `1116` with no event `1120`
- the official PE root and Policy Manager passes did not show a fresh direct read of the target values in the same detection window

## Current classification

Current state:

- validated
- not app-mapped
- research-gated
- `Class C`

That is the right class for now.

The key exists, the values are partly understood, and the live runtime story is much tighter now. But the active current-build control surface is still split, so this stays research-gated instead of moving into a one-click app write.
