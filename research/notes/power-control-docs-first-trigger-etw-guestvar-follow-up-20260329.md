# Power-Control Docs-First Trigger ETW Guest-Return Follow-Up (2026-03-29)

This follow-up replaced guest-to-host ETL/CSV copy with a guest-return summary pattern for the remaining docs-first power-control candidates. The guest now completes ETW capture and CSV parsing inside the VM, then returns a compact JSON summary through VMware guest variables. The earlier copy-back failures were narrowed to tooling transport, and the guest summary path itself was hardened by switching the CSV pass to streamed parsing instead of loading the full tracerpt CSV into memory.

Canonical artifacts:

- `evidence/files/vm-tooling-staging/power-control-docs-first-trigger-etw-guestvar-20260329-233504/summary.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-trigger-etw-guestvar-20260329-233504/results.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-trigger-etw-guestvar-20260329-234455/summary.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-trigger-etw-guestvar-20260329-234455/results.json`
- `registry-research-framework/tools/run-power-control-docs-first-trigger-etw-guestvar.ps1`

Result summary:

- `power.control.hibernate-enabled-default`
  - `status=exact-line-no-query`
  - `etl_exists=true`, `csv_exists=true`, `exact_line_count=1`, `exact_query_hits=0`
  - trigger failure count remained `1`
  - the ETW follow-up is still environment-limited because the VMware baseline cannot perform a real hibernation transition
- `power.control.mf-buffering-threshold`
  - `status=exact-line-no-query`
  - `etl_exists=true`, `csv_exists=true`, `exact_line_count=1`, `exact_query_hits=0`
- `power.control.timer-rebase-threshold-on-drips-exit`
  - `status=exact-line-no-query`
  - `etl_exists=true`, `csv_exists=true`, `exact_line_count=1`, `exact_query_hits=0`
- `power.control.class1-initial-unpark-count`
  - `status=no-guestvar`
  - VMware Tools dropped before the guest-return summary could be read back on the processor-plan trigger lane
- `power.control.perf-calculate-actual-utilization`
  - `status=no-guestvar`
  - VMware Tools dropped before the guest-return summary could be read back on the perf-plan-stress trigger lane

Interpretation:

- The new guest-return transport pattern is now valid and materially better than the earlier copy-back flow.
- `HibernateEnabledDefault` should remain decision-gated because the VM firmware limitation blocks a real hibernation trigger on this baseline.
- `MfBufferingThreshold` and `TimerRebaseThresholdOnDripsExit` improved from generic trigger ambiguity to `exact-line-no-query`, but still do not have an exact runtime read.
- `Class1InitialUnparkCount` and `PerfCalculateActualUtilization` still need either a more resilient trigger environment or a lower-level lane because VMware Tools dropped during the trigger run before the summary could be returned.
