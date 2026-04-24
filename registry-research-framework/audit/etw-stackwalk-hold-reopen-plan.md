# ETW Stackwalk Hold Reopen Plan

- Default run mode: `dry-run`
- Default selected jobs: `0`
- Default skipped hold jobs: `2`
- Reopen candidates: `2`

## Candidates

### power.control.allow-audio-to-enable-execution-required-power-requests

- Feature area: `Control Power Requests`
- Missing layer: `intentional-hold`
- Blockers: `['audio-execution-required-no-current-build-registry-seeding-path', 'audio-execution-required-no-primary-current-build-doc', 'intentional-hold']`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`
- Run id: `wave4-allow-audio-e2e`
- Host ETL path: `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl`

Prerequisites:
- Land a current-build boot/init reader or registry seeding caller proof.
- Land a primary current-build Microsoft document for the exact value semantics.
- Explicitly reopen the lane before dispatching runtime capture.

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run
```

### power.control.allow-system-required-power-requests

- Feature area: `Control Power Requests`
- Missing layer: `intentional-hold`
- Blockers: `['intentional-hold', 'system-execution-required-no-current-build-registry-seeding-path']`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`
- Run id: `wave4-allow-system-required-e2e`
- Host ETL path: `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl`

Prerequisites:
- Land a current-build boot/init reader or registry seeding caller proof.
- Explicitly reopen the lane before dispatching runtime capture.

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run
```
