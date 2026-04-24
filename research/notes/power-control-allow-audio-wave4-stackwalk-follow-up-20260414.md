# power.control.allow-audio-to-enable-execution-required-power-requests Wave 4 stackwalk follow-up - 2026-04-14

The audio execution-required sibling now has a retained exact runtime query lane on the current build.

What was captured:

- The salvaged guest summary for `wave4-allow-audio-e2e` finished `status = ok`, uploaded both ETL and XML, and reported `stack_field_hit_count = 450254`.
- The tracerpt XML contains explicit `reg.exe query HKLM\SYSTEM\CurrentControlSet\Control\Power /v AllowAudioToEnableExecutionRequiredPowerRequests` command lines.
- The same XML contains exact `KeyName = AllowAudioToEnableExecutionRequiredPowerRequests` hits.
- The normalized bundle now exists at `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/normalized-registry-bundle.json` with `event_count = 5734` and `caller_stack_event_count = 2080`.

What this closes:

- The old `audio-execution-required-megatrigger-etw-no-hit-current-build` wording is no longer the right top-level gap. We now have an exact current-build runtime query for the audio-specific value.
- The remaining top blockers are narrower: Microsoft still does not publish a primary current-build document for the exact internal audio-specific setting, and the internal `Control\Power` seeding path is still inferred from INIT-table/static analysis rather than resolved to a named public routine.

What landed after the ETW capture:

- Refreshing the Ghidra autotrigger pipeline from the retained bundle produced `seed_count = 1`, `symbol_resolution_request_count = 16`, and `symbol_resolution_batch_job_count = 5` for the audio candidate.
- The grouped artifacts now resolve the retained caller stack through `reg.exe!QueryValue`, `reg.exe!QueryRegistry`, `kernelbase.dll!RegGetValueW`, `kernelbase.dll!RegQueryValueExW`, `kernelbase.dll!BaseRegQueryValueInternal`, `ntdll.dll!NtQueryValueKey`, `kernel32.dll!BaseThreadInitThunk`, and kernel-side `ntoskrnl.exe!NtQueryValueKey`, `EtwpTraceRegistry`, `EtwpTraceStackWalk`, and `KiSystemServiceStart`.
- The audio runtime lane is now aligned with the system-required sibling on explicit query/read proof. The remaining gap is the earlier boot/init seeding route, not the existence of a current-build runtime reader path.
