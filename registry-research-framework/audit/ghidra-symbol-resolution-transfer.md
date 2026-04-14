# Ghidra Symbol Resolution Transfer

- Transfer status: `ready`
- Operator blocker: `transfer-pack-ready`
- Next action: `Copy the listed repo files and use the exported commands on the destination KVM-capable host.`
- Selected jobs: `10`
- Candidate count: `1`
- Required repo files: `9`
- Missing repo files: `0`

## Transfer Jobs

- `ghidra-symbol-07-ntoskrnl-exe-0x327b4d` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-07-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-08-ntoskrnl-exe-0x3ed794` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-08-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-09-ntoskrnl-exe-0x3edd84` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-09-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-10-ntoskrnl-exe-0x6be358` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-10-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-11-ntoskrnl-exe-0x87108c` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-11-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-12-ntoskrnl-exe-0xae49f6` -> `ntoskrnl.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-12-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-13-reg-exe-0x128b` -> `reg.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-13-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-14-reg-exe-0x379d` -> `reg.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-14-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-15-reg-exe-0x65c6` -> `reg.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-15-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`
- `ghidra-symbol-16-reg-exe-0x6775` -> `reg.exe` | patterns=1 | candidates=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-16-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests`

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
