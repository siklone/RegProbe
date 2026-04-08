## Scope

Capture the fresh-reboot Windows service state for qga and relate it to the already-retained host-side KVM channel diagnostics for the execution-required pair.

## Findings

1. A fresh post-reboot guest console query now exists on the active KVM desktop:
   - `sc query QEMU-GA`
   - it showed the service in `STATE : 1 STOPPED`
   - the same capture showed `WIN32_EXIT_CODE : 1067 (0x42b)`
2. The same guest console session also confirms the crash is not purely cosmetic:
   - the service is not merely delayed or left pending after boot
   - it is already stopped at the first captured admin prompt after reboot
3. A manual restart partially recovers the guest side:
   - `sc start QEMU-GA`
   - a follow-up `sc query QEMU-GA`
   - shows `STATE : 4 RUNNING`
4. The host-side qga state improves only part-way after that manual restart:
   - `query-chardev` flips `charchannel1` to `frontend-open=true`
   - `info qtree` flips the qga port to `port 2, guest on, host off`
5. The host-side accept path is still broken even after the guest-side service reaches `RUNNING`:
   - `virsh qemu-agent-command ... '{"execute":"guest-ping"}'`
   - still returns `QEMU guest agent is not available due to an error`
   - direct host-side `AF_UNIX` connect to `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
   - still returns `Connection refused`
6. The guest-side event log now contributes a matching service-level signal:
   - `wevtutil qe System /c:20 /rd:true /f:text | findstr /i QEMU`
   - returned `The QEMU Guest Agent service terminated unexpectedly. It has done this 1 time(s).`

## Interpretation

The remaining KVM blocker for `AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` is now split into two retained layers instead of one vague guest-control gap. First, a fresh reboot leaves `QEMU-GA` stopped with `WIN32_EXIT_CODE 1067`, and the Windows System log confirms an unexpected service termination. Second, manually starting the service only recovers the guest side part-way: the qga chardev becomes `frontend-open=true` and the qtree port becomes `guest on, host off`, but host-side `guest-ping` still fails and direct connects to the qga unix socket still return `Connection refused`. So the current KVM state is no longer "missing qga install" or even just "generic qga protocol failure." It is now a retained combination of boot-time qga service crash plus a persistent host/libvirt/QEMU-side attach or accept failure after manual guest recovery.
