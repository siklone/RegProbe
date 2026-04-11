## Scope

Determine whether the persistent host-side qga unix-socket `Connection refused` state reflects a generic QEMU unix-socket limitation or something narrower inside the live libvirt-managed qga channel.

## Findings

1. The live libvirt-managed qga socket still refused direct host-side AF_UNIX connects:
   - `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
   - Python `socket.connect(...)` returned `ConnectionRefusedError(111, 'Connection refused')`
2. A temporary control chardev created inside the same running VM process accepted a host-side connect immediately:
   - `virsh qemu-monitor-command ... '{"execute":"chardev-add",...,"id":"codextest",...,"path":"/tmp/regprobe-qga-socktest.sock"}'`
   - direct AF_UNIX connect to `/tmp/regprobe-qga-socktest.sock` returned `CONNECT_OK`
3. `query-chardev` showed both sockets as unix backends with `server=on`:
   - `charchannel1` -> `disconnected:unix:/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0,server=on`
   - `codextest` -> `disconnected:unix:/tmp/regprobe-qga-socktest.sock,server=on`
4. The contrast happened inside the same live `qemu-system-x86_64` process and the temporary chardev was removed immediately after the probe:
   - this rules out a generic host-to-QEMU session-socket inability on the current host
   - it leaves the failure centered on the libvirt-managed qga/virtserial channel path or its associated runtime state

## Interpretation

The remaining execution-required runtime blocker is now narrower than a generic host/libvirt/QEMU unix-socket problem. The same live QEMU process can expose a temporary unix-socket chardev that accepts a host-side connection, while the libvirt-managed qga channel in that same process still returns `ConnectionRefusedError(111, 'Connection refused')`. That makes the persistent failure look specific to the qga/virtserial channel's attach state, lifecycle, or libvirt-managed integration rather than to basic AF_UNIX reachability on the host.
