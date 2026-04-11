# System I/O AllowRemoteDASD KVM Procmon Runtime Follow-up

Date: 2026-04-06
Candidate: `system.io-allow-remote-dasd`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the intended Session Manager I/O path in the Linux KVM guest with a raw-I/O trigger instead of the older VMware ETW path-aware lane
- check whether a live Procmon capture surfaces the intended `AllowRemoteDASD` read or at least the removable-storage collision path
- confirm that the KVM guest helper can drive a real runtime replay with the new `session-manager-io-raw-burst` trigger

## Result
- the corrected KVM replay produced a real Procmon CSV and uploaded a text summary plus host-review metadata
- the valid replay stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- the host-side review counted `267176` CSV rows and still found `0` lines containing:
  - `Session Manager\I/O System`
  - `AllowRemoteDASD`
  - `RemovableStorageDevices`
- this reinforces the existing runtime gate from a second transport family rather than weakening the earlier static collision story

## Artifacts
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-20260406b/allowremotedasd-procmon-kvm-20260406b.txt`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-20260406b/allowremotedasd-procmon-kvm-20260406b-summary.json`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-20260406b/host-review.json`

## Lane Notes
- an initial malformed replay was discarded because the unquoted `RegistryPath` was truncated at the first space and collected generic `Session Manager` noise instead of the intended `I/O System` path
- the clean replay used a fully quoted `RegistryPath` and is the only one carried forward as evidence
- the guest helper path itself worked: the KVM bridge staged the probe scripts, ran the raw-I/O burst, and copied the resulting artifacts back to the host

## Short Take
- `AllowRemoteDASD` now has a KVM Procmon runtime replay in addition to its earlier ETW and PDB-backed static packages
- the intended Session Manager I/O path still does not show up as a live read
- the removable-storage collision remains the strongest static explanation, while runtime evidence stays cleanly negative
