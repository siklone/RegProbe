# Promoted App QA Coverage

- Generated UTC: 2026-05-07T18:09:47Z
- History entries: 82
- Promoted app-QA candidates: 242
- Covered: 230
- Uncovered: 12
- Coverage: 95.04%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 9
- Explorer: 17
- Network: 28
- Notifications: 5
- Performance: 3
- Peripheral: 3
- Power: 10
- Privacy: 63
- Security: 21
- System: 46
- Visibility: 21

## Uncovered Categories

- Privacy: 4
- System: 6
- Visibility: 2

## Recommended Next Batches

- System: 5 uncovered cards | coverage 88.46%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages --run-kvm --json`
- Visibility: 2 uncovered cards | coverage 91.3%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar --run-kvm --json`
- Privacy: 4 uncovered cards | coverage 94.03%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.hide-username-at-signin --id privacy.limit-diagnostic-log-collection --id privacy.limit-dump-collection --id privacy.troubleshooter-dont-run`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.hide-username-at-signin --id privacy.limit-diagnostic-log-collection --id privacy.limit-dump-collection --id privacy.troubleshooter-dont-run --run-kvm --json`

## Remaining Uncovered Sample

- `privacy.hide-username-at-signin` | Display of Username During Sign-In | Privacy
- `privacy.limit-diagnostic-log-collection` | Advanced Diagnostic Log Collection | Privacy
- `privacy.limit-dump-collection` | Diagnostic Dump Collection | Privacy
- `privacy.troubleshooter-dont-run` | Recommended Troubleshooting for Known Problems | Privacy
- `system.services.disable-connected-user-experiences` | Connected User Experiences and Telemetry Service | System
- `system.services.disable-print-notifications` | Print Notification Service | System
- `system.services.disable-print-spooler` | Print Spooler Service | System
- `system.services.disable-windows-search` | Windows Search Service | System
- `system.verbose-status-messages` | Verbose Status Messages | System
- `system.wait-to-kill-service-timeout` | Service Shutdown Timeout | System
- `visibility.disable-window-animations` | Window Animations | Visibility
- `visibility.hide-people-bar` | People Bar on the Taskbar | Visibility
