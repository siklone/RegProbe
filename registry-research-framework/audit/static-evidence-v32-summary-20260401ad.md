# Static Evidence v3.2 Audit

- Generated: 2026-04-01T14:59:09.798767Z
- Ghidra artifacts scanned: 8
- PDB-missing artifacts: 0
- Ghidra bloat artifacts: 0
- Branch-template-missing artifacts: 0
- URL references: 431
- Unique URLs: 228
- Broken URLs: 0
- Context review URLs: 111
- Reviewed context-fit URLs: 111

## Priority queue

| Tweak | Priority | Status | Reason | Link statuses |
| --- | --- | --- | --- | --- |
| system.priority-control | 1 | resolved | Nohuto flagged Win32PrioritySeparation for overstated semantics and static-proof quality. | reachable_manual_review |
| power.disable-network-power-saving.policy | 1 | resolved | Nohuto flagged SystemResponsiveness doc interpretation inside the network/MMCSS child record. | reachable_manual_review, reviewed_context_fit |

## PDB-missing sample


## Link issues sample

- `reachable_manual_review` https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/overview-server-message-block-signing
- `reachable_manual_review` https://learn.microsoft.com/en-us/troubleshoot/windows-server/printing/troubleshoot-printing-scenarios
- `reviewed_context_mismatch` https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/network/using-registry-values-to-enable-and-disable-task-offloading
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-storage-reserve?view=windows-11
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism/dismgetreservedstoragestate-function?view=windows-11
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-server/administration/performance-tuning/role/web-server/
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-server/storage/storage-spaces/manage-smb-multichannel
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/bitlocker-csp
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/defender-csp
