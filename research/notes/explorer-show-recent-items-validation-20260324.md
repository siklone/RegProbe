# Explorer ShowRecent Validation (2026-03-24)

## Setting

- Key: `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer`
- Value: `ShowRecent`
- Type: `REG_DWORD`

## Meaning

- `0` = do not show recent items
- `1` = show recent items

Source quality note:

- Microsoft Learn describes the related Explorer feature as `showRecentlyUsedFiles`.
- The direct registry contract is corroborated here with the 25H2 dump and a live Procmon runtime probe.

## 25H2 Dump Corroboration

```text
research/_source-mirrors/win-registry/records/25H2.txt
/Registry/Machine/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer : ShowRecent
/Registry/User/<CURRENT_USER_SID>/SOFTWARE/Microsoft/WINDOWS/CurrentVersion/Explorer : ShowRecent
```

## Procmon Runtime Evidence

```text
Source file:
research/evidence-files/vm-tooling-staging/showrecent-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/ShowRecent
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/ShowRecent
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_EXISTS=True RESTORED_VALUE=0
```

## Raw Hit Files

```text
research/evidence-files/vm-tooling-staging/showrecent-0-hits.csv
research/evidence-files/vm-tooling-staging/showrecent-1-hits.csv
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The validated VM user baseline was `0` and was restored after the probe.

