# Hiber File Size Percent Lightweight Runtime Follow-Up (2026-03-30)

This follow-up moved `power.control.hiber-file-size-percent` from the residual string-hit queue into the tools-hardened lightweight ETW lane on `RegProbe-Baseline-ToolsHardened-20260330`.

Trigger order:
- hibernate toggle attempt:
  - `powercfg /hibernate on`
  - `powercfg /hibernate off`
- disk and power-state context refresh:
  - `powercfg /a`
- minifilter-style I/O burst:
  - repeated file creation and removal under `C:\RegProbe-Diag\hiber-io-burst`

Final result:
- the clean run is:
  - `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/summary.json`
  - `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/results.json`
  - `evidence/files/vm-tooling-staging/power-control-lightweight-runtime-20260330-164001/power-control-hiber-file-size-percent/summary.json`
- `short-trigger-etw` ended as `exact-line-no-query`
- the ETW output contained exact `HiberFileSizePercent` lines, but no exact `RegQueryValue` hit
- the split trace stop phase did not become the deciding phase because `logman stop` returned an empty failure on that branch
- shell health stayed clean before and after the run

Interpretation:
- this is stronger than a pure static/string candidate because the runtime lane did surface exact `HiberFileSizePercent` lines on the tools-hardened baseline
- it is still below `Class A` because the lane did not produce an exact live read
- the current VMware baseline also remains hibernation-limited, so the toggle trigger cannot be treated as a full native hibernation proof

Project decision:
- package `power.control.hiber-file-size-percent` as `Class B`
- use `runtime_no_read` as the current gating reason
