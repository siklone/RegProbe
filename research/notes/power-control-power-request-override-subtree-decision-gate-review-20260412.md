# power.control.power-request-override-subtree decision gate review - 2026-04-12

## Decision

Keep `power.control.power-request-override-subtree` blocked on specific evidence gaps.

The retained runtime trace proves the override family is active, but it still shows root queries and `NAME NOT FOUND` probes under `Driver`, `Process`, and `Service` rather than stable leaf-value reads. The static context is adjacent, not leaf-specific, and the restore story is still only a retained subtree-presence snapshot.

This is not an intentional hold and not a documentation-first review. Promotion requires stable leaf semantics, a cleaner reader binding, and a reliable restore model.
