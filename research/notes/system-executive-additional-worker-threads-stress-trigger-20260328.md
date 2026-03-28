# system.executive-additional-worker-threads stress trigger - 2026-03-28

## Summary

- The Executive worker-thread lane now has a shell-safe post-boot stress-trigger Procmon pass on the clean `RegProbe` baseline.
- The trigger combined `tasklist /svc`, `sc query type= service state= all`, `wevtutil el`, a CIM service export, and parallel service/event/file enumeration jobs.
- The lane produced a real `PML`, a real `CSV`, normalized service/event snapshots, and a stress-job state export.
- Even with that broader post-boot trigger surface, the filtered lane still returned `MATCH_COUNT=0` for:
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
  - `UuidSequenceNumber`

## Source artifacts

- Probe summary: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/summary.json`
- Probe text export: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress.txt`
- Tasklist snapshot: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-tasklist.txt`
- Service Control snapshot: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-sc-all.txt`
- Event log list: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-event-logs.txt`
- System event sample: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-system-events.json`
- CIM service export: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-cim-services.json`
- Stress job states: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress-stress-jobs.json`
- Raw placeholder: `evidence/files/vm-tooling-staging/executive-worker-threads-stress-20260328-184856/executive-worker-threads-stress.pml.md`

## Result

- `PML_EXISTS=True`
- `CSV_EXISTS=True`
- `MATCH_COUNT=0`
- Shell health stayed clean before and after the trigger run.
- The guest also returned the service, event-log, and stress-job context files successfully.

## Why this matters

This closes the cheap conditional-read question for the current VM baseline more tightly than the boot-log lane alone.

The Executive pair is no longer waiting on "maybe a post-boot stress condition will surface it." A real post-boot stressed Procmon lane now exists, stayed shell-safe, and still failed to produce an exact read for either worker-thread value or the adjacent `UuidSequenceNumber` value.

That does not retire the lane, because current-build static evidence and the clean baseline export still keep it active as a real candidate. But it does strengthen the current negative-proof posture: on the current `Win25H2Clean` VMware baseline, both boot logging and a shell-safe post-boot stress trigger still stop short of a direct live read for the Executive worker-thread pair.
