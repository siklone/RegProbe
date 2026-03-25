# Explorer HideDrivesWithNoMedia Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced`
- Value: `HideDrivesWithNoMedia`
- Type: `REG_DWORD`

## Meaning

- `0` = show empty drives
- `1` = hide empty drives

Source quality note:

- Microsoft Learn describes the related Explorer advanced setting as `hideDrivesWithNoMedia`.
- The direct registry contract is corroborated here with the 25H2 dump and a live Procmon runtime probe.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/win-registry/records/25H2.txt
/Registry/User/<CURRENT_USER_SID>/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer/Advanced : HideDrivesWithNoMedia
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/vm-tooling-staging/hideemptydrives-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/HideDrivesWithNoMedia
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Advanced/HideDrivesWithNoMedia
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_EXISTS=False
```

## Raw Hit Files

```text
research/evidence-files/vm-tooling-staging/hideemptydrives-0-hits.csv
research/evidence-files/vm-tooling-staging/hideemptydrives-1-hits.csv
```

## Notes

- Explorer was restarted into `This PC` after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The baseline value was absent and was restored to the absent state after the probe.

