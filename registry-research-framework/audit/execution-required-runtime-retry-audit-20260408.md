# Execution-Required Runtime Retry Audit

Date: 2026-04-08
Target path: `HKLM\SYSTEM\CurrentControlSet\Control\Power`
Probe glob: `power-control-batch-mega-trigger-runtime-primary-*/summary.json`

## Outcome

- Parsed retained runs: `10`
- Runs that armed both execution-required values from `null` to `1`: `10`
- Summary statuses: `{'aborted-recovered': 10}`
- Candidate statuses: `{'aborted-recovered': 20}`
- Every parsed retained mega-trigger runtime retry ended `aborted-recovered` for both execution-required candidates.
- None of the parsed retries produced an exact query hit or exact line hit for the pair.

## Artifacts

- `registry-research-framework/audit/execution-required-runtime-retry-audit-20260408.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-225635/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-225635/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-225635/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-232214/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-232214/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-232214/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-234542/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-234542/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260401-234542/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-024017/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-024017/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-024017/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-034940/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-034940/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-034940/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-043108/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-043108/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-043108/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-051919/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-051919/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-051919/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-060001/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-060001/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-060001/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-064318/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-064318/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-064318/state.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-144724/summary.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-144724/results.json`
- `evidence/files/vm-tooling-staging/power-control-batch-mega-trigger-runtime-primary-20260402-144724/state.json`

## Interpretation

- The execution-required pair no longer lacks repeated runtime trigger attempts. The retained mega-trigger family was exercised many times on the current build.
- The unresolved gap is now narrower than a generic `runtime-trace` miss: the repeated trigger family is unstable for this pair and consistently recovers before yielding an exact registry read.
- This leaves the next proof path as either a narrower exact-read lane or a different runtime surface, not another generic mega-trigger retry.
