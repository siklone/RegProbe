# Execution-Required Live-State and Reader KVM Follow-Up

Date: 2026-04-08
Target binary: `C:\Windows\System32\ntoskrnl.exe`
Probes: `local-kd-allowsystemrequired-20260408a`, `local-kd-allowaudio-20260408a`, `local-kd-powerrequest-reader-20260408a`, `powerrequest-executionrequired-ghidra-20260408a`

## Outcome

- A retained local-KD live-state pass confirmed `nt!PopPowerRequestConvertSystemToExecution = 1`.
- A second retained local-KD live-state pass confirmed `nt!PopPowerRequestActiveAudioEnablesExecutionRequired = 1`.
- `PopPowerRequestHandleExecutionEnablementUpdate` directly reads `PopPowerRequestConvertSystemToExecution` before calling `PopPowerRequestEvaluateExecutionRequiredStatus`.
- `PopPowerRequestEvaluateExecutionRequiredStatus` directly reads `PopPowerRequestActiveAudioEnablesExecutionRequired` and `PopExecutionRequiredTimeout` while evaluating execution-required state.
- The timeout-setting callback remains timeout-specific: `PopPowerRequestExecutionRequiredSettingCallback` writes `PopExecutionRequiredTimeout`, rearms the timer, and then calls `PopPowerRequestHandleExecutionEnablementUpdate`.
- The symbol-seeded Ghidra pass naturally resolved `PopPowerRequestEvaluateExecutionRequiredStatus`, `PopPowerRequestSetExecutionRequiredTimeoutTimer`, and `PopPowerRequestExecutionRequiredSettingCallback` through `PopExecutionRequiredTimeout`, which structurally corroborates the live KD reader path.

## Artifacts

- `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowsystemrequired-20260408a/local-kd.log`
- `evidence/files/vm-tooling-staging/local-kd-allowaudio-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowaudio-20260408a/local-kd.log`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd.log`
- `evidence/files/ghidra/powerrequest-executionrequired-ghidra-20260408a/summary.json`
- `evidence/files/ghidra/powerrequest-executionrequired-ghidra-20260408a/evidence.json`
- `evidence/files/ghidra/powerrequest-executionrequired-ghidra-20260408a/ghidra-matches.md`

## Interpretation

- The execution-required pair is no longer blocked on missing live current-build state: both visible kernel globals are present and set to `1`, matching the repo power notes.
- The pair is also no longer blocked on missing direct reader proof: `PopPowerRequestHandleExecutionEnablementUpdate` and `PopPowerRequestEvaluateExecutionRequiredStatus` are retained current-build readers for the system and audio booleans.
- The timeout callback and Ghidra hits reinforce that the visible family is still callback-, timer-, and in-memory-state driven.
- The remaining blocker is earlier seeding: the retained current-build evidence still does not show whether absent `Control\Power` values are materialized during initialization or whether the modern family stays runtime-driven without a registry-backed seeding path.
