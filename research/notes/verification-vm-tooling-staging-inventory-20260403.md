# VM Tooling Staging Inventory

Date: `2026-04-03`

This is a verification-only inventory of `evidence/files/vm-tooling-staging`.

Safe cleanup boundary:

- Do not delete any directory or file referenced by the current WinDbg / debug-baseline audit artifacts.
- Do not delete anything while it is still referenced by active summary, results, or status JSON.
- Treat all entries under `vm-tooling-staging` as scratch until a cleanup review proves otherwise.

Protected refs:

- `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-20260403.json`
- `registry-research-framework/audit/power-control-windbg-singlekey-allow-system-required-power-requests-status-20260403.json`
- `registry-research-framework/audit/power-control-mega-trigger-v2-status-20260402.json`
- `registry-research-framework/audit/configure-kernel-debug-baseline.json`
- `registry-research-framework/audit/bcd-current-20260403.txt`

Inventory snapshot:

- Directory count: `187`
- File count: `641`
- WindDbg trace directories: `27`
- Power-control primary sessions: `10`
- Power-control storage preflight sessions: `10`
- CPU idle sessions: `18`

Cleanup review candidates are listed in the JSON report only; no destructive cleanup is performed.
