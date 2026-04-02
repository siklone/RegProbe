# Pipeline v3.1

`v3.1` is the active orchestration layer for RegProbe research output. It routes runtime, static, behavior, and audit metadata into `full-evidence.json`, then turns that into a classification decision.

Wave 1 quality hardening tightened the contract in six places:

- artifact metadata is mandatory for physical evidence
- `staged` manifests stay visible, but no longer count as captured proof
- runtime wrappers emit explicit capture truth fields
- `evidence` vs `operational` collection mode is explicit
- kernel, boot, and driver records must satisfy live runner coverage
- VM credentials resolve through a shared secure helper instead of repo literals

## Output Shape

`full-evidence.json` now expects structured `artifact_refs` entries.

Required fields for any physical artifact:

- `path`
- `sha256`
- `size`
- `collected_utc`

Optional passthrough fields may still appear when relevant:

- `id`
- `filename`
- `release_url`
- `kind`
- `exists`
- `placeholder`

Loose string refs are legacy input only. The pipeline normalizes them before writing the final evidence surface.

## Runtime Lane Truthfulness

Runtime wrappers should emit these fields consistently:

- `status`
- `capture_status`
- `capture_artifacts`
- `collection_mode`
- `runner_required`

`capture_status` has strong semantics:

- `captured`
  physical capture artifacts exist
- `staged`
  a plan or manifest exists, but no proof artifact was captured yet
- `missing-capture`
  the lane claimed capture semantics, but the physical artifact is absent
- `runner-ok`
  pipeline-level validation passed for the lane
- `missing-required-runner`
  audit metadata required a live runner and none was available
- `staged-without-capture`
  a runner exists only as staged metadata and does not satisfy evidence requirements

The pipeline never treats `staged` as equivalent to executed evidence.

## Runner Coverage Policy

Runner coverage enforcement is config-driven.

Current policy file:

- [runner-coverage-policy.json](H:/D/Dev/RegProbe/registry-research-framework/config/runner-coverage-policy.json)

Default rule:

- if `suspected_layer` is `kernel`, `boot`, or `driver`, require a real mapped runtime capture lane
- if `boot_phase_relevant = true`, require the same even when the layer label is incomplete
- `user-mode` records may remain optional or staged

Enforcement lives in [v31_pipeline.py](H:/D/Dev/RegProbe/registry-research-framework/pipeline/v31_pipeline.py). Resolver scripts stay advisory.

## Collection Modes

Research runners expose `-CollectionMode evidence|operational`.

Default:

- `evidence`

`evidence` mode expectations:

- do not auto-rollback
- preserve pre-change export
- preserve post-change export
- mark `rollback_pending = true`

`operational` mode:

- allows legacy convenience rollback behavior
- should not be used by default when collecting proof

If rollback happens later for an evidence run, preserve the trail:

- export before rollback
- export after rollback
- write a diff artifact

## VM Credential Handling

All repo-tracked VMware scripts should resolve credentials through:

- [scripts/vm/_vmrun-common.ps1](H:/D/Dev/RegProbe/scripts/vm/_vmrun-common.ps1)

Resolution order:

1. explicit `PSCredential` or secure parameter input
2. environment variables
3. DPAPI-protected CLIXML credential file outside the repo

`vmrun` still requires credentials at invocation time. The repo contract is narrower:

- no plaintext guest passwords in tracked scripts
- masked logging for `vmrun` argv
- shared auth resolution instead of per-script literals

## Sanitization

Public artifact sanitization is config-driven.

Script:

- [sanitize_public_artifacts.ps1](H:/D/Dev/RegProbe/scripts/sanitize_public_artifacts.ps1)

Config:

- [artifact-sanitization-rules.json](H:/D/Dev/RegProbe/registry-research-framework/config/artifact-sanitization-rules.json)

Supported rule types:

- `literal`
- `regex`

Default behavior is dry-run. `-Apply` also writes a change manifest and backup trail.

## Validation Expectations

Wave 1 is considered healthy when all of the following are true:

- no `full-evidence.json` writes bare string `artifact_refs`
- kernel, boot, and driver records cannot finish green with `staged` or missing physical capture
- repo-tracked VM scripts contain no plaintext guest password literals
- sanitization works from config without code edits

## Current Roadmap After Wave 1

Wave 1 does not replace the active research roadmap. It hardens its output contract.

Current execution order:

1. safe mega-trigger stabilization
2. `WinDbg` dead-flag lane for no-hit leftovers
3. source-enrichment cross-checks

All three should write the same artifact metadata, collection mode, runner coverage, and sanitization-friendly outputs described above.

## Wave 2 Metadata Layers

The pipeline now also publishes second-order research metadata instead of keeping it implicit:

- freshness
  `os_build`, `evidence_collected_utc`, `revalidation_needed_on_major_update`, `expires_after_build`
- reproducibility
  baseline VM identity, snapshot name, and tracked tool context
- interaction graph
  grouped key clusters with partial-risk notes
- tweak dependencies
  `requires`, `conflicts_with`, `recommended_with`
- anti-cheat risk
  advisory-only risk tags for gaming and kernel integrity concerns
- negative evidence
  standardized packages for archived or no-hit records

Generated surfaces:

- [regression-history.json](H:/D/Dev/RegProbe/research/regression-history.json)
- [evidence-not-found/index.json](H:/D/Dev/RegProbe/research/evidence-not-found/index.json)

Behavior lanes can now distinguish:

- `significant-change`
- `no-demonstrated-change`
- `insufficient`

Non-significant benchmark deltas should not be written up as wins.
