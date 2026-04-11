## Scope

Check whether the persistent qga socket refusal is caused by the libvirt qemu channel-directory path itself.

## Findings

1. A temporary QMP socket backend created directly inside the same live libvirt channel directory accepted a host-side AF_UNIX connection:
   - path: `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/codex.same-dir.sock`
   - direct connect result: `CONNECT_OK`
2. The live qga socket in that same directory still refused the same connect pattern:
   - path: `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
   - direct connect result: `ConnectionRefusedError(111, 'Connection refused')`
3. The temporary backend was removed immediately after the contrast probe.

## Interpretation

The libvirt qemu `run/channel` directory itself is not the source of the persistent host-side refusal. A temporary unix-socket backend in that same directory can accept a host-side connection, while the live qga socket beside it still refuses one. That leaves the failure centered on the live qga channel state or qga integration path rather than on the host path prefix or directory placement.
