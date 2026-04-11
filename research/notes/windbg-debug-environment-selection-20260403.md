# WinDbg Debug Environment Selection

- Date: `20260403`
- Selected long-term target: `Hyper-V`
- Long-term status: `blocked-prereqs`
- Immediate environment: `VMware-debug-only`
- Immediate environment status: `ready-short-try`
- Fallback if blocked: `VMware-debug-only`

## Host Signals
- HyperVisorPresent: `True`
- Hyper-V feature state: `Disabled`
- Hyper-V PowerShell feature state: `Disabled`
- VirtualMachinePlatform state: `Enabled`

## Decision
- Freeze the current VMware WinDbg lane as known blocked.
- Treat Hyper-V as the preferred debugger-first target environment.
- If Hyper-V prerequisites are still blocked, allow one short fresh VMware debug-only try instead of returning to the frozen lane.
- If that short VMware debug-only try reproduces the same transport blocker, stop and move directly to Hyper-V prerequisites.

