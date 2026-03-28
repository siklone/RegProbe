# power.session-watchdog-timeouts verdict

## Classification

- `Class C`

## Why

- baseline existence is confirmed on Win25H2Clean
- current-build ntoskrnl string hits and Ghidra fallback artifacts exist
- repo-side PoFx pseudocode ties the pair to directed power watchdog timeout globals
- a reboot-verified boot trace preserved the same values after boot

## Why not higher

- no shipped RegProbe mapping exists yet
- no validated non-default timeout pair exists yet
- no direct live read of the exact watchdog values has been captured yet

## Current posture

Keep this lane active as research only. Do not ship it as an end-user tweak.
