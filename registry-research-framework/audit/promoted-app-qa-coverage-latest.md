# Promoted App QA Coverage

- Generated UTC: 2026-05-07T17:15:34Z
- History entries: 66
- Promoted app-QA candidates: 242
- Covered: 201
- Uncovered: 41
- Coverage: 83.06%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 7
- Explorer: 13
- Network: 24
- Notifications: 5
- Performance: 3
- Peripheral: 3
- Power: 10
- Privacy: 53
- Security: 17
- System: 41
- Visibility: 21

## Uncovered Categories

- Developer: 2
- Explorer: 4
- Network: 4
- Privacy: 14
- Security: 4
- System: 11
- Visibility: 2

## Recommended Next Batches

- Explorer: 4 uncovered cards | coverage 76.47%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay --run-kvm --json`
- Developer: 2 uncovered cards | coverage 77.78%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory --run-kvm --json`
- System: 5 uncovered cards | coverage 78.85%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.priority-control --id system.reliability-timestamp-enabled --id system.services.disable-bluetooth-audio-gateway --id system.services.disable-bluetooth-support --id system.services.disable-bluetooth-user-service`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.priority-control --id system.reliability-timestamp-enabled --id system.services.disable-bluetooth-audio-gateway --id system.services.disable-bluetooth-support --id system.services.disable-bluetooth-user-service --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 79.1%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-switchback.policy --id privacy.disable-telemetry-change-notifications --id privacy.disable-telemetry-optin-ui --id privacy.disable-wer --id privacy.disable-windows-location-provider`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-switchback.policy --id privacy.disable-telemetry-change-notifications --id privacy.disable-telemetry-optin-ui --id privacy.disable-wer --id privacy.disable-windows-location-provider --run-kvm --json`
- Security: 4 uncovered cards | coverage 80.95%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.powershell-unrestricted --id security.threat-file-hash-logging --id security.trusted-path-credential-prompting --id security.uac-never-notify`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.powershell-unrestricted --id security.threat-file-hash-logging --id security.trusted-path-credential-prompting --id security.uac-never-notify --run-kvm --json`

## Remaining Uncovered Sample

- `developer.windows-dev-mode` | Windows Developer Mode | Developer
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer
- `explorer.show-info-tips` | Show Explorer Info Tips | Explorer
- `explorer.show-protected-operating-system-files` | Show Protected Operating System Files | Explorer
- `explorer.show-recent-items` | Show Recent Items In Home | Explorer
- `explorer.show-type-overlay` | Display File Icons On Thumbnails | Explorer
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
