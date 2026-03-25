# Explorer ShowCompColor Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced`
- Value: `ShowCompColor`
- Type: `REG_DWORD`

## Meaning

- `0` = do not color compressed and encrypted NTFS files
- `1` = color compressed and encrypted NTFS files

Source: Microsoft Open Specifications, `GlobalFolderOptionsVista`, `showCompColor`.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg
"ShowCompColor"=dword:00000001
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/missing/showcompcolor-result-txt.md

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowCompColor
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowCompColor
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_VALUE=1
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The 25H2 default hive aligns with the documented enabled state (`1`).

