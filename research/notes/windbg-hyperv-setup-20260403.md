# Hyper-V Debug Setup Plan

- Status: `blocked-prereqs`
- Debug VM name: `RegProbe-Debug-HyperV`
- Baseline: `RegProbe-Debug-HyperV-Baseline-20260403`
- VM role: `debug_arbiter_only`
- Transport candidates: `serial, kdnet`

## Role Split
- `VMware`: runtime research lanes
- `Hyper-V`: debug arbiter only

## Provisioning Steps
- Enable Microsoft-Hyper-V and Microsoft-Hyper-V-Management-PowerShell on the host.
- Reboot the host so Hyper-V cmdlets and services are fully available.
- Create a Generation 2 VM named RegProbe-Debug-HyperV under the configured storage root.
- Keep the VM debugger-first and minimal; do not reuse the runtime VMware scratch surface.
- Enable kernel debugging and record a clean checkpoint plus debug-enabled checkpoint.
- Bring up serial transport first; if it is still unreliable, evaluate KDNET as the fallback transport.
- Do not run single-key arbiter semantics until transport smoke is reproducible in two consecutive runs.

