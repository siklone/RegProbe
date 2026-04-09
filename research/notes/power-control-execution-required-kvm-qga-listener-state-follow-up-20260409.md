## Scope

Check whether the live qga unix socket is simply missing a host-side listener, or whether it is already present as a qemu-owned listening socket when direct host-side connect still fails.

## Findings

1. Host-side socket tables show the live qga path as an active qemu-owned listener:
   - `ss -xlpn` reports `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0` as `LISTEN`
   - the listener is owned by `qemu-system-x86` pid `8704`, fd `107`
2. `lsof -U -a -p 8704` confirms the same picture:
   - the live qga path is present as `type=STREAM (LISTEN)`
   - the sibling monitor socket is also present on the same qemu pid
3. Direct host-side `AF_UNIX` connect results still split:
   - qga path: `ConnectionRefusedError(111, 'Connection refused')`
   - monitor path: `BlockingIOError(11, 'Resource temporarily unavailable')`
4. Both socket paths are ordinary filesystem sockets with the same ownership and mode family:
   - both are `srwxrwxr-x`
   - both are owned by `rai:rai`

## Interpretation

The remaining blocker is no longer explained by a missing host-side listener on the qga path. The live qga socket is already visible as a qemu-owned `LISTEN` endpoint in both `ss` and `lsof`, yet direct host-side connect still returns `ConnectionRefusedError(111, 'Connection refused')`. Because the sibling qemu monitor socket on the same process does not fail with the same refusal, the remaining failure is now best framed as qga-listener-specific accept or integration behavior rather than simple listener absence, guest-side open state, or host path placement.
