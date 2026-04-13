# ETW Stackwalk Capture Plan

- Status: `ready`
- Profile: `kernel-registry-stackwalk-v1`
- Capture phase: `runtime`
- Run id: `wave4-registry-stackwalk`
- Duration seconds: `60`
- Stack expected: `True`
- Stackwalk events: `RegCreateKey, RegOpenKey, RegQueryKey, RegSetValue, RegQueryValue, RegDeleteValue, RegCloseKey`

## Commands

### preflight

```powershell
where xperf.exe
```
```powershell
where tracerpt.exe
```

### prepare

```powershell
powershell -NoProfile -Command "New-Item -ItemType Directory -Force -Path 'C:\RegProbe-Diag\etw-stackwalk\wave4-registry-stackwalk' | Out-Null"
```
```powershell
xperf -stop
```

### start

```powershell
xperf -on PROC_THREAD+LOADER+REGISTRY -stackwalk RegCreateKey+RegOpenKey+RegQueryKey+RegSetValue+RegQueryValue+RegDeleteValue+RegCloseKey -BufferSize 1024 -MinBuffers 64 -MaxBuffers 256 -f C:\RegProbe-Diag\etw-stackwalk\wave4-registry-stackwalk\wave4-registry-stackwalk.raw.etl
```

### wait

```powershell
powershell -NoProfile -Command "Start-Sleep -Seconds 60"
```

### stop

```powershell
xperf -d C:\RegProbe-Diag\etw-stackwalk\wave4-registry-stackwalk\wave4-registry-stackwalk.etl
```

### parse_xml

```powershell
tracerpt C:\RegProbe-Diag\etw-stackwalk\wave4-registry-stackwalk\wave4-registry-stackwalk.etl -o C:\RegProbe-Diag\etw-stackwalk\wave4-registry-stackwalk\wave4-registry-stackwalk.xml -of XML
```

### repo_parse

```powershell
python3 registry-research-framework/scripts/parse_etl_registry_touches.py --input evidence/files/etw-stackwalk/wave4-registry-stackwalk/wave4-registry-stackwalk.etl
```

## Notes

- Run from an elevated Windows shell with Windows Performance Toolkit installed.
- Copy the final ETL into evidence/files/etw-stackwalk/wave4-registry-stackwalk/wave4-registry-stackwalk.etl before running repo_parse.
- The start command enables registry stack walking; the parser expects tracerpt XML fields such as Stack or CallStack.
- If caller_stack remains empty, rerun with a narrower trigger window or move the ETL to WPA/xperf for stack inspection.
