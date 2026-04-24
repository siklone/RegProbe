# Power / Kernel Symbol Hunt Review Rubric - 2026-04-22

Bu rubric, `run-power-kernel-symbol-hunt.py` ile uretilen dort local-KD artifact'i review etmek icindir:

- `local-kd-execution-required-init-walker-20260422a`
- `local-kd-execution-required-consumers-20260422a`
- `local-kd-execution-required-setting-callback-20260422a`
- `local-kd-global-timer-resolution-reader-20260422a`

## Required Markers

### execution-required-init-walker

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `PopPowerRequestConvertSystemToExecution`
- `PopPowerRequestActiveAudioEnablesExecutionRequired`
- `0x140C48AB8`
- `REGPROBE_LOCALKD_END`

Strong markers:

- `0x140C483EF`
- `0x140C48414`
- retained wrapper split that keeps the boolean-seeding lane narrow

Weak markers:

- only isolated symbol listing without useful `u` body

### execution-required-consumers

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `PopPowerRequestHandleExecutionEnablementUpdate`
- `PopPowerRequestCallbackExecutionRequired`
- `PopPowerRequestEvaluateExecutionRequiredStatus`
- `PopExecutionRequiredTimeout`
- `REGPROBE_LOCALKD_END`

Strong markers:

- consumer-side branching that still matches the known execution-required family
- useful `uf` bodies instead of wrapper-only listings

Weak markers:

- only symbol names with no meaningful body

### execution-required-setting-callback

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `PopPowerRequestExecutionRequiredSettingCallback`
- `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
- `PopExecutionRequiredTimeout`
- `REGPROBE_LOCALKD_END`

Strong markers:

- clear timeout-only branch
- visible `PopPowerRequestSetExecutionRequiredTimeoutTimer`
- clear separation between timeout-setting path and unresolved `Allow*ExecutionRequired*` boolean seeding path

Weak markers:

- callback symbol only, without a useful `uf` body

### global-timer-resolution-reader

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `KiGlobalTimerResolutionRequests`
- `REGPROBE_LOCALKD_END`

Strong markers:

- bounded nearby timer-resolution shortlist
- helper/caller names that are more specific than generic wildcard fog

Weak markers:

- only broad `*TimerResolution*` surface with no better discriminator

## Choose Exactly One Outcome

- `execution-required-seeding-retained`
- `timeout-branch-separated`
- `timer-anchor-retained-without-reader`
- `symbol-regression-or-wrapper-fog`

## Outcome Mapping

### execution-required-seeding-retained

Pick this when the init-walker and consumer passes still expose the known execution-required globals, wrappers, and consumer chain clearly enough to preserve the current narrow seeding hypothesis.

Next move: keep the lane on the retained init-walker hypothesis and avoid widening into another generic runtime replay.

### timeout-branch-separated

Pick this when the setting-callback pass cleanly proves the timeout-setting path again, which lets the team avoid confusing timeout semantics with the unresolved `Allow*ExecutionRequired*` boolean seed path.

Next move: treat timeout behavior as corroborated and keep the boolean-seeding question isolated.

### timer-anchor-retained-without-reader

Pick this when the global timer pass still retains `KiGlobalTimerResolutionRequests`, but does not produce a stronger helper/caller shortlist.

Next move: keep the lane `active-narrow`; do not reopen broad runtime work yet.

### symbol-regression-or-wrapper-fog

Pick this when one or more required passes lose their retained anchors, regress into missing symbols, or only keep wrapper/wildcard fog.

Next move: stop widening the lane and reacquire the exact anchor before making new claims.

## Red Flags

- missing `REGPROBE_LOCALKD_BEGIN` or `REGPROBE_LOCALKD_END`
- exact retained symbol missing from its required pass
- only wildcard listings with no useful `u` or `uf` body
- timeout callback being misread as boolean-seeding proof

## Non-Goals

- another broad ETW/Procmon/WPR replay is not progress for this bundle by itself
- generic `*Power*` or `*TimerResolution*` symbol spray is not progress
- timeout callback reconfirmation alone is not proof of `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests`
