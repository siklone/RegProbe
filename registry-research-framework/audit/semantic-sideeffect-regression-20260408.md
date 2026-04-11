# Semantic Sideeffect Regression Audit

Cross-format registry exports and dump text can produce large naive line churn while representing the same registry state. Semantic diff collapses those pairs back to zero modified values.

## Session Manager Power baseline (.reg vs .txt)

- Before: `evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.reg`
- After: `evidence/files/vm-tooling-staging/session-manager-power-baseline-20260328-080010/session-manager-power-baseline.txt`
- Naive line churn: `+15` / `-49`
- Semantic values: `added=0` / `removed=0` / `modified=0`

## Power Control root dump (.reg vs .txt)

- Before: `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.reg`
- After: `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`
- Naive line churn: `+4287` / `-9988`
- Semantic values: `added=0` / `removed=0` / `modified=0`
