## Scope

Check whether the persistent qga refusal is specific to qga special handling, or whether any libvirt-managed unix `virtio` channel on the same running domain behaves the same way.

## Findings

1. A temporary libvirt-managed unix channel attached successfully on the live domain:
   - `virsh attach-device ... --live`
   - target name: `org.codex.libvirttest.0`
   - socket path: `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/codex.libvirttest.sock`
2. The live domain XML and `info qtree` showed the attached channel clearly:
   - alias `channel2`
   - `port 3, guest off, host off`
3. Direct host-side `AF_UNIX` connect to the temporary libvirt-managed socket succeeded:
   - result: `CONNECT_OK`
4. The live qga socket still refused at the same time:
   - path: `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
   - result: `ConnectionRefusedError(111, 'Connection refused')`
5. The temporary channel detached cleanly:
   - `virsh detach-device ... --live`
   - the temporary socket disappeared
   - the live qga socket remained

## Interpretation

The persistent refusal is no longer explainable as a generic libvirt-managed unix `virtio` channel problem. A temporary channel attached through libvirt on the same running domain creates a host-connectable socket immediately, while the live qga socket still refuses the same direct connect pattern. That leaves the blocker centered on qga-specific libvirt/QEMU integration or qga-special channel handling rather than on libvirt-managed unix channels in general.
