# power.control.hiber-file-size-percent decision gate review - 2026-04-12

## Decision

Keep `power.control.hiber-file-size-percent` blocked on `runtime_no_read`.

The record has a strong path and state package: clean baseline default, repo power-note coverage, current-build string evidence, lightweight ETW context, KVM local-KD symbol/state confirmation, adjacent `Control\Power` runtime activity, and a reboot observation proving the value persists at `0` across a real guest reboot.

The remaining gap is still exact runtime attribution. The retained runtime lanes did not capture a live `HiberFileSizePercent` registry read, so this stays evidence-missing rather than promoted. It is not an intentional hold and not a documentation-first review.
