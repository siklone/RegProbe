# system.executive-additional-worker-threads

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr`

Validated cross-layer record. The Session Manager Executive worker-thread pair now has a clean baseline export on Win25H2Clean, a bounded boot-time ETL extract that proves runtime access to Session Manager/Executive, exact current-build ntoskrnl string hits, preserved Ghidra fallback artifacts, a supporting ReactOS semantic hypothesis, and a tools-hardened lightweight ETW follow-up that produced exact RegQueryValue hits for both AdditionalCriticalWorkerThreads and AdditionalDelayedWorkerThreads under a concurrent I/O and process-burst trigger. That is enough for Class A within this project even though the lane remains research-only and non-actionable in the app.

## Current verdict

The Executive worker-thread pair now has a clean baseline export, a bounded boot-time ETL extract that proves early Session Manager/Executive activity from System (PID 4), exact current-build ntoskrnl string hits, current-build Ghidra fallback artifacts, and a tools-hardened lightweight ETW follow-up that produced exact RegQueryValue hits for both AdditionalCriticalWorkerThreads and AdditionalDelayedWorkerThreads under a concurrent burst trigger. That is enough for Class A within this project even though the lane remains research-only and not app-actionable.
