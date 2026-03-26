Win32PrioritySeparation validation on Win25H2Clean

- Registry path: `HKLM/System/CurrentControlSet/Control/PriorityControl/Win32PrioritySeparation`
- Capture process: `wmiprvse.exe`
- Trigger path: `Get-CimInstance Win32_OperatingSystem` with `ForegroundApplicationBoost`, `QuantumLength`, and `QuantumType`

Observed runtime reads:

```text
State 2
wmiprvse.exe RegQueryValue HKLM/System/CurrentControlSet/Control/PriorityControl/Win32PrioritySeparation
SUCCESS Type: REG_DWORD, Length: 4, Data: 2

State 38
wmiprvse.exe RegQueryValue HKLM/System/CurrentControlSet/Control/PriorityControl/Win32PrioritySeparation
SUCCESS Type: REG_DWORD, Length: 4, Data: 38
```

Evidence files:

- `research/evidence-files/procmon/system.priority-control/prioritycontrol-state-2.txt`
- `research/evidence-files/procmon/system.priority-control/prioritycontrol-state-2.hits.csv`
- `research/evidence-files/procmon/system.priority-control/prioritycontrol-state-38.txt`
- `research/evidence-files/procmon/system.priority-control/prioritycontrol-state-38.hits.csv`

Key finding:

```text
The current 25H2 WMI surface reads Win32PrioritySeparation directly and returns the same
baseline 2 and app-profile 38 values that the VM reboot suite and bounded benchmarks already used.
```
