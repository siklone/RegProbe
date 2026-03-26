TaskbarAnimations validation on Win25H2Clean

- Registry path: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/TaskbarAnimations`
- Process: `explorer.exe`
- Capture method: reversible Procmon pass with Explorer restart after writing each state

Observed runtime reads:

```text
State 0
explorer.exe RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/TaskbarAnimations
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1
explorer.exe RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/TaskbarAnimations
SUCCESS Type: REG_DWORD, Length: 4, Data: 1
```

Evidence files:

- `research/evidence-files/procmon/taskbar-animations-procmon-validation-20260326/taskbaranimations-state-0.txt`
- `research/evidence-files/procmon/taskbar-animations-procmon-validation-20260326/taskbaranimations-state-0.hits.csv`
- `research/evidence-files/procmon/taskbar-animations-procmon-validation-20260326/taskbaranimations-state-1.txt`
- `research/evidence-files/procmon/taskbar-animations-procmon-validation-20260326/taskbaranimations-state-1.hits.csv`

Ghidra follow-up:

- Binary: `C:/Windows/System32/Taskbar.dll`
- String: `TaskbarAnimations`
- Export: `research/evidence-files/ghidra/taskbar-animations-procmon-validation-20260326/taskbar-taskbaranimations-ghidra.md`

Key finding:

```text
FUN_180057100 calls SystemParametersInfoW(0x1042, ...) and then reads
SOFTWARE/Microsoft/Windows/CurrentVersion/Explorer/Advanced/TaskbarAnimations
through Ordinal_123(..., L"TaskbarAnimations", 1).
```
