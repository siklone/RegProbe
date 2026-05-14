# V36 Clean State Report

Generated: `2026-05-08T11:38:15.000794Z`
Campaign: `v36-repository-zero-pending`
Status: `clean-state`

This report is the zero-pending snapshot for the v3.6 research surface. It combines promotion gates, rejected closure lanes, the promotion review pack, the blocked worklist, and app retest readiness into one audit contract.

## Dashboard

| Metric | Value |
|---|---:|
| Total records | 356 |
| Promoted | 261 |
| Rejected | 95 |
| Blocked | 0 |
| Promotion eligible | 0 |
| Revalidation pending | 0 |
| Invalid gate entries | 0 |
| Unclassified rejected | 0 |
| Active backlog | 0 |
| Limbo count | 0 |
| App surface entries | 265 |
| Apply-allowed records | 261 |
| Records missing validation proof | 17 |

## Clean-State Checks

| Check | Result |
|---|---:|
| `no_blocked_gate_entries` | `PASS` |
| `no_revalidation_pending_gate_entries` | `PASS` |
| `no_promotion_eligible_gate_entries` | `PASS` |
| `no_invalid_gate_entries` | `PASS` |
| `no_unclassified_rejected_records` | `PASS` |
| `no_promotion_review_records` | `PASS` |
| `blocked_worklist_empty` | `PASS` |
| `app_retest_readiness_pass` | `PASS` |
| `all_records_classified` | `PASS` |

## Rejected Archive

| Metric | Value |
|---|---:|
| Total rejected | 95 |
| Evidence-backed rejected | 40 |
| Deprecated records | 55 |

### Closure Kind Counts

| Closure kind | Count |
|---|---:|
| `deprecated-record` | 55 |
| `environment-limited-validation-lane` | 2 |
| `intentional-hold-closed` | 19 |
| `non-reversible-or-high-risk-action` | 17 |
| `protected-acl-not-actionable` | 1 |
| `security-hold-closed` | 1 |

## Verification

| Surface | Value |
|---|---|
| `app_retest_readiness` | `PASS` |
| `blocked_worklist_count` | `0` |
| `kvm_app_smoke_status` | `ok` |
| `kvm_lane_health_status` | `ok` |
| `missing_rollback_story_count` | `0` |

## Source Artifacts

| Artifact | Path |
|---|---|
| `promotion_gates` | `research/promotion-gates.json` |
| `rejected_closure_ledger` | `registry-research-framework/audit/rejected-closure-ledger.json` |
| `promotion_eligible_review_pack` | `registry-research-framework/audit/promotion-eligible-review-pack.json` |
| `blocked_worklist` | `registry-research-framework/audit/blocked-worklist.json` |
| `app_retest_readiness` | `registry-research-framework/audit/app-retest-readiness-latest.json` |

## Next Phase

- `qga-etw-ghidra-backfill` (optional): Backfill deeper ETW/Ghidra bundles for promoted records if the VM transport lane needs more proof density.
- `v37-candidate-discovery` (optional): Start a new candidate wave only after preserving this v36 zero-pending snapshot.
- `app-retest` (recommended): Use the app retest readiness report before manual Windows validation of cards, evidence drawers, apply, verify, and rollback.
