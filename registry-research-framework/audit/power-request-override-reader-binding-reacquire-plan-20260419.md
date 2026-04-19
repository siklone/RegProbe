# PowerRequestOverride Reader-Binding Reacquire Plan - 2026-04-19

- Plan status: `ready`
- Operator posture: `run-narrow-kd-pass`
- Purpose: `Reacquire the missing response-side disassembly and pair it with the UMPO message boundary before widening the lane.`

## Inputs

- `research/notes/power-control-power-request-override-subtree-reader-binding-targets-20260419.md`
- `research/notes/power-control-power-request-override-subtree-message-boundary-hypothesis-20260419.md`
- `research/notes/power-control-power-request-override-subtree-reader-binding-discriminator-plan-20260419.md`
- `research/notes/power-control-power-request-override-subtree-kd-callee-shortlist-20260419.md`
- `registry-research-framework/audit/power-request-override-reader-binding-discriminator-20260419.json`

## Required Reacquire Artifacts

### local-kd-powerrequest-response-reacquire

- Goal: retain stdout for `uf nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- Command file: `registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt`
- Must include:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`

### local-kd-powerrequest-umpo-message-reacquire

- Goal: retain stdout for `uf nt!PopUmpoSendPowerMessage`
- Command file: `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`
- Must include:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`

## Command Order

```text
x nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopUmpoSendPowerMessage
x nt!*PowerRequest*Override*
x nt!*Umpo*PowerRequest*
```

## Good Outcomes

- A direct registry read or exact leaf-specific helper appears in the response-side path.
- The response-side path exposes a clear payload-apply or leaf-class decode boundary.
- The UMPO message path exposes a concrete transport or opcode clue that justifies a bounded user-mode pivot.

## Stop Conditions

- Stop if the response-side reacquire only yields wrapper logic and no new semantic signal.
- Stop if the path clearly exits into a message or user-mode boundary without a better kernel-side discriminator.
- Stop if the only new output is callback or queue plumbing already captured by retained notes.

## Explicit Non-Goals

- Do not reopen a generic `*PowerRequest*Reg*` symbol hunt first.
- Do not rerun another broad subtree runtime capture first.
- Do not spend the next sprint on another `powercfg` materialization cycle.

## Success Definition

- Minimum: both response-side and UMPO message stdout artifacts are retained in-repo.
- Preferred: one reacquired path exposes either a direct registry read, a leaf-class consumer decode, or a clear UMPO pivot boundary.
