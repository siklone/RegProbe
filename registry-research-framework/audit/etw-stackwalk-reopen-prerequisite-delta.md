# ETW Stackwalk Reopen Prerequisite Delta

- Ledger status: `deferred`
- Delta status: `blocked`
- Candidate count: `2`
- Blocked candidate count: `2`
- Clear candidate count: `0`
- Outstanding reason counts: `{'await-seeding-pivot': 2, 'await-primary-doc': 1, 'explicit-reopen-required': 2}`
- Unique prerequisite count: `3`
- Next action: `Use the delta entries to land the next outstanding prerequisite before reopening the ETW lane.`

## Unique Prerequisites

- `Explicitly reopen the lane before dispatching runtime capture.` -> `['power.control.allow-audio-to-enable-execution-required-power-requests', 'power.control.allow-system-required-power-requests']`
- `Land a current-build boot/init reader or registry seeding caller proof.` -> `['power.control.allow-audio-to-enable-execution-required-power-requests', 'power.control.allow-system-required-power-requests']`
- `Land a primary current-build Microsoft document for the exact value semantics.` -> `['power.control.allow-audio-to-enable-execution-required-power-requests']`

## Entries

### power.control.allow-audio-to-enable-execution-required-power-requests

- Delta status: `blocked`
- Outstanding reason codes: `['await-seeding-pivot', 'await-primary-doc', 'explicit-reopen-required']`
- Outstanding reason classes: `['evidence-gap', 'operator-decision']`
- Remaining to ready: `3`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`

### power.control.allow-system-required-power-requests

- Delta status: `blocked`
- Outstanding reason codes: `['await-seeding-pivot', 'explicit-reopen-required']`
- Outstanding reason classes: `['evidence-gap', 'operator-decision']`
- Remaining to ready: `2`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`
