# Ghidra Symbol Resolution Handoff

- Handoff status: `ready`
- Operator blocker: `symbol-resolution-ready`
- Next action: `Run the prepared symbol-resolution jobs locally.`
- Prepared jobs: `4`
- Runnable jobs: `4`
- Selected jobs: `4`
- Blocked jobs: `0`
- Candidate count: `4`

## Selected Jobs

- `ghidra-symbol-01-ntoskrnl-exe-syntheticpower-control-allow-audio-to-enable-execution-required-power-requestsresolver` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-01-power-control-allow-audio-to-enable-execution-required-power-requests --pattern AllowAudioToEnableExecutionRequiredPowerRequests`
- `ghidra-symbol-02-ntoskrnl-exe-0x1920` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-02-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-03-ntoskrnl-exe-0x2c80` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-03-system-kernel-dpc-watchdog-profile-cluster --pattern DpcWatchdogProfileBufferSizeBytes`
- `ghidra-symbol-04-ntoskrnl-exe-0xfffff80512340002` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-04-power-session-watchdog-timeouts --pattern WatchdogResumeTimeout`

## Blocked Jobs

- none

## Diagnostics

- Resolution kind counts: `{"module_offset": 2, "plain_text": 1, "raw_address": 1}`
- Missing input counts: `{}`
- Runner available: `True`
