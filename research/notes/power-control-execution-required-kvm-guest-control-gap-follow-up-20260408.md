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
11. A direct foreground repro now shows the failure survives the Windows service wrapper:
   - `sc stop QEMU-GA` changes host-side `guest-ping` back to `Guest agent is not responding: QEMU guest agent is not connected`
   - `qemu-ga.exe -v -p \\.\Global\org.qemu.guest_agent.0` then brings `guest-ping` back to `QEMU guest agent is not available due to an error`
   - the foreground console prints continuous `debug: dispatch` lines instead of a simple open-path failure
12. A new host-side socket-state audit shows the qga channel still fails before a usable frontend attach:
   - `virsh qemu-monitor-command ... --pretty '{"execute":"query-chardev"}'` keeps `charchannel1` at `frontend-open=false`
   - `virsh qemu-monitor-command ... --hmp 'info qtree'` keeps the qga port at `port 2, guest off, host off`
   - the healthy control case remains visible because the spice channel reports `guest on, host on`
13. The host-side unix sockets also split cleanly:
   - direct Python `AF_UNIX` connect to the libvirt monitor socket succeeds
   - the qga unix socket at `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0` returns `Connection refused`
   - so the remaining KVM guest-control issue is no longer just "guest-ping returns an error"; the host-side qga socket itself still does not accept a usable client path
14. Official QEMU interoperability docs make that host-side split more meaningful:
   - the QMP reference defines `frontend-open` as whether the frontend device attached to a chardev backend is in open or closed state
   - the QGA protocol reference says a client only begins real wire synchronization after initial connection, via `guest-sync-delimited` / `guest-sync`
   - because the retained host-side qga socket already fails at `connect()` with `Connection refused`, the current KVM blocker sits before guest-agent protocol sync rather than inside the later JSON command phase
15. A later live guest-state contrast then showed the raw refusal survives both service states:
   - before stop, `guest-ping` returned `QEMU guest agent is not available due to an error`, `info qtree` showed `port 2, guest on, host off`, and direct `AF_UNIX` connect to the live qga socket still returned `ConnectionRefusedError(111, 'Connection refused')`
   - after `sc stop QEMU-GA`, `guest-ping` fell back to `QEMU guest agent is not connected` and `info qtree` changed to `port 2, guest off, host off`, but the same direct connect still returned `ConnectionRefusedError(111, 'Connection refused')`
   - after `sc start QEMU-GA`, the host returned to the stronger `guest-ping` error plus `guest on, host off`, while the direct socket refusal still stayed unchanged
16. A later host-side listener snapshot then showed the qga path is not missing a listener:
   - `ss -xlpn` and `lsof -U -a -p <qemu pid>` both show `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0` as a qemu-owned `LISTEN` socket
   - direct host-side connect to that qga path still returns `ConnectionRefusedError(111, 'Connection refused')`
   - the sibling qemu monitor socket on the same process instead returns `BlockingIOError(11, 'Resource temporarily unavailable')`, not `ConnectionRefusedError(111, 'Connection refused')`
17. A later live libvirt-channel contrast then showed the blocker is not generic to libvirt-managed unix `virtio` channels:
   - a temporary `virsh attach-device --live` channel named `org.codex.libvirttest.0` created `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/codex.libvirttest.sock`
   - direct host-side `AF_UNIX` connect to that temporary libvirt-managed socket returned `CONNECT_OK`
   - during the same window the live qga socket still returned `ConnectionRefusedError(111, 'Connection refused')`

## Interpretation

The remaining gap for `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` is still environment-gated rather than runner-missing, but the blocker is now narrower than a generic guest-side qga fault. The repo has a narrow path-aware runtime lane for both records, the qemu guest-agent channel is attached on the active KVM session, the bootstrap ISO is restored, the guest-local installer can optionally bootstrap the Windows qemu guest agent, and a live keystroke fallback now exists for the focused KVM desktop. After qga MSI install, visible `VIOSERIALPORT` devices, guest-tools MSI install, reboot, direct foreground qga launches, an explicit service-state flip between `guest on` and `guest off`, a host-side listener snapshot that still shows the qga path as a qemu-owned `LISTEN` socket, and a live libvirt-channel contrast in which a temporary libvirt-managed unix `virtio` channel connects successfully while qga still refuses, the live host-side state now looks qga-special rather than generic to libvirt-managed channels. On the active host, the next decisive step is therefore to debug qga-specific host/libvirt/QEMU integration behavior rather than to keep treating the issue as missing runtime-runner plumbing, a simple Windows-side protocol exception, the guest merely having the port open, the host lacking a listener, or libvirt-managed unix channels in general.

## Audit artifacts

- [execution-required-kvm-guest-control-gap-20260408.json](../../registry-research-framework/audit/execution-required-kvm-guest-control-gap-20260408.json)
- [execution-required-kvm-guest-control-gap-20260408.md](../../registry-research-framework/audit/execution-required-kvm-guest-control-gap-20260408.md)
