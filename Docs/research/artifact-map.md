# Research Artifact Map

Generated: `2026-05-14T16:08:18Z`

This is the contributor entrypoint for current research artifacts. Use it
instead of browsing raw audit folders first.

## Current Gates

- App-card contracts: `258` pass, `0` fail.
- Promoted app QA latest: `14` live success, `0` live failure.
- Contributor Lab VM smoke: `ok`.
- Operator96 app-card ready: `0`.
- Operator96 noisy results: `0`; non-ok results: `0`.
- Cleanup delete-eligible items: `0`.
- Cleanup retained inventory: `89`; reference migration needed: `0`; retention decision queue: `66`; audit-only retained: `19`.

## Rules

- `end_user_surface`: Normal users start from the WPF app and validated app-surface records, not raw audit folders.
- `operator96_surface`: Operator96 remains Contributor Lab / research observation unless ready_for_bounded_app_card is positive and all gates stay clean.
- `cleanup`: Do not delete archived/raw evidence unless the cleanup quarantine ledger reports live_reference_count=0 and a replacement or explicit obsolete reason exists. Use the retained inventory plan to reduce references before deletion.
- `performance_claims`: No benchmark/performance claim ships from a single noisy, low-confidence, or community-only observation.

## Canonical Artifacts

| ID | Tier | Status | Audience | Path | Use when | Avoid when |
|---|---|---|---|---|---|---|
| `app-surface-cards` | `canonical` | `reference` | end-user app, contributor | `Docs/research/app-surface/validated-registry-values.json` | You need to know which cards normal users may see. | You are evaluating unshipped Operator96 or raw experiment records. |
| `app-retest-readiness` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/app-retest-readiness-latest.json` | Before manual app retest or after changing cards/evidence/rollback mapping. | You need a per-card live apply report; use promoted-app-qa instead. |
| `app-card-contracts` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/app-card-evidence-contracts-latest.json` | After changing card copy, evidence drawer data, or app-surface records. | You need to prove registry mutation works; use promoted-app-qa. |
| `promoted-app-qa-live-batch` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/promoted-app-qa-batch-latest.json` | You need evidence that a representative shipped-card batch still applies and rolls back. | You need total coverage across every card; use promoted-app-qa-coverage. |
| `promoted-app-qa-coverage` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/promoted-app-qa-coverage-latest.json` | You need to know whether any promoted app-QA category remains uncovered. | You need newest run detail; use promoted-app-qa-live-batch. |
| `single-tweak-lookup` | `canonical-script` | `reference` | contributor, agentic AI | `registry-research-framework/scripts/check_single_tweak.py` | A user asks whether a key/value exists, is read, or is written by the app. | You need live app mutation; use check_single_tweak_app_qa.py or KVM app QA. |
| `operator96-low-noise-aggregate` | `canonical-research` | `ok` | contributor, research | `registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json` | You need to know whether noisy/non-ok reruns remain. | You are building normal end-user app cards. |
| `operator96-app-surface-review` | `canonical-research` | `research-only-ok` | contributor, research | `registry-research-framework/audit/operator96-app-surface-review-20260510.json` | You need to decide whether any Operator96 record may enter app cards. | You want an optimization claim; this review blocks unbounded claims. |
| `cleanup-quarantine-ledger` | `canonical-safety-ledger` | `no-delete-eligible` | maintainer, contributor | `registry-research-framework/audit/cleanup-quarantine-ledger-20260514.json` | Before deleting or moving any archived/raw evidence or historical parse artifact. | You are looking for shipped app state; use app-surface/readiness artifacts. |
| `cleanup-retained-inventory-plan` | `canonical-action-plan` | `retained-plan-ready` | maintainer, contributor | `registry-research-framework/audit/cleanup-retained-inventory-plan-20260514.json` | After the quarantine ledger reports retained inventory and you need to reduce references or decide explicit retention. | You need the deletion safety contract itself; use cleanup-quarantine-ledger. |
| `vm-health` | `canonical-latest` | `ok` | contributor, VM operator | `registry-research-framework/audit/vm-health-check-latest.json` | Before ETW, Ghidra, app deploy smoke, or registry mutation experiments. | You need historical VM incident context. |
| `kvm-app-publish-deploy-smoke` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/kvm-app-publish-deploy-smoke-latest.json` | After WPF/app-shell changes or before manual app retesting. | You need card-level apply/rollback evidence. |
| `kvm-contributor-lab-smoke` | `canonical-latest` | `ok` | contributor, release QA | `registry-research-framework/audit/kvm-app-contributor-lab-smoke-latest.json` | After Contributor Lab, startup navigation, or contributor readiness UI changes. | You need normal end-user card apply/rollback evidence. |
| `rejected-closure-ledger` | `historical-archive` | `archive` | contributor, audit | `registry-research-framework/audit/rejected-closure-ledger.md` | You need to understand why rejected does not mean evidence missing. | You are looking for active backlog. |
| `v36-clean-state-report` | `historical-checkpoint` | `archive` | contributor, audit | `registry-research-framework/audit/v36-clean-state-report.md` | You need the historical clean-state audit snapshot. | You need today's app/VM retest state. |

## Do Not Start From Raw Parse Folders

These paths are valid historical evidence, but they are not the first stop
for normal app QA or contributor onboarding:

- `registry-research-framework/audit/registry-value-experiments*`
- `registry-research-framework/audit/operator96-low-noise-rerun-tranche-*`
- `evidence/raw/**`
- `evidence/files/vm-tooling-staging/**`

If one of those files appears stale, add it to the cleanup quarantine
ledger and prove zero live references before deleting it.
