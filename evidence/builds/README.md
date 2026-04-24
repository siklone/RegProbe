# Build Baselines

Registry exports used for cross-build delta analysis.

When a new Windows build is available:

1. Export `HKLM` and `HKCU` from a clean VM.
2. Run `scripts/research/compare_builds.py`.
3. Records with changed keys get `regression_required = true`.
4. Only those records need re-investigation.

## 2026-04-24 note

The `25H2` baseline export completed inside the guest, but QGA file download
timed out while pulling the `.reg` payloads back to the host. The checked-in
placeholder files preserve the guest-side hash/size metadata and the recovery
instructions for a future re-download.
