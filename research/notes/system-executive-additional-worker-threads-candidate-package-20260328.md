# system.executive-additional-worker-threads candidate package - 2026-03-28

## What changed

- Added a draft research record for `system.executive-additional-worker-threads`
- Added a matching v3.1 evidence bundle under `evidence/records/system.executive-additional-worker-threads`
- Imported a clean `Session Manager\\Executive` baseline export into the repo evidence tree

## Why this is still draft-only

The Executive worker-thread pair is now evidence-backed, but it is not app-ready:

- there is no shipped RegProbe provider or UI mapping
- there is no direct live read of the exact Executive worker-thread values yet
- the main semantic lead still depends on forced-boundary current-build Ghidra artifacts

## Why it still matters

The lane is stronger than a speculative intake now:

- clean baseline existence is confirmed
- current-build ntoskrnl exact string hits and Ghidra fallback artifacts exist
- both values are present together at `0` on the clean baseline
- the pair is stable enough to package as one combined candidate surface

## Candidate lane shape

Keep the pair together as one candidate lane:

- `AdditionalCriticalWorkerThreads`
- `AdditionalDelayedWorkerThreads`

Do not split them or treat them as an end-user tweak yet.

## Retained audit artifact

- [system-executive-additional-worker-threads-candidate-package-20260328.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/system-executive-additional-worker-threads-candidate-package-20260328.json)
