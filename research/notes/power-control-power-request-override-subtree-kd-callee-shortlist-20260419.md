# Power Request Override Subtree KD Callee Shortlist - 2026-04-19

## Why This Note Exists

The `PowerRequestOverride` lane already has:

- retained subtree presence
- retained runtime subtree access
- reversible `Process`, `Service`, and `Driver` leaf proofs through `powercfg /requestsoverride`
- narrowed kernel targets around the override response and UMPO message boundary

The next refinement is not another broad hypothesis note.

It is a concrete shortlist of the first meaningful callees already visible in retained local-KD output, plus one important artifact gap that the next debugger sprint should close immediately.

## Retained Artifacts Read

- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-umpo-20260408a/local-kd-powerrequest-umpo-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-init-20260408a/local-kd-powerrequest-init-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-resplineage-20260408a/local-kd-powerrequest-resplineage-20260408a-summary.json`

## Concrete KD Facts

### 1. `PopPowerRequestOverrideInitialize` is still just a dispatcher

Retained init disassembly shows:

- `PopAcquirePowerRequestPushLock`
- iteration over `PopPowerRequestObjectList`
- one meaningful per-object call: `PopUmpoSendPowerRequestOverrideQuery`
- then `PopReleaseRwLock`

This confirms again that the init path is not the reader. It is only the object-iteration handoff into the UMPO query lane.

### 2. `PopUmpoSendPowerRequestOverrideQuery` has a very small immediate callee set

The retained UMPO disassembly shows exactly these meaningful direct calls:

1. `PoStoreRequester`
2. `ExAllocatePool2`
3. `PoStoreRequester` again for the allocated payload
4. `PopUmpoSendPowerMessage`
5. `ExFreePoolWithTag`

What this means:

- the function is clearly packaging requester state
- it allocates a small `Umpo`-tagged buffer
- it hands the payload to `PopUmpoSendPowerMessage`
- it does not expose any registry API in the retained path

So the next kernel-side discriminator should not spend time redisassembling `PopUmpoSendPowerRequestOverrideQuery` from scratch unless symbol drift is suspected.

### 3. The clearest already-visible transport boundary is still `PopUmpoSendPowerMessage`

Because the retained UMPO path stops at:

- requester serialization
- payload allocation
- `PopUmpoSendPowerMessage`

the remaining send-side question is now:

- what exact message shape or opcode does `PopUmpoSendPowerMessage` carry for the override query lane

This is a narrower next question than another broad `*PowerRequest*Reg*` sweep.

### 4. The response-side symbol is known, but the retained repo artifact is incomplete

The retained reglineage pass confirms that the checked-in build exposes:

- `PopPowerRequestHandleRequestOverrideQueryResponse`
- `PopPowerRequestOverrideInitialize`
- `PopUmpoSendPowerRequestOverrideQuery`
- `PopUmpoSendPowerRequestOverrideCleanup`

And the retained resplineage summary confirms that the debugger command set already included:

- `uf nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- `uf nt!PopPowerRequestCallbackWorker`
- `uf nt!PopPowerRequestCallbackExecutionRequired`

But the repository retains only the resplineage summary JSON, not the paired stdout disassembly text for that run.

That matters because:

- the next sprint should not assume that the response-side disassembly is already recoverable from repo contents
- the first debugger task should explicitly reacquire `uf nt!PopPowerRequestHandleRequestOverrideQueryResponse`

## Shortlist For The Next Sprint

### First commands

```text
uf nt!PopPowerRequestHandleRequestOverrideQueryResponse
uf nt!PopUmpoSendPowerMessage
```

### If the response-side output is thin

Follow only the first meaningful callee that still belongs to the bounded override-response family.

Do not immediately widen to:

- generic callback plumbing
- broad `*PowerRequest*Reg*` wildcards
- another subtree-wide runtime capture

### If the response-side output is unavailable again

Treat that as an artifact-gap problem first, not as evidence about the runtime model.

The reacquisition priority is:

1. exact stdout for `uf nt!PopPowerRequestHandleRequestOverrideQueryResponse`
2. exact stdout for `uf nt!PopUmpoSendPowerMessage`
3. only then any broader wildcard or user-mode follow-up

## Practical Conclusion

The retained repo evidence already tells us that `PopUmpoSendPowerRequestOverrideQuery` is mostly a packaging shim.

So the next debugger sprint should start from:

- reacquiring the missing response-side disassembly text
- then checking whether `PopUmpoSendPowerMessage` or the response handler is the first place where real override semantics become visible
