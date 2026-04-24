# Power / Kernel Trial Matrix

Date: 2026-04-22

This note is the compact trial-and-error matrix for the hottest unresolved power/kernel candidates. It is intentionally narrower than the execution slate: it captures what we already tried, what actually came back, what counted as a dead end or weak lane, and what the next justified experiment is.

## `power.control.allow-system-required-power-requests`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `AllowSystemRequiredPowerRequests`

### What already worked

- Repo docs plus Microsoft hidden-policy docs agree on semantics.
- Clean baseline shows the value is absent.
- Current-build KD resolved the target global: `nt!PopPowerRequestConvertSystemToExecution = 1`.
- Current-build INIT-table and init-walker work tied the exact registry name to the same global.
- Wave 4 ETW stackwalk resolved an exact runtime query path:
  - `reg.exe!QueryValue`
  - `kernelbase.dll!RegGetValueW`
  - `ntdll.dll!NtQueryValueKey`
  - `ntoskrnl.exe!NtQueryValueKey`
  - `ntoskrnl.exe!EtwpTraceRegistry`

### What failed or stayed weak

- Broad mega-trigger retries were exhausted and kept ending `aborted-recovered` with zero exact query hits.
- QGA/WPR boot lane produced a retained target-specific no-hit.
- The current-build seeding path is still inferred, not named.

### Why it is still blocked

- Exact runtime query proof exists now, but exact boot/init seeding caller does not.
- The real blocker is no longer “does the value ever get queried?” It is “what current-build routine seeds or consumes it at init time?”

### Next narrow experiment

- Static/KD follow-through from the known target global and bound descriptor row:
  - start at `nt!PopPowerRequestConvertSystemToExecution`
  - walk backward to the exact copy/set site fed by the retained `Power` descriptor row
  - prefer naming the seeding caller over replaying another broad runtime trigger

## `power.control.allow-audio-to-enable-execution-required-power-requests`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `AllowAudioToEnableExecutionRequiredPowerRequests`

### What already worked

- Repo docs carry the default.
- Clean baseline shows the value is absent.
- KD resolved the target global: `nt!PopPowerRequestActiveAudioEnablesExecutionRequired = 1`.
- INIT-table and init-walker recovery tied the exact value name to that global.
- Tooling now has a dedicated ETW profile for this candidate: `execution-required-audio-stackwalk-v1`.

### What failed or stayed weak

- Old broad ETW / reboot-style lanes stayed weak or no-hit.
- The broad mega-trigger family is no longer informative here.
- We still do not have a primary Microsoft page that names this exact audio-specific value.

### Why it is still blocked

- Unlike the system-required sibling, this one still lacks a retained exact runtime query proof on the focused lane.
- It also lacks both a named seeding caller and a primary current-build Microsoft doc for the exact internal setting.

### Next narrow experiment

- Use the dedicated Wave 4 profile, not the old generic plan:

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run
```

- Success condition:
  - exact value-level hit for `AllowAudioToEnableExecutionRequiredPowerRequests`
  - caller stack resolved through user-mode and kernel-mode query surfaces
- If that still no-hits, stop replaying and go back to naming the seeding path.

## `power.control.power-request-override-subtree`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Values: subtree root plus `Process`, `Service`, `Driver`

### What already worked

- Baseline root dump proves the subtree exists.
- Runtime path trace shows 15 subtree hits from `svchost.exe`.
- Local KD surfaced override-family symbols:
  - `PopPowerRequestHandleRequestOverrideQueryResponse`
  - `PopPowerRequestOverrideInitialize`
  - `PopUmpoSendPowerRequestOverrideQuery`
  - `PopUmpoSendPowerRequestOverrideCleanup`
- `powercfg /requestsoverride` was proven to create and remove reversible Process / Service / Driver leaves.
- ETW stackwalk captured root and control-value reads.

### What failed or stayed weak

- Runtime hits are subtree-level, not a bounded leaf consumer proof.
- Static context is strong but still adjacent; it has not named the exact leaf reader.
- No supported app-facing apply mapping yet.

### Why it is still blocked

- We have storage proof and reversible write proof, but not the exact live consumer binding.
- The question is no longer “is this subtree real?” The question is “which current-build path turns these leaves into behavior?”

### Next narrow experiment

- Keep runtime writes deterministic with the documented surface:

```powershell
powercfg /requestsoverride PROCESS RegProbeOverrideProof.exe DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride SERVICE Audiosrv DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride DRIVER ACPI DISPLAY SYSTEM AWAYMODE
```

- Pair that with leaf-consumer static/KD focus at:
  - `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
  - `nt!PopUmpoSendPowerMessage`
  - `nt!PopUmpoSendPowerRequestOverrideQuery`

## `system.kernel.global-timer-resolution-requests`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value: `GlobalTimerResolutionRequests`

### What already worked

- Repo docs carry the default.
- Baseline shows the value is absent.
- Current-build string hit exists in `ntoskrnl.exe`.
- KD resolved `nt!KiGlobalTimerResolutionRequests = 0`.
- INIT descriptor binding was found for the same global.
- 2026-04-18 runtime sprint completed ETW, Procmon, and WPR/QGA end-to-end.

### What failed or stayed weak

- ETW retained only helper-query noise.
- Procmon failed on `SaveAs`.
- Clean WPR/QGA rerun retained subtree activity but zero exact `GlobalTimerResolutionRequests` hits.

### Why it is still blocked

- We have good static evidence and better negative runtime evidence, but still no exact current-build value-level read.
- Broad runtime replay has already been shown to be low-yield here.

### Next narrow experiment

- Do not rerun broad ETW/Procmon/WPR just to reconfirm the same no-hit.
- Next acceptable pass must start from the known global and find the concrete read site or trigger family before any new trace attempt.

## `power.control.power-watchdog-timeout-cluster`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Representative value: `PowerWatchdogDrvSetMonitorTimeoutMsec`

### What already worked

- Repo docs carry defaults.
- Baseline shows the family is absent.

### What failed or stayed weak

- Broad current-build string work gave no useful symbol/string pivot.
- No exact runtime-read evidence exists.
- No primary Microsoft page names the internal family.

### Why it is still blocked

- This is still pre-pivot. We do not yet know the exact reader, writer, or initializer.

### Next narrow experiment

```powershell
pwsh -File "registry-research-framework/tools/ghidra-headless-analyze.ps1" -TargetBinary "ntoskrnl.exe" -OutputName "ghidra-power-watchdog-timeout-cluster-20260422" -Patterns "PowerWatchdogDrvSetMonitorTimeoutMsec"
```

- If that no-hits, expand only to immediate siblings; do not widen to the full power family.

## `power.control.hiber-file-size-percent`

- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `HiberFileSizePercent`

### What already worked

- Repo docs, baseline, exact string hit, KD symbol family, live KD value, reboot observation, and exact WPR `QueryValue` event all exist.

### What failed or stayed weak

- Early lanes lacked direct runtime-read proof, but that gap is now effectively closed.

### Why it is not the hottest target anymore

- The blocker is product posture: this is still a raw power-manager value and intentionally research-only.
- This lane no longer needs more trial-and-error before the harder unresolved reader-binding candidates above.

## Bottom line

The main wasted motion to avoid now is broad replay:

- not another generic mega-trigger for the execution-required pair
- not another subtree-only replay for `GlobalTimerResolutionRequests`
- not another “does PowerRequestOverride exist?” pass

The next useful work is narrower:

- exact seeding caller for the execution-required pair
- exact live consumer for `PowerRequestOverride`
- first current-build pivot for the watchdog-timeout family
