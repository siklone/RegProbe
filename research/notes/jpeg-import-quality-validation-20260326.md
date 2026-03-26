JPEGImportQuality validation on Win25H2Clean

- Registry path: `HKCU\Control Panel\Desktop\JPEGImportQuality`
- Runtime trigger: Explorer shell verb `setdesktopwallpaper` on a JPEG copy
- Code-side binary: `C:\Windows\System32\shell32.dll`

Observed runtime read:

```text
Explorer.EXE RegQueryValue HKCU\Control Panel\Desktop\JPEGImportQuality
SUCCESS Type: REG_DWORD, Length: 4, Data: 100
```

Code-side finding:

```text
shell32.dll contains the JPEGImportQuality string and a decompiled xref in
FUN_1800bf050, the shell transcode path that uses JPEGImportQuality during
wallpaper JPEG handling.
```

Evidence files:

- `research/evidence-files/vm-tooling-staging/jpegimportquality-procmon-20260326/jpegimportquality-state-100.txt`
- `research/evidence-files/vm-tooling-staging/jpegimportquality-procmon-20260326/jpegimportquality-state-100.hits.csv`
- `research/evidence-files/vm-tooling-staging/ghidra-probes/shell32-jpegimportquality-ghidra-20260326-070959/shell32-jpegimportquality-ghidra.md`
