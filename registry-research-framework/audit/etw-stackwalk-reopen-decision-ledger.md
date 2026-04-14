# ETW Stackwalk Reopen Decision Ledger

- Pack status: `ready`
- Ledger status: `deferred`
- Reopen candidates: `2`
- Deferred candidates: `2`
- Review-ready candidates: `0`
- Next action: `Keep the ETW lane closed until one of the listed prerequisites lands.`

## Entries

### power.control.allow-audio-to-enable-execution-required-power-requests

- Decision state: `defer`
- Reason codes: `['await-seeding-pivot', 'await-primary-doc', 'explicit-reopen-required']`
- Blockers: `['audio-execution-required-no-current-build-registry-seeding-path', 'audio-execution-required-no-primary-current-build-doc', 'intentional-hold']`
- Prerequisites: `['Land a current-build boot/init reader or registry seeding caller proof.', 'Land a primary current-build Microsoft document for the exact value semantics.', 'Explicitly reopen the lane before dispatching runtime capture.']`
- Next review trigger: `Revisit after a current-build seeding-path pivot and a primary Microsoft doc both land.`
- Run id: `wave4-allow-audio-e2e`
- Host ETL path: `evidence/files/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl`

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run
```

### power.control.allow-system-required-power-requests

- Decision state: `defer`
- Reason codes: `['await-seeding-pivot', 'explicit-reopen-required']`
- Blockers: `['intentional-hold', 'system-execution-required-no-current-build-registry-seeding-path']`
- Prerequisites: `['Land a current-build boot/init reader or registry seeding caller proof.', 'Explicitly reopen the lane before dispatching runtime capture.']`
- Next review trigger: `Revisit after a current-build boot/init reader or registry seeding caller pivot lands.`
- Run id: `wave4-allow-system-required-e2e`
- Host ETL path: `evidence/files/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl`

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run
```
