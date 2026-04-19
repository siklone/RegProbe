# Power Request Override Subtree Message-Boundary Hypothesis - 2026-04-19

## Purpose

This note tightens the remaining blocker for `power.control.power-request-override-subtree` using only already-retained local-KD artifacts.

The question is no longer whether the subtree stores real leaf values. That is already proven by the 2026-04-18 `Process`, `Service`, and `Driver` powercfg proofs.

The remaining question is where the live current-build reader or consumer boundary actually sits.

## Reviewed Retained Artifacts

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a-summary.json`

## Observed Facts

### 1. The retained reader lane did not surface `CmQueryValueKey`

The retained reader summary is explicitly keyed to `nt!CmQueryValueKey`, but the summary reports `query_symbol_seen: false`.

The paired retained stdout for:

- `PopPowerRequestEvaluateExecutionRequiredStatus`
- `PopPowerRequestHandleExecutionEnablementUpdate`
- `PopPowerRequestExecutionRequiredSettingCallback`
- `PopPowerRequestCallbackExecutionRequired`

contains no `CmQueryValueKey` hit at all.

Interpretation:

The currently retained timeout/execution-required reader lane is not the direct `PowerRequestOverride` leaf reader.

### 2. `PopPowerRequestOverrideInitialize` is a dispatch point over existing objects

Retained local-KD shows:

- it walks `PopPowerRequestObjectList`
- it conditionally calls `PopUmpoSendPowerRequestOverrideQuery`

Interpretation:

This is an object-iteration bootstrap path, not a direct registry materialization path.

### 3. `PopUmpoSendPowerRequestOverrideQuery` stores requester state and sends a power message

Retained local-KD shows:

- `PoStoreRequester`
- `ExAllocatePool2` with an `Umpo` tag
- `PopUmpoSendPowerMessage`

Interpretation:

The visible kernel path is packaging a query payload for a message exchange. That is stronger evidence for a message-boundary hypothesis than for an in-kernel direct leaf reader.

### 4. The retained response-lineage pass still points at the response side, not a `*Reg*` helper

The retained response-lineage summary runs:

- `x nt!*PowerRequest*Reg*`
- `uf nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- `uf nt!PopPowerRequestCallbackWorker`
- `uf nt!PopPowerRequestCallbackExecutionRequired`

and still keeps the lane centered on:

- `PopPowerRequestHandleRequestOverrideQueryResponse`
- callback-worker / callback-execution paths

rather than on a named `*PowerRequest*Reg*` helper.

Interpretation:

If a direct current-build registry reader exists, it is still not naturally surfaced in the retained kernel-side symbol set. The best remaining boundary is the override query/response message lane.

## Working Hypothesis

The current build likely handles `PowerRequestOverride` through a split boundary:

- kernel-side code packages or applies override-query state
- UMPO or an adjacent power-service boundary likely participates in the actual read / response / apply flow

This is only a hypothesis, not proof. But it is better supported now than a broad "keep searching the kernel for a registry helper" strategy.

## What This Rules Out

This note does not prove that:

- the subtree is user-mode only
- the kernel never reads any override state
- the storage model exposed by powercfg is the entire live behavior model

It only narrows the next cheapest investigation target.

## Best Next Step

A good next sprint should stay narrow:

1. disassemble `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
2. inspect `nt!PopUmpoSendPowerMessage`
3. only then decide whether the next escalation is:
   - deeper kernel-side response tracing
   - or a user-mode `powrprof` / power-service boundary investigation

## Conclusion

The subtree lane no longer needs another generic "registry reader hunt".

The best remaining target is the override message boundary, starting with the response handler.
