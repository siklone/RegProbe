# Explorer ShowDriveLettersFirst Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer`
- Value: `ShowDriveLettersFirst`
- Type: `REG_DWORD`

## Meaning

- `0` = do not show drive letters first
- `1` = show drive letters first

Source: Microsoft Open Specifications, `GlobalFolderOptionsVista`, `showDriveLetter`.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/win-registry/records/25H2.txt
/Registry/Machine/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer : ShowDriveLettersFirst
/Registry/User/.Default/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer : ShowDriveLettersFirst
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/vm-tooling-staging/showdrivelettersfirst-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/ShowDriveLettersFirst
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/ShowDriveLettersFirst
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_EXISTS=False
```

## Raw Hit Files

```text
research/evidence-files/vm-tooling-staging/showdrivelettersfirst-0-hits.csv
research/evidence-files/vm-tooling-staging/showdrivelettersfirst-1-hits.csv
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The baseline value was absent before the probe and was restored to the absent state at the end.

