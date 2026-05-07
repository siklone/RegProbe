# Promoted App QA Coverage

- Generated UTC: 2026-05-07T16:58:14Z
- History entries: 56
- Promoted app-QA candidates: 242
- Covered: 169
- Uncovered: 73
- Coverage: 69.83%

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
- Privacy: 43
- Security: 17
- System: 36
- Visibility: 16

## Uncovered Categories

- Audio: 1
- Developer: 2
- Explorer: 4
- Network: 9
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 24
- Security: 4
- System: 16
- Visibility: 7

## Recommended Next Batches

- Notifications: 2 uncovered cards | coverage 60.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id notifications.disable-mirroring --id notifications.disable-tile`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id notifications.disable-mirroring --id notifications.disable-tile --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 64.18%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-pca-diagnostics.policy --id privacy.disable-phone-linking --id privacy.disable-program-compatibility-assistant --id privacy.disable-recall --id privacy.disable-resume`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-pca-diagnostics.policy --id privacy.disable-phone-linking --id privacy.disable-program-compatibility-assistant --id privacy.disable-recall --id privacy.disable-resume --run-kvm --json`
- Audio: 1 uncovered cards | coverage 66.67%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id audio.show-hidden-devices`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id audio.show-hidden-devices --run-kvm --json`
- Peripheral: 1 uncovered cards | coverage 66.67%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id peripheral.disable-autoplay`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id peripheral.disable-autoplay --run-kvm --json`
- Network: 5 uncovered cards | coverage 67.86%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-enable-multichannel --id network.smb-enable-quic --id network.smb-encrypt-data --id network.smb-increase-client-metadata-cache --id network.smb-reject-unencrypted-access`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.smb-enable-multichannel --id network.smb-enable-quic --id network.smb-encrypt-data --id network.smb-increase-client-metadata-cache --id network.smb-reject-unencrypted-access --run-kvm --json`

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
