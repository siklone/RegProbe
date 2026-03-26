# Explorer SeparateProcess Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced`
- Value: `SeparateProcess`
- Type: `REG_DWORD`

## Meaning

- `0` = use the default shared Explorer process model
- `1` = launch folder windows in a separate process

Source: Microsoft Open Specifications, `GlobalFolderOptionsVista`, `separateProcess`.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg
"SeparateProcess"=dword:00000000

research/_source-mirrors/win-registry/records/25H2.txt
/Registry/User/<CURRENT_USER_SID>/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : SeparateProcess
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/procmon/explorer-separate-process-validation-20260324/separateprocess-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/SeparateProcess
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/SeparateProcess
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_VALUE=0
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The 25H2 default hive aligns with the documented disabled/default state (`0`).

