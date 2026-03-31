# system.priority-control

- Class: `A`
- Pipeline: `v3.2`
- Official doc: `true`
- Cross-layer: `true`
- Cross verification: `insufficient`
- Layer set: `runtime_procmon, behavior_wpr, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, procmon, wpr, benchmark, reboot`

Win25H2Clean now has a strong current-build evidence chain for Win32PrioritySeparation as an observed registry surface: reversible 2 -> 38 -> 2 VM proof, live wmiprvse.exe Procmon reads for both states, and bounded rebooted benchmark runs. The raw 0x26 bitmask semantics remain repo interpretation rather than a modern Microsoft-published contract.

## Current verdict

Win25H2Clean reversible proof, the later rebooted VM benchmark pass, and the wmiprvse.exe Procmon reads for both states are enough to treat 2 and 38 as current-build observed runtime states. This verdict does not rely on Microsoft publishing the raw 0x26 bitmask semantics.
