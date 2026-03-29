# Kernel/Power 96 Phase 0 Existence Pass

Date: 2026-03-29

Source note: `research/notes/kernel-power-96-key-routing-20260327.md`

Repo truth: the broad batch is `96`, not `101`. The routing note dated `2026-03-27` explicitly says the pasted batch contains `96` `reg add` lines.

## Outcome

- Total candidates: `96`
- Parent path exists: `94`
- Value exists: `21`
- Path only: `73`
- Path missing: `2`
- Read errors: `0`

## Manifest Surfaces

- `registry-research-framework/audit/kernel-power-96-phase0-candidates-20260329.json` is the richer routed candidate package used by the successful live Phase 0 run.
- `registry-research-framework/audit/kernel-power-96-phase0-manifest-20260329.json` is the normalized manifest retained so failed/successful probe summaries do not point at a missing source package.

## Route Buckets

- `existing-covered`: total `8`, value-exists `3`, path-only `4`, path-missing `1`
- `research-only`: total `5`, value-exists `2`, path-only `3`, path-missing `0`
- `docs-first-new-candidate`: total `63`, value-exists `7`, path-only `55`, path-missing `1`
- `net-new`: total `20`, value-exists `9`, path-only `11`, path-missing `0`

## First Queue

- `docs-first-new-candidate-power-control-value-exists` -> `7` candidates, lane `docs-static-triage`
  Candidates: `power.control.class1-initial-unpark-count`, `power.control.hibernate-enabled`, `power.control.hibernate-enabled-default`, `power.control.lid-reliability-state`, `power.control.mf-buffering-threshold`, `power.control.perf-calculate-actual-utilization`, `power.control.timer-rebase-threshold-on-drips-exit`
- `net-new-session-manager-executive-value-exists` -> `3` candidates, lane `candidate-follow-up`
  Candidates: `system.executive-additional-critical-worker-threads`, `system.executive-additional-delayed-worker-threads`, `system.executive-uuid-sequence-number`
- `net-new-session-manager-power-value-exists` -> `3` candidates, lane `candidate-follow-up`
  Candidates: `power.session-power-setting-profile`, `power.session-watchdog-resume-timeout`, `power.session-watchdog-sleep-timeout`
- `net-new-power-control-value-exists` -> `2` candidates, lane `candidate-follow-up`
  Candidates: `power.customize-during-setup`, `power.source-settings-version`
- `net-new-session-manager-io-value-exists` -> `1` candidates, lane `candidate-follow-up`
  Candidates: `system.io-allow-remote-dasd`

## Special Notes

- `power.force-hibernate-disabled.policy` and `power.throttling.power-throttling-off` are the only `path-missing` cases on this baseline.
- `session-manager-kernel` stayed `path-only` across all `22` candidates in this pass.
- `session-manager-power` and `session-manager-executive` remain the strongest live kernel families because they expose existing values on the clean baseline.
- The phase completed with shell healthy before and after execution.
