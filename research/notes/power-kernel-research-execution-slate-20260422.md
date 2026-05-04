# Power / Kernel Research Execution Slate

Date: 2026-04-22

This note is the pivot back to substantive registry research work. It is intentionally about exact keys, exact values, what we already learned, which lanes burned time without yielding a reader binding, and what the next concrete attempt is.

## 1. `AllowAudioToEnableExecutionRequiredPowerRequests`

- Candidate: `power.control.allow-audio-to-enable-execution-required-power-requests`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Current strongest facts:
  - Repo docs say default is `1`.
  - Clean baseline shows the value is absent.
  - Current-build KD resolved the backing global and showed live value `1`.
  - Current-build INIT-table work tied the registry name to the same target global.
  - Wave 4 ETW stackwalk plus grouped Ghidra follow-up captured an exact runtime query lane through `RegGetValueW` / `NtQueryValueKey`.
- What already failed or stayed unresolved:
  - No primary Microsoft page names this exact audio-specific internal value.
  - We still do not have a named current-build boot/init seeding caller.
- Next attempts:
  - Re-read effective ETW config before reopening:

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config
```

  - If we explicitly reopen the hold, run the full include-holds ETW capture:

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run
```

  - In parallel, keep the static lane narrow: resolve the exact boot/init writer or seeding caller rather than repeating broad runtime replay.
  - Practical target symbols/functions: `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`, the unlabeled INIT walker around the retained `0x140C48AB8` path, and any setter/copy site fed by that descriptor row.

## 2. `AllowSystemRequiredPowerRequests`

- Candidate: `power.control.allow-system-required-power-requests`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `AllowSystemRequiredPowerRequests`
- Current strongest facts:
  - Repo docs say default is `1`.
  - Clean baseline shows the value is absent.
  - Microsoft documents the public hidden `SYSTEMREQUIRED` policy family and `0/1` semantics.
  - KD resolved live current-build `nt!PopPowerRequestConvertSystemToExecution = 1`.
  - INIT-table work ties the exact registry name to the same target global.
  - Wave 4 ETW stackwalk captured the runtime query path.
- What already failed or stayed unresolved:
  - We still lack the exact current-build boot/init seeding caller.
  - The public policy docs explain semantics, but not the internal `Control\Power` seeding path.
- Next attempts:

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run
```

  - Static focus is tighter than “search the whole binary”: follow writes/loads touching `nt!PopPowerRequestConvertSystemToExecution` and the descriptor-table row that binds `AllowSystemRequiredPowerRequests`.

## 3. `PowerRequestOverride` subtree

- Candidate: `power.control.power-request-override-subtree`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Values / leaves:
  - `Process`
  - `Service`
  - `Driver`
  - subtree root metadata such as `RuleCount`, `ENABLED`, `DISABLED`
- Current strongest facts:
  - Baseline subtree presence is proven.
  - Runtime path hits show `svchost.exe` touching root plus `Driver`, `Process`, and `Service`.
  - Wildcard KD surfaced current-build override-family symbols including `PopPowerRequestHandleRequestOverrideQueryResponse` and `PopUmpoSendPowerRequestOverrideQuery`.
  - `powercfg /requestsoverride` was proven to materialize and remove reversible `Process`, `Service`, and `Driver` leaves.
  - ETW stackwalk captured exact subtree root and control-value reads.
- What already failed or stayed unresolved:
  - Exact live reader binding is still not named.
  - Static context is adjacent and strong, but still not leaf-specific enough for promotion.
  - No supported app-surface mapping yet.
- Next attempts:
  - Reproduce leaf writes deterministically with the documented surface:

```powershell
powercfg /requestsoverride PROCESS RegProbeOverrideProof.exe DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride SERVICE Audiosrv DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride DRIVER ACPI DISPLAY SYSTEM AWAYMODE
powercfg /requestsoverride PROCESS RegProbeOverrideProof.exe
powercfg /requestsoverride SERVICE Audiosrv
powercfg /requestsoverride DRIVER ACPI
```

  - While doing that, keep KD / static focus on the exact message boundary the blocker already points at:
    - `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
    - `nt!PopUmpoSendPowerMessage`
    - `nt!PopUmpoSendPowerRequestOverrideQuery`
  - The goal is not “prove the subtree exists” again; it is “name the reader/consumer that turns those leaves into behavior”.

