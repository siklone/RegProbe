## KVM local KD smoke follow-up (2026-04-06)

Host-driven local kernel debugger smoke now works on the KVM guest from the internal `nvme1n1p1` workspace.

Evidence:
- `evidence/files/vm-tooling-staging/local-kd-kvm-20260406g/local-kd-kvm-20260406g-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-kvm-20260406g/local-kd-kvm-20260406g.log`
- `evidence/files/vm-tooling-staging/local-kd-kvm-20260406g/local-kd-kvm-20260406g.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-kvm-20260406g/local-kd-kvm-20260406g.txt`

What passed:
- `kd.exe` resolved from the Windows Kits debugger bundle.
- `symchk.exe` resolved and returned `exit_code = 0`.
- Local KD attached to the running guest kernel (`attached = true`).
- The smoke completed without timing out (`completed = true`).
- The query command `x nt!CmQueryValueKey` succeeded and returned `fffff800\`97474900`.

Operational note:
- `symchk` still materializes `symchk.txt` as a directory-like output target on this guest, so the helper now treats uploads as leaf-only files and records `symchk_log_is_directory = true` instead of failing the whole run.

Interpretation:
- KVM is no longer limited to Procmon/Ghidra-only follow-ups. We now have a repeatable host-driven path for targeted local KD symbol queries against the live guest, which is the right next transport for Executive- and UUID-adjacent follow-up work.
