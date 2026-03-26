Reliability timestamp follow-up on Win25H2Clean

- Targeted values:
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows NT/Reliability/TimeStampEnabled`
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows NT/Reliability/TimeStampInterval`
  - `HKLM/SOFTWARE/Microsoft/Windows/CurrentVersion/Reliability/TimeStampInterval`
- New live leads:
  - `DiagTrack` runs as its own `svchost.exe -k utcsvc -p`
  - `QueueReporting` runs `%windir%/system32/wermgr.exe -upload`
  - `wercplsupport` is demand-start for the Problem Reports control-panel surface

Observed follow-up results:

```text
Our Ghidra export on diagtrack.dll found the TimeStampInterval string on 25H2.
Restarting DiagTrack produced an adjacent Reliability read:
HKLM/SOFTWARE/Microsoft/Windows/CurrentVersion/Reliability/PBR/LastSuccessfulRefreshTime
```

```text
Bounded WER QueueReporting and Problem Reports UI Procmon passes still did not
show live reads for TimeStampEnabled or TimeStampInterval on the policy or
fallback paths.
```

Conclusion:

```text
The current 25H2 live stack is now narrowed to DiagTrack and WER-adjacent
Reliability code paths, but the exact TimeStampEnabled / TimeStampInterval
runtime read is still not surfaced in Procmon.
```

Evidence files:

- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/diagtrack-service.txt`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-diagtrack-restart-1.txt`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-diagtrack-restart-1.csv`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-werqueue-1.txt`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-werqueue-1.csv`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-wercpl-1.txt`
- `research/evidence-files/vm-tooling-staging/reliability-follow-up-20260326/reliability-wercpl-1.csv`
- `research/evidence-files/ghidra/reliability-timestamp-follow-up-20260326/diagtrack-reliability-ghidra.md`
