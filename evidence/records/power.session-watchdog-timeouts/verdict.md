# power.session-watchdog-timeouts verdict

## Classification

- `Class C`

## Why

- baseline existence is confirmed on Win25H2Clean
- current-build ntoskrnl string hits and Ghidra fallback artifacts exist
- repo-side PoFx pseudocode ties the pair to directed power watchdog timeout globals
- a reboot-verified boot trace preserved the same values after boot
- a filtered ETL registry review shows real Session Manager\Power access by System and later svchost.exe during boot
- a working Procmon boot-log pass reproduced adjacent Session Manager\Power traffic from System during boot
- the svchost lead is now narrowed to the DcomLaunch service host group that contains the Power service
- a shell-safe post-boot DcomLaunch/Power Procmon trigger still produced no matching watchdog reads

## Why not higher

- no shipped RegProbe mapping exists yet
- no validated non-default timeout pair exists yet
- no direct live read of WatchdogResumeTimeout or WatchdogSleepTimeout has been captured yet, even after the targeted DcomLaunch/Power follow-up

## Current posture

Keep this lane active as research only. Do not ship it as an end-user tweak.
