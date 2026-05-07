# Promoted App QA Coverage

- Generated UTC: 2026-05-07T17:14:00Z
- History entries: 65
- Promoted app-QA candidates: 242
- Covered: 196
- Uncovered: 46
- Coverage: 80.99%

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
- Privacy: 48
- Security: 17
- System: 41
- Visibility: 21

## Uncovered Categories

- Developer: 2
- Explorer: 4
- Network: 4
- Privacy: 19
- Security: 4
- System: 11
- Visibility: 2

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 71.64%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy --run-kvm --json`
- Explorer: 4 uncovered cards | coverage 76.47%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay --run-kvm --json`
- Developer: 2 uncovered cards | coverage 77.78%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory --run-kvm --json`
- System: 5 uncovered cards | coverage 78.85%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.priority-control --id system.reliability-timestamp-enabled --id system.services.disable-bluetooth-audio-gateway --id system.services.disable-bluetooth-support --id system.services.disable-bluetooth-user-service`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.priority-control --id system.reliability-timestamp-enabled --id system.services.disable-bluetooth-audio-gateway --id system.services.disable-bluetooth-support --id system.services.disable-bluetooth-user-service --run-kvm --json`
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
- `privacy.disable-rsop-logging` | Resultant Set of Policy Logging | Privacy
- `privacy.disable-search-box-suggestions` | File Explorer Search Box Suggestions | Privacy
- `privacy.disable-sensors` | Windows Sensors | Privacy
- `privacy.disable-steps-recorder` | Steps Recorder | Privacy
- `privacy.disable-suggestions.policy` | Windows Suggestion Surfaces Policy Group | Privacy
- `privacy.disable-switchback.policy` | Disable SwitchBack Compatibility Policy | Privacy
- `privacy.disable-telemetry-change-notifications` | Diagnostic Data Change Notifications | Privacy
- `privacy.disable-telemetry-optin-ui` | Diagnostic Data Opt-In Settings UI | Privacy
- `privacy.disable-wer` | Windows Error Reporting | Privacy
- `privacy.disable-windows-location-provider` | Windows Location Provider | Privacy
