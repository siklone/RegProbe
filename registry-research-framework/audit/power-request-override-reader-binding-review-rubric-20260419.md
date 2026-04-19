# PowerRequestOverride Reader-Binding Review Rubric - 2026-04-19

This rubric is for reviewing the next two reacquired local-KD artifacts:

- `local-kd-powerrequest-response-reacquire`
- `local-kd-powerrequest-umpo-message-reacquire`

## Required Markers

### Response-side artifact

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `PopPowerRequestHandleRequestOverrideQueryResponse`
- `REGPROBE_LOCALKD_END`

Strong markers:

- `CmQueryValueKey`
- `ZwQueryValueKey`
- `NtQueryValueKey`
- `Process`
- `Service`
- `Driver`

Weak markers:

- `PopPowerRequestUpdateWorkItem`
- `PopPowerRequestCallbackWorker`
- `PopPowerRequestCallbackExecutionRequired`

### UMPO message artifact

Must contain:

- `REGPROBE_LOCALKD_BEGIN`
- `PopUmpoSendPowerMessage`
- `REGPROBE_LOCALKD_END`

Strong markers:

- `ALPC`
- `rpc`
- `message`
- `opcode`
- `requester`

Weak markers:

- `ExAllocatePool2`
- `ExFreePoolWithTag`
- `PoStoreRequester`

## Outcome Mapping

### direct-registry-read

Pick this when the response-side path exposes `CmQueryValueKey`, `ZwQueryValueKey`, `NtQueryValueKey`, or a clearly leaf-specific `*Reg*` helper.

Next move: stay kernel-side and capture the exact read/apply chain.

### consumer-semantics-without-read

Pick this when the response-side path shows `Process` / `Service` / `Driver` branching or stable request-bit semantics without an exact read helper.

Next move: document the consumer semantics, but keep the record blocked on exact reader binding.

### umpo-boundary-is-best-signal

Pick this when the strongest new clue comes from `PopUmpoSendPowerMessage` transport or payload handling instead of the response-side path.

Next move: pivot to a bounded power-service or `powrprof` follow-up.

### wrapper-only-path

Pick this when both reacquired paths are mostly wrappers, allocator/requester scaffolding, or queue plumbing.

Next move: follow only the first non-wrapper callee and stop if the family broadens.

## Red Flags

- Missing `REGPROBE_LOCALKD_BEGIN` or `REGPROBE_LOCALKD_END`
- Missing stdout for one of the two required reacquire artifacts
- Only wildcard symbol output retained, with no real `uf` body

## Non-Goals

- Another `powercfg` materialization cycle is not progress for this lane.
- Generic callback or queue plumbing alone is not reader-binding proof.
- A broad `*PowerRequest*Reg*` hunt should not happen before these two reacquired artifacts are reviewed.
