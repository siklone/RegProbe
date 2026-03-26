Fullscreen Optimizations validation on Win25H2Clean

- Registry path: `HKCU/System/GameConfigStore`
- Capture process: `svchost.exe` hosting `resourcepolicyserver.dll`
- Trigger path: Gaming and Advanced Graphics Settings pages plus a bounded Game Bar presence start

Observed runtime reads:

```text
svchost.exe RegQueryValue HKCU/System/GameConfigStore/GameDVR_DXGIHonorFSEWindowsCompatible
SUCCESS Type: REG_DWORD, Length: 4, Data: 1
```

Evidence files:

- `research/evidence-files/procmon/system.disable-fullscreen-optimizations/fullscreen-diag.txt`
- `research/evidence-files/procmon/system.disable-fullscreen-optimizations/fullscreen-diag.hits.csv`

Automated follow-up:

- `research/evidence-files/procmon/system.disable-fullscreen-optimizations/fullscreen-optimizations-probe.txt`
- `research/evidence-files/procmon/system.disable-fullscreen-optimizations/fullscreen-optimizations-probe.json`
- `research/evidence-files/procmon/system.disable-fullscreen-optimizations/fullscreen-optimizations-probe.hits.csv`

The later hidden Settings/Game Bar automation pass did not reproduce the
earlier `svchost.exe` read. It still mattered because it pinned the current
shell-stable snapshot baseline before and after the app tuple:

```text
GameDVR_FSEBehavior = missing
GameDVR_FSEBehaviorMode = 2
GameDVR_HonorUserFSEBehaviorMode = 0
GameDVR_DXGIHonorFSEWindowsCompatible = 0
```

That means the older targeted `svchost.exe` read remains the positive runtime
proof, while the later automated pass now documents the current VM baseline and
shows that the trigger path is not yet strong enough to reproduce all tuple
reads on demand.

Ghidra follow-up:

- Binary: `C:/Windows/System32/ResourcePolicyServer.dll`
- Strings: `GameDVR_FSEBehavior`, `GameDVR_FSEBehaviorMode`, `GameDVR_HonorUserFSEBehaviorMode`, `GameDVR_DXGIHonorFSEWindowsCompatible`, `GameConfigStore`
- Export: `research/evidence-files/ghidra/system.disable-fullscreen-optimizations/ghidra-matches.md`
- Run log: `research/evidence-files/ghidra/system.disable-fullscreen-optimizations/ghidra-run.log`
- Structured summary: `research/evidence-files/ghidra/system.disable-fullscreen-optimizations/evidence.json`

Key finding:

```text
ResourcePolicyServer.dll carries the GameConfigStore RPC server code path, opens
HKCU/System/GameConfigStore, and validates GameDVR_* values through the same
gameconfigstoreserver.cpp family that backs the current tuple mapping.
```
