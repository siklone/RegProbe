## Scope

Narrow the post-bootstrap KVM guest-control blocker for the execution-required pair from a generic qga runtime/protocol fault to the concrete Windows virtio-serial path resolution layer.

## Findings

1. The guest no longer runs an obviously stale qga payload:
   - a live in-guest `robocopy` replacement copied the modern 2025 qga payload over `C:\Program Files\qemu-ga`
   - the replacement copied 13 modern files including a `4.3 m` `qemu-ga.exe`
2. The Windows service still starts, but the host-side channel remains disconnected:
   - `sc query QEMU-GA` showed the service in `RUNNING` state with binary path `"C:\Program Files\qemu-ga\qemu-ga.exe" -d`
   - host-side HMP `info chardev` still reported `charchannel1: filename=disconnected:unix:.../org.qemu.guest_agent.0`
3. The guest exposes two concrete `VIOSERIALPORT` child devices:
   - `vport0p1`
   - `vport0p2`
4. The active domain XML cleanly maps the guest-agent channel onto virtio-serial port 2:
   - `com.redhat.spice.0` uses port `1`
   - `org.qemu.guest_agent.0` uses port `2`
5. The obvious guest-side path aliases are not materialized as openable device paths:
   - `Test-Path '\\.\Global\org.qemu.guest_agent.0'` returned `False`
   - `Test-Path '\\.\Global\vport0p2'` returned `False`
   - `Test-Path '\\.\vport0p2'` returned `False`
6. Direct qga launches on those obvious aliases all fail at the path-open step:
   - `qemu-ga.exe -v -m virtio-serial -p \\.\Global\org.qemu.guest_agent.0`
   - `qemu-ga.exe -v -m virtio-serial -p \\.\Global\vport0p2`
   - `qemu-ga.exe -v -m virtio-serial -p \\.\vport0p2`
   - each failed with `critical: error opening path`, then `critical: error opening channel`, and finally `critical: failed to create guest agent channel`
7. DeviceClasses evidence shows the qga-side child really exists, but only as an encoded interface entry:
   - `Control\DeviceClasses` contains an encoded interface key for the `...&02` `VioSerialPort` child
   - the naive `org.qemu.guest_agent.0` and `vport0p2` aliases are therefore not the actual openable path on this guest

## Interpretation

The remaining KVM blocker is now narrower than "qga installed but broken" or a generic post-bootstrap protocol fault. The guest has a modern qga payload, a running Windows service, a connected virtio-serial controller, and visible `VIOSERIALPORT` child devices, but the guest-agent channel still shows as `disconnected` on the host and the obvious path aliases do not exist or open. That narrows the current failure to Windows virtio-serial path resolution for the qga port, likely between the encoded `Control\DeviceClasses` interface entry and the concrete `CreateFile()` path that `qemu-ga.exe` must use on this stack.
