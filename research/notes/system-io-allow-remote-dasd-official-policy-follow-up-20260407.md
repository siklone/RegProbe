# System I/O AllowRemoteDASD Official Policy Follow-up

Date: 2026-04-07
Candidate: `system.io-allow-remote-dasd`

## Objective
- check whether Microsoft now documents `AllowRemoteDASD` under a first-party policy path that matches the existing Ghidra and KVM local-KD collision story
- decide whether the Session Manager `I/O System` candidate remains an active draft lane or is retired as a historical collision trail

## Result
- Microsoft Learn now documents `AllowRemoteDASD` as the registry value for the removable-storage policy **All Removable Storage: Allow direct access in remote sessions**
- the documented registry mapping is `Software\Policies\Microsoft\Windows\RemovableStorageDevices` with value name `AllowRemoteDASD`
- that official policy mapping matches the earlier current-build Ghidra route and the later live KVM local-KD disassembly of `nt!IopAllowRemoteDASD`
- the intended Session Manager `I/O System` value still exists in phase-0 baseline data, but the current-build kernel route and the first-party policy doc now point to the removable-storage policy family instead

## Artifacts
- `research/notes/system-io-allow-remote-dasd-kvm-local-kd-disasm-follow-up-20260406.md`
- `research/records/system.io-allow-remote-dasd.json`

## Source
- Microsoft Learn: `ADMX_RemovableStorage Policy CSP`
- policy name: `All Removable Storage: Allow direct access in remote sessions`
- registry key: `Software\Policies\Microsoft\Windows\RemovableStorageDevices`
- registry value: `AllowRemoteDASD`

## Short Take
- this lane no longer looks like an unresolved Session Manager `I/O System` tweak
- it now looks like a historical collision where the observed baseline value shares a name with a documented removable-storage policy control
- the right repo posture is to keep the old baseline observation as evidence, but retire the Session Manager `I/O System` candidate from the live draft set
