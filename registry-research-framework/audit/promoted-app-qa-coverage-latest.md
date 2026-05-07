# Promoted App QA Coverage

- Generated UTC: 2026-05-07T16:06:19Z
- History entries: 44
- Promoted app-QA candidates: 242
- Covered: 134
- Uncovered: 108
- Coverage: 55.37%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 7
- Explorer: 13
- Network: 14
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 7
- Privacy: 33
- Security: 12
- System: 26
- Visibility: 11

## Uncovered Categories

- Audio: 1
- Developer: 2
- Explorer: 4
- Network: 14
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 34
- Security: 9
- System: 26
- Visibility: 12

## Recommended Next Batches

- Visibility: 5 uncovered cards | coverage 47.83%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-lock-screen-motion --id visibility.disable-lock-screen-slideshow --id visibility.disable-spotlight-action-center --id visibility.disable-spotlight-desktop-collection --id visibility.disable-spotlight-features`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-lock-screen-motion --id visibility.disable-lock-screen-slideshow --id visibility.disable-spotlight-action-center --id visibility.disable-spotlight-desktop-collection --id visibility.disable-spotlight-features --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 49.25%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-local-security-questions --id privacy.disable-location-consent --id privacy.disable-location-consent-system --id privacy.disable-location-scripting --id privacy.disable-location-services`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-local-security-questions --id privacy.disable-location-consent --id privacy.disable-location-consent-system --id privacy.disable-location-scripting --id privacy.disable-location-services --run-kvm --json`
- System: 5 uncovered cards | coverage 50.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.graphics-tdr-level --id system.graphics-tdr-limit-count --id system.graphics-tdr-limit-time --id system.kernel-thread-dpc-enable --id system.memory-clear-pagefile-at-shutdown`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.graphics-tdr-level --id system.graphics-tdr-limit-count --id system.graphics-tdr-limit-time --id system.kernel-thread-dpc-enable --id system.memory-clear-pagefile-at-shutdown --run-kvm --json`
- Network: 5 uncovered cards | coverage 50.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.enable-lltdio --id network.prefer-ipv4 --id network.require-ntlm-ssp-client-session-security --id network.smb-disable-leasing --id network.smb-enable-large-mtu`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.enable-lltdio --id network.prefer-ipv4 --id network.require-ntlm-ssp-client-session-security --id network.smb-disable-leasing --id network.smb-enable-large-mtu --run-kvm --json`
- Security: 5 uncovered cards | coverage 57.14%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-wu-driver-updates --id security.enable-defender-maps-advanced-membership --id security.enable-dynamic-lock --id security.enable-sudo --id security.hide-defender-exclusions-from-local-admins`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-wu-driver-updates --id security.enable-defender-maps-advanced-membership --id security.enable-dynamic-lock --id security.enable-sudo --id security.hide-defender-exclusions-from-local-admins --run-kvm --json`

## Remaining Uncovered Sample

- `audio.show-hidden-devices` | Show Hidden Audio Devices | Audio
- `developer.windows-dev-mode` | Windows Developer Mode | Developer
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer
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
- `network.smb-increase-client-metadata-cache` | SMB Client Metadata Cache Size Bundle | Network
- `network.smb-reject-unencrypted-access` | SMB Server Reject Unencrypted Access | Network
- `network.smb-require-dialect-3_1_1` | Require SMB Dialect 3.1.1 | Network
- `network.smb-require-signing-client` | SMB Client Signing Requirement | Network
- `network.smb-require-signing-server` | SMB Server Signing Requirement | Network
