# Explorer ShowStatusBar Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced`
- Value: `ShowStatusBar`
- Type: `REG_DWORD`

## Meaning

- `0` = hide the File Explorer status bar
- `1` = show the File Explorer status bar

Source quality note:

- Microsoft Learn describes `showStatusBar` as a File Explorer Classic advanced setting.
- The direct registry contract is corroborated here with the 25H2 dump/default hive and a live Procmon runtime probe.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/win-registry/records/25H2.txt
/Registry/User/<CURRENT_USER_SID>/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : ShowStatusBar
/Registry/WC/SILOB04FC3BC-ED56-586E-C6A2-C7D436B49F47USER_SID/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : ShowStatusBar
/Registry/WC/SILOE8BB395E-8543-4725-80BE-B249432F28B1USER_SID/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : ShowStatusBar

research/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg
"ShowStatusBar"=dword:00000001
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/vm-tooling-staging/showstatusbar-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowStatusBar
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/ShowStatusBar
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_EXISTS=True RESTORED_VALUE=0
```

## Raw Hit Files

```text
research/evidence-files/vm-tooling-staging/showstatusbar-0-hits.csv
research/evidence-files/vm-tooling-staging/showstatusbar-1-hits.csv
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The validated VM user baseline was `0`, while the 25H2 default-user hive still shows `1`.

