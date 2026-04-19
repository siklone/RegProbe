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
- Preflight only: `python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only`
- That dry-run preview already includes the scratch ledger paths plus the exact dated promotion targets for the current run id.
- The `--verify-only` payload now returns `ready_for_execute`, the verifier `blockers` list, an explicit output contract preview, and a `next_steps` block so the operator sees the recommended follow-up command immediately. The dry-run preview mirrors that same contract before the VM lane is touched.

## Bundle Verifier

- `python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py`
- Markdown summary: `python3 registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py --markdown`
- Same preflight through the pipeline entry point: `python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only`
- The verifier now returns `ready_for_execute`, a compact summary block, an explicit `blockers` list, and concrete next-step guidance.
- Expected JSON contract: `ready_for_execute`, `summary`, `blockers`, `next_steps`
- The execute path runs this verifier by default before it touches the VM.
- When the bundle is not ready, the pipeline `next_steps.recommended_example` falls back to the markdown summary command for quick human triage.
- Only bypass it intentionally: `python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --skip-bundle-verifier`

## Runner

- `python3 scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py`

## Promotion

- Scratch outputs stay local-only and gitignored by default:
  - `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.json`
  - `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.md`
- After review, promote the draft into dated audit files:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id <dated-run-id>
```

The promote step refuses to overwrite an existing dated ledger unless `--force` is passed intentionally.

If you just want to confirm the destination names first:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id <dated-run-id> --dry-run
```

For the current default run id, the dated target pair is:

- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.json`
- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-power-request-override-reader-binding-reacquire.md`

Exact promote command for the current default run id:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id power-request-override-reader-binding-reacquire
```

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
