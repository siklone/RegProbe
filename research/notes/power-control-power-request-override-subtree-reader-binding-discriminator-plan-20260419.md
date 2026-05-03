# Power Request Override Subtree Reader-Binding Discriminator Plan - 2026-04-19

## Purpose

This note turns the narrowed `PowerRequestOverride` reader-binding target set into a concrete debugger decision plan.

The lane no longer needs another generic subtree search. The next sprint should answer a smaller question:

- does the live binding become visible in `PopPowerRequestHandleRequestOverrideQueryResponse`
- or does the real boundary stay on the UMPO message path behind `PopUmpoSendPowerMessage`

This plan exists so the next debugger pass can stop quickly on strong evidence instead of drifting back into a broad symbol hunt.

## Inputs This Plan Assumes

- `research/notes/power-control-power-request-override-subtree-reader-binding-targets-20260419.md`
- `research/notes/power-control-power-request-override-subtree-message-boundary-hypothesis-20260419.md`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.stdout.txt`

Retained discriminator artifacts:

- [power-request-override-reader-binding-discriminator-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-discriminator-20260419.md)
- [power-request-override-reader-binding-discriminator-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-discriminator-20260419.json)
- [power-request-override-handoff-bundle-verification-sweep-20260423.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-handoff-bundle-verification-sweep-20260423.json)

## Primary Commands

Start narrow and preserve the order:

```text
x nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopUmpoSendPowerRequestOverrideQuery
uf nt!PopUmpoSendPowerMessage
x nt!*PowerRequest*Override*
x nt!*Umpo*PowerRequest*
```

If symbol visibility is poor, add:

```text
uf nt!PopPowerRequestCallbackWorker
uf nt!PopPowerRequestUpdateWorkItem
```

These are fallback context helpers, not the primary target.

## Decision Matrix

### Outcome A: direct registry read appears in the response or an immediate callee

Examples:

- `CmQueryValueKey`
- `ZwQueryValueKey`
- `NtQueryValueKey`
- a clearly named `*Reg*` helper with exact subtree or leaf context

Interpretation:

- this is the first strong current-build kernel reader-binding proof
- the lane can stay kernel-side

Next move:

1. capture the exact call chain
2. note whether the path names `Process`, `Service`, or `Driver`
3. compare the observed read/apply path against the proven `powercfg` storage model

### Outcome B: response handler only deserializes payload and updates request state

Examples:

- request-object field writes
- queueing `PopPowerRequestUpdateWorkItem`
- no visible registry API in the response-side path

Interpretation:

- the response side is likely an apply boundary, not the original reader
- the best next lane is still the UMPO message boundary, not a broad registry pass

Next move:

1. inspect `PopUmpoSendPowerMessage` for transport shape
2. identify whether the boundary suggests ALPC / RPC / internal power-service dispatch
3. only then decide whether to escalate to a user-mode power-service lane

### Outcome C: response path exposes leaf-class branching or request-bit semantics without a direct read

Examples:

- explicit handling for process/service/driver classes
- a stable all-request bitmask that matches the observed `7`
- named request-type decoding before queueing work

Interpretation:

- this is not full reader proof
- but it is strong live-consumer evidence that can be compared against the proven storage model

Next move:

1. document the branching or bitmask evidence
2. keep the lane blocked on exact reader binding
3. treat the result as consumer-side corroboration, not promotion proof

### Outcome D: `PopUmpoSendPowerMessage` is the only informative boundary

Examples:

- serialized requester packaging
- message opcode or payload shaping
- no exact registry helper on either the send or response side

Interpretation:

- the cheapest next escalation is message-boundary tracing or user-mode static work
- not Procmon, and not another broad `*PowerRequest*Reg*` sweep

Next move:

1. pivot to the user-mode side of the UMPO boundary
2. prefer a bounded `powrprof` / power-service follow-up over a broad runtime trace
3. keep the kernel lane as context, not as the primary unresolved gap

### Outcome E: the target functions are too thin or mostly wrappers

Examples:

- immediate tail-calls
- thin thunking into unnamed helpers
- mostly setup / cleanup with no material decode logic

Interpretation:

- the current named symbols are acting as wrappers
- the real next task is to identify the first non-wrapper callee that owns the message or response payload

Next move:

1. follow the first meaningful callee only
2. stop once the path leaves the bounded override-response family
3. avoid reopening the whole power-request subsystem

## What Counts As Good Progress

Good progress for the next sprint is not "found more symbols."

Good progress is one of:

- an exact registry read in the override-response lane
- a clear response-side payload apply path
- a concrete UMPO transport boundary that justifies a user-mode follow-up
- a direct consumer-side bitmask or leaf-class decode that can be compared with the observed `7` storage model

## What Does Not Count

These are not sufficient by themselves:

- another broad wildcard symbol list
- another generic `*PowerRequest*Reg*` no-hit result
- more subtree presence proof
- another `powercfg` materialization cycle

Those questions are already closed enough for this lane.

## Stop Conditions

Stop the next sprint early if:

- the response-side pass produces only wrapper logic and no new semantic signal
- the path clearly exits into a user-mode or transport boundary without any better kernel-side discriminator
- the only new output is generic queue/callback plumbing already captured by retained notes

At that point the correct move is to pivot lanes, not to keep widening the same kernel sweep.

## Conclusion

The next debugger sprint should behave like a discriminator run, not a discovery run.

The main job is to decide whether the unresolved live binding is:

- still meaningfully kernel-side in `PopPowerRequestHandleRequestOverrideQueryResponse`
- or already past the UMPO message boundary
