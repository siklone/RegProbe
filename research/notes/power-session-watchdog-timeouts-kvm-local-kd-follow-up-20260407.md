# power.session-watchdog-timeouts KVM local-KD follow-up - 2026-04-07

## Summary

- A Linux KVM local-KD follow-up attached to the live `regprobe-win11-25h2-session` guest and resolved the current-build watchdog symbol family directly in the running kernel.
- The live kernel exposed:
  - `PopWatchdogResumeTimeout = 0x78` (`120`)
  - `PopWatchdogSleepTimeout = 0x12c` (`300`)
  - `PopFxDirectedPowerUpTimeoutMs = 0x3a980` (`240000`)
  - `PopFxDirectedPowerDownTimeoutMs = 0x668a0` (`420000`)
- Those directed-power timeout globals line up with the existing repo PoFx pseudocode lead:
  - `(120 + 120) * 1000 = 240000`
  - `(300 + 120) * 1000 = 420000`
- This does not replace the missing exact live registry-read lane, but it does confirm that the current-build running kernel still carries both watchdog registry values into the live directed-power timeout state.

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-watchdog-values-20260407a/local-kd-watchdog-values-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-values-20260407a/local-kd-watchdog-values-20260407a.log`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-sleep-20260407a/local-kd-watchdog-sleep-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-sleep-20260407a/local-kd-watchdog-sleep-20260407a.log`

## Why this matters

The watchdog lane previously had strong static and adjacent runtime evidence, but its live current-build state still leaned heavily on repo-side PoFx pseudocode and weaker forced-boundary ntoskrnl fallback artifacts. This KVM local-KD follow-up tightens that gap.

It now shows, on a running guest, that:

- the watchdog registry-value symbols themselves still exist as live kernel globals
- the live values still match the clean baseline pair `120 / 300`
- the directed-power millisecond globals also exist and currently match the repo pseudocode derivation

That is stronger than the older VMware-only runtime story, even though it still stops short of a decisive exact registry-read capture for the pair.
