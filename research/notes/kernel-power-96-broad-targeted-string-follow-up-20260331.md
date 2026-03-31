# Kernel Power 96 Broad Targeted String Follow-up (2026-03-31)

This pass finally takes the broad residual queue out of "path-only hold" and pushes it through one coherent string-first lane instead of opening dozens of tiny ad hoc probes.

Input truth stayed anchored to the existing 96-key intake:

- [phase0 follow-up](../../registry-research-framework/audit/kernel-power-96-phase0-follow-up-20260329.json)
- [remaining groups](../../registry-research-framework/audit/kernel-power-96-remaining-groups-20260330.json)
- [broad manifest](../../registry-research-framework/audit/kernel-power-96-broad-targeted-string-manifest-20260331.json)

The broad batch intentionally excluded:

- already completed or decision-gated candidates
- existing-covered app surfaces
- value-exists candidates that already went through safe-static or path-aware lanes
- path-missing hold items

That left `69` candidates for one exact string-first pass on the tools-hardened VM baseline.

## Runner

- [run-targeted-string-batch-probe.ps1](../../scripts/vm/run-targeted-string-batch-probe.ps1)

The runner keeps the same exact-match semantics as the single-candidate targeted probe, but batches a whole manifest through one guest session. It prefers `strings.exe` when available and falls back to direct ASCII and UTF-16 byte matching when it is not.

## Canonical Evidence

- [summary](../../evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/summary.json)
- [results](../../evidence/files/vm-tooling-staging/targeted-string-batch-primary-20260331-135356/results.json)
- [follow-up audit](../../registry-research-framework/audit/kernel-power-96-broad-targeted-string-follow-up-20260331.json)
- [hit queue](../../registry-research-framework/audit/kernel-power-96-broad-targeted-string-hit-queue-20260331.json)

## Net Result

The split is much stronger than the earlier residual value-exists cleanup:

- `69` total candidates
- `63` exact hits
- `6` no-hits

Family breakdown:

- `power-control`: `29` hit / `5` no-hit
- `session-manager-kernel`: `21` hit / `0` no-hit
- `session-manager-executive`: `4` hit / `0` no-hit
- `session-manager-power`: `4` hit / `0` no-hit
- `session-manager-io`: `3` hit / `0` no-hit
- `power-modern-sleep`: `2` hit / `0` no-hit
- `policy-system`: `0` hit / `1` no-hit

All `63` hits landed in `ntoskrnl.exe` during this first pass. That does not promote anything on its own, but it sharply narrows where the next runtime work should go.

## Hit Queue

The strongest next runtime groups are now obvious:

1. `session-manager-kernel` exact-hit batch (`21`)
2. `power-control` exact-hit batch (`29`)
3. `session-manager-executive` exact-hit batch (`4`)
4. `session-manager-power` exact-hit batch (`4`)
5. `session-manager-io` exact-hit batch (`3`)

The `power-modern-sleep` pair also hit, but it is lower ROI because the current VMware environment is still the limiting factor for modern-standby behavior.

## No-hit Hold

The only clear no-hit group inside the broad path-only queue is the `PowerWatchdog*` timeout cluster:

- `power.control.power-watchdog-drv-set-monitor-timeout-msec`
- `power.control.power-watchdog-dwm-sync-flush-timeout-msec`
- `power.control.power-watchdog-po-callout-timeout-msec`
- `power.control.power-watchdog-power-on-gdi-timeout-msec`
- `power.control.power-watchdog-request-queue-timeout-msec`

And the standalone policy candidate:

- `policy.enable-local-logon-sid`

These should stay in lower-priority hold unless a stronger docs-backed or path-aware reason appears.

## Decision

The broad batch is no longer a vague backlog. It is now split into:

- a large exact-hit queue that is worth real runtime grouping
- a small no-hit hold that should not consume immediate research time

The next practical move is to open the first runtime group on `session-manager-kernel`, not to go back to the weak residual no-hit lanes.
