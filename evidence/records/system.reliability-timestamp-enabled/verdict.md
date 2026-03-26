# system.reliability-timestamp-enabled

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot, official_doc`
- Tools: `official-doc, procmon, ghidra, ghidra_no_function_fallback, reboot`

Microsoft's ADMX_Reliability policy page now closes the main contract gap for this record by mapping Enable Persistent Time Stamp directly to Software/Policies/Microsoft/Windows NT/Reliability/TimeStampEnabled and documenting the enabled, disabled, and not-configured states. Decompiled OsEventsTimestampInterval still explains the companion TimeStampInterval cadence, the 24-hour cap, and the current-version fallback path, while the 25H2 Procmon and Ghidra follow-up keeps the current-build DiagTrack lead and adjacent Reliability/PBR runtime read in repo evidence.

## Current verdict

Microsoft now closes the main contract gap by publishing the exact TimeStampEnabled policy path and enable/disable semantics. The decompiled reader still supplies the companion TimeStampInterval logic and 24-hour cap, and the 25H2 follow-up work keeps the current-build DiagTrack lead in repo evidence. That is enough to treat the app's gate-plus-cap pair as an app-ready policy mapping even though the live Procmon pass still narrows to an adjacent Reliability branch instead of the exact target values.
