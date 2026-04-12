# power.control.allow-audio-to-enable-execution-required-power-requests decision gate review - 2026-04-12

## Decision

Keep `power.control.allow-audio-to-enable-execution-required-power-requests` blocked.

The current package has strong reader-side and binding evidence: adjacent Microsoft documentation covers generic execution-required and audio-active behavior, live KD resolved `PopPowerRequestActiveAudioEnablesExecutionRequired = 1`, disassembly shows current-build consumers, and INIT-table analysis binds the exact value name to the expected global.

The remaining gaps are still specific: Microsoft has no primary current-build documentation for this exact audio-specific internal value, the registry seeding path is inferred rather than resolved to a named/public routine, and no exact runtime registry read has been captured.
