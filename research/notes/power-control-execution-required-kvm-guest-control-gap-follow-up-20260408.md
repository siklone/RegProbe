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
6. The current domain XML points to a bootstrap ISO path that is now restored on the host checkout:
   - `/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/dist/regprobe-kvm-bootstrap.iso`
   - the rebuilt ISO now exists and carries a guest-local installer plus validation-agent payloads
7. The restored ISO is still only a manual bootstrap surface:
   - it can seed guest-local files under `C:\Tools\Scripts` and `C:\Tools\ValidationController`
   - it does not by itself provide host-side guest exec or result copy-back

## Interpretation

The remaining gap for `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` is now environment-gated rather than runner-missing. The repo has a narrow path-aware runtime lane for both records, the bootstrap ISO is restored, and a guest-local installer now exists, but the active KVM session still cannot be driven through qemu guest agent and the legacy controller surface still assumes VMware Tools and `vmrun`. On the current host, the next decisive step is either to add a real host-side KVM guest-control surface or to execute the narrow lane from a vmrun-capable environment.
