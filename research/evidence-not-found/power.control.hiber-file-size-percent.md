# power.control.hiber-file-size-percent

- Class: `B`
- Record status: `draft`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `behavior_wpr`
- Tools: `etw, wpr`

## Why it stays negative

Docs/static evidence is strong and the lightweight ETW lane surfaced exact HiberFileSizePercent lines, but the current VMware runtime still did not produce an exact live read.

## Attached references

- `repo-doc` Repo power notes for HiberFileSizePercent -> Docs/power/power.md
- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `repo-doc` Residual value-exists string triage for HiberFileSizePercent -> evidence/files/vm-tooling-staging/registry-batch-string-20260330-141213/results.json and research/notes/kernel-power-96-residual-value-exists-static-triage-20260330.md
- `etw-trace` Tools-hardened lightweight ETW follow-up for HiberFileSizePercent -> evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/summary.json and evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/results.json and research/notes/power-control-hiber-file-size-percent-lightweight-runtime-20260330.md
