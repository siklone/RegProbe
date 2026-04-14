# ETW Stackwalk Reopen Operator Brief

- Delta status: `blocked`
- Pack status: `ready`
- Brief status: `blocked`
- Operator blocker: `reopen-prerequisites-blocked`
- Next action: `Do not run the include-holds commands yet; land the next unlock prerequisite first.`
- Candidate count: `2`
- Blocked candidates: `2`
- Review-ready candidates: `0`

## Entries

### power.control.allow-audio-to-enable-execution-required-power-requests

- Brief status: `blocked`
- Operator posture: `do-not-run`
- Remaining to ready: `3`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-audio-to-enable-execution-required-power-requests --run
```

### power.control.allow-system-required-power-requests

- Brief status: `blocked`
- Operator posture: `do-not-run`
- Remaining to ready: `2`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`
- Next action hint: `Reopen only when a boot/init reader or registry seeding caller pivot becomes available.`

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests
```

```bash
python3 registry-research-framework/scripts/run_etw_stackwalk_dispatch_batch.py --include-holds --candidate-id power.control.allow-system-required-power-requests --run
```
