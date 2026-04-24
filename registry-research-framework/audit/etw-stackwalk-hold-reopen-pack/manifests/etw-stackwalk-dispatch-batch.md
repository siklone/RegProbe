# ETW Stackwalk Dispatch Batch

- Batch status: `ready`
- Mapped candidates: `2`
- Ready capture configs: `2`
- Dispatch recommended now: `0`
- Active candidates: `0`
- Intentional-hold candidates: `2`
- Profiles used: `execution-required-audio-stackwalk-v1, execution-required-system-stackwalk-v1`

## Candidates

### power.control.allow-audio-to-enable-execution-required-power-requests

- Queue state: `blocked`
- Promotion state: `blocked`
- Missing layer: `intentional-hold`
- Actionability: `hold`
- Blockers: `['audio-execution-required-no-current-build-registry-seeding-path', 'audio-execution-required-no-primary-current-build-doc', 'intentional-hold']`
- Profile: `execution-required-audio-stackwalk-v1`
- Registry target: `HKLM\SYSTEM\CurrentControlSet\Control\Power` / `AllowAudioToEnableExecutionRequiredPowerRequests`
- Run id: `wave4-allow-audio-e2e`
- Host ETL path: `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl`
- Stackwalk events: `RegCreateKey, RegOpenKey, RegQueryKey, RegSetValue, RegQueryValue, RegDeleteValue, RegCloseKey`
- Dispatch recommended: `False`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --print-effective-config
```

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --ingest-to-repo --refresh-ghidra
```

### power.control.allow-system-required-power-requests

- Queue state: `blocked`
- Promotion state: `blocked`
- Missing layer: `intentional-hold`
- Actionability: `hold`
- Blockers: `['intentional-hold', 'system-execution-required-no-current-build-registry-seeding-path']`
- Profile: `execution-required-system-stackwalk-v1`
- Registry target: `HKLM\SYSTEM\CurrentControlSet\Control\Power` / `AllowSystemRequiredPowerRequests`
- Run id: `wave4-allow-system-required-e2e`
- Host ETL path: `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl`
- Stackwalk events: `RegCreateKey, RegOpenKey, RegQueryKey, RegSetValue, RegQueryValue, RegDeleteValue, RegCloseKey`
- Dispatch recommended: `False`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --print-effective-config
```

```bash
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --candidate-id power.control.allow-system-required-power-requests --ingest-to-repo --refresh-ghidra
```
