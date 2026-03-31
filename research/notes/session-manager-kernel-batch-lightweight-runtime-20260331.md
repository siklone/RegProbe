# Session Manager Kernel lightweight runtime (2026-03-31)

The `session-manager-kernel` broad batch was rerun on `RegProbe-Baseline-ToolsHardened-20260330` with the fixed nonblocking VMware start path and a guest-processed ETW parse. The lane wrote all 21 candidate DWORD values under `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`, rebooted once, stopped the boot trace, ran `tracerpt` inside the guest, and copied back only compact JSON summaries.

Net result:

- `system.kernel.disable-exception-chain-validation` produced an exact runtime read with `exact_query_hits = 4`.
- `0` candidates landed in `exact-line-no-query`.
- `0` candidates landed in `path-only-hit`.
- `20` candidates stayed `no-hit`.

Residual hold after this batch:

- `system.kernel.always-track-io-boosting`
- `system.kernel.disable-control-flow-guard-export-suppression`
- `system.kernel.disable-light-weight-suspend`
- `system.kernel.enable-per-cpu-clock-tick-scheduling`
- `system.kernel.enable-tick-accumulation-from-accounting-periods`
- `system.kernel.force-bugcheck-for-dpc-watchdog`
- `system.kernel.force-foreground-boost-decay`
- `system.kernel.force-idle-grace-period`
- `system.kernel.force-parking-requested`
- `system.kernel.global-timer-resolution-requests`
- `system.kernel.hyper-start-disabled`
- `system.kernel.interrupt-steering-flags`
- `system.kernel.long-dpc-queue-threshold`
- `system.kernel.long-dpc-runtime-threshold`
- `system.kernel.max-dynamic-tick-duration`
- `system.kernel.maximum-cooperative-idle-search-width`
- `system.kernel.rebalance-min-priority`
- `system.kernel.timer-check-flags`
- `system.kernel.xstate-context-lookaside-per-proc-max-depth`
- `system.kernel.enable-wer-user-reporting`

Runtime lane references:

- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/summary.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/results.json`
- `evidence/files/vm-tooling-staging/session-manager-kernel-batch-lightweight-runtime-primary-20260331-171654/state.json`

The VM stayed shell-healthy before and after the single reboot. This batch is now strong enough to promote `system.kernel.disable-exception-chain-validation` into a standalone research record while keeping the other 20 Session Manager Kernel values in residual hold.
