# Ghidra Symbol Resolution Handoff

- Handoff status: `ready`
- Operator blocker: `symbol-resolution-ready`
- Next action: `Run the prepared symbol-resolution jobs locally.`
- Prepared jobs: `16`
- Runnable jobs: `10`
- Selected jobs: `10`
- Blocked jobs: `6`
- Candidate count: `1`

## Selected Jobs

- `ghidra-symbol-07-ntoskrnl-exe-0x327b4d` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-07-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x327B4D`
- `ghidra-symbol-08-ntoskrnl-exe-0x3ed794` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-08-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x3ED794`
- `ghidra-symbol-09-ntoskrnl-exe-0x3edd84` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-09-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x3EDD84`
- `ghidra-symbol-10-ntoskrnl-exe-0x6be358` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-10-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x6BE358`
- `ghidra-symbol-11-ntoskrnl-exe-0x87108c` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-11-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x87108C`
- `ghidra-symbol-12-ntoskrnl-exe-0xae49f6` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-12-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0xAE49F6`
- `ghidra-symbol-13-reg-exe-0x128b` -> `reg.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-13-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x128B`
- `ghidra-symbol-14-reg-exe-0x379d` -> `reg.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-14-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x379D`
- `ghidra-symbol-15-reg-exe-0x65c6` -> `reg.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-15-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x65C6`
- `ghidra-symbol-16-reg-exe-0x6775` -> `reg.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-16-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x6775`

## Blocked Jobs

- `ghidra-symbol-01-kernelbase-dll-0x2e436` missing_inputs=['guest_binary_path'] missing_host_tools=[]
- `ghidra-symbol-02-kernelbase-dll-0x2edab` missing_inputs=['guest_binary_path'] missing_host_tools=[]
- `ghidra-symbol-03-kernelbase-dll-0x30aad` missing_inputs=['guest_binary_path'] missing_host_tools=[]
- `ghidra-symbol-04-kernel32-dll-0x2e8d7` missing_inputs=['guest_binary_path'] missing_host_tools=[]
- `ghidra-symbol-05-ntdll-dll-0x161d74` missing_inputs=['guest_binary_path'] missing_host_tools=[]
- `ghidra-symbol-06-ntdll-dll-0x8c48c` missing_inputs=['guest_binary_path'] missing_host_tools=[]

## Diagnostics

- Resolution kind counts: `{"module_offset": 16}`
- Missing input counts: `{"guest_binary_path": 6}`
- Runner available: `True`
