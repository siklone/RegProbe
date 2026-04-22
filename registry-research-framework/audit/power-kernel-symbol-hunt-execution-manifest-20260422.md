# Power / Kernel Symbol Hunt Execution Manifest - 2026-04-22

- Status: `ready`
- Operator posture: `run-narrow-kd-pass`
- Purpose: `Reacquire the execution-required init walker, the timeout setting callback path, the execution-required consumer disassembly, and the global timer-resolution symbol surface in one narrow local-KD bundle.`

## Runner

- Path: `scripts/vm-kvm/run-power-kernel-symbol-hunt.py`

Dry-run the planned commands without touching the VM:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py --dry-run
```

Run the four planned passes:

```bash
python3 scripts/vm-kvm/run-power-kernel-symbol-hunt.py
```

## Records Covered

- `power.control.allow-system-required-power-requests`
- `power.control.allow-audio-to-enable-execution-required-power-requests`
- `system.kernel.global-timer-resolution-requests`

## Passes

### execution-required-init-walker

- Output name: `local-kd-execution-required-init-walker-20260422a`
- Command file: `registry-research-framework/audit/execution-required-init-walker-reacquire-local-kd-20260422.txt`
- Goals:
  - confirm the exact execution-required globals are still symbolized
  - reacquire the unlabeled INIT walker at `0x140C48AB8`
  - retain the two wrapper callers at `0x140C483EF` and `0x140C48414`
- Expected KD markers:
  - `PopPowerRequestConvertSystemToExecution`
  - `PopPowerRequestActiveAudioEnablesExecutionRequired`
  - `0x140C48AB8`

### execution-required-consumers

- Output name: `local-kd-execution-required-consumers-20260422a`
- Command file: `registry-research-framework/audit/execution-required-consumers-reacquire-local-kd-20260422.txt`
- Goals:
  - reacquire the exact consumer-side disassembly
  - verify the live timeout global alongside the consumer chain
- Expected KD markers:
  - `PopPowerRequestHandleExecutionEnablementUpdate`
  - `PopPowerRequestCallbackExecutionRequired`
  - `PopPowerRequestEvaluateExecutionRequiredStatus`
  - `PopExecutionRequiredTimeout`

### execution-required-setting-callback

- Output name: `local-kd-execution-required-setting-callback-20260422a`
- Command file: `registry-research-framework/audit/execution-required-setting-callback-reacquire-local-kd-20260422.txt`
- Goals:
  - reacquire the exact timeout-setting callback that writes `PopExecutionRequiredTimeout`
  - keep the `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT` gate visible in the same retained pass
  - separate timeout-setting semantics from the unresolved `Allow*ExecutionRequired*` boolean seeding path
- Expected KD markers:
  - `PopPowerRequestExecutionRequiredSettingCallback`
  - `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
  - `PopPowerRequestSetExecutionRequiredTimeoutTimer`
  - `PopExecutionRequiredTimeout`

### global-timer-resolution-reader

- Output name: `local-kd-global-timer-resolution-reader-20260422a`
- Command file: `registry-research-framework/audit/global-timer-resolution-reader-reacquire-local-kd-20260422.txt`
- Goals:
  - reacquire the exact `KiGlobalTimerResolutionRequests` symbol and live value
  - enumerate nearby timer-resolution symbol surface before spending another runtime sprint
- Expected KD markers:
  - `KiGlobalTimerResolutionRequests`
  - `*GlobalTimer*Resolution*`
  - `*TimerResolution*`

## Good Outcomes

- The execution-required init walker still resolves cleanly enough to keep the seeding-path hunt narrow.
- The setting-callback pass still shows the timeout-only branch clearly enough that we do not confuse it with the boolean seeding pair.
- The execution-required consumer trio still exposes the same live reader chain.
- The timer-resolution pass produces a bounded symbol shortlist that justifies a narrower next static step.

## Stop Conditions

- Stop if the init-walker pass no longer retains the known addresses or symbol names.
- Stop if the consumer pass regresses into missing symbols or wrapper-only output.
- Stop if the global timer pass only yields the exact same broad symbol fog without a narrower reader clue.

## Explicit Non-Goals

- Do not reopen another broad mega-trigger runtime replay for the execution-required pair.
- Do not reopen another broad subtree replay for `GlobalTimerResolutionRequests`.
- Do not widen into generic `*Power*` or generic `*Request*` symbol hunting before checking the retained narrow targets above.
