# system.executive-additional-worker-threads follow-up package - 2026-03-28

## Summary

This package records the next escalation ladder for the unresolved Executive worker-thread lane after the successful Procmon boot-log run still returned zero exact-value hits.

The follow-up ideas are:

1. Kernel breakpoint on `nt!CmQueryValueKey`
2. More specific ETW filtering on the Executive path
3. ReactOS source grep for hypothesis support
4. Trigger and stress conditions to surface conditional reads

## Applied now

An ETW-specific keyword review was executed against the current bounded Executive ETW extract:

- input: `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260328-090631/registry-dump-session-manager-executive.txt`
- filtered review: `evidence/files/vm-tooling-staging/executive-etw-keyword-review-20260328-180226/executive-etw-keyword-review.txt`
- summary: `evidence/files/vm-tooling-staging/executive-etw-keyword-review-20260328-180226/summary.json`

Result:

- `Session Manager\Executive`: `6`
- `AdditionalCriticalWorkerThreads`: `0`
- `AdditionalDelayedWorkerThreads`: `0`
- `UuidSequenceNumber`: `2`

That means the ETW-specific filter tightened the lane, but it still only surfaced adjacent `UuidSequenceNumber` traffic rather than the exact worker-thread pair.

## Next step 1 - WinDbg kernel breakpoint

Strongest possible proof:

- set a kernel breakpoint on `nt!CmQueryValueKey`
- break only when the queried key path matches `Session Manager\Executive`
- inspect the call stack and caller module

Why it helps:

- if the pair is really read by kernel or early-boot code, this is the shortest route to the exact reader
- it bypasses the ambiguity of ETW and Procmon surface filtering

Current status:

- not executed yet
- requires a dedicated kernel-debug-capable VM lane
- should be treated as decision-gated because it changes the validation environment

## Next step 2 - ETW-specific capture

Current status:

- partially applied through the exact-keyword review above
- a dedicated `Kernel-Registry` provider capture is still available as a stronger follow-up if needed

Why it helps:

- narrows the runtime lane from generic boot ETW review to exact Executive path and value names
- can confirm whether the pair is absent from the provider surface or simply lost in broader trace noise

## Next step 3 - ReactOS source grep

Hypothesis support only:

- grep ReactOS for `AdditionalCriticalWorkerThreads`
- grep ReactOS for `AdditionalDelayedWorkerThreads`
- grep ReactOS for `UuidSequenceNumber`

Why it helps:

- may reveal a semantic lead or adjacent subsystem name
- useful for hypothesis generation before another heavy VM run

Constraint:

- even if it finds something, it should stay supporting evidence, not promotion-grade proof

## Next step 4 - Trigger and stress lane

Candidate triggers:

- service-start pressure
- worker queue pressure
- higher background load during and immediately after boot

Why it helps:

- the pair may be read only under queue pressure or a specific executive condition
- a negative boot log plus a negative stressed lane would be more decisive than boot-only evidence

Current status:

- not executed yet
- best deferred until the ETW-specific and ReactOS hypothesis passes are exhausted

## Recommendation

Use this escalation order:

1. keep the exact ETW filter result as the current lightweight negative proof
2. run a ReactOS/source hypothesis pass next because it is cheap
3. if still unresolved, decide between:
   - a dedicated kernel-debug lane
   - a stress-trigger runtime lane

This keeps the current lane honest without immediately paying the cost of a kernel-debug environment.
