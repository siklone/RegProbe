# power.control.class1-initial-unpark-count

- Class: `C`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra`
- Tools: `procmon, ghidra, ghidra_no_function_fallback`

Draft candidate package for Class1InitialUnparkCount under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed Class1InitialUnparkCount=64, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the shared clean-baseline Procmon follow-up stayed shell-safe but did not materialize usable per-candidate output files. The current app does not ship this raw value as a standalone supported tweak surface.

## Current verdict

Class1InitialUnparkCount is now packaged as a real candidate with live baseline existence, exact repo-doc hits, shared ntoskrnl string and Ghidra corroboration, and a clean-baseline runtime follow-up. It still stays research-gated because the app does not ship this raw registry value as a standalone supported surface and the shared runtime lane did not materialize usable exact-read artifacts.
