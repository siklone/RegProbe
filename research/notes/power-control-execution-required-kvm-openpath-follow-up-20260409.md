## Scope

Retest whether Windows virtio-serial path resolution is still the real blocker for the execution-required pair after the earlier KVM vioser-path diagnosis.

## Findings

1. A new guest-side helper now probes qga paths directly from inside Windows:
   - `scripts/vm/diagnose-qga-vioserial-path.ps1`
   - it enumerates `VIOSERIALPORT` devices, pulls candidate interface paths from `pnputil` and `Control\DeviceClasses`, and attempts direct `CreateFile()` opens
2. The default qga alias is now proven openable from the guest:
   - `AliasTest=\\.\Global\org.qemu.guest_agent.0 => OK`
3. The exact `...&02...` DeviceClasses-derived path is also openable:
   - the retained guest probe reports `OpenTest=...&02... => OK`
4. The sibling `...&01...` path is not the qga port:
   - the retained guest probe reports `OpenTest=...&01... => FAIL 5 Access is denied`
5. The naive `vport0p2` aliases still do not resolve:
   - `AliasTest=\\.\Global\vport0p2 => FAIL 2`
   - `AliasTest=\\.\vport0p2 => FAIL 2`
6. Restarting the Windows service does not restore host-side guest control even after the alias/open-path proof:
   - `sc start QEMU-GA`
   - `sc query QEMU-GA`
   - the service returns to `RUNNING`
   - host-side `guest-ping` still fails with `QEMU guest agent is not available due to an error`
   - host-side HMP `info chardev` still shows the qga channel as `disconnected`
7. A second new guest-side helper now launches qga on the exact openable `...&02...` path:
   - `scripts/vm/run-qga-on-open-vioserial-path.ps1`
   - it reads the probe output, selects the `...&02...` `OpenTest=... => OK` path, and starts `qemu-ga.exe -v -m virtio-serial -p <exact path>`
8. Even the exact `...&02...` foreground launch lands in the same failure family:
   - the new window enters a continuous `debug: dispatch` loop
   - host-side `guest-ping` still returns `QEMU guest agent is not available due to an error`
   - host-side HMP still reports the qga chardev as `disconnected`

## Interpretation

The earlier working theory that the remaining KVM blocker was Windows virtio-serial path resolution is no longer supported by the latest live evidence. The guest can open both the default `\\.\Global\org.qemu.guest_agent.0` alias and the exact `...&02...` DeviceClasses-derived interface path, and qga can be launched explicitly on that exact path, yet host-side qga control still remains in the same disconnected/error state. The unresolved layer is now post-open qga runtime/protocol or libvirt/KVM handshake behavior, not path discovery.
