JPEGImportQuality validation on Win25H2Clean

- Registry path: `HKCU/Control Panel/Desktop/JPEGImportQuality`
- Runtime trigger: Explorer shell verb `setdesktopwallpaper` on a JPEG copy
- Code-side binary: `C:/Windows/System32/shell32.dll`

Observed runtime read:

```text
Explorer.EXE RegQueryValue HKCU/Control Panel/Desktop/JPEGImportQuality
SUCCESS Type: REG_DWORD, Length: 4, Data: 100
```

Code-side finding:

```text
shell32.dll contains the JPEGImportQuality string and a decompiled xref in
FUN_1800bf050, the shell transcode path that uses JPEGImportQuality during
wallpaper JPEG handling.
```

Evidence files:

- `evidence/files/procmon/jpeg-import-quality-validation-20260326/jpegimportquality-state-100.txt`
- `evidence/files/procmon/jpeg-import-quality-validation-20260326/jpegimportquality-state-100.hits.csv`
- `evidence/files/ghidra/system.disable-jpeg-reduction/shell32-jpegimportquality-ghidra.md`
