# DefaultDynamicHeteroCpuPolicy OCCT/WPR Pass

This note records an additional Win25H2Clean pass using OCCT and WinSAT while WPR was active for `DefaultDynamicHeteroCpuPolicy`.

## Method

1. OCCT was driven from the guest UI on the CPU stress surface and benchmark surface.
2. WPR was started through the guest wrapper scripts.
3. OCCT benchmark/stress text output was captured from the guest UI automation tree.
4. WinSAT memory load was launched under WPR.
5. A separate guest WPR trace was left at `C:\Temp\vm-tooling-staging\occt-wpr.etl`.
6. The WinSAT memory trace was copied back to the host.

## Observations

- OCCT captured live CPU and memory load figures while the VM was running.
- The benchmark/stress text output included `CPU - Started`, `CPU - Stopped`, and sample CPU values.
- A guest WPR trace was successfully produced for the OCCT session.
- WinSAT memory produced a usable WPR trace.
- A concurrent WinSAT disk attempt did not produce a reusable score because another WinSAT copy was already running, so this pass does not claim a disk benchmark result.

## Selected Output

| Source | Observed |
| --- | --- |
| OCCT UI snapshot | `CPU - Started`, `CPU - Stopped`, and live CPU/memory figures |
| OCCT benchmark text | `CPU - 250.75`, `CPU - 136.24`, `CPU - 488.13` |
| WinSAT memory trace | `H:\Temp\vm-tooling-staging\winsat-mem.etl` |
| Guest WPR trace | `C:\Temp\vm-tooling-staging\occt-wpr.etl` |

## Artifacts

- `H:\Temp\vm-tooling-staging\occt-bench-click.txt`
- `H:\Temp\vm-tooling-staging\occt-bench-debug.txt`
- `H:\Temp\vm-tooling-staging\occt-bench-state.txt`
- `H:\Temp\vm-tooling-staging\winsat-mem.etl`
- `C:\Temp\vm-tooling-staging\occt-wpr.etl`
