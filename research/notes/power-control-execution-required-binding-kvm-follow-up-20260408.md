# Execution-Required Power-Request Binding KVM Follow-Up

Date: 2026-04-08
Target binary: `C:\Windows\System32\ntoskrnl.exe`
Probes: `local-kd-powerrequest-reglineage-20260408a`, `powerrequest-executionrequired-binding-ghidra-20260408a`

## Outcome

- The retained local-KD wildcard lineage pass returned no visible `nt!*PowerRequest*Reg*` symbols on the current build.
- The same wildcard pass showed that `x nt!*PowerRequest*Setting*` resolves only `PopPowerRequestExecutionRequiredSettingCallback`.
- The execution-required symbol family remains visible through timeout and callback helpers such as `PopPowerRequestExecutionRequiredTimeoutCallback`, `PopPowerRequestSetExecutionRequiredTimeoutTimer`, `PopPowerRequestExecutionRequiredTimeoutWorker`, `PopPowerRequestCallbackExecutionRequired`, and the audio boolean `PopPowerRequestActiveAudioEnablesExecutionRequired`.
- A symbol-seeded Ghidra pass on `PopPowerRequestExecutionRequiredSettingCallback` and `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT` naturally resolved only the timeout-setting callback itself. Ghidra reported zero direct or bounded indirect references to the callback symbol and one resolved GUID reference inside the callback body.
- The decompiled callback is timeout-specific: it validates `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`, expects a 4-byte payload, writes `PopExecutionRequiredTimeout`, rearms the timeout timer, and then calls `PopPowerRequestHandleExecutionEnablementUpdate`.

## Artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/local-kd.log`
- `evidence/raw/ghidra/powerrequest-executionrequired-binding-ghidra-20260408a/summary.json`
- `evidence/raw/ghidra/powerrequest-executionrequired-binding-ghidra-20260408a/evidence.json`
- `evidence/raw/ghidra/powerrequest-executionrequired-binding-ghidra-20260408a/ghidra-matches.md`

## Interpretation

- The visible current-build setting-binding story is narrower than the repo power notes imply for the `Allow*ExecutionRequired*` pair.
- The only naturally resolved setting callback is timeout-specific and keyed to `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`; it does not prove an exact binding site for `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests`.
- Combined with the generic query/store disassembly, this strengthens the current conclusion: the visible execution-required power-request family is callback-, timer-, UMPO-, and in-memory-setting driven, while a `Control\Power` registry seeding path for the pair remains unproven on the current build.
