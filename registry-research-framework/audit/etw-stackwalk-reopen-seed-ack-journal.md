# ETW Stackwalk Reopen Seed Ack Journal

- Ack status: `awaiting-application`
- Ack mode: `apply-seed`
- Receipt status: `pending`
- Rotation status: `seed-pending`
- Rotation mode: `seed-from-baseline`
- Operator blocker: `seed-not-yet-applied`
- Next action: `Run the seed commands, then regenerate the seed receipt and rotation ledger.`
- Top rotation candidate: `power.control.allow-audio-to-enable-execution-required-power-requests`

## Verification

- Previous snapshot present: `False`
- Previous matches current snapshot: `False`
- Previous matches retained baseline: `False`
- Rotation prerequisites pending: `True`

## Commands

- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.json registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.json`
- `cp registry-research-framework/audit/etw-stackwalk-reopen-baseline-archive/manifests/etw-stackwalk-reopen-snapshot.md registry-research-framework/audit/etw-stackwalk-reopen-snapshot.previous.md`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_transition_summary.py`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_seed_receipt.py`
- `python3 registry-research-framework/scripts/generate_etw_stackwalk_reopen_rotation_ledger.py`
