# Promoted App QA Coverage

- Generated UTC: 2026-05-07T15:46:15Z
- History entries: 39
- Promoted app-QA candidates: 242
- Covered: 119
- Uncovered: 123
- Coverage: 49.17%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 7
- Explorer: 8
- Network: 14
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 7
- Privacy: 28
- Security: 12
- System: 21
- Visibility: 11

## Uncovered Categories

- Audio: 1
- Developer: 2
- Explorer: 9
- Network: 14
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 39
- Security: 9
- System: 31
- Visibility: 12

## Recommended Next Batches

- System: 5 uncovered cards | coverage 40.38%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.enable-game-mode --id system.enable-hags --id system.enable-indexing-encrypted-items --id system.graphics-tdr-ddi-delay --id system.graphics-tdr-delay`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.enable-game-mode --id system.enable-hags --id system.enable-indexing-encrypted-items --id system.graphics-tdr-ddi-delay --id system.graphics-tdr-delay --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 41.79%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-feedback-notifications --id privacy.disable-file-history --id privacy.disable-font-providers --id privacy.disable-kms-activation-telemetry --id privacy.disable-language-list-access`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-feedback-notifications --id privacy.disable-file-history --id privacy.disable-font-providers --id privacy.disable-kms-activation-telemetry --id privacy.disable-language-list-access --run-kvm --json`
- Explorer: 5 uncovered cards | coverage 47.06%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.enable-explorer-compact-mode --id explorer.launch-folder-windows-in-a-separate-process --id explorer.show-compressed-and-encrypted-files-in-color --id explorer.show-drive-letters-first --id explorer.show-full-path`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.enable-explorer-compact-mode --id explorer.launch-folder-windows-in-a-separate-process --id explorer.show-compressed-and-encrypted-files-in-color --id explorer.show-drive-letters-first --id explorer.show-full-path --run-kvm --json`
- Visibility: 5 uncovered cards | coverage 47.83%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-lock-screen-motion --id visibility.disable-lock-screen-slideshow --id visibility.disable-spotlight-action-center --id visibility.disable-spotlight-desktop-collection --id visibility.disable-spotlight-features`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-lock-screen-motion --id visibility.disable-lock-screen-slideshow --id visibility.disable-spotlight-action-center --id visibility.disable-spotlight-desktop-collection --id visibility.disable-spotlight-features --run-kvm --json`
- Network: 5 uncovered cards | coverage 50.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.enable-lltdio --id network.prefer-ipv4 --id network.require-ntlm-ssp-client-session-security --id network.smb-disable-leasing --id network.smb-enable-large-mtu`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.enable-lltdio --id network.prefer-ipv4 --id network.require-ntlm-ssp-client-session-security --id network.smb-disable-leasing --id network.smb-enable-large-mtu --run-kvm --json`

## Remaining Uncovered Sample

- `audio.show-hidden-devices` | Show Hidden Audio Devices | Audio
- `developer.windows-dev-mode` | Windows Developer Mode | Developer
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer
- `explorer.enable-explorer-compact-mode` | Enable Explorer Compact View | Explorer
- `explorer.launch-folder-windows-in-a-separate-process` | Launch Folder Windows in a Separate Process | Explorer
- `explorer.show-compressed-and-encrypted-files-in-color` | Show Compressed and Encrypted Files in Color | Explorer
- `explorer.show-drive-letters-first` | Show Drive Letters First | Explorer
- `explorer.show-full-path` | Show Full Path in Explorer | Explorer
- `explorer.show-info-tips` | Show Explorer Info Tips | Explorer
- `explorer.show-protected-operating-system-files` | Show Protected Operating System Files | Explorer
- `explorer.show-recent-items` | Show Recent Items In Home | Explorer
- `explorer.show-type-overlay` | Display File Icons On Thumbnails | Explorer
- `network.enable-lltdio` | LLTD Mapper I/O Driver Policy | Network
- `network.prefer-ipv4` | IPv4 Preference Override | Network
- `network.require-ntlm-ssp-client-session-security` | Require NTLM SSP Client Session Security and 128-bit Encryption | Network
- `network.smb-disable-leasing` | SMB Server Leasing | Network
- `network.smb-enable-large-mtu` | SMB Client Large MTU | Network
- `network.smb-enable-multichannel` | SMB Multichannel | Network
- `network.smb-enable-quic` | SMB over QUIC | Network
- `network.smb-encrypt-data` | SMB Server Encryption Requirement | Network
