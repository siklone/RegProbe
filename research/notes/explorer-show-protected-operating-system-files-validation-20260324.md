# Explorer ShowSuperHidden Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced`
- Value: `ShowSuperHidden`
- Type: `REG_DWORD`

## Meaning

- `0` = hide protected operating system files
- `1` = show protected operating system files

Source: Microsoft Open Specifications, `GlobalFolderOptionsVista`, `showSuperHidden`.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg
"ShowSuperHidden"=dword:00000000

research/_source-mirrors/win-registry/records/25H2.txt
/Registry/User/<CURRENT_USER_SID>/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : ShowSuperHidden
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/procmon/explorer-show-protected-operating-system-files-validation-20260324/showsuperhidden-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowSuperHidden
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowSuperHidden
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_SHOWSUPERHIDDEN_VALUE=1
RESTORED_HIDDEN_VALUE=1
```

## Raw Hit Files

```text
research/evidence-files/procmon/explorer-show-protected-operating-system-files-validation-20260324/showsuperhidden-0-hits.csv
research/evidence-files/procmon/explorer-show-protected-operating-system-files-validation-20260324/showsuperhidden-1-hits.csv
```

## Notes

- The probe held `Hidden = 1` while toggling `ShowSuperHidden` so the protected-file gate was exercised in the mode where hidden items are already visible.
- Explorer was restarted after each state change.
- This validates live Explorer consumption on the Win25H2Clean VM and aligns with the 25H2 dump plus the Microsoft Open Specifications description.

