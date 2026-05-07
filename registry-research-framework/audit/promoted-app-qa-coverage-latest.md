# Promoted App QA Coverage

- Generated UTC: 2026-05-07T17:10:54Z
- History entries: 63
- Promoted app-QA candidates: 242
- Covered: 188
- Uncovered: 54
- Coverage: 77.69%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 7
- Explorer: 13
- Network: 24
- Notifications: 5
- Performance: 3
- Peripheral: 3
- Power: 7
- Privacy: 48
- Security: 17
- System: 41
- Visibility: 16

## Uncovered Categories

- Developer: 2
- Explorer: 4
- Network: 4
- Power: 3
- Privacy: 19
- Security: 4
- System: 11
- Visibility: 7

## Recommended Next Batches

- Visibility: 5 uncovered cards | coverage 69.57%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-spotlight-settings --id visibility.disable-spotlight-third-party --id visibility.disable-spotlight-welcome --id visibility.disable-wcn-wizards --id visibility.disable-widgets`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-spotlight-settings --id visibility.disable-spotlight-third-party --id visibility.disable-spotlight-welcome --id visibility.disable-wcn-wizards --id visibility.disable-widgets --run-kvm --json`
- Power: 3 uncovered cards | coverage 70.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.hide-sleep-option --id power.optimize-cpu-boost --id power.optimize-gaming-network`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.hide-sleep-option --id power.optimize-cpu-boost --id power.optimize-gaming-network --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 71.64%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy --run-kvm --json`
- Explorer: 4 uncovered cards | coverage 76.47%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.show-info-tips --id explorer.show-protected-operating-system-files --id explorer.show-recent-items --id explorer.show-type-overlay --run-kvm --json`
- Developer: 2 uncovered cards | coverage 77.78%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.windows-dev-mode --id developer.wsl2-memory --run-kvm --json`

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
- `power.hide-sleep-option` | Show Sleep Option | Power
- `power.optimize-cpu-boost` | Optimize CPU Performance Boost | Power
- `power.optimize-gaming-network` | Games MMCSS Task Profile | Power
- `privacy.disable-rsop-logging` | Resultant Set of Policy Logging | Privacy
- `privacy.disable-search-box-suggestions` | File Explorer Search Box Suggestions | Privacy
- `privacy.disable-sensors` | Windows Sensors | Privacy
- `privacy.disable-steps-recorder` | Steps Recorder | Privacy
- `privacy.disable-suggestions.policy` | Windows Suggestion Surfaces Policy Group | Privacy
- `privacy.disable-switchback.policy` | Disable SwitchBack Compatibility Policy | Privacy
- `privacy.disable-telemetry-change-notifications` | Diagnostic Data Change Notifications | Privacy
