Reliability timestamp Procmon attempts on Win25H2Clean

Targeted paths:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows NT/Reliability/TimeStampEnabled`
- `HKLM/SOFTWARE/Policies/Microsoft/Windows NT/Reliability/TimeStampInterval`
- `HKLM/SOFTWARE/Microsoft/Windows/CurrentVersion/Reliability/TimeStampInterval`

Tried triggers:

- `perfmon.exe /rel`
- a bounded WER crash path using `FailFast`
- WMI queries for `Win32_ReliabilityStabilityMetrics` and `Win32_ReliabilityRecords`

Current result:

```text
All three Procmon passes produced CSV output but no reads on the target
Reliability policy or fallback values inside the capture window.
```

Evidence files:

- `research/evidence-files/procmon/system.reliability-timestamp-enabled/perfmon-rel.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/wer-crash.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/wmi-reliability.txt`
