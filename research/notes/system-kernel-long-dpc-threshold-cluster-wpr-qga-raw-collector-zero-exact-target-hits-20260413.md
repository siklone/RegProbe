# system.kernel-long-dpc-threshold-cluster WPR/QGA raw collector no-hit - 2026-04-13

## Result

The targeted QGA-launched WPR boot-registry lane for the Long-DPC threshold cluster observed a real reboot, kept the guest stable, and exercised the new raw-collector salvage fallback when the requested target ETL was missing.

- Target key: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
- Primary value: `LongDpcRuntimeThreshold`
- Additional fragment: `LongDpcQueueThreshold`
- Run id: `long-dpc-threshold-cluster-wpr-qga-20260413a`
- Transport: `qga`
- Target ETL saved by `wpr -stopboot`: `false`
- Raw collector salvage: `true`
- Raw collector ETL size: `262,144` bytes
- Raw collector CSV size: `2,677` bytes
- Normalized bundle: `status = ok`, `event_count = 0`
- Exact hits: `LongDpcRuntimeThreshold = 0`, `LongDpcQueueThreshold = 0`

## Interpretation

This does not close `runtime_no_read`. The retained raw collector is the internal WPR collector, not the full requested Power/Registry target ETL, so it is treated as a tool-hardening and negative-evidence artifact rather than a decisive runtime trace.

It still improves the lane. Before this run, target-ETL-missing cases collapsed into `normalized-bundle-missing`; now the guest collector can salvage any retained WPR collector, convert it with `tracerpt`, filter exact fragments, and emit a normalized empty bundle when there are no target hits. The Long-DPC cluster therefore has a fresh QGA boot attempt with explicit zero-hit accounting for both sibling value names, while the real blocker remains exact runtime registry-read proof.

The next step is not another identical WPR retry. It is either a fixed WPR boot profile that reliably saves the requested Registry target ETL, or a debugger-assisted descriptor-consumption trace for `KiLongDpcQueueThreshold` and `KiLongDpcRuntimeThreshold`.

## Artifact Set

- [long-dpc-threshold-cluster-wpr-qga-20260413a-summary-arm.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413/long-dpc-threshold-cluster-wpr-qga-20260413a-summary-arm.json)
- [long-dpc-threshold-cluster-wpr-qga-20260413a-summary.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413/long-dpc-threshold-cluster-wpr-qga-20260413a-summary.json)
- [long-dpc-threshold-cluster-wpr-qga-20260413a.normalized.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413/long-dpc-threshold-cluster-wpr-qga-20260413a.normalized.json)
- [long-dpc-threshold-cluster-wpr-qga-20260413a.raw-collector.csv](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413/long-dpc-threshold-cluster-wpr-qga-20260413a.raw-collector.csv)
- [long-dpc-threshold-cluster-wpr-qga-20260413a.raw-collector.hits.csv](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/evidence/files/vm-tooling-staging/long-dpc-threshold-cluster-wpr-qga-raw-collector-zero-exact-target-hits-20260413/long-dpc-threshold-cluster-wpr-qga-20260413a.raw-collector.hits.csv)
