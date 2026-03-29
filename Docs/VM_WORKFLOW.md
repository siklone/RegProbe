# VM Workflow

Runtime validation for this repository happens in the `Win25H2Clean` VMware guest.

## Rule

- Do not run live app validation on the host.
- Keep the guest visible in VMware Workstation. Do not switch validation lanes to `nogui`.
- Use the VM for:
  - live RegProbe runs
  - registry and policy experiments
  - Procmon captures
  - WPR/WPA traces
  - ETW collection
  - Ghidra headless analysis
- Use the host only for source editing, docs, artifact review, and offline prep.

## Canonical Baseline

- VM identity: `Win25H2Clean`
- Canonical runtime snapshot: `RegProbe-Baseline-20260328`
- Seed snapshot used to build the canonical baseline: `baseline-20260327-regprobe-visible-shell-stable`

New runtime work should start from `RegProbe-Baseline-20260328`, not from older shell-stable snapshots.

The baseline wrapper lives at:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\new-regprobe-defender-excluded-baseline.ps1
```

The shared snapshot resolver used by active research scripts lives at:

```text
scripts/vm/_resolve-vm-baseline.ps1
registry-research-framework/config/vm-baselines.json
```

## Defender Exclusion Rule

Defender stays enabled. We do not disable real-time protection. The canonical baseline applies bounded exclusions only for trusted tooling roots and processes:

- Paths:
  - `C:\Tools`
  - `C:\RegProbe-Diag`
- Processes:
  - `powershell.exe`
  - `Procmon64.exe`
  - `wpr.exe`
  - `wpa.exe`
  - `xperf.exe`
  - `java.exe`
  - `javaw.exe`
  - `diskspd.exe`
  - `winsat.exe`
  - `RegProbe.App.exe`
  - `RegProbe.ElevatedHost.exe`

Do not add broad user-profile exclusions, `%TEMP%`, or entire drives.

The guest-side exclusion helper is:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\apply-defender-tooling-exclusions.ps1
```

The baseline audit package is tracked in:

```text
registry-research-framework/audit/regprobe-baseline-defender-exclusions-20260328.json
```

## Tooling Available In The VM

### Performance
- WPR
- WPA
- xperf
- WinSAT
- DiskSpd

### Process And Registry Tracing
- Procmon
- safe Procmon wrapper

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
- `C:\Tools\Java\jdk-21.0.10+7`
- `C:\Tools\Ghidra\ghidra_12.0.4_PUBLIC`

## Validation Smokes

Minimal tooling smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\run-vm-tooling-minimal-diagnostic.ps1
```

Visible app launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\run-app-launch-smoke-host.ps1
```

Shell health check:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\get-vm-shell-health.ps1
```

The canonical baseline is considered healthy only when:

- `explorer.exe` is present
- `sihost.exe` is present
- `ShellHost.exe` is present
- `ctfmon.exe` is present

## Stepwise WPR And Reboot Lanes

Reboot-sensitive and WPR-heavy research lanes should use explicit substeps instead of one monolithic script.

Canonical step shape:

- `A`: baseline read plus candidate write
- `B`: reboot plus post-boot confirmation
- `C1`: WPR start plus proof that tracing is active
- `C2`: WPR stop to a known ETL path
- `C3`: guest-side ETL existence check
- `C4`: host copy-back of the ETL or explicit copy failure
- `D`: restore baseline plus post-restore confirmation

Why this matters:

- every step can be rerun on its own
- every step writes its own summary
- the first failing primitive is visible without reinterpreting the whole lane

The current reference implementation is the CPU idle lane:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\run-cpu-idle-states-runtime-probe.ps1
```

Step wrappers also exist for direct inspection:

- `scripts/vm/run-cpu-idle-states-orchestration-step-a.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-b.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c1.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c2.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c3.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c4.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-d.ps1`

The successful excluded-baseline reference package is:

```text
evidence/files/vm-tooling-staging/cpu-idle-runtime-20260329-015521/summary.json
evidence/files/vm-tooling-staging/cpu-idle-stepwise-20260329-015521/session.json
```

## Procmon And WPR Helpers

Safe Procmon capture:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Tools\Scripts\procmon-safe.ps1 -DurationSeconds 90 -MaxMegabytes 256
```

Manual WPR start:

```cmd
C:\Tools\Scripts\wpr-start-general.cmd
```

Manual WPR stop:

```cmd
C:\Tools\Scripts\wpr-stop.cmd C:\Tools\Perf\capture.etl
```

Open an ETL in WPA:

```cmd
C:\Tools\Scripts\wpa.cmd C:\Tools\Perf\capture.etl
```

## Shell Incidents

If a lane drops the desktop, shell host, input, or app launch path, log it:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\log-vm-incident.ps1 -RecordId system.disable-shortcut-arrow -TestId shortcut-arrow-noarrow-probe -Symptom "Desktop disappeared after Explorer restart"
```

Incidents are tracked in:

- `research\vm-incidents.json`
- `research\evidence-audit.json`

## Cleanup

Host cleanup:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\cleanup-host-validation-artifacts.ps1 -Apply
```

Guest cleanup:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\cleanup-guest-validation-artifacts.ps1 -Apply
```

## Notes

- Historical evidence can still mention older snapshot names. Do not rewrite those records just to normalize naming.
- The canonical baseline may change later, but active runtime scripts should resolve their default snapshot through the shared baseline config instead of hardcoding a snapshot name.
