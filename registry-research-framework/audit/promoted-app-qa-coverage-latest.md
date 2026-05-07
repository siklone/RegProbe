# Promoted App QA Coverage

- Generated UTC: 2026-05-07T17:24:52Z
- History entries: 71
- Promoted app-QA candidates: 242
- Covered: 212
- Uncovered: 30
- Coverage: 87.6%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 9
- Explorer: 17
- Network: 24
- Notifications: 5
- Performance: 3
- Peripheral: 3
- Power: 10
- Privacy: 53
- Security: 17
- System: 46
- Visibility: 21

## Uncovered Categories

- Network: 4
- Privacy: 14
- Security: 4
- System: 6
- Visibility: 2

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 79.1%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-switchback.policy --id privacy.disable-telemetry-change-notifications --id privacy.disable-telemetry-optin-ui --id privacy.disable-wer --id privacy.disable-windows-location-provider`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-switchback.policy --id privacy.disable-telemetry-change-notifications --id privacy.disable-telemetry-optin-ui --id privacy.disable-wer --id privacy.disable-windows-location-provider --run-kvm --json`
- Security: 4 uncovered cards | coverage 80.95%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.powershell-unrestricted --id security.threat-file-hash-logging --id security.trusted-path-credential-prompting --id security.uac-never-notify`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.powershell-unrestricted --id security.threat-file-hash-logging --id security.trusted-path-credential-prompting --id security.uac-never-notify --run-kvm --json`
- Network: 4 uncovered cards | coverage 85.71%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-require-dialect-3_1_1 --id network.smb-require-signing-client --id network.smb-require-signing-server --id network.smb-set-cipher-suite-order`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-require-dialect-3_1_1 --id network.smb-require-signing-client --id network.smb-require-signing-server --id network.smb-set-cipher-suite-order --run-kvm --json`
- System: 5 uncovered cards | coverage 88.46%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages --run-kvm --json`
- Visibility: 2 uncovered cards | coverage 91.3%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar --run-kvm --json`

## Remaining Uncovered Sample

- `network.smb-require-dialect-3_1_1` | Require SMB Dialect 3.1.1 | Network
- `network.smb-require-signing-client` | SMB Client Signing Requirement | Network
- `network.smb-require-signing-server` | SMB Server Signing Requirement | Network
- `network.smb-set-cipher-suite-order` | SMB Cipher Suite Order | Network
- `privacy.disable-switchback.policy` | Disable SwitchBack Compatibility Policy | Privacy
- `privacy.disable-telemetry-change-notifications` | Diagnostic Data Change Notifications | Privacy
- `privacy.disable-telemetry-optin-ui` | Diagnostic Data Opt-In Settings UI | Privacy
- `privacy.disable-wer` | Windows Error Reporting | Privacy
- `privacy.disable-windows-location-provider` | Windows Location Provider | Privacy
- `privacy.disable-windows-tips` | Turn Off Windows Tips | Privacy
- `privacy.hide-last-logged-in-user` | Display of the Last Signed-In Username | Privacy
- `privacy.hide-recommended-personalized-sites` | Start Personalized Site Recommendations | Privacy
- `privacy.hide-recommended-personalized-sites-user` | Start Personalized Site Recommendations (Current User) | Privacy
- `privacy.hide-recommended-section` | Start Menu Recommended Section | Privacy
- `privacy.hide-username-at-signin` | Display of Username During Sign-In | Privacy
- `privacy.limit-diagnostic-log-collection` | Advanced Diagnostic Log Collection | Privacy
- `privacy.limit-dump-collection` | Diagnostic Dump Collection | Privacy
- `privacy.troubleshooter-dont-run` | Recommended Troubleshooting for Known Problems | Privacy
- `security.powershell-unrestricted` | Windows PowerShell Script Execution Policy | Security
- `security.threat-file-hash-logging` | Microsoft Defender Threat File Hash Logging | Security
