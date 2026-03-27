JPEGImportQuality v3.1 runtime lane on Win25H2Clean

- Registry path: `HKCU/Control Panel/Desktop/JPEGImportQuality`
- Snapshot: `baseline-20260327-shell-stable`
- Runtime trigger: shell wallpaper apply on a JPEG working copy
- Source JPEG: `%SystemRoot%/Web/Wallpaper/Windows/img0.jpg`

Observed runtime lane result:

```text
before: value missing
applied: JPEGImportQuality = 100
restored: value missing
wallpaper apply: candidate invoked = true
wallpaper restore: invoked = true
shell after: explorer=true, sihost=true, shellhost=true, ctfmon=true
```

Evidence files:

- `evidence/files/vm-tooling-staging/jpeg-import-quality-runtime-20260327-124349/summary.json`
- `evidence/files/vm-tooling-staging/jpeg-import-quality-runtime-20260327-124349/jpeg-import-quality-runtime.etl.md`
