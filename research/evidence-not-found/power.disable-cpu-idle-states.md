# power.disable-cpu-idle-states

- Class: `A`
- Record status: `validated`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `static_ghidra, behavior_wpr, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, etw, ghidra, wpr, benchmark, reboot`

## Why it stays negative

This record is cross-layer verified. The project treats strong proof for undocumented raw registry surfaces as Class A even when app actionability stays separate.

## Attached references

- `official-doc` Microsoft Learn: PPM Notifications -> https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/ppm-notifications
- `repo-code` Current app implementation -> app/Services/TweakProviders/PowerTweakProvider.cs
- `registry-observation` nohuto power trace for DisableIdleStatesAtBoot -> research/_source-mirrors/win-registry/records/Power.txt
- `vm-test` Win25H2Clean reversible probe for the CPU idle-state bundle -> evidence/files/vm-tooling-staging/cpu_idle_probe.json
- `repo-doc` Repo power notes -> Docs/power/power.md
- `ghidra-trace` Our Ghidra follow-up - ntoskrnl CPU idle string/xref probes -> evidence/files/ghidra/power.disable-cpu-idle-states/cpu-idle-registry-name-ghidra.md and evidence/files/ghidra/power.disable-cpu-idle-states/cpu-idle-internal-name-ghidra.md
