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

Ghidra follow-up:

- Binary: `C:/Windows/System32/ResourcePolicyServer.dll`
- Strings: `GameDVR_FSEBehavior`, `GameDVR_FSEBehaviorMode`, `GameDVR_HonorUserFSEBehaviorMode`, `GameDVR_DXGIHonorFSEWindowsCompatible`, `GameConfigStore`
- Export: `research/evidence-files/ghidra/system.disable-fullscreen-optimizations/resourcepolicysrv-fullscreen-ghidra.md`

Key finding:

```text
ResourcePolicyServer.dll carries the GameConfigStore RPC server code path, opens
HKCU/System/GameConfigStore, and validates GameDVR_* values through the same
gameconfigstoreserver.cpp family that backs the current tuple mapping.
```