## 4. `GlobalTimerResolutionRequests`

- Candidate: `system.kernel.global-timer-resolution-requests`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Value: `GlobalTimerResolutionRequests`
- Current strongest facts:
  - Repo docs say default is `0`.
  - Clean baseline shows the value is absent.
  - Current-build string hit exists in `ntoskrnl.exe`.
  - KD resolved live `KiGlobalTimerResolutionRequests = 0`.
  - INIT descriptor binding was found for the same global.
  - 2026-04-18 runtime sprint already ran ETW, Procmon, and WPR/QGA lanes; exact value hit is still missing.
- What already failed or stayed unresolved:
  - Broad runtime capture keeps yielding subtree-only or helper-query noise.
  - No primary Microsoft page names this exact internal registry seed.
- Next attempts:
  - Do not spend another pass on broad replay until there is a narrower pivot.
  - Preferred next step is a narrow static/KD pivot from the bound global outward:
    - find exact conditional read site for `KiGlobalTimerResolutionRequests`
    - identify whether the read is boot/init only, policy-refresh driven, or lazy query-time
  - Only rerun runtime capture if we can tie it to a concrete trigger that mutates global timer-resolution policy state instead of replaying generic power/timer activity.

## 5. `PowerWatchdog*TimeoutMsec` family

- Candidate: `power.control.power-watchdog-timeout-cluster`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Values in scope:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec`
  - adjacent `PowerWatchdog*TimeoutMsec` siblings from repo docs
- Current strongest facts:
  - Repo docs carry defaults.
  - Clean baseline shows all values absent.
  - Broad current-build string work returned no useful symbol or string pivot.
- What already failed or stayed unresolved:
  - No current-build string hit.
  - No runtime read proof.
  - No primary Microsoft page for the internal family.
- Next attempts:
  - This is a true static-RE lane, not a runtime lane right now.
  - Proposed narrow Ghidra command:

```powershell
pwsh -File "registry-research-framework/tools/ghidra-headless-analyze.ps1" -TargetBinary "ntoskrnl.exe" -OutputName "ghidra-power-watchdog-timeout-cluster-20260422" -Patterns "PowerWatchdogDrvSetMonitorTimeoutMsec"
```

  - If that produces no bounded reader/writer, widen only to immediate siblings, not the whole power family.

## 6. `HiberFileSizePercent`

- Candidate: `power.control.hiber-file-size-percent`
- Key: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value: `HiberFileSizePercent`
- Current strongest facts:
  - This one is materially further along than the others.
  - Repo docs, baseline, KD, reboot observation, and a normalized WPR boot `QueryValue` event are already in hand.
  - The remaining blocker is product posture, not missing runtime-read evidence.
- What that means:
  - This is not a high-value “trial and error” target right now.
  - It is research-complete enough for a research-only hold and should not steal cycles from the unresolved reader-binding lanes above.

## Immediate priority order

1. `power.control.power-request-override-subtree`
   Goal: exact live reader/consumer binding.
2. `power.control.allow-system-required-power-requests`
   Goal: exact current-build seeding caller.
3. `power.control.allow-audio-to-enable-execution-required-power-requests`
   Goal: exact current-build seeding caller plus final semantics boundary.
4. `system.kernel.global-timer-resolution-requests`
   Goal: stop broad no-hit replays, find a real read pivot.
5. `power.control.power-watchdog-timeout-cluster`
   Goal: first current-build pivot, likely via Ghidra/static RE.

## Bottom line

The real research lane is not blocked by missing ideas; it is blocked by missing exact reader/writer naming for a small set of power/kernel values. The most useful next work is not more parser hygiene and not more generic “run everything” sweeps. It is narrow candidate-by-candidate work on:

- exact key / value bindings,
- exact current-build consumer or seeding caller,
- and only then another runtime replay if the trigger is newly justified.
