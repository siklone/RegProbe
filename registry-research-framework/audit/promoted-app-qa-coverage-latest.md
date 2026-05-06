# Promoted App QA Coverage

- Generated UTC: 2026-05-06T21:28:55Z
- History entries: 21
- Promoted app-QA candidates: 242
- Covered: 64
- Uncovered: 178
- Coverage: 26.45%

## Covered Categories

- Audio: 2
- Cleanup: 1
- Developer: 2
- Explorer: 8
- Network: 4
- Notifications: 3
- Performance: 3
- Peripheral: 2
- Power: 7
- Privacy: 8
- Security: 7
- System: 11
- Visibility: 6

## Uncovered Categories

- Audio: 1
- Developer: 7
- Explorer: 9
- Network: 24
- Notifications: 2
- Peripheral: 1
- Power: 3
- Privacy: 59
- Security: 14
- System: 41
- Visibility: 17

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 11.94%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.deny-app-access.policy --id privacy.disable-activity-history --id privacy.disable-app-diagnostics --id privacy.disable-app-suggestions --id privacy.disable-appcompat-engine.policy`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.deny-app-access.policy --id privacy.disable-activity-history --id privacy.disable-app-diagnostics --id privacy.disable-app-suggestions --id privacy.disable-appcompat-engine.policy --run-kvm --json`
- Network: 5 uncovered cards | coverage 14.29%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-lltd --id network.disable-mdns --id network.disable-netbios --id network.disable-netbios-resolution --id network.disable-plaintext-smb-passwords`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id network.disable-lltd --id network.disable-mdns --id network.disable-netbios --id network.disable-netbios-resolution --id network.disable-plaintext-smb-passwords --run-kvm --json`
- System: 5 uncovered cards | coverage 21.15%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.disable-game-recording-broadcasting --id system.disable-jpeg-reduction --id system.disable-restartable-apps --id system.disable-search-highlights-policy --id system.disable-search-remote-queries`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.disable-game-recording-broadcasting --id system.disable-jpeg-reduction --id system.disable-restartable-apps --id system.disable-search-highlights-policy --id system.disable-search-remote-queries --run-kvm --json`
- Developer: 5 uncovered cards | coverage 22.22%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.enable-windows-long-paths --id developer.nodejs-performance --id developer.powershell-execution --id developer.python-path-fix --id developer.ssh-agent-autostart`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id developer.enable-windows-long-paths --id developer.nodejs-performance --id developer.powershell-execution --id developer.python-path-fix --id developer.ssh-agent-autostart --run-kvm --json`
- Visibility: 5 uncovered cards | coverage 26.09%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-acrylic-logon --id visibility.disable-common-control-animations --id visibility.disable-first-signin-animation --id visibility.disable-lock-screen-camera --id visibility.disable-lock-screen-changes`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-acrylic-logon --id visibility.disable-common-control-animations --id visibility.disable-first-signin-animation --id visibility.disable-lock-screen-camera --id visibility.disable-lock-screen-changes --run-kvm --json`

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
