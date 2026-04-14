# ETW Stackwalk Execution Manifest

- Status: `idle`
- Include holds: `False`
- Requested candidates: `power.control.allow-audio-to-enable-execution-required-power-requests, power.control.allow-system-required-power-requests`
- Missing candidates: ``
- Selected entries: `0`
- Excluded entries: `2`
- Default selected jobs: `0`
- Default skipped hold jobs: `2`
- Next action: `Review excluded hold candidates and reopen intentionally if needed.`

## Entries

### power.control.allow-audio-to-enable-execution-required-power-requests

- Selected: `False`
- Selection reason: `excluded`
- Actionability: `hold`
- Blockers: `['audio-execution-required-no-current-build-registry-seeding-path', 'audio-execution-required-no-primary-current-build-doc', 'intentional-hold']`
- Registry target: `HKLM\SYSTEM\CurrentControlSet\Control\Power` / `AllowAudioToEnableExecutionRequiredPowerRequests`
- Run id: `wave4-allow-audio-e2e`
- Host ETL path: `evidence/files/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config
```

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --ingest-to-repo --refresh-ghidra
```

Prerequisites:
- Land a current-build boot/init reader or registry seeding caller proof.
- Land a primary current-build Microsoft document for the exact value semantics.
- Explicitly reopen the lane before dispatching runtime capture.

### power.control.allow-system-required-power-requests

- Selected: `False`
- Selection reason: `excluded`
- Actionability: `hold`
- Blockers: `['intentional-hold', 'system-execution-required-no-current-build-registry-seeding-path']`
- Registry target: `HKLM\SYSTEM\CurrentControlSet\Control\Power` / `AllowSystemRequiredPowerRequests`
- Run id: `wave4-allow-system-required-e2e`
- Host ETL path: `evidence/files/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config
```

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra
```

Prerequisites:
- Land a current-build boot/init reader or registry seeding caller proof.
- Explicitly reopen the lane before dispatching runtime capture.
