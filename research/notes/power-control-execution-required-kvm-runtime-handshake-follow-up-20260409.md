## Scope

Probe the post-open qga failure mode more directly after path discovery was closed as the leading KVM blocker for the execution-required pair.

## Findings

1. A new guest-side runtime diagnoser now exists:
   - `scripts/vm/diagnose-qga-runtime-handshake.ps1`
   - it stops existing qga processes, relaunches qga on the exact openable `...&02...` path with `-v -l`, tails the logfile, and scans recent Windows Application events
2. The exact-path runtime launch still does not produce a useful Windows-side error signal:
   - the retained console capture shows the logfile tail dominated by repeated `debug: dispatch`
   - there is no new explicit open-path failure
3. The same retained console capture also shows no recent matching Application events:
   - `=== APPLICATION EVENTS ===`
   - `NO_RECENT_MATCHING_EVENTS`
4. Host-side qga control still does not recover:
   - after the runtime-handshake probe, `guest-ping` returned `Guest agent is not responding: QEMU guest agent is not connected`
   - HMP `info chardev` still reported the qga channel as `disconnected`

## Interpretation

The KVM qga blocker is now narrower than a generic runtime/protocol failure, but it is also quieter than expected from inside Windows. After path discovery was closed, the next retained probe still does not surface a new Windows event-log explanation or a crisp qga startup error. The guest-side signal currently collapses to repeated `debug: dispatch` lines with no fresh matching Application events, while the host continues to see a disconnected qga channel. The remaining issue therefore looks like a silent post-launch qga stall or libvirt/KVM-side handshake failure rather than a missing path or a clear Windows-side runtime exception.
