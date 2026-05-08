# Promotion-Eligible Review Pack

Generated: `2026-05-08T10:04:37.098992Z`

This pack covers records that have no active evidence blocker but still need a final shipping decision.

| Metric | Value |
|---|---:|
| Total records | 4 |
| Promote | 0 |
| Promote with warnings | 1 |
| Conditional promote | 1 |
| Hold closed | 2 |
| Reject | 0 |

## Preconditions

| Check | Value |
|---|---:|
| `blocked_count` | `0` |
| `unclassified_rejected` | `0` |
| `all_records_confidence_high` | `True` |
| `all_records_next_missing_layer_none` | `True` |

## Decision Matrix

| Record | Risk | Recommended action | App mapping | Evidence | Manual checks | Rationale |
|---|---|---|---|---:|---|---|
| `power.control.hibernate-enabled` | `low` | `CONDITIONAL-PROMOTE` | `not-mapped` | 6 | vm, app-mapping | The raw value has evidence, but product promotion should be conditional because platform firmware support gates the real behavior. |
| `power.control.perf-calculate-actual-utilization` | `medium-high` | `PROMOTE-WITH-WARNINGS` | `matches-research` | 9 | none | Evidence is strong and rollback is known, but CPU utilization semantics are broad enough to require warning labels. |
| `system.executive-additional-worker-threads` | `high` | `INTENTIONAL-HOLD-CLOSED` | `matches-research` | 14 | none | The value is evidence-full, but the shipping decision should stay closed because the safe preset is machine-dependent and high risk. |
| `system.kernel.disable-exception-chain-validation` | `critical-security` | `INTENTIONAL-HOLD-CLOSED` | `matches-research` | 5 | none | This is evidence-full but intentionally non-actionable because it is a security mitigation bypass surface. |
