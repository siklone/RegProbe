# ThreadDpcEnable VM Suite (2026-03-24)

## Scope

- Key: `HKLM/SYSTEM/CurrentControlSet/Control/Session Manager/Kernel`
- Value: `ThreadDpcEnable` (`REG_DWORD`)
- Candidate state under test: `0`
- Baseline before both runs: value missing
- Validation environment: `Win25H2Clean`

```text
Documented contract
Source: Microsoft Learn - Introduction to threaded DPCs
Key: HKLM/System/CurrentControlSet/Control/SessionManager/Kernel/ThreadDpcEnable
Meaning: Threaded DPCs are enabled by default; setting the value to 0 disables them and makes them execute as ordinary DPCs.
```

```text
CPU bounded run
Test ID: thread-dpc-enable-0-cpu3
Flow: baseline missing -> set 0 -> reboot -> idle settle -> WinSAT CPU + WPR -> restore missing -> reboot
Measured duration: 30.31 s
Idle snapshot before benchmark: CPU 2.72, Disk 0.00
Perf sample summary: CPU avg 7.29%, CPU max 22.65%, disk transfers/sec avg 135.07, max 610.11
Artifacts:
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-cpu3.watch.txt
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-cpu3.etl.md
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-cpu3.winsat.txt
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-cpu3.perf.csv
```

```text
Memory bounded run
Test ID: thread-dpc-enable-0-mem2
Flow: baseline missing -> set 0 -> reboot -> idle settle -> WinSAT mem + WPR -> restore missing -> reboot
Measured duration: 30.50 s
Idle snapshot before benchmark: CPU 2.39, Disk 0.07
Perf sample summary: CPU avg 5.26%, CPU max 15.54%, disk transfers/sec avg 113.74, max 628.56
Artifacts:
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-mem2.watch.txt
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-mem2.etl.md
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-mem2.winsat.txt
- research/evidence-files/vm-tooling-staging/thread-dpc-enable-0-mem2.perf.csv
```

## Findings

- Both bounded VM runs completed with real apply and restore reboots.
- The guest returned to the original missing baseline after each run.
- This suite confirms that the documented `0` state is exercisable and reversible in the VM.
- This is not a claim that `ThreadDpcEnable = 0` improves performance; it is a bounded runtime corroboration pass for the documented disable state.
