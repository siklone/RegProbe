## Scope

Test whether the freshly observed qga boot-time crash is deterministic by checking a later reboot without changing the installed `QEMU-GA` service configuration.

## Findings

1. A later retained reboot no longer reproduced the earlier boot-stop state:
   - the first retained post-reboot admin console now shows `START_TYPE : 2 AUTO_START`
   - the same console also shows `STATE : 4 RUNNING`
2. That means the earlier `STOPPED / 1067` capture is real but not deterministic:
   - no delayed-auto configuration was retained in the later service query
   - the service reached `RUNNING` again while still configured as ordinary `AUTO_START`
3. The stable part of the failure remained unchanged across that reboot:
   - host-side `guest-ping` still returned `QEMU guest agent is not available due to an error`
   - `query-chardev` still reported `frontend-open=true`
   - `info qtree` still reported `port 2, guest on, host off`
   - direct host-side connect to the qga unix socket still returned `Connection refused`
4. The host daemon logs now match the retained socket behavior:
   - `journalctl`
   - `virtqemud` logged `failed to connect to agent socket: Connection refused`
   - the same retained slice also logged `Cannot connect to QEMU guest agent for regprobe-win11-25h2-session`

## Interpretation

The current KVM qga blocker is now better described as a stable host-side attach failure plus an intermittent Windows service-start issue, not a single deterministic boot-time service crash. One retained reboot captured `QEMU-GA` stopped with exit code 1067 and an unexpected-termination System log entry, but a later reboot with the service still configured as `AUTO_START` came back `RUNNING` on its own. Despite that automatic recovery, the host side remained unchanged: `frontend-open=true`, `guest on, host off`, `guest-ping` error, direct `Connection refused` on the qga unix socket, and matching `virtqemud` journal lines that explicitly say `failed to connect to agent socket: Connection refused`. So the persistent blocker is still the host/libvirt/QEMU-side attach or accept path; the boot-time service crash now looks intermittent rather than foundational.
