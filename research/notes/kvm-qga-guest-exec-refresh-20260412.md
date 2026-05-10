# KVM QGA guest-exec refresh - 2026-04-12

## Summary

The active KVM guest now supports a real qemu guest-agent execution lane again.

The bootstrap ISO was rebuilt with a fresh `qemu-ga-x86_64.msi`, attached to `regprobe-win11-25h2-session`, and installed from the guest admin shell with:

```powershell
msiexec /i D:\extras\QEMU_GA_X86_64.MSI /qn /norestart
```

## Observed change

Before the refresh, `guest-info` reported an old `0.12.1` agent with no `guest-exec` support.

After the MSI refresh, `guest-info` stabilized at:

- version `110.0.2`
- `guest-exec`
- `guest-exec-status`
- `guest-file-open`
- `guest-file-read`
- `guest-file-write`
- `guest-file-close`

That is enough to retire the keyboard-only command lane for many guest tasks.

## Smoke check

The new repo helper:

```bash
python3 scripts/vm-kvm/qga-exec.py \
  --domain regprobe-win11-25h2-session \
  --path cmd.exe \
  --arg /c \
  --arg "echo qga-helper-ok"
```

returned:

```text
stdout: qga-helper-ok
exitcode: 0
```

The follow-up file helper:

```bash
python3 scripts/vm-kvm/qga-put-file.py \
  --domain regprobe-win11-25h2-session \
  --source /tmp/qga-file-ok.txt \
  --destination 'C:\RegProbe-Diag\qga-file-ok.txt'
```

uploaded a 12-byte host file into the guest, and a `guest-exec` PowerShell read-back returned:

```text
qga-file-ok
```

## Why this matters

This does not solve the execution-required pair by itself, but it removes a lot of avoidable friction from the KVM runtime lane. We can now run guest commands, poll status, and move files through qga-supported surfaces instead of relying on long send-key sequences, ISO short-name quirks, or fragile foreground shells.

## Audit artifact

- [kvm-qga-guest-exec-refresh-20260412.json](../../registry-research-framework/audit/kvm-qga-guest-exec-refresh-20260412.json)
