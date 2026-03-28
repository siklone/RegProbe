# ReactOS hypothesis - system.executive-additional-worker-threads

- Source repository: `https://github.com/reactos/reactos`
- Commit: `3908cbf5b437e86f02d3d5fd56ad55387375a428`
- Date reviewed: `2026-03-28`

## Relevant findings

1. `ntoskrnl/config/cmdata.c`
   - lines `490-499` contain:
     - `Session Manager\Executive`
     - `AdditionalCriticalWorkerThreads`
     - `AdditionalDelayedWorkerThreads`
   - both values are present in the registry data table, but they map to `DummyData`

2. `ntoskrnl/ex/work.c`
   - lines `33-37` declare:
     - `ExpAdditionalCriticalWorkerThreads`
     - `ExpAdditionalDelayedWorkerThreads`
   - lines `541-548` clamp both values to `16` and add them to the base delayed and critical worker-thread counts

3. `ntoskrnl/ex/uuid.c`
   - lines `81-82` and `136-137` bind `UuidSequenceNumber` to `\Registry\Machine\Software\Microsoft\Rpc`
   - this supports treating `UuidSequenceNumber` as a separate adjacent lane rather than proof for the Executive worker-thread pair

## Interpretation

ReactOS gives a strong semantic hypothesis:

- `AdditionalCriticalWorkerThreads` appears intended to add extra critical worker threads
- `AdditionalDelayedWorkerThreads` appears intended to add extra delayed worker threads
- both values are capped before being merged into the final worker-thread counts

## Limits

This is supporting evidence only.

Why:

- ReactOS is not current-build Windows 11
- the `cmdata.c` table maps both names to `DummyData`, so the registry name presence is stronger than the registry wiring proof
- promotion should still depend on Windows runtime and current-build binary evidence
