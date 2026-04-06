# System I/O AllowRemoteDASD KVM Local KD Disassembly Follow-up

Date: 2026-04-06
Candidate: `system.io-allow-remote-dasd`
Guest: `regprobe-win11-25h2-session`

## Objective
- move the KVM local-KD lane beyond symbol resolution and inspect the live current-build `IopAllowRemoteDASD` path directly
- check whether the running guest still points the function at the removable-storage policy path that earlier Ghidra work had already suggested

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `uf nt!IopAllowRemoteDASD` showed a compact live current-build path that calls `IopOpenRegistryKey` and then `IopGetRegistryValue`
- the disassembly loaded two live string literals at `0xfffff800\`972c32a0` and `0xfffff800\`972c3340`
- `du 0xfffff800\`972c32a0` resolved the key path to `\REGISTRY\MACHINE\SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices`
- `du 0xfffff800\`972c3340` resolved the value-name literal to `AllowRemoteDASD`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-disasm-20260406a/local-kd-allowremotedasd-disasm-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-disasm-20260406a/local-kd-allowremotedasd-disasm-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-strings-20260406a/local-kd-allowremotedasd-strings-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-strings-20260406a/local-kd-allowremotedasd-strings-20260406a.log`

## Short Take
- KVM local KD now confirms the same removable-storage policy collision that earlier Ghidra work had already surfaced
- on the running current-build guest, `IopAllowRemoteDASD` is not path-ambiguous anymore; its inspected key path is `\REGISTRY\MACHINE\SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices`
- the remaining blocker is direct runtime observation of that path on the working guest, not uncertainty about which path the current-build function targets
