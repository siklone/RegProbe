# Promoted App QA Coverage

- Generated UTC: 2026-05-07T17:03:53Z
- History entries: 59
- Promoted app-QA candidates: 242
- Covered: 178
- Uncovered: 64
- Coverage: 73.55%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 7
- Explorer: 13
- Network: 19
- Notifications: 5
- Performance: 3
- Peripheral: 3
- Power: 7
- Privacy: 48
- Security: 17
- System: 36
- Visibility: 16

## Uncovered Categories

- Developer: 2
- Explorer: 4
- Network: 9
- Power: 3
- Privacy: 19
- Security: 4
- System: 16
- Visibility: 7

## Recommended Next Batches

- Network: 5 uncovered cards | coverage 67.86%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-enable-multichannel --id network.smb-enable-quic --id network.smb-encrypt-data --id network.smb-increase-client-metadata-cache --id network.smb-reject-unencrypted-access`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-enable-multichannel --id network.smb-enable-quic --id network.smb-encrypt-data --id network.smb-increase-client-metadata-cache --id network.smb-reject-unencrypted-access --run-kvm --json`
- System: 5 uncovered cards | coverage 69.23%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.ntfs-disable-8dot3 --id system.ntfs-disable-last-access --id system.ntfs-enable-long-paths --id system.ntfs-reset-memory-usage --id system.ntfs-reset-mft-zone`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.ntfs-disable-8dot3 --id system.ntfs-disable-last-access --id system.ntfs-enable-long-paths --id system.ntfs-reset-memory-usage --id system.ntfs-reset-mft-zone --run-kvm --json`
- Visibility: 5 uncovered cards | coverage 69.57%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-spotlight-settings --id visibility.disable-spotlight-third-party --id visibility.disable-spotlight-welcome --id visibility.disable-wcn-wizards --id visibility.disable-widgets`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-spotlight-settings --id visibility.disable-spotlight-third-party --id visibility.disable-spotlight-welcome --id visibility.disable-wcn-wizards --id visibility.disable-widgets --run-kvm --json`
- Power: 3 uncovered cards | coverage 70.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.hide-sleep-option --id power.optimize-cpu-boost --id power.optimize-gaming-network`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.hide-sleep-option --id power.optimize-cpu-boost --id power.optimize-gaming-network --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 71.64%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-rsop-logging --id privacy.disable-search-box-suggestions --id privacy.disable-sensors --id privacy.disable-steps-recorder --id privacy.disable-suggestions.policy --run-kvm --json`

## Remaining Uncovered Sample

- `developer.windows-dev-mode` | Windows Developer Mode | Developer
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer
- `explorer.show-info-tips` | Show Explorer Info Tips | Explorer
- `explorer.show-protected-operating-system-files` | Show Protected Operating System Files | Explorer
- `explorer.show-recent-items` | Show Recent Items In Home | Explorer
- `explorer.show-type-overlay` | Display File Icons On Thumbnails | Explorer
- `network.smb-enable-multichannel` | SMB Multichannel | Network
- `network.smb-enable-quic` | SMB over QUIC | Network
- `network.smb-encrypt-data` | SMB Server Encryption Requirement | Network
- `network.smb-increase-client-metadata-cache` | SMB Client Metadata Cache Size Bundle | Network
- `network.smb-reject-unencrypted-access` | SMB Server Reject Unencrypted Access | Network
- `network.smb-require-dialect-3_1_1` | Require SMB Dialect 3.1.1 | Network
- `network.smb-require-signing-client` | SMB Client Signing Requirement | Network
- `network.smb-require-signing-server` | SMB Server Signing Requirement | Network
- `network.smb-set-cipher-suite-order` | SMB Cipher Suite Order | Network
- `power.hide-sleep-option` | Show Sleep Option | Power
- `power.optimize-cpu-boost` | Optimize CPU Performance Boost | Power
- `power.optimize-gaming-network` | Games MMCSS Task Profile | Power
- `privacy.disable-rsop-logging` | Resultant Set of Policy Logging | Privacy
- `privacy.disable-search-box-suggestions` | File Explorer Search Box Suggestions | Privacy
