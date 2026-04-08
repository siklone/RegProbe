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
5. The active libvirt domain now exposes a qemu guest agent channel, but not a connected guest-control surface:
   - `python3 scripts/vm/ensure-kvm-qga-channel.py --emit-json`
   - `virsh qemu-agent-command regprobe-win11-25h2-session '{"execute":"guest-ping"}'`
   - returned `Guest agent is not responding: QEMU guest agent is not connected`
6. The current domain XML points to a bootstrap ISO path that is now restored on the host checkout:
   - `/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/dist/regprobe-kvm-bootstrap.iso`
   - the rebuilt ISO now exists and carries a guest-local installer plus validation-agent payloads
7. The restored ISO now also supports optional qemu guest agent bootstrap:
   - `python3 scripts/vm/build-kvm-bootstrap-iso.py --qga-installer /absolute/path/to/qemu-ga-x86_64.msi`
   - `powershell -ExecutionPolicy Bypass -File .\install-guest-validation-agent-local.ps1 -InstallQemuGuestAgent`
8. The restored ISO is still a manual bootstrap surface:
   - it can seed guest-local files under `C:\Tools\Scripts` and `C:\Tools\ValidationController`
   - it can optionally install/start the Windows qemu guest agent
   - it still does not by itself provide host-side guest exec or result copy-back
9. A live guest-side bootstrap attempt now exists on the active desktop:
   - `scripts/vm/send-kvm-text.py` can type into the focused guest window through `virsh send-key`
   - the live attempt installed the official qga MSI, confirmed a `QEMU-GA` service in `RUNNING` state, confirmed visible `VIOSERIALPORT` devices, installed the official `virtio-win-gt-x64.msi`, and rebooted
10. The remaining guest-control failure mode is now stronger and narrower than before:
   - `virsh qemu-agent-command regprobe-win11-25h2-session '{"execute":"guest-ping"}'`
   - now returns `QEMU guest agent is not available due to an error`

## Interpretation

The remaining gap for `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` is now environment-gated rather than runner-missing. The repo has a narrow path-aware runtime lane for both records, the qemu guest-agent channel is attached on the active KVM session, the bootstrap ISO is restored, the guest-local installer can optionally bootstrap the Windows qemu guest agent, and a live keystroke fallback now exists for the focused KVM desktop. The blocker is now narrower than simple missing install/service state: after qga MSI install, visible `VIOSERIALPORT` devices, guest-tools MSI install, and reboot, host-side `guest-ping` still returns `QEMU guest agent is not available due to an error`. On the current host, the next decisive step is either to debug that guest-side qga runtime/protocol fault further or to execute the same narrow lane from a vmrun-capable environment.
