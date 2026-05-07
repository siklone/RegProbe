# Promoted App QA Coverage

- Generated UTC: 2026-05-07T16:50:10Z
- History entries: 52
- Promoted app-QA candidates: 242
- Covered: 154
- Uncovered: 88
- Coverage: 63.64%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 7
- Explorer: 13
- Network: 19
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 7
- Privacy: 38
- Security: 12
- System: 31
- Visibility: 16

## Uncovered Categories

- Audio: 1
- Developer: 2
- Explorer: 4
- Network: 9
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 29
- Security: 9
- System: 21
- Visibility: 7

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 56.72%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-mdm-enrollment --id privacy.disable-message-sync --id privacy.disable-offline-files.policy --id privacy.disable-onesettings-downloads --id privacy.disable-online-tips`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-mdm-enrollment --id privacy.disable-message-sync --id privacy.disable-offline-files.policy --id privacy.disable-onesettings-downloads --id privacy.disable-online-tips --run-kvm --json`
- Security: 5 uncovered cards | coverage 57.14%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-wu-driver-updates --id security.enable-defender-maps-advanced-membership --id security.enable-dynamic-lock --id security.enable-sudo --id security.hide-defender-exclusions-from-local-admins`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-wu-driver-updates --id security.enable-defender-maps-advanced-membership --id security.enable-dynamic-lock --id security.enable-sudo --id security.hide-defender-exclusions-from-local-admins --run-kvm --json`
- System: 5 uncovered cards | coverage 59.62%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.memory-disable-paging-executive --id system.memory-large-system-cache-client --id system.memory-nonpaged-pool-dynamic --id system.memory-paged-pool-dynamic --id system.memory-registry-quota-default`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.memory-disable-paging-executive --id system.memory-large-system-cache-client --id system.memory-nonpaged-pool-dynamic --id system.memory-paged-pool-dynamic --id system.memory-registry-quota-default --run-kvm --json`
- Notifications: 2 uncovered cards | coverage 60.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id notifications.disable-mirroring --id notifications.disable-tile`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id notifications.disable-mirroring --id notifications.disable-tile --run-kvm --json`
- Audio: 1 uncovered cards | coverage 66.67%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id audio.show-hidden-devices`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id audio.show-hidden-devices --run-kvm --json`

## Remaining Uncovered Sample

- `audio.show-hidden-devices` | Show Hidden Audio Devices | Audio
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
- `notifications.disable-mirroring` | Notification Mirroring | Notifications
- `notifications.disable-tile` | Tile Notifications | Notifications
- `peripheral.disable-autoplay` | Disable AutoPlay | Peripheral
- `power.hide-sleep-option` | Show Sleep Option | Power
