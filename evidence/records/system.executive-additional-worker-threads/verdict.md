# system.executive-additional-worker-threads verdict

## Classification

- `Class C`

## Why

- baseline existence is confirmed on Win25H2Clean
- current-build ntoskrnl exact string hits and Ghidra fallback artifacts exist
- both values are present together at `0` on the clean baseline

## Why not higher

- no shipped RegProbe mapping exists yet
- no direct live read of the exact Executive worker-thread values has been captured yet
- the strongest semantic lead still depends on forced-boundary current-build Ghidra artifacts

## Current posture

Keep this lane active as research only. Do not ship it as an end-user tweak.
