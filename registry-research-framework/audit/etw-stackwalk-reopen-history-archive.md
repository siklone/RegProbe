# ETW Stackwalk Reopen History Archive

- History status: `seed-required`
- History seed source: `baseline-archive`
- Transition status: `baseline`
- Current snapshot id: `ec5b6c91b4e6`
- Previous snapshot id: `None`
- Retained baseline snapshot id: `ec5b6c91b4e6`
- Operator blocker: `seed-previous-snapshot-from-baseline-archive`
- Next action: `Promote the retained baseline snapshot into snapshot.previous before expecting history-driven reopen diffs.`
- Manifest files copied: `6`
- Seed files copied: `2`
- Command files written: `3`
- Pack files checksummed: `13`

## Commands

- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json`
- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md`
- `mkdir -p registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6 && cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6/etw-stackwalk-reopen-snapshot.json && cp registry-research-framework/audit/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-history-store/ec5b6c91b4e6/etw-stackwalk-reopen-snapshot.md`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py`
