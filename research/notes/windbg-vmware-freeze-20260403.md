# VMware WinDbg Freeze

- Date: `20260403`
- Parser and public-symbol work are preserved, not discarded.
- Confirmed public symbol: ``nt!CmQueryValueKey``
- Confirmed value-name argument: ``@rdx``
- Freeze reason: the current VMware named-pipe transport contract remains unreliable for classification-grade arbiter work.
- Future work moves to a debugger-first environment instead of re-spending cycles on the same VMware transport envelope.

## References
- `registry-research-framework/audit/windbg-transport-findings-20260403.json`
- `registry-research-framework/audit/windbg-pipe-launch-matrix-20260403.json`

