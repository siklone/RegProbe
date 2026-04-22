# Power / Kernel Symbol Hunt Result Ledger Template - 2026-04-22

Bu template, `run-power-kernel-symbol-hunt.py` sonrasi dort artifact review'u icin kullanilir.

## Run Metadata

- Run ID: `<replace-with-run-id>`
- VM: `Win25H2Clean`

## Artifact Paths

- Init walker stdout: `<path>`
- Init walker summary: `<path>`
- Consumers stdout: `<path>`
- Consumers summary: `<path>`
- Setting callback stdout: `<path>`
- Setting callback summary: `<path>`
- Global timer stdout: `<path>`
- Global timer summary: `<path>`

## Marker Review

### execution-required-init-walker

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

### execution-required-consumers

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

### execution-required-setting-callback

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

### global-timer-resolution-reader

- Required markers present: `true/false`
- Strong markers seen: `<list>`
- Weak markers seen: `<list>`

## Choose Exactly One Outcome

- `execution-required-seeding-retained`
- `timeout-branch-separated`
- `timer-anchor-retained-without-reader`
- `symbol-regression-or-wrapper-fog`

## Why

Write one short justification that cites the concrete marker set or exact symbol/body detail seen in the retained stdout.

## Red Flags

List any of:

- missing `REGPROBE_LOCALKD_BEGIN` / `REGPROBE_LOCALKD_END`
- missing exact retained symbol from one required pass
- wildcard-only output with no useful `u` or `uf` body
- timeout callback being misread as boolean-seeding proof

## Stop Condition Triggered

- `true/false`

If true, stop the lane and state which retained anchor regressed.

## Next Move

- Lane: `<execution-required|global-timer|hold>`
- Exact target: `<symbol-or-pass>`

Keep the same non-goals:

- do not reopen a broad ETW/Procmon/WPR replay first
- do not widen into generic `*Power*` or generic `*TimerResolution*` symbol hunting first
- do not treat timeout callback output as proof of the unresolved boolean-seeding pair
