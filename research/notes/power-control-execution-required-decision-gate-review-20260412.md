# power.control execution-required decision gate review - 2026-04-12

## Summary

`AllowSystemRequiredPowerRequests` and `AllowAudioToEnableExecutionRequiredPowerRequests` now have a much sharper blocker than the older generic decision-gate wording suggested.

The current package has reader-side and binding evidence: live KD resolved the target globals, disassembly showed the execution-required consumers, Ghidra corroborated the reader family, and the INIT descriptor/static walker analysis tied both exact value names to the expected current-build globals.

What is still missing is the current-build registry seeding path. The unlabeled INIT walker is strong enough to explain how the descriptor table is consumed, but it is not yet a named/public seeding routine and it does not provide an exact live registry read for either value.

## Decision

Keep both candidates research-only.

The active blockers are:

- `no-current-build-registry-seeding-path`
- exact-runtime-read-no-hit (`system-execution-required-wpr-boot-no-hit-current-build` / `audio-execution-required-megatrigger-etw-no-hit-current-build`)

For the audio-specific value, keep the additional `no-primary-current-build-doc` blocker because Microsoft documentation covers the adjacent generic execution-required and audio-active behavior, but not the exact internal `AllowAudioToEnableExecutionRequiredPowerRequests` value.

For the system-required value, Microsoft documents the public hidden `Allow system required requests` family, but the internal `Control\Power\AllowSystemRequiredPowerRequests` seeding path is still inferred from current-build static/KD evidence rather than documented or captured as an exact live registry read.

## Next useful lane

The next useful lane is not another broad trigger. It should either symbol-resolve/name the INIT walker that consumes the descriptor table or capture an exact registry read for the two values in a stable runtime lane.
