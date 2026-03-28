# system.executive-additional-worker-threads ReactOS hypothesis - 2026-03-28

## Summary

- A shallow official ReactOS source pass now exists for the Executive worker-thread lane.
- ReactOS does contain both exact value names:
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
- The strongest semantic lead is in `ntoskrnl/ex/work.c`, where both values are represented as internal variables, clamped, and then added to the base delayed and critical worker-thread counts.

## Source artifacts

- supporting artifact: `evidence/files/external/reactos/system.executive-additional-worker-threads/reactos-hypothesis-20260328.md`
- structured summary: `evidence/files/external/reactos/system.executive-additional-worker-threads/summary.json`

## What ReactOS suggests

- `AdditionalCriticalWorkerThreads` behaves like an additive count for the critical worker queue
- `AdditionalDelayedWorkerThreads` behaves like an additive count for the delayed worker queue
- both values are bounded before they are merged into the final thread counts

## Important limit

ReactOS is useful here as a hypothesis source, not as primary promotion proof.

The reason is important:

- the ReactOS registry table in `ntoskrnl/config/cmdata.c` includes both names under `Session Manager\Executive`
- but the table currently maps both names to `DummyData`

So the source strongly suggests intended semantics, but it does not by itself prove current-build Windows behavior or exact registry wiring.

## Adjacent lane correction

The ReactOS `UuidSequenceNumber` implementation points to `\Registry\Machine\Software\Microsoft\Rpc`, not `Session Manager\Executive`.

That lines up with the Windows-side ETW result: `UuidSequenceNumber` is adjacent runtime activity, not direct proof for the Executive worker-thread pair.

## Result

This pass strengthens the semantic hypothesis for the Executive lane without changing its class.

It is the right kind of evidence to use before paying the cost of:

- a WinDbg kernel breakpoint lane
- or a stress-trigger runtime lane
