# System I/O AllowRemoteDASD KVM Local KD Follow-up

Date: 2026-04-06
Candidate: `system.io-allow-remote-dasd`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify that the new KVM local-KD lane can resolve the current-build live kernel symbol for the main static `AllowRemoteDASD` candidate
- check whether the symbolized static result is reproducible against the running guest and not just a Ghidra-only transport artifact

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `symchk.exe` returned `0`, the debugger query completed, and `x nt!IopAllowRemoteDASD` resolved directly in the guest
- the live kernel reported `fffff800\`970cb370 nt!IopAllowRemoteDASD (IopAllowRemoteDASD)`
- this confirms the current-build symbol is present and queryable in the running KVM guest

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-20260406h/local-kd-allowremotedasd-20260406h-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-20260406h/local-kd-allowremotedasd-20260406h.log`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-20260406h/local-kd-allowremotedasd-20260406h.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-allowremotedasd-20260406h/local-kd-allowremotedasd-20260406h.txt`

## Short Take
- KVM local KD now confirms `IopAllowRemoteDASD` in the live guest kernel, which strengthens the current-build symbol story behind the earlier Ghidra package
- this does not, by itself, prove the intended `Session Manager\\I/O System` registry path is the live reader
- the record still stays gated by runtime no-hit and path-context uncertainty
