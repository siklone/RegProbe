# PowerRequestOverride Reader-Binding Result Ledger Template - 2026-04-19

Use this template after the next two reacquired local-KD artifacts land:

- `local-kd-powerrequest-response-reacquire`
- `local-kd-powerrequest-umpo-message-reacquire`

The pipeline autofill outputs are local-only scratch drafts. After review, promote them into dated audit files instead of committing the ignored autofill names directly:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id <dated-run-id>
```

By default the promote step refuses to overwrite an existing dated ledger; use `--force` only for an intentional replacement.

Use `--dry-run` first if you want to confirm the dated target names before moving the draft files.

## Fill-After-Run Fields

### Run Metadata

- Run ID: `<replace-with-run-id>`
- VM: `Win25H2Clean`

### Artifact Paths

- Response stdout: `<path>`
- Response summary: `<path>`
- UMPO stdout: `<path>`
- UMPO summary: `<path>`

### Marker Review

#### Response artifact

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

#### UMPO artifact

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

## Choose Exactly One Outcome

- `direct-registry-read`
- `consumer-semantics-without-read`
- `umpo-boundary-is-best-signal`
- `wrapper-only-path`

## Why

Write one short justification that cites the actual marker set or concrete symbol/call detail seen in stdout.

## Red Flags

List any of:

- missing `REGPROBE_LOCALKD_BEGIN` / `REGPROBE_LOCALKD_END`
- missing stdout for one required artifact
- wildcard-only output with no useful `uf` body

## Stop Condition Triggered

- `true/false`

If true, stop the lane and explain which stop condition fired.

## Next Move

- Lane: `<kernel-side|umpo-boundary|power-service-follow-up>`
- Exact target: `<symbol-or-boundary>`

Keep the same non-goals:

- do not reopen a broad `*PowerRequest*Reg*` hunt
- do not rerun a subtree-wide runtime capture first
- do not spend the next sprint on another `powercfg` materialization cycle
