# ETW Stackwalk Reopen Baseline Archive

- Generated UTC: `2026-04-15T02:29:15.370547Z`
- Archive status: `baseline-ready`
- Transition status: `baseline`
- Retained snapshot id: `ec5b6c91b4e6`
- Operator blocker: `retain-baseline-for-next-diff`
- Next action: `Retain this snapshot as the next previous baseline before expecting diff-driven transition summaries.`
- Manifest files copied: `None`
- Command files written: `None`
- Pack files checksummed: `None`

## Commands

- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json`
- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py`
