# ETW Stackwalk Reopen Seed Receipt

- Receipt status: `pending`
- Receipt mode: `await-seed`
- Operator blocker: `seed-not-applied`
- Next action: `Apply the retained baseline seed commands, then refresh the transition summary and rotation ledger.`
- Current snapshot id: `ec5b6c91b4e6`
- Previous snapshot id: `None`
- Retained baseline snapshot id: `ec5b6c91b4e6`

## Verification

- Previous snapshot present: `False`
- Previous matches current snapshot: `False`
- Previous matches retained baseline: `False`

## Commands

- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json`
- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py`
