# Power Session Watchdog Timeouts Runtime Prep

Date: 2026-03-28

Lane label: `power.session-watchdog-timeouts`

## Why this lane exists

The current kernel and power intake narrowed the strongest next queue to a single pair:

- `WatchdogResumeTimeout`
- `WatchdogSleepTimeout`

This note packages the current baseline, static evidence, and next-step guidance so the next boot and runtime pass can start from one place instead of replaying the earlier intake work.

## Baseline registry proof

Guest export:

- query: `evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.txt`
- reg: `evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.reg`
- metadata: `evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/metadata.json`

Current baseline values:

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout = 0x78` (`120`)
- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout = 0x12c` (`300`)
- adjacent baseline signal: `PowerSettingProfile = 0`

The export succeeded cleanly and did not disturb shell health.

## Static evidence already in hand

Repo-side semantic lead:

- `Docs/privacy/assets/sleepstudy-PoFxInitPowerManagement.c`

Relevant lines in that asset show:

- `PopFxDirectedPowerUpTimeoutMs = 1000 * (PopWatchdogResumeTimeout + 120)`
- `PopFxDirectedPowerDownTimeoutMs = 1000 * (PopWatchdogSleepTimeout + 120)`

Static binary evidence:

- string probe summary: `evidence/files/vm-tooling-staging/registry-batch-string-20260328-003236/summary.json`
- Ghidra review note: `research/notes/kernel-power-next-gate-ghidra-review-20260328.md`
- Ghidra markdown: `evidence/files/ghidra/kernel-power-nextgate-ntoskrnl/ghidra-matches.md`

Current reading:

- both names exist in `ntoskrnl.exe`
- both names resolve to stable xref addresses
- the fallback decompilation is still forced-boundary output, so it is structural evidence, not final semantics proof

## Guardrails

- keep the pair together
- do not split into separate lanes yet
- do not use Frida
- treat the next runtime pass as `boot or power-manager adjacent`, not ordinary user-mode settings validation

## Recommended next step

The next concrete lane should be a boot-oriented ETW or WPR pass for the pair under:

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`

Machine-readable companion:

- `registry-research-framework/audit/power-session-watchdog-timeouts-runtime-prep-20260328.json`
