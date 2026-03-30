# Executive Worker Threads Lightweight Runtime Follow-Up (2026-03-30)

The tools-hardened follow-up for `system.executive-additional-worker-threads` ran on `RegProbe-Baseline-ToolsHardened-20260330`.

What changed:
- The runtime lane switched from heavy Procmon / boot-log dependence to a lightweight ETW pass.
- The trigger used concurrent file I/O plus short process-spawn bursts.
- Two implementation bugs were fixed before the final run:
  - the `reg add` target path needed explicit quoting because `Session Manager\Executive` contains spaces
  - the split ETW phases needed a shared trace session name and shared ETL/CSV paths

Final result:
- `short-trigger-etw` produced exact `RegQueryValue` hits for both:
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
- the split trace path also produced exact hits after the session-name and ETL-path fixes
- shell health stayed clean before and after the run
- the final summary is:
  - `evidence/files/vm-tooling-staging/executive-worker-threads-lightweight-runtime-20260330-122422/summary.json`
  - `evidence/files/vm-tooling-staging/executive-worker-threads-lightweight-runtime-20260330-122422/system-executive-additional-worker-threads/summary.json`

Why this matters:
- the lane is no longer relying only on adjacent `Session Manager\Executive` traffic
- the exact worker-thread pair now has:
  - baseline registry observation
  - current-build static string + Ghidra evidence
  - exact runtime ETW reads

Project decision:
- promote `system.executive-additional-worker-threads` to `Class A`
- keep it research-only and non-actionable in the app
