# Branch Cleanup Ledger

- Generated UTC: `2026-05-14T00:45:00Z`
- Main checkpoint: `7a8b89a91ee328e8adfe42f551f69387c569230c`
- Source PR: `#325`
- Merge strategy: squash

This ledger records branch cleanup candidates before pruning stale local
remote-tracking refs or deleting any remote branch. It does not authorize
deleting evidence, archived reports, ETLs, PMLs, XMLs, or parse artifacts.

## Policy

Delete a remote branch only when all of these are true:

- No open pull request points at the branch.
- The branch was merged or superseded by a merged PR.
- The artifacts are present on `main`.
- The branch name is listed in this ledger.

Do not delete:

- `dependabot/nuget/tests.integration/Microsoft.NET.Test.Sdk-18.5.1`
- Any branch with an open pull request.
- Any branch whose artifacts are not represented on `main`.

## Candidates

| Branch | Status | Replacement | Reason |
|---|---|---|---|
| `codex/standardize-app-low-noise-20260512` | merged-and-delete-requested | `main@7a8b89a91ee328e8adfe42f551f69387c569230c` | PR #325 merged by squash. |
| `codex/operator96-low-noise-rerun-tranche-*` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json` | Tranche artifacts are merged and summarized. |
| `codex/operator96-low-noise-rerun-plan` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/operator96-low-noise-rerun-plan-20260510.json` | Planning artifacts are merged. |
| `codex/operator96-low-noise-aggregate-app-review` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/operator96-app-surface-review-20260510.json` | Aggregate and app-surface review artifacts are merged. |
| `codex/operator96-app-surface-review` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/operator96-app-surface-review-20260510.json` | Review artifacts are merged. |
| `codex/app-retest-vm-qa-20260512` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/promoted-app-qa-batch-latest.json` | App QA artifacts are merged. |
| `codex/app-retest-card-contract-sweep` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/app-card-evidence-contracts-latest.json` | Card contract artifacts are merged. |
| `codex/app-retest-single-tweak-systemresponsiveness` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/single-tweak-app-qa-systemresponsiveness-live-latest.json` | Single tweak QA artifacts are merged. |
| `codex/cleanup-quarantine-ledger-refresh` | stale-remote-tracking-prune-candidate | `registry-research-framework/audit/cleanup-quarantine-ledger-20260510.json` | Cleanup quarantine ledger is merged. |

After this ledger is committed, `git remote prune origin` may be used to remove
local stale remote-tracking refs. Keep Dependabot PR `#298` separate.
