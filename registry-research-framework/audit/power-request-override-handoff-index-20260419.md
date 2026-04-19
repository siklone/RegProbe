# PowerRequestOverride Handoff Index - 2026-04-19

This is the single-entry index for the current `power.control.power-request-override-subtree` handoff package.

## Read Order

1. `research/records/power.control.power-request-override-subtree.json`
   Current canonical record.
2. `research/notes/power-control-power-request-override-subtree-runtime-proof-20260418.md`
   Process-leaf runtime proof.
3. `research/notes/power-control-power-request-override-subtree-driver-service-proof-20260418.md`
   Service and driver leaf runtime proof.
4. `research/notes/power-control-power-request-override-subtree-reader-binding-targets-20260419.md`
   Narrowed debugger target set.
5. `research/notes/power-control-power-request-override-subtree-message-boundary-hypothesis-20260419.md`
   Why the lane narrows to the response/message boundary.
6. `research/notes/power-control-power-request-override-subtree-kd-callee-shortlist-20260419.md`
   Immediate callee shortlist and retained artifact gap.
7. `registry-research-framework/audit/power-request-override-reader-binding-discriminator-20260419.md`
   High-level discriminator audit.
8. `registry-research-framework/audit/power-request-override-reader-binding-reacquire-plan-20260419.md`
   Operator-facing reacquire plan.
9. `scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py`
   One-shot pipeline runner for reacquire plus prefilled ledger generation.
10. `scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py`
   Top-level KVM wrapper for the two KD reacquire passes.
11. `registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt`
   Response-side KD command file.
12. `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`
   UMPO message KD command file.
13. `registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.md`
   Post-run stdout review rubric.
14. `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.md`
   Post-run result write-back template.

## Pipeline Runner

- `python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py`
- Dry-run first: `python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run`

## Runner

- `python3 scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py`

## Ready-To-Run Files

- `registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt`
- `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`

## Do Not Reopen

- generic `*PowerRequest*Reg*` symbol hunt
- broad subtree runtime capture
- another `powercfg` materialization cycle

## Expected Next Decision

After the two reacquired local-KD artifacts land, classify the run as one of:

- `direct-registry-read`
- `consumer-semantics-without-read`
- `umpo-boundary-is-best-signal`
- `wrapper-only-path`
