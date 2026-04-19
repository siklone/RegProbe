# PowerRequestOverride Reader-Binding Discriminator - 2026-04-19

## Purpose

This audit freezes the next narrow debugger run for `power.control.power-request-override-subtree`.

The lane no longer needs more proof that the subtree can store stable override leaves. That question is already strong enough through the retained root/runtime evidence plus the 2026-04-18 `powercfg /requestsoverride` proofs for `Process`, `Service`, and `Driver`.

The remaining job is narrower:

- determine whether `PopPowerRequestHandleRequestOverrideQueryResponse` exposes the live binding
- or confirm that the real boundary has already shifted to the UMPO message lane behind `PopUmpoSendPowerMessage`

## Retained Inputs

- `research/notes/power-control-power-request-override-subtree-reader-binding-targets-20260419.md`
- `research/notes/power-control-power-request-override-subtree-message-boundary-hypothesis-20260419.md`
- `research/notes/power-control-power-request-override-subtree-reader-binding-discriminator-plan-20260419.md`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.stdout.txt`

## Primary Commands

```text
x nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopUmpoSendPowerRequestOverrideQuery
uf nt!PopUmpoSendPowerMessage
x nt!*PowerRequest*Override*
x nt!*Umpo*PowerRequest*
```

Fallback context only:

```text
uf nt!PopPowerRequestCallbackWorker
uf nt!PopPowerRequestUpdateWorkItem
```

## Decision Matrix

### 1. Direct registry read appears

- Signal: `CmQueryValueKey`, `ZwQueryValueKey`, `NtQueryValueKey`, or an exact leaf-specific `*Reg*` helper appears in the response-side path.
- Meaning: this is the first strong current-build kernel reader-binding proof.
- Next move: stay kernel-side, capture the exact call chain, and compare any named `Process` / `Service` / `Driver` reads against the proven `powercfg` storage model.

### 2. Response side only applies request state

- Signal: the path only updates request state or queues work, with no visible registry API.
- Meaning: the response side is likely an apply boundary, not the original reader.
- Next move: inspect `PopUmpoSendPowerMessage` and prepare a bounded user-mode power-service follow-up if the message boundary is the strongest remaining discriminator.

### 3. Consumer semantics appear without a direct read

- Signal: the path exposes leaf-class branching or a stable request-bit model, including something that lines up with the observed `7`.
- Meaning: this is strong live-consumer corroboration, but not a full reader-binding proof.
- Next move: document the semantics, keep the record blocked on exact binding, and avoid overclaiming promotion readiness.

### 4. UMPO boundary is the best new signal

- Signal: payload shaping, message transport, or a clear boundary around `PopUmpoSendPowerMessage` is more informative than anything in the response-side code.
- Meaning: the unresolved live binding has effectively narrowed to the UMPO boundary.
- Next move: pivot to a bounded `powrprof` / power-service investigation instead of widening the kernel or Procmon lane.

### 5. The named targets are wrappers only

- Signal: the targets are thin wrappers, tail-calls, or setup/cleanup helpers with no material decode logic.
- Meaning: the next task is only to follow the first non-wrapper callee that still belongs to the bounded override-response family.
- Next move: stop once the path broadens into generic power-request plumbing.

## Stop Conditions

- Stop if the response-side pass produces only wrapper logic and no new semantic signal.
- Stop if the path clearly exits into a user-mode or transport boundary without any better kernel-side discriminator.
- Stop if the only new output is queue or callback plumbing already captured by retained notes.

## Explicit Non-Goals

Do not spend the next sprint on:

- another broad `PowerRequestOverride` subtree search
- another generic `*PowerRequest*Reg*` no-hit pass
- another `powercfg` materialization cycle

Those questions are already closed enough for this lane.

## Conclusion

Treat the next debugger pass as a discriminator run, not a discovery run.

If the kernel-side response path stays weak, the correct next move is a bounded UMPO or power-service pivot.
