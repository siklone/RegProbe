# ETW Stackwalk Reopen Rotation Ledger

- Rotation status: `seed-pending`
- Rotation mode: `seed-from-baseline`
- History status: `seed-required`
- Transition status: `baseline`
- Operator blocker: `seed-previous-snapshot-from-history-archive`
- Next action: `Seed snapshot.previous from the retained baseline snapshot before expecting rotation-aware reopen diffs.`
- Current snapshot id: `ec5b6c91b4e6`
- Previous snapshot id: `None`
- Retained baseline snapshot id: `ec5b6c91b4e6`
- Rotation candidate count: `2`
- Prerequisite count: `2`

## Prerequisites

- `seed-previous-snapshot`
- `refresh-transition-summary`

## Entries

### power.control.allow-audio-to-enable-execution-required-power-requests

- Transition type: `added`
- Rotation disposition: `seed-baseline`
- Current journal state: `deferred`
- Previous journal state: `None`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`

### power.control.allow-system-required-power-requests

- Transition type: `added`
- Rotation disposition: `seed-baseline`
- Current journal state: `deferred`
- Previous journal state: `None`
- Next unlock prerequisite: `Land a current-build boot/init reader or registry seeding caller proof.`
