# PowerRequestOverride Reader-Binding Execution Manifest - 2026-04-19

- Status: `ready`
- Operator posture: `run-narrow-kd-pass`
- Next action: `Prefer the one-shot pipeline runner; if needed, fall back to the dedicated KVM wrapper or the two local-KD command files, then classify the result with the review rubric.`
- Selected count: `2`

## Pipeline Runner

- Path: `scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py`
- Example:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py
```

Dry-run the planned commands and expected artifact paths without touching the VM:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run
```

That dry-run payload also previews the scratch ledger paths and the exact dated promotion targets derived from the current run id.

## Runner

- Path: `scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py`
- Example:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py
```

## Entries

### local-kd-powerrequest-response-reacquire

- Command file: `registry-research-framework/audit/power-request-override-response-reacquire-local-kd-20260419.txt`
- Goal: reacquire response-side disassembly for `PopPowerRequestHandleRequestOverrideQueryResponse`
- Required outputs:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`
- Success markers:
  - `REGPROBE_LOCALKD_BEGIN`
  - `PopPowerRequestHandleRequestOverrideQueryResponse`
  - `REGPROBE_LOCALKD_END`

### local-kd-powerrequest-umpo-message-reacquire

- Command file: `registry-research-framework/audit/power-request-override-umpo-message-reacquire-local-kd-20260419.txt`
- Goal: reacquire UMPO message disassembly for `PopUmpoSendPowerMessage`
- Required outputs:
  - `stdout.txt`
  - `summary.json`
  - `local-kd.txt`
- Success markers:
  - `REGPROBE_LOCALKD_BEGIN`
  - `PopUmpoSendPowerMessage`
  - `REGPROBE_LOCALKD_END`

## Review Inputs

- `registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.md`
- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.md`

## Promotion

- Scratch outputs:
  - `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.json`
  - `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.md`
- Policy: these autofill outputs are local-only scratch drafts and stay gitignored by default.
- Promote only after review:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id <dated-run-id>
```

The promote step refuses to overwrite an existing dated ledger unless `--force` is passed intentionally.

Dry-run the target names first if you want to confirm the dated output paths without moving files:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id <dated-run-id> --dry-run
```

## Non-Goals

- Do not reopen a generic `*PowerRequest*Reg*` symbol hunt first.
- Do not rerun a broad subtree runtime capture first.
- Do not spend the next sprint on another `powercfg` materialization cycle.
