# Static Evidence v3.2 Audit

- Generated: 2026-04-01T15:29:28.291445Z
- Ghidra artifacts scanned: 8
- PDB-missing artifacts: 0
- Ghidra bloat artifacts: 0
- Branch-template-missing artifacts: 0
- URL references: 431
- Unique URLs: 228
- Broken URLs: 0
- Context review URLs: 72
- Reviewed context-fit URLs: 150

## Priority queue

| Tweak | Priority | Status | Reason | Link statuses |
| --- | --- | --- | --- | --- |
| system.priority-control | 1 | resolved | Nohuto flagged Win32PrioritySeparation for overstated semantics and static-proof quality. | reachable_manual_review |
| power.disable-network-power-saving.policy | 1 | resolved | Nohuto flagged SystemResponsiveness doc interpretation inside the network/MMCSS child record. | reviewed_context_fit |

## PDB-missing sample


## Link issues sample

- `reachable_manual_review` https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/overview-server-message-block-signing
- `reviewed_context_mismatch` https://learn.microsoft.com/en-us/windows-hardware/drivers/display/tdr-registry-keys
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-appdeviceinventory
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement#allowautomaticapparchiving
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement#allowgamedvr
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-autoplay#disallowautoplayfornonvolumedevices
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-autoplay#turnoffautoplay
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-connectivity#disablecrossdeviceresume
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience#allowfindmydevice
- `reachable_manual_review` https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience#allowspotlightcollection
