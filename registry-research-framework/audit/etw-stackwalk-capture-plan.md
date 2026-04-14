# ETW Stackwalk Capture Plan

- Status: `ready`
- Profile: `execution-required-audio-stackwalk-v1`
- Capture phase: `runtime`
- Run id: `wave4-allow-audio-e2e`
- Duration seconds: `60`
- Candidate id: `power.control.allow-audio-to-enable-execution-required-power-requests`
- Registry path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- Value name: `AllowAudioToEnableExecutionRequiredPowerRequests`
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
powershell -NoProfile -Command "New-Item -ItemType Directory -Force -Path 'C:\RegProbe-Diag\etw-stackwalk\wave4-allow-audio-e2e' | Out-Null"
```
```powershell
xperf -stop
```

### start

```powershell
xperf -on PROC_THREAD+LOADER+REGISTRY -stackwalk RegCreateKey+RegOpenKey+RegQueryKey+RegSetValue+RegQueryValue+RegDeleteValue+RegCloseKey -BufferSize 1024 -MinBuffers 64 -MaxBuffers 256 -f C:\RegProbe-Diag\etw-stackwalk\wave4-allow-audio-e2e\wave4-allow-audio-e2e.raw.etl
```

### wait

```powershell
powershell -NoProfile -Command "Start-Sleep -Seconds 60"
```

### stop

```powershell
xperf -d C:\RegProbe-Diag\etw-stackwalk\wave4-allow-audio-e2e\wave4-allow-audio-e2e.etl
```

### parse_xml

```powershell
tracerpt C:\RegProbe-Diag\etw-stackwalk\wave4-allow-audio-e2e\wave4-allow-audio-e2e.etl -o C:\RegProbe-Diag\etw-stackwalk\wave4-allow-audio-e2e\wave4-allow-audio-e2e.xml -of XML
```

### repo_parse

```powershell
python3 registry-research-framework/scripts/parse_etl_registry_touches.py --input evidence/files/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl
```

### repo_guest_capture

```powershell
python3 scripts/vm-kvm/run-guest-etw-stackwalk-capture.py --run-id wave4-allow-audio-e2e --duration-seconds 60 --registry-path HKLM\SYSTEM\CurrentControlSet\Control\Power --value-name AllowAudioToEnableExecutionRequiredPowerRequests --ingest-to-repo --refresh-ghidra
```

## Notes

- Run from an elevated Windows shell with Windows Performance Toolkit installed.
- Copy the final ETL into evidence/files/etw-stackwalk/wave4-allow-audio-e2e/wave4-allow-audio-e2e.etl before running repo_parse.
- The start command enables registry stack walking; the parser expects tracerpt XML fields such as Stack or CallStack.
- The repo_guest_capture command is the preferred host-side lane when the focused KVM guest is available because it launches the guest helper, ingests the ETL/XML into the repo, and refreshes caller-stack follow-up automatically.
- If caller_stack remains empty, rerun with a narrower trigger window or move the ETL to WPA/xperf for stack inspection.
