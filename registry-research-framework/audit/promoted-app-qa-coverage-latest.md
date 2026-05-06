# Promoted App QA Coverage

- Generated UTC: 2026-05-06T18:51:02Z
- History entries: 14
- Promoted app-QA candidates: 242
- Covered: 49
- Uncovered: 193
- Coverage: 20.25%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 2
- Explorer: 8
- Network: 4
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 2
- Privacy: 8
- Security: 2
- System: 6
- Visibility: 6

## Uncovered Categories

- Audio: 1
- Developer: 7
- Explorer: 9
- Network: 24
- Notifications: 2
- Peripheral: 1
- Power: 8
- Privacy: 59
- Security: 19
- System: 46
- Visibility: 17

## Recommended Next Batches

- Security: 5 uncovered cards | coverage 9.52%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-downloads-blocking --id security.disable-enhanced-defender-notifications --id security.disable-ntfs-encryption --id security.disable-p2p-updates --id security.disable-picture-password`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id security.disable-downloads-blocking --id security.disable-enhanced-defender-notifications --id security.disable-ntfs-encryption --id security.disable-p2p-updates --id security.disable-picture-password --run-kvm --json`
- System: 5 uncovered cards | coverage 11.54%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.bsod-display-parameters --id system.disable-auto-maintenance --id system.disable-background-gp-updates --id system.disable-clipboard-redirection --id system.disable-fullscreen-optimizations`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.bsod-display-parameters --id system.disable-auto-maintenance --id system.disable-background-gp-updates --id system.disable-clipboard-redirection --id system.disable-fullscreen-optimizations --run-kvm --json`
- Privacy: 5 uncovered cards | coverage 11.94%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.deny-app-access.policy --id privacy.disable-activity-history --id privacy.disable-app-diagnostics --id privacy.disable-app-suggestions --id privacy.disable-appcompat-engine.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.deny-app-access.policy --id privacy.disable-activity-history --id privacy.disable-app-diagnostics --id privacy.disable-app-suggestions --id privacy.disable-appcompat-engine.policy --run-kvm --json`
- Network: 5 uncovered cards | coverage 14.29%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-lltd --id network.disable-mdns --id network.disable-netbios --id network.disable-netbios-resolution --id network.disable-plaintext-smb-passwords`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-lltd --id network.disable-mdns --id network.disable-netbios --id network.disable-netbios-resolution --id network.disable-plaintext-smb-passwords --run-kvm --json`
- Power: 5 uncovered cards | coverage 20.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.disable-cpu-idle-states --id power.disable-network-power-saving.policy --id power.disable-power-throttling --id power.hide-hibernate-option --id power.hide-lock-option`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.disable-cpu-idle-states --id power.disable-network-power-saving.policy --id power.disable-power-throttling --id power.hide-hibernate-option --id power.hide-lock-option --run-kvm --json`

## Remaining Uncovered Sample

- `audio.show-hidden-devices` | Show Hidden Audio Devices | Audio
- `developer.enable-windows-long-paths` | Windows Long Paths | Developer
- `developer.nodejs-performance` | Global Node.js Memory Limit Override | Developer
- `developer.powershell-execution` | PowerShell Script Execution Policy | Developer
- `developer.python-path-fix` | Enable Windows Long Paths for Python Workflows | Developer
- `developer.ssh-agent-autostart` | SSH Agent Auto-start | Developer
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
- `network.disable-lltd` | Set LLTD Policies to Default Behavior | Network
- `network.disable-mdns` | Set mDNS Policy to Local Settings | Network
- `network.disable-netbios` | NetBIOS over TCP/IP | Network
