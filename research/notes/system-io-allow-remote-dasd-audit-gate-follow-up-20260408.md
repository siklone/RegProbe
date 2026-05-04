# system.io-allow-remote-dasd audit gate follow-up - 2026-04-08

## Summary

- `system.io-allow-remote-dasd` was still showing `next_missing_layer = procmon` in audit.
- The retained record evidence already makes that next step misleading.
- The main current-build route does not land on the intended `Session Manager\\I/O System` path.
- It lands on `\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices\\AllowRemoteDASD`, while the intended path-aware ETW lane stayed a clean no-hit.

## Why the audit needed correction

- The draft does not have a missing generic runtime lane problem.
- It has a path-attribution problem:
  - baseline existence proves the Session Manager value exists
  - current-build static routing points at the removable-storage policy path instead
  - intended-path runtime ETW stayed no-hit
- In that state, another generic Procmon ask is not the highest-signal missing layer.

## Decision impact

- keep the record at `Class B`
- keep the record blocked by:
  - `runtime_no_read`
  - `path_context_unclear`
- treat the remaining gap as `decision-gate` in audit, not as a missing generic `procmon` lane

## Canonical retained references

- record: `research/records/system.io-allow-remote-dasd.json`
- path-aware static note: `research/notes/system-io-allow-remote-dasd-path-aware-follow-up-20260330.md`
- static summary: `evidence/files/path-aware/path-aware-static-20260330-194412/system-io-allow-remote-dasd/summary.json`
- ghidra matches: `evidence/raw/ghidra/system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-20260330-194412/ghidra-matches.md`
- intended-path runtime summary: `evidence/files/path-aware/path-aware-runtime-20260330-220218/system-io-allow-remote-dasd/summary.json`
