# PowerRequestOverride Reader-Binding Execution Manifest - 2026-04-19

- Status: `ready`
- Operator posture: `run-narrow-kd-pass`
- Next action: `Run the two local-KD command files, retain stdout/summary/local-kd artifacts for both, then classify the result with the review rubric.`
- Selected count: `2`

## Entries

### local-kd-powerrequest-response-reacquire

- Command file: `registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt`
- Goal: reacquire response-side disassembly for `PopPowerRequestHandleRequestOverrideQueryResponse`
- Required outputs:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`
- Success markers:
  - `REGPROBE_LOCALKD_BEGIN`
  - `PopPowerRequestHandleRequestOverrideQueryResponse`
  - `REGPROBE_LOCALKD_END`

### local-kd-powerrequest-umpo-message-reacquire

- Command file: `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`
- Goal: reacquire UMPO message disassembly for `PopUmpoSendPowerMessage`
- Required outputs:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`
- Success markers:
  - `REGPROBE_LOCALKD_BEGIN`
  - `PopUmpoSendPowerMessage`
  - `REGPROBE_LOCALKD_END`

## Review Inputs

- `registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.md`
- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.md`

## Non-Goals

- Do not reopen a generic `*PowerRequest*Reg*` symbol hunt first.
- Do not rerun a broad subtree runtime capture first.
- Do not spend the next sprint on another `powercfg` materialization cycle.
