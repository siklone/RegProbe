## Scope

Narrow the post-bootstrap KVM guest-control blocker for the execution-required pair by testing whether a direct foreground qga launch on the expected guest-agent path behaves differently from the installed Windows service.

## Findings

1. The live guest-side bootstrap result remained reproducible on the active desktop:
   - `QEMU-GA` was present as a Windows service
   - host-side `guest-ping` still failed with `QEMU guest agent is not available due to an error`
2. Stopping the Windows service changed the host-side failure mode:
   - `sc stop QEMU-GA`
   - host-side `virsh qemu-agent-command ... '{"execute":"guest-ping"}'`
   - returned `Guest agent is not responding: QEMU guest agent is not connected`
3. A direct foreground launch on the expected guest-agent path reproduced the stronger failure mode instead of fixing it:
   - `cd /d "C:\Program Files\qemu-ga"`
   - `qemu-ga.exe -v -p \\.\Global\org.qemu.guest_agent.0`
   - host-side `guest-ping` returned `QEMU guest agent is not available due to an error`
4. The foreground console did not exit immediately or show a simple open-path failure:
   - it printed continuous `debug: dispatch` lines in the console window
5. A quick namespace probe remained inconclusive but still useful:
   - `powershell -NoLogo -Command "Test-Path '\\.\Global\org.qemu.guest_agent.0'; Test-Path '\\.\Global\com.redhat.spice.0'"`
   - both probes returned `Access is denied` and then `False`

## Interpretation

The remaining KVM blocker is now narrower than "service not installed" or "service not started." Stopping `QEMU-GA` reverts host-side guest control to `not connected`, while launching `qemu-ga.exe -v -p \\.\Global\org.qemu.guest_agent.0` directly in the foreground brings the state back to `available due to an error`. That means the failure survives an explicit foreground launch on the expected port and is no longer explained by the Windows service wrapper alone. The remaining issue is a guest-side qga runtime or protocol fault on the current KVM stack, plus the still-vmrun-oriented controller surface.
