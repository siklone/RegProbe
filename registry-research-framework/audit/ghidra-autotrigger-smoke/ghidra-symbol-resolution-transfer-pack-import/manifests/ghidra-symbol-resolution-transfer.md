# Ghidra Symbol Resolution Transfer

- Transfer status: `ready`
- Operator blocker: `transfer-pack-ready`
- Next action: `Copy the listed repo files and use the exported commands on the destination KVM-capable host.`
- Selected jobs: `4`
- Candidate count: `4`
- Required repo files: `9`
- Missing repo files: `0`

## Transfer Jobs

- `ghidra-symbol-01-ntoskrnl-exe-syntheticpower-control-allow-audio-to-enable-execution-required-power-requestsresolver` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-01-power-control-allow-audio-to-enable-execution-required-power-requests --pattern AllowAudioToEnableExecutionRequiredPowerRequests`
- `ghidra-symbol-02-ntoskrnl-exe-0x1920` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-02-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-03-ntoskrnl-exe-0x2c80` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-03-system-kernel-dpc-watchdog-profile-cluster --pattern DpcWatchdogProfileBufferSizeBytes`
- `ghidra-symbol-04-ntoskrnl-exe-0xfffff80512340002` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-04-power-session-watchdog-timeouts --pattern WatchdogResumeTimeout`

## Required Repo Paths

- `scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py`
- `scripts/vm-kvm/ensure-guest-admin-shell.py`
- `scripts/vm-kvm/type-to-guest.py`
- `scripts/vm-kvm/guest_bridge.py`
- `scripts/vm-kvm/summary_contract_lib.py`
- `scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1`
- `scripts/vm/guest-tools/ghidra-headless.cmd`
- `scripts/vm/ghidra/ExportBranchAnalysis.java`
- `scripts/vm/ghidra/SetPdbSymbolRepository.java`

## Missing Repo Paths

- none
