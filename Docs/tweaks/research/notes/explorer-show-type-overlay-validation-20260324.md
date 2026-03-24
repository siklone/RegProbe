# Explorer ShowTypeOverlay Validation (2026-03-24)

## Setting

- Key: `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`
- Value: `ShowTypeOverlay`
- Type: `REG_DWORD`

## Meaning

- `0` = do not show file icons on thumbnails
- `1` = show file icons on thumbnails

Source: Microsoft Open Specifications, `GlobalFolderOptionsVista`, `displayIconThumb`.

## 25H2 Dump Corroboration

```text
Docs/tweaks/_source-mirrors/win-registry/records/25H2.txt
\Registry\User\<CURRENT_USER_SID>\SOFTWARE\Microsoft\WINDOWS\CurrentVersion\Explorer\Advanced : ShowTypeOverlay
\Registry\WC\SILOB04FC3BC-ED56-586E-C6A2-C7D436B49F47USER_SID\SOFTWARE\Microsoft\WINDOWS\CurrentVersion\Explorer\Advanced : ShowTypeOverlay
\Registry\WC\SILOE8BB395E-8543-4725-80BE-B249432F28B1USER_SID\SOFTWARE\Microsoft\WINDOWS\CurrentVersion\Explorer\Advanced : ShowTypeOverlay

Docs/tweaks/_source-mirrors/regkit/assets/defaults/HKCU25H2.reg
"ShowTypeOverlay"=dword:00000001
```

## Procmon Runtime Evidence

```text
Source file:
H:\Temp\vm-tooling-staging\showtypeoverlay-result.txt

State 0:
Explorer.EXE RegQueryValue HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\ShowTypeOverlay
SUCCESS Type: REG_DWORD, Length: 4, Data: 0

State 1:
Explorer.EXE RegQueryValue HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\ShowTypeOverlay
SUCCESS Type: REG_DWORD, Length: 4, Data: 1

Restore:
RESTORED_EXISTS=True RESTORED_VALUE=1
```

## Raw Hit Files

```text
H:\Temp\vm-tooling-staging\showtypeoverlay-0-hits.csv
H:\Temp\vm-tooling-staging\showtypeoverlay-1-hits.csv
```

## Notes

- Explorer was restarted after each state change.
- The probe confirmed live Explorer consumption for both 0 and 1 on Win25H2Clean.
- The validated VM user baseline was `1`, which matches the 25H2 default-user hive.
