# Static Pipeline Backlog

This file tracks the post-v3.2 static-analysis work that is either live now or intentionally queued for follow-up.

## Live In The Pipeline

- Bounded static branch template is now the default review shape:
  - register focus
  - flag focus
  - compare condition
  - jump condition
  - branch effect
- Stack-aware notes are first-class output. Static exports now call out stack-relative access so reviewers do not overclaim semantics from RSP/RBP-heavy snippets.
- Exception-aware review gating is live. `INT3`, `INT1`, `UD2`, `HLT`, and `ICEBP` adjacency now force review-only output.
- Heuristic triage scoring is live. Static exports attach a `0-100` score plus compact reasons instead of treating string hits as enough by themselves.
- Function confidence is now explicit:
  - `symbolized_branch`
  - `string_only_review`
- Doc vs static separation is now explicit in `doc_source`. Microsoft docs can carry policy/intent, but binary semantics must come from static or runtime evidence.
- Cross-tool parity reports now follow one compact sentence:
  - Ghidra function
  - IDA function
  - branch match/conflict
  - value map
  - verdict
- Dynamic import resolution probing now has a dedicated lane:
  - `registry-research-framework/tools/run-import-dynamic-resolution-probe.ps1`
  - `scripts/find_dynamic_resolution_patterns.py`

## Follow-Up Lanes

- Kernel memory / paging notes lane
  - Goal: capture when virtual-to-physical translation, paging state, or memory-manager behavior becomes the real blocker.
  - Status: queued for future work. Not needed for normal throughput yet.

- Hypervisor / EPT lane
  - Goal: support early-boot, hidden-read, and watchdog-class investigations that stay opaque under normal VM traces.
  - Status: queued for future work. Valuable, but expensive enough to keep out of the default pipeline.
