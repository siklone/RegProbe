# system.executive-additional-worker-threads verdict

## Classification

- `Class C`

## Why

- baseline existence is confirmed on Win25H2Clean
- a bounded boot-time ETL extract proves early `Session Manager\Executive` activity from `System (PID 4)`
- a real Procmon boot-log lane completed cleanly but still returned zero exact-value hits
- a shell-safe post-boot stress trigger lane also completed cleanly and still returned zero exact-value hits
- current-build ntoskrnl exact string hits and Ghidra fallback artifacts exist
- both values are present together at `0` on the clean baseline

## Why not higher

- no shipped RegProbe mapping exists yet
- no direct live read of the exact Executive worker-thread values has been captured yet
- even the successful Procmon boot-log and post-boot stress trigger lanes still returned `MATCH_COUNT=0`
- the strongest semantic lead still depends on forced-boundary current-build Ghidra artifacts

## Current posture

Keep this lane active as research only. Do not ship it as an end-user tweak.
