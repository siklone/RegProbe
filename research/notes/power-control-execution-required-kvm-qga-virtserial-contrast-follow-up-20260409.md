## Scope

Determine whether the persistent host-side refusal is specific only to the libvirt-managed qga socket path or whether it affects any virtserialport-attached unix socket inside the same live QEMU process.

## Findings

1. A temporary attached virtserial port hotplugged cleanly on the live VM:
   - QMP `device_add` created `virtserialport` id `codexport`
   - port metadata showed `nr = 16` and name `org.codex.porttest.0`
2. Its paired socket backend accepted a host-side AF_UNIX connection immediately:
   - temporary path `/tmp/regprobe-qga-porttest.sock`
   - direct connect result: `CONNECT_OK`
3. The live qga channel still refused the same host-side connect pattern:
   - `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
   - direct connect result: `ConnectionRefusedError(111, 'Connection refused')`
4. `info qtree` separated the two ports cleanly:
   - temporary `codexport` port 16: `guest off, host off`
   - live qga channel1 port 2: `guest on, host off`
5. The temporary port and backend were removed immediately after the contrast probe.

## Interpretation

The remaining KVM blocker is now narrower than both a generic host-to-QEMU unix-socket problem and a generic virtserialport-attached socket problem. Inside the same running QEMU process, a hot-added virtserialport with its own unix-socket backend accepted a host-side connection, while the live `org.qemu.guest_agent.0` channel still refused one. That leaves the retained failure centered on the qga channel's specific runtime state or libvirt/qga integration path, not on virtserial hotplug or unix-socket reachability in general.
