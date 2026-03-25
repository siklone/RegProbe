# VM Workflow

This repository is validated in the `Win25H2Clean` VMware VM.

## Rule

- Do not run live app validation on the host.
- Keep the VM visible in VMware Workstation. Do not switch the validation lane to `nogui`.
- Use the VM for:
  - registry and policy experiments
  - performance testing
  - Procmon captures
  - WPR/WPA traces
  - Ghidra headless analysis

## Tooling Available in the VM

### Performance
- WPR
- WPA
- xperf
- WinSAT
- DiskSpd
- AIDA64 Extreme (manual visible cross-check)

### Process / File Tracing
- Procmon
- Safe Procmon wrapper with capture limits

### Reverse Engineering
- Ghidra
- Java 21 for Ghidra headless

## Installed Paths

- `C:\Tools\Sysinternals\Procmon64.exe`
- `C:\Tools\Scripts\procmon-safe.ps1`
- `C:\Tools\Scripts\wpr-start-general.cmd`
- `C:\Tools\Scripts\wpr-stop.cmd`
- `C:\Tools\Scripts\wpa.cmd`
- `C:\Tools\Scripts\wpr.cmd`
- `C:\Tools\Scripts\xperf.cmd`
- `C:\Tools\Scripts\ghidra-headless.cmd`
- `C:\Tools\Perf\diskspd.exe`
- `C:\Tools\AIDA64Extreme\aida64.exe`
- `C:\Tools\Java\jdk-21.0.10+7`
- `C:\Tools\Ghidra\ghidra_12.0.4_PUBLIC`

## Safe Procmon Capture

Use the wrapper instead of raw Procmon for experiments that may generate a lot of data:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Tools\Scripts\procmon-safe.ps1 -DurationSeconds 90 -MaxMegabytes 256
```

The wrapper:

- starts Procmon with a backing file
- stops after the requested duration
- stops early if the backing file reaches the configured size limit

## WPR / WPA

Start a capture:

```cmd
C:\Tools\Scripts\wpr-start-general.cmd
```

Stop a capture and write the ETL:

```cmd
C:\Tools\Scripts\wpr-stop.cmd C:\Tools\Perf\capture.etl
```

Open the result:

```cmd
C:\Tools\Scripts\wpa.cmd C:\Tools\Perf\capture.etl
```

## Ghidra Headless

Example:

```cmd
C:\Tools\Scripts\ghidra-headless.cmd C:\Tools\GhidraProjects\OpenTraceProject analysis -import C:\Path\To\Binary.exe
```

## Validation Smokes

Tool-health smoke:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Tools\Scripts\tool-health-smoke.ps1
```

Published app launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Tools\Scripts\app-launch-smoke.ps1 -PublishZipPath C:\Tools\Inbound\app-publish.zip
```

The active benchmark lane is:

- `WinSAT CPU + WPR`
- `WinSAT mem + WPR`
- `DiskSpd + WPR`
- `AIDA64` for manual visible cross-check

Manual reboot-sensitive comparison runs can use:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\run-manual-value-benchmark.ps1 -TestName priority-control -RegistryPath HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl -ValueName Win32PrioritySeparation -BaselineValue 2 -CandidateValue 38
```

The active suite avoids EULA-gated third-party stress tools.

## Shell Health And Incidents

Check the shell before and after risky families:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\get-vm-shell-health.ps1
```

If a test drops the desktop, shell host, input, Store app startup, or app launch path, log it:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\log-vm-incident.ps1 -RecordId system.disable-shortcut-arrow -TestId shortcut-arrow-noarrow-probe -Symptom "Desktop disappeared after Explorer restart"
```

Incidents are tracked in `research\vm-incidents.json` and folded into `research\evidence-audit.json`.

Before Explorer, graphics, input, or Defender work, capture a shell-stable snapshot:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\ensure-shell-stable-snapshot.ps1
```

## Snapshot Rule For High-Risk Lanes

Before testing high-risk families, create or revert a clean VM snapshot.

Use this for lanes like:

- Defender policy keys
- `stornvme`
- `USBHUB3`
- `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows`

Example:

```powershell
& "C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe" snapshot H:\Yedek\VMs\Win25H2Clean\Win25H2.vmx baseline-20260324-high-risk-lane
```

Dump collection for these lanes can use:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\export-high-risk-dumps.ps1
```

That script exports:

- Defender policy root
- Defender `Policy Manager`
- `Control\Power`
- `stornvme`
- `USBHUB3`
- `CurrentVersion\Windows`

The exporter will start the VM if it is powered off. Missing keys are written as metadata instead of being treated as a script failure.

## Cleanup

Host cleanup keeps only referenced artifacts and the current offline tool cache:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\cleanup-host-validation-artifacts.ps1 -Apply
```

Guest cleanup archives desktop clutter, drops old validation output, and removes OCCT leftovers:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\cleanup-guest-validation-artifacts.ps1 -Apply
```

## Bootstrapping Notes

- The tooling is staged through the VM shared folder during setup.
- `JAVA_HOME` and `GHIDRA_HOME` are set in the VM machine environment.
- Procmon EULA is pre-accepted in the guest profile used for validation.
