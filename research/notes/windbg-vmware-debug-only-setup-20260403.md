# VMware Debug-Only Fallback Plan

- Status: `ready-to-short-try`
- Debug VM name: `Win25H2DebugOnly`
- Baseline: `RegProbe-Debug-VMwareOnly-Baseline-20260403`
- VM role: `debug_arbiter_only`
- Source runtime profile: `primary`
- Source snapshot: `RegProbe-Baseline-ToolsHardened-20260330`
- Trial policy: `short-transport-only`

## Decision Rules
- This is a fresh debugger-first fallback VM, not the frozen VMware WinDbg lane.
- Run only a short transport-first try here.
- If the current transport blocker reproduces, stop and move directly to Hyper-V prerequisites.

