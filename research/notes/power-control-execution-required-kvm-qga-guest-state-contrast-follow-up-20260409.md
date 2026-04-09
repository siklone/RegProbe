## Scope

Check whether the persistent host-side qga socket refusal changes when the live Windows `QEMU-GA` service flips the qga virtserial port between `guest on` and `guest off`.

## Findings

1. Before stopping the Windows service:
   - `virsh qemu-agent-command regprobe-win11-25h2-session '{"execute":"guest-ping"}'` returned `QEMU guest agent is not available due to an error`
   - `info qtree` showed the live qga port at `port 2, guest on, host off`
   - direct host-side `AF_UNIX` connect to `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0` returned `ConnectionRefusedError(111, 'Connection refused')`
2. After typing `sc stop QEMU-GA` into the live admin console:
   - `guest-ping` fell back to `QEMU guest agent is not connected`
   - `info qtree` changed the live qga port to `port 2, guest off, host off`
   - the same direct host-side `AF_UNIX` connect still returned `ConnectionRefusedError(111, 'Connection refused')`
3. After typing `sc start QEMU-GA` into the same live console:
   - `guest-ping` returned to `QEMU guest agent is not available due to an error`
   - `info qtree` returned the live qga port to `port 2, guest on, host off`
   - the same direct host-side `AF_UNIX` connect still returned `ConnectionRefusedError(111, 'Connection refused')`

## Interpretation

The persistent host-side refusal is now independent of the guest-side open state of the live qga port. Flipping `QEMU-GA` between `guest on` and `guest off` changes the host-visible `guest-ping` wording and the `info qtree` guest-state bit, but it does not change the raw refusal on the live qga socket. That leaves the blocker centered on the live libvirt-managed qga channel object or its attach/handshake path rather than on the guest simply keeping the port open.
