# Static Evidence v3.2 Audit

- Generated: 2026-03-31T18:47:46.989344Z
- Ghidra artifacts scanned: 8
- PDB-missing artifacts: 8
- Ghidra bloat artifacts: 6
- URL references: 443
- Unique URLs: 235
- Broken URLs: 10
- Context review URLs: 220

## Priority queue

| Tweak | Priority | Reason | Link statuses |
| --- | --- | --- | --- |
| system.priority-control | 1 | Nohuto flagged Win32PrioritySeparation for overstated semantics and static-proof quality. | reachable_but_mismatch |
| power.disable-network-power-saving.policy | 1 | Nohuto flagged SystemResponsiveness doc interpretation inside the network/MMCSS child record. | reachable_but_mismatch |

## PDB-missing sample

- `evidence/files/ghidra/kernel-power-existing-ntoskrnl/ghidra-matches.md`
- `evidence/files/ghidra/kernel-power-nextgate-ntoskrnl/ghidra-matches.md`
- `evidence/files/ghidra/policy-system-enable-virtualization-ntoskrnl-exe-path-aware-20260330-222908/ghidra-matches.md`
- `evidence/files/ghidra/power-control-docs-first-ntoskrnl-20260329-134435/ghidra-matches.md`
- `evidence/files/ghidra/system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-20260330-194412/ghidra-matches.md`
- `evidence/files/ghidra/system.disable-fullscreen-optimizations/ghidra-matches.md`
- `evidence/files/ghidra/system.kernel-serialize-timer-expiration/ghidra-matches.md`
- `evidence/files/ghidra/system.reliability-timestamp-enabled/ghidra-matches.md`

## Link issues sample

- `broken_url` https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gppref/1b851616-4af6-4646-b741-9300b3348b5a
- `broken_url` https://learn.microsoft.com/en-us/previous-versions/windows/desktop/xperf/automatic-maintenance
- `broken_url` https://learn.microsoft.com/en-us/troubleshoot/windows-server/backup-and-storage/low-disk-space-error-due-to-full-mft
- `broken_url` https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/introduction-to-processor-idle-states
- `broken_url` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-cloudcontent
- `broken_url` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowscopilot#turnoffwindowscopilot
- `broken_url` https://support.microsoft.com/en-au/topic/troubleshoot-microsoft-defender-antivirus-settings-9dd824c2-44cf-85a7-bbe1-e0d6ddb8786d
- `broken_url` https://support.microsoft.com/en-us/windows/configure-windows-to-automate-startup-of-apps-when-you-sign-in-4c95407c-6451-49bc-9c2c-799aafac486d
- `broken_url` https://terminal.1.24.10621.0/TerminalApp.dll;
- `broken_url` https://terminal.1.24.10621.0/TerminalApp.dll; evidence/files/ghidra/developer.terminal-dev-mode/terminal-ghidra.txt; evidence/files/ghidra/developer.terminal-dev-mode/terminal-ghidra-enabledebugtap.txt
