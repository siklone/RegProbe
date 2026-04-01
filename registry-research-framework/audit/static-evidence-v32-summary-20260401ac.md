# Static Evidence v3.2 Audit

- Generated: 2026-04-01T14:43:07.227665Z
- Ghidra artifacts scanned: 8
- PDB-missing artifacts: 0
- Ghidra bloat artifacts: 0
- Branch-template-missing artifacts: 0
- URL references: 431
- Unique URLs: 228
- Broken URLs: 0
- Context review URLs: 119
- Reviewed context-fit URLs: 103

## Priority queue

| Tweak | Priority | Status | Reason | Link statuses |
| --- | --- | --- | --- | --- |
| system.priority-control | 1 | resolved | Nohuto flagged Win32PrioritySeparation for overstated semantics and static-proof quality. | reachable_manual_review |
| power.disable-network-power-saving.policy | 1 | resolved | Nohuto flagged SystemResponsiveness doc interpretation inside the network/MMCSS child record. | reachable_manual_review, reviewed_context_fit |

## PDB-missing sample


## Link issues sample

- `reachable_manual_review` https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/overview-server-message-block-signing
- `reachable_manual_review` https://learn.microsoft.com/en-us/troubleshoot/windows-server/printing/troubleshoot-printing-scenarios
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/modern-standby
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-kequerydpcwatchdoginformation
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/using-agestore
- `reviewed_context_mismatch` https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/install/hklm-system-currentcontrolset-services-registry-tree
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/introduction-to-threaded-dpcs
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/ppm-notifications
