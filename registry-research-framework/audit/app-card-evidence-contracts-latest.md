# App Card Evidence Contract Sweep

- Status: PASS
- Generated UTC: 2026-05-14T08:37:30Z
- Candidates: 258
- Passing: 258
- Failing: 0
- Required card fields: TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes
- Required proof lanes: docs, runtime, source, rollback

## Categories

- Audio: 3
- Cleanup: 1
- Developer: 9
- Explorer: 17
- Misc: 5
- Network: 28
- Notifications: 5
- Performance: 3
- Peripheral: 8
- Power: 16
- Privacy: 67
- Security: 21
- System: 52
- Visibility: 23

## Failures

- No card/evidence contract failures.

## Sample Passing Records

- `audio.disable-beep` | System Beep Driver | Audio | evidence=7 | runtime=4
- `audio.show-disconnected-devices` | Show Disconnected Audio Devices | Audio | evidence=6 | runtime=4
- `audio.show-hidden-devices` | Show Hidden Audio Devices | Audio | evidence=5 | runtime=3
- `cleanup.disable-reserved-storage` | Disable Reserved Storage | Cleanup | evidence=5 | runtime=1
- `developer.docker-performance` | Docker Desktop WSL 2 Backend | Developer | evidence=3 | runtime=0
- `developer.dotnet-telemetry-disable` | .NET CLI Telemetry Opt-Out | Developer | evidence=6 | runtime=3
- `developer.enable-windows-long-paths` | Windows Long Paths | Developer | evidence=6 | runtime=4
- `developer.nodejs-performance` | Global Node.js Memory Limit Override | Developer | evidence=7 | runtime=3
- `developer.powershell-execution` | PowerShell Script Execution Policy | Developer | evidence=6 | runtime=3
- `developer.python-path-fix` | Enable Windows Long Paths for Python Workflows | Developer | evidence=8 | runtime=3
- `developer.ssh-agent-autostart` | SSH Agent Auto-start | Developer | evidence=6 | runtime=3
- `developer.windows-dev-mode` | Windows Developer Mode | Developer | evidence=8 | runtime=4
- `developer.wsl2-memory` | WSL 2 Memory Limit | Developer | evidence=1 | runtime=0
- `explorer.always-show-icons-never-thumbnails` | Always Show Icons, Never Thumbnails | Explorer | evidence=6 | runtime=4
- `explorer.disable-low-disk-space-warning` | Low Disk Space Warning | Explorer | evidence=6 | runtime=3
- `explorer.disable-taskbar-chat` | Taskbar Chat Icon | Explorer | evidence=7 | runtime=3
- `explorer.enable-explorer-compact-mode` | Enable Explorer Compact View | Explorer | evidence=7 | runtime=3
- `explorer.hide-empty-drives` | Hide Empty Drives | Explorer | evidence=7 | runtime=4
- `explorer.launch-folder-windows-in-a-separate-process` | Launch Folder Windows in a Separate Process | Explorer | evidence=7 | runtime=4
- `explorer.show-compressed-and-encrypted-files-in-color` | Show Compressed and Encrypted Files in Color | Explorer | evidence=7 | runtime=4
