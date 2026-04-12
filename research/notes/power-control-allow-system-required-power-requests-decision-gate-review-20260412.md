# power.control.allow-system-required-power-requests decision gate review - 2026-04-12

## Decision

Keep `power.control.allow-system-required-power-requests` blocked.

The current package has strong reader-side and binding evidence: Microsoft documents the public hidden system-required setting family, live KD resolved `PopPowerRequestConvertSystemToExecution = 1`, disassembly shows current-build consumers, and INIT-table analysis binds the exact value name to the expected global.

The remaining gap is still specific: the internal `Control\Power\AllowSystemRequiredPowerRequests` registry seeding path is inferred rather than resolved to a named/public current-build routine, and no exact runtime registry read has been captured.
