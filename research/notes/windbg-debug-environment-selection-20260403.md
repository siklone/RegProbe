# WinDbg Debug Environment Selection

- Date: `20260403`
- Selected long-term target: `Hyper-V`
- Selected status: `blocked-prereqs`
- Immediate phase: `prepare-hyperv-prereqs`
- Fallback if blocked: `VMware-debug-only`

## Host Signals
- HyperVisorPresent: `True`
- Hyper-V feature state: `Disabled`
- Hyper-V PowerShell feature state: `Disabled`
- VirtualMachinePlatform state: `Enabled`

## Decision
- Freeze the current VMware WinDbg lane as known blocked.
- Treat Hyper-V as the debugger-first target environment.
- Do not widen single-key WinDbg semantics again until the new environment transport is proven.

