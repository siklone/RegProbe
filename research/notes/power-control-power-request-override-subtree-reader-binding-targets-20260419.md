# Power Request Override Subtree Reader-Binding Targets - 2026-04-19

## Why This Note Exists

The `power.control.power-request-override-subtree` lane now has:

- retained subtree presence
- retained runtime subtree access
- retained override-family lineage
- retained ETW root/control-value reads
- reversible `Process`, `Service`, and `Driver` leaf materialization through the documented `powercfg /requestsoverride` surface

That means the old blocker is no longer "do these leaves exist or store stable values at all?".

The remaining blocker is narrower:

- which current-build component actually reads or consumes those leaves at runtime
- whether that live reader path is fully aligned with the observed powercfg storage model

This note freezes the next debugger/static target set so the next sprint starts from the right symbols rather than reopening the whole subtree lane.

## Artifacts Reviewed

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reader-20260408a/local-kd-powerrequest-reader-20260408a.stdout.txt`
- `research/notes/power-control-power-request-override-subtree-static-context-follow-up-20260408.md`
- `research/notes/power-control-power-request-override-subtree-driver-service-proof-20260418.md`

## Narrowed Current-Build Story

### 1. `PopPowerRequestOverrideInitialize` is an override-query fan-out, not a registry reader

Retained local-KD shows:

- `PopPowerRequestOverrideInitialize` acquires the power-request lock
- walks `PopPowerRequestObjectList`
- calls `PopUmpoSendPowerRequestOverrideQuery` for eligible entries

That makes it a bootstrap/fan-out point for per-request override queries, not the direct registry leaf reader.

### 2. `PopUmpoSendPowerRequestOverrideQuery` packages requester state and sends a message

Retained local-KD shows:

- `PoStoreRequester` is used to serialize requester identity
- an `Umpo`-tagged buffer is allocated
- `PopUmpoSendPowerMessage` is called

This function clearly packages and sends an override query, but the visible path still does not expose a direct registry read. The strongest current hypothesis is that the reader/consumer boundary sits on the UMPO response side or in the user-mode power service path behind the message exchange.

### 3. The visible timeout-specific callback path is a red herring for subtree binding

Retained reader disassembly shows:

- `PopPowerRequestExecutionRequiredSettingCallback` is keyed to `GUID_EXECUTION_REQUIRED_REQUEST_TIMEOUT`
- it writes `PopExecutionRequiredTimeout`
- then rearms the timeout timer and calls `PopPowerRequestHandleExecutionEnablementUpdate`

This is useful context for the broader family, but it does not resolve the `PowerRequestOverride` subtree reader.

### 4. The current best remaining kernel target is the override-query response path

The retained wildcard lineage exposed:

- `PopPowerRequestHandleRequestOverrideQueryResponse`
- `PopPowerRequestOverrideInitialize`
- `PopUmpoSendPowerRequestOverrideQuery`
- `PopUmpoSendPowerRequestOverrideCleanup`

Given the stronger 2026-04-18 leaf proofs, the best next kernel-side question is no longer "can we find any override-family symbol?" but:

- where does the response path deserialize or apply the `Process` / `Service` / `Driver` override payload
- whether that response path names or implies the live reader boundary more directly than the send path

## Best Next Debugger Targets

### Primary targets

- `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- `nt!PopUmpoSendPowerRequestOverrideQuery`
- `nt!PopUmpoSendPowerMessage`
- `nt!PopUmpoSendPowerRequestOverrideCleanup`

### Secondary targets

- wildcard `nt!*PowerRequest*Override*`
- wildcard `nt!*Umpo*PowerRequest*`
- any current-build user-mode `powrprof` / `umpo` / power-service handlers reachable from the override message path

## Recommended Next Sprint Shape

1. Start with a dedicated local-KD pass on `PopPowerRequestHandleRequestOverrideQueryResponse`.
2. If that still only shows message unpack/apply logic without a clear reader, pivot to the UMPO message boundary rather than re-running broad subtree captures.
3. Only after that, decide whether a user-mode static pass or debugger-assisted `powrprof` / power-service trace is justified.

## Concrete Next Commands

The next narrow pass should prefer commands like:

```text
x nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
x nt!*PowerRequest*Override*
x nt!*Umpo*PowerRequest*
uf nt!PopUmpoSendPowerMessage
```

If the response path still does not expose the reader boundary, the next cheapest escalation is not another broad Procmon pass. It is a message-boundary or user-mode power-service follow-up tied specifically to the override query/response lane.

## Conclusion

The subtree lane no longer needs another "does the storage model exist?" sprint.

The next good sprint is a targeted reader-binding sprint centered on the override response/message boundary.
