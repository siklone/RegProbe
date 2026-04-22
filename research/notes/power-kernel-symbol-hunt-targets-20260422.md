# Power / Kernel Symbol Hunt Targets

Date: 2026-04-22

This note is the symbol-and-address level follow-up to the execution slate and trial matrix. It is meant to answer a narrower question: if we sit down with KD, Ghidra, or objdump right now, which exact globals, descriptor rows, routines, and commands deserve the next hour?

## 1. Execution-required pair: exact binding artifacts already in hand

### Exact registry names

- `AllowSystemRequiredPowerRequests`
- `AllowAudioToEnableExecutionRequiredPowerRequests`

### Exact string offsets from current-build `ntoskrnl.exe`

- `AllowAudioToEnableExecutionRequiredPowerRequests` -> file offset `0xBF6530`
- `AllowSystemRequiredPowerRequests` -> file offset `0xBF65E0`

### Exact descriptor rows already recovered

- `Power` + `AllowSystemRequiredPowerRequests` -> `0x140FD7114`
- `Power` + `AllowAudioToEnableExecutionRequiredPowerRequests` -> `0x140FD71A0`

### Exact globals already aligned

- `0x140FD7114` <-> `nt!PopPowerRequestConvertSystemToExecution`
- `0x140FD71A0` <-> `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`
- `0x140FD70B0` <-> `nt!PopExecutionRequiredTimeout`

### Exact INIT walker already recovered

- unlabeled current-build `INIT` walker at `0x140C48AB8`
- wrapper callers:
  - `0x140C483EF` with `r8b=0`
  - `0x140C48414` with `r8b=1`

### Exact row instances called out in prior work

- `0x140C76250`: `Power` + `AllowSystemRequiredPowerRequests` -> `0x140FD7114`
- `0x140C76280`: `Power` + `AllowAudioToEnableExecutionRequiredPowerRequests` -> `0x140FD71A0`

## 2. Execution-required pair: strongest consumer-side routines

### System-required side

- `nt!PopPowerRequestHandleExecutionEnablementUpdate`
- `nt!PopPowerRequestCallbackExecutionRequired`
- `nt!PopPowerRequestEvaluateExecutionRequiredStatus`

The point here is not to rediscover that the value matters. We already know it does. The point is to walk backward from these consumers toward the seeding/copy site fed by the descriptor table.

### Audio-specific side

- `nt!PopPowerRequestEvaluateExecutionRequiredStatus`
- `nt!PopPowerRequestActiveAudioEnablesExecutionRequired`

This side should be treated as a sibling of the system-required lane, not as a separate broad mystery. The same descriptor machinery already binds the value name to a concrete target global.

## 3. Best next debugger commands for the execution-required pair

### KD symbol reacquire

```text
x nt!PopPowerRequestConvertSystemToExecution
x nt!PopPowerRequestActiveAudioEnablesExecutionRequired
x nt!PopPowerRequestHandleExecutionEnablementUpdate
x nt!PopPowerRequestCallbackExecutionRequired
x nt!PopPowerRequestEvaluateExecutionRequiredStatus
uf nt!PopPowerRequestHandleExecutionEnablementUpdate
uf nt!PopPowerRequestCallbackExecutionRequired
uf nt!PopPowerRequestEvaluateExecutionRequiredStatus
```

### KD global-state sanity check

```text
dd nt!PopPowerRequestConvertSystemToExecution L1
dd nt!PopPowerRequestActiveAudioEnablesExecutionRequired L1
dd nt!PopExecutionRequiredTimeout L1
```

### Host-side static hunt around the known INIT walker

- start from `0x140C48AB8`
- keep any xref or relocation walk constrained to:
  - the `0x140C72E30` table load
  - the two wrapper callers at `0x140C483EF` / `0x140C48414`
  - the copy helper `0x140C4CB20`

### What counts as success

- naming the seeding routine that consumes the descriptor row
- or proving that the recovered unlabeled `INIT` walker is itself the decisive seeding routine

## 4. PowerRequestOverride subtree: strongest unresolved boundary

### Exact subtree

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- leaf families:
  - `Process`
  - `Service`
  - `Driver`

### Exact symbols already surfaced

- `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- `nt!PopPowerRequestOverrideInitialize`
- `nt!PopUmpoSendPowerRequestOverrideQuery`
- `nt!PopUmpoSendPowerRequestOverrideCleanup`
- `nt!PopUmpoSendPowerMessage`

### Strongest existing interpretation

- runtime storage proof exists
- reversible `powercfg /requestsoverride` writes exist
- the remaining question is whether the clearest reader binding appears:
  - in `PopPowerRequestHandleRequestOverrideQueryResponse`
  - or behind the UMPO transport boundary in `PopUmpoSendPowerMessage`

## 5. Best next debugger commands for PowerRequestOverride

```text
x nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopUmpoSendPowerMessage
uf nt!PopUmpoSendPowerRequestOverrideQuery
uf nt!PopPowerRequestOverrideInitialize
```

### What to look for

- payload shaping
- message opcode or transport discriminator
- leaf-name materialization
- any point where override response data becomes semantically meaningful rather than just copied/queued

### What does not count as progress

- another proof that the subtree exists
- another proof that `powercfg /requestsoverride` writes reversible leaves
- another generic wildcard symbol list

## 6. GlobalTimerResolutionRequests hunt target

### Exact registry value

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\GlobalTimerResolutionRequests`

### Exact global already aligned

- `nt!KiGlobalTimerResolutionRequests = 0`

### Strongest current evidence

- repo docs default
- baseline absence
- exact current-build string hit
- KD live state
- INIT descriptor binding
- repeated runtime no-hit lanes, including the clean 2026-04-18 WPR/QGA rerun

### Best next move

- do not spend the next hour replaying ETW/Procmon/WPR
- spend it isolating a real reader or seeding caller for `KiGlobalTimerResolutionRequests`

## 7. PowerWatchdog timeout family hunt target

### Representative value

- `PowerWatchdogDrvSetMonitorTimeoutMsec`

### Why it is different

- unlike the execution-required pair, we do not yet have a current-build binding row or global alignment
- unlike PowerRequestOverride, we do not yet have a runtime transport boundary worth chasing

### Best next move

- tight Ghidra/static search centered on:
  - `PowerWatchdogDrvSetMonitorTimeoutMsec`
  - immediate `PowerWatchdog*TimeoutMsec` siblings only

## 8. Practical research order for the next live session

1. Reacquire exact disassembly for `PopPowerRequestHandleRequestOverrideQueryResponse` and `PopUmpoSendPowerMessage`.
2. Reacquire exact disassembly for the execution-required consumer trio and walk backward toward the descriptor-fed seeding site.
3. Only after those two passes, spend time on `GlobalTimerResolutionRequests` reader discovery.
4. Treat the watchdog timeout family as the clean “first pivot” static lane, not an urgent runtime lane.

## Bottom line

The next real research wins will not come from broader replay. They will come from naming a few exact routines:

- the execution-required seeding routine behind the `0x140C48AB8` descriptor walker
- the first informative override boundary between `PopPowerRequestHandleRequestOverrideQueryResponse` and `PopUmpoSendPowerMessage`
- and a first real reader/initializer pivot for `KiGlobalTimerResolutionRequests`
