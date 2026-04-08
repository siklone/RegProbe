## Scope

Lock the remaining execution-required runtime-trace gap to the actual KVM guest-control environment instead of leaving it framed as missing trigger or runner design.

## Findings

1. Both remaining execution-required records are now mapped to the dedicated narrow runtime tool:
   - `registry-research-framework/tools/run-path-aware-runtime-probe.ps1`
2. That runtime tool is still VMware-oriented:
   - it imports `scripts/vm/_vmrun-common.ps1`
   - it carries a literal `vmrun.exe` path
3. The repo controller documentation and controller script are also still VMware-oriented:
   - `Docs/VM_VALIDATION_CONTROLLER.md` describes a shared-folder controller workspace
   - `scripts/vm/host-validation-controller.ps1` drives `runProgramInGuest` through `vmrun`
4. The active libvirt domain is running:
   - `regprobe-win11-25h2-session`
5. The active libvirt domain does not expose a qemu guest agent control surface:
   - `virsh qemu-agent-command regprobe-win11-25h2-session '{"execute":"guest-ping"}'`
   - returned `argument unsupported: QEMU guest agent is not configured`
6. The current domain XML also points to a missing bootstrap ISO path on the host checkout:
   - `/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/dist/regprobe-kvm-bootstrap.iso`
   - host file absent during this audit

## Interpretation

The remaining gap for `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` is now environment-gated rather than runner-missing. The repo has a narrow path-aware runtime lane for both records, but the active KVM session cannot be driven through qemu guest agent, and the legacy controller surface still assumes VMware Tools and `vmrun`. On the current host, the next decisive step is either to restore a usable KVM guest-control/bootstrap surface or to execute the narrow lane from a vmrun-capable environment.
