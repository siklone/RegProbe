# ETW Stackwalk Reopen Baseline Archive

- Archive status: `baseline-ready`
- Transition status: `baseline`
- Retained snapshot id: `ec5b6c91b4e6`
- Operator blocker: `retain-baseline-for-next-diff`
- Next action: `Retain this snapshot as the next previous baseline before expecting diff-driven transition summaries.`
- Manifest files copied: `4`
- Command files written: `2`
- Pack files checksummed: `8`

## Commands

- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json`
- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py`
