# policy.system.enable-virtualization decision gate review - 2026-04-12

## Decision

Keep `policy.system.enable-virtualization` blocked on `runtime_no_read`.

The record already has baseline existence, repo documentation, exact current-build string evidence, adjacent UAC-policy context, shell-safe runtime attempts, KVM bootlog work, and local-KD path/family confirmation. That is enough to keep the record in the research queue, but not enough to promote it.

The remaining gap is specific: none of the retained runtime lanes captured an exact live read of `EnableVirtualization`. This is evidence-missing, not an intentional hold and not a documentation-first review.
