# Promoted App QA Coverage

- Generated UTC: 2026-05-06T22:00:04Z
- History entries: 30
- Promoted app-QA candidates: 242
- Covered: 94
- Uncovered: 148
- Coverage: 38.84%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 7
- Explorer: 8
- Network: 9
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 7
- Privacy: 18
- Security: 7
- System: 16
- Visibility: 11

## Uncovered Categories

- Audio: 1
- Developer: 2
- Explorer: 9
- Network: 19
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 49
- Security: 14
- System: 36
- Visibility: 12

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 26.87%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-biometrics-logon --id privacy.disable-camera --id privacy.disable-cli-telemetry --id privacy.disable-consumer-account-content --id privacy.disable-copilot`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-biometrics-logon --id privacy.disable-camera --id privacy.disable-cli-telemetry --id privacy.disable-consumer-account-content --id privacy.disable-copilot --run-kvm --json`
- System: 5 uncovered cards | coverage 30.77%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.disable-search-web-results --id system.disable-storage-sense --id system.disable-storage-sense-temp-cleanup --id system.disable-store-open-with --id system.dwm-disable-overlay-min-fps`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.disable-search-web-results --id system.disable-storage-sense --id system.disable-storage-sense-temp-cleanup --id system.disable-store-open-with --id system.dwm-disable-overlay-min-fps --run-kvm --json`
- Network: 5 uncovered cards | coverage 32.14%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-smart-name-resolution --id network.disable-smb1 --id network.disable-smb2 --id network.disable-wifi-sense --id network.enable-lltd-responder`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-smart-name-resolution --id network.disable-smb1 --id network.disable-smb2 --id network.disable-wifi-sense --id network.enable-lltd-responder --run-kvm --json`
- Security: 5 uncovered cards | coverage 33.33%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-remote-assistance --id security.disable-system-mitigations --id security.disable-system-restore --id security.disable-windows-firewall --id security.disable-windows-update.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-remote-assistance --id security.disable-system-mitigations --id security.disable-system-restore --id security.disable-windows-firewall --id security.disable-windows-update.policy --run-kvm --json`
- Explorer: 5 uncovered cards | coverage 47.06%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.enable-explorer-compact-mode --id explorer.launch-folder-windows-in-a-separate-process --id explorer.show-compressed-and-encrypted-files-in-color --id explorer.show-drive-letters-first --id explorer.show-full-path`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id explorer.enable-explorer-compact-mode --id explorer.launch-folder-windows-in-a-separate-process --id explorer.show-compressed-and-encrypted-files-in-color --id explorer.show-drive-letters-first --id explorer.show-full-path --run-kvm --json`

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
- `network.disable-smart-name-resolution` | Smart Multi-Homed Name Resolution | Network
- `network.disable-smb1` | SMBv1 Server Protocol Support | Network
- `network.disable-smb2` | SMBv2 and SMBv3 Server Protocol Support | Network
- `network.disable-wifi-sense` | Wi-Fi Sense Suggested Hotspot Policy | Network
- `network.enable-lltd-responder` | LLTD Responder Driver Policy | Network
- `network.enable-lltdio` | LLTD Mapper I/O Driver Policy | Network
- `network.prefer-ipv4` | IPv4 Preference Override | Network
- `network.require-ntlm-ssp-client-session-security` | Require NTLM SSP Client Session Security and 128-bit Encryption | Network
