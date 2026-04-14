# Ghidra Symbol Resolution Handoff

- Handoff status: `ready`
- Operator blocker: `symbol-resolution-ready`
- Next action: `Run the prepared symbol-resolution jobs locally.`
- Prepared jobs: `5`
- Runnable jobs: `5`
- Selected jobs: `3`
- Blocked jobs: `0`
- Candidate count: `1`

## Selected Jobs

- `ghidra-symbol-05-ntdll-dll-0x161d74` -> `ntdll.dll` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntdll.dll --output-name ghidra-symbolized-ntdll-dll-0x161d74-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntdll.dll+0x161D74 --module-offset ntdll.dll+0x8C48C`
- `ghidra-symbol-07-ntoskrnl-exe-0x327b4d` -> `ntoskrnl.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\ntoskrnl.exe --output-name ghidra-symbolized-ntoskrnl-exe-0x327b4d-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset ntoskrnl.exe+0x327B4D --module-offset ntoskrnl.exe+0x3ED794 --module-offset ntoskrnl.exe+0x3EDD84 --module-offset ntoskrnl.exe+0x6BE358 --module-offset ntoskrnl.exe+0x87108C --module-offset ntoskrnl.exe+0xAE49F6`
- `ghidra-symbol-13-reg-exe-0x128b` -> `reg.exe` | candidates=1 | patterns=1
  command: `python3 scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py --binary-path C:\Windows\System32\reg.exe --output-name ghidra-symbolized-reg-exe-0x128b-power-control-allow-system-required-power-requests --pattern AllowSystemRequiredPowerRequests --module-offset reg.exe+0x128B --module-offset reg.exe+0x379D --module-offset reg.exe+0x65C6 --module-offset reg.exe+0x6775`

## Blocked Jobs

- `ghidra-symbol-01-kernelbase-dll-0x2e436` missing_inputs=[] missing_host_tools=[]
- `ghidra-symbol-04-kernel32-dll-0x2e8d7` missing_inputs=[] missing_host_tools=[]

## Diagnostics

- Resolution kind counts: `{"module_offset": 16}`
- Missing input counts: `{}`
- Runner available: `True`
