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

Hidden structured Procmon follow-up:

```text
A later hidden-launch pass used a dedicated reliability-timestamp probe script
that set TimeStampEnabled=1, TimeStampInterval=86400, and the fallback
TimeStampInterval=1, then ran DiagTrack restart, Consolidator, QueueReporting,
perfmon /rel, and Problem Reports UI triggers in one window. It produced
MATCH_COUNT=360 and RUNTIME_MATCH_COUNT=3, but the only runtime reads were the
same adjacent Reliability\PBR path on svchost.exe.
```

Address-seeded Ghidra fallback:

```text
The first diagtrack export left unresolved blocks behind as <no function>.
A second address-seeded pass re-opened 18038fce0 and 18026bfc0, forced
temporary function boundaries, and wrote the resulting decompile snippets
plus structured evidence.json into the repo-backed ghidra folder.
```

Evidence files:

- `research/evidence-files/procmon/system.reliability-timestamp-enabled/diagtrack-service.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-diagtrack-restart-1.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-diagtrack-restart-1.hits.csv`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-werqueue-1.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-wercpl-1.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-timestamp-probe.txt`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-timestamp-probe.json`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-timestamp-probe.hits.csv`
- `research/evidence-files/procmon/system.reliability-timestamp-enabled/reliability-timestamp-probe.runtime.hits.csv`
- `research/evidence-files/ghidra/system.reliability-timestamp-enabled/ghidra-matches.md`
- `research/evidence-files/ghidra/system.reliability-timestamp-enabled/ghidra-run.log`
- `research/evidence-files/ghidra/system.reliability-timestamp-enabled/evidence.json`
