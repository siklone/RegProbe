# Game Mode Full Validation Suite (2026-03-23)

## Scope

- Record: [system.enable-game-mode.review.json](/H:/D/Dev/WPF-Windows-optimizer-with-safe-reversible-tweaks/Docs/tweaks/research/records/system.enable-game-mode.review.json)
- Registry path: `HKCU\Software\Microsoft\GameBar`
- Value name: `AutoGameModeEnabled`
- Type: `REG_DWORD`
- States exercised in this pass: `0` and `1`
- Validation environment: `Win25H2Clean` guest VM

## Procmon Evidence

```text
State 1
Process: SystemSettings.exe
Operation: RegQueryValue
Path: HKU\...\Software\Microsoft\GameBar\AutoGameModeEnabled
Result: SUCCESS
Detail: Type: REG_DWORD, Length: 4, Data: 1
```

```text
State 0
Process: SystemSettings.exe
Operation: RegQueryValue
Path: HKU\...\Software\Microsoft\GameBar\AutoGameModeEnabled
Result: SUCCESS
Detail: Type: REG_DWORD, Length: 4, Data: 0
```

Raw Procmon artifacts:

- [gamemode_admin_probe.txt](/H:/Temp/vm-tooling-staging/gamemode_admin_probe.txt)
- [gamemode_admin_zero_probe.txt](/H:/Temp/vm-tooling-staging/gamemode_admin_zero_probe.txt)
- [gamemode_admin_probe.csv](/H:/Temp/vm-tooling-staging/gamemode_admin_probe.csv)

Interpretation:

- The Settings surface reads `AutoGameModeEnabled` live.
- Both `0` and `1` were observed through reversible guest-side captures.
- This is strong evidence for the registry mapping itself.

## Code-Side Corroboration

Decompiled source reference:

- [gamemode-GamingHandlers.c](/H:/D/Dev/WPF-Windows-optimizer-with-safe-reversible-tweaks/Docs/system/assets/gamemode-GamingHandlers.c)
- Mirror copy: [gamemode-GamingHandlers.c](/H:/D/Dev/WPF-Windows-optimizer-with-safe-reversible-tweaks/Docs/tweaks/_source-mirrors/win-config/system/assets/gamemode-GamingHandlers.c)

Relevant code-side signal:

```text
CreateReference(..., L"AutoGameModeEnabled", ...)
```

Interpretation:

- The decompiled Game Mode handler path contains the `AutoGameModeEnabled` string explicitly.
- This does not replace runtime proof, but it corroborates that the setting is part of a real Game Mode code path rather than a random orphan value.

## Runtime Suite

Important correction:

- The runs below are valid as toggle, launch, trace, and restore evidence.
- They are not valid OCCT score benchmarks yet.
- The OCCT window still displayed the End User License Agreement during the captures.
- [OCCT.config.json](/H:/Temp/occt_config.json) still reported `EulaVersion = 0` after the acceptance attempt.
- Because of that, the OCCT portion must be treated as launch-and-trace evidence only.

### State 0

```text
STATE=0
BASELINE_VALUE=0
WPR_STARTED=1
START_BUTTONS=1
BUTTON=START|X=223|Y=786|W=192|H=42
START_CLICK=1
BENCHMARK_OBSERVATION_SECONDS=24.51
ETL_EXISTS=True
PERF_CSV_EXISTS=True
RESTORED=0
```

Artifacts:

- [gamemode-occt-state-0-v2.txt](/H:/Temp/vm-tooling-staging/gamemode-occt-state-0-v2.txt)
- [gamemode-occt-state-0-v2.perf.csv](/H:/Temp/vm-tooling-staging/gamemode-occt-state-0-v2.perf.csv)
- [gamemode-occt-state-0-v2.etl](/H:/Temp/vm-tooling-staging/gamemode-occt-state-0-v2.etl)

Observed counters:

- Average CPU: `4.89`
- CPU min/max: `3.62` / `7.63`
- Average disk transfers/sec: `4.46`
- Average disk latency: `0.003852`
- Average committed memory: `2147.17 MB`

OCCT state note:

- The `START` button was found and invoked.
- The visible text still included the EULA modal content, so this run must not be interpreted as a valid OCCT score export.

### State 1

```text
STATE=1
BASELINE_VALUE=0
WPR_STARTED=1
START_BUTTONS=1
BUTTON=START|X=223|Y=786|W=192|H=42
START_CLICK=1
BENCHMARK_OBSERVATION_SECONDS=24.52
ETL_EXISTS=True
PERF_CSV_EXISTS=True
RESTORED=0
```

Artifacts:

- [gamemode-occt-state-1-v2.txt](/H:/Temp/vm-tooling-staging/gamemode-occt-state-1-v2.txt)
- [gamemode-occt-state-1-v2.perf.csv](/H:/Temp/vm-tooling-staging/gamemode-occt-state-1-v2.perf.csv)
- [gamemode-occt-state-1-v2.etl](/H:/Temp/vm-tooling-staging/gamemode-occt-state-1-v2.etl)

Observed counters:

- Average CPU: `5.07`
- CPU min/max: `2.28` / `9.83`
- Average disk transfers/sec: `5.19`
- Average disk latency: `0.004682`
- Average committed memory: `2106.64 MB`

OCCT state note:

- The `START` button was found and invoked.
- The visible text still included the EULA modal content, so this run must not be interpreted as a valid OCCT score export.

## Interpretation

What this suite proves:

- `AutoGameModeEnabled` is a live registry-mapped setting read by `SystemSettings.exe`.
- Both `0` and `1` can be exercised safely in the VM.
- The OCCT UI can be launched and driven while WPR is recording.
- The value can be restored to the guest baseline after each run.

What this suite does **not** prove:

- It does not prove a strong performance winner between `0` and `1`.
- It does not yet prove a valid OCCT benchmark score because the EULA modal remained active.
- This pass used one short bounded observation run per state, not a repeated benchmark campaign.
- The most reliable runtime artifacts here are the ETL traces, the perf samples, and the safe restore behavior, not an OCCT score export.

## Bottom Line

This record is now supported by three layers at once:

1. Procmon semantics
2. Code-side corroboration
3. Short VM runtime suite with OCCT launch traces plus WPR

That is enough to say the Game Mode value is real, consumed, and safely traceable on this build. It is **not** enough to claim that `0` or `1` is measurably faster, and it is **not** enough to claim a valid OCCT benchmark result until the OCCT EULA is genuinely accepted and stops appearing in the runtime captures.
