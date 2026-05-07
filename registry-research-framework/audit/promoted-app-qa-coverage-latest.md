# Promoted App QA Coverage

- Generated UTC: 2026-05-07T18:07:07Z
- History entries: 81
- Promoted app-QA candidates: 242
- Covered: 225
- Uncovered: 17
- Coverage: 92.98%

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
- Privacy: 58
- Security: 21
- System: 46
- Visibility: 21

## Uncovered Categories

- Privacy: 9
- System: 6
- Visibility: 2

## Recommended Next Batches

- Privacy: 5 uncovered cards | coverage 86.57%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-windows-tips --id privacy.hide-last-logged-in-user --id privacy.hide-recommended-personalized-sites --id privacy.hide-recommended-personalized-sites-user --id privacy.hide-recommended-section`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id privacy.disable-windows-tips --id privacy.hide-last-logged-in-user --id privacy.hide-recommended-personalized-sites --id privacy.hide-recommended-personalized-sites-user --id privacy.hide-recommended-section --run-kvm --json`
- System: 5 uncovered cards | coverage 88.46%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id system.services.disable-connected-user-experiences --id system.services.disable-print-notifications --id system.services.disable-print-spooler --id system.services.disable-windows-search --id system.verbose-status-messages --run-kvm --json`
- Visibility: 2 uncovered cards | coverage 91.3%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id visibility.disable-window-animations --id visibility.hide-people-bar --run-kvm --json`

## Remaining Uncovered Sample

- `privacy.disable-windows-tips` | Turn Off Windows Tips | Privacy
- `privacy.hide-last-logged-in-user` | Display of the Last Signed-In Username | Privacy
- `privacy.hide-recommended-personalized-sites` | Start Personalized Site Recommendations | Privacy
- `privacy.hide-recommended-personalized-sites-user` | Start Personalized Site Recommendations (Current User) | Privacy
- `privacy.hide-recommended-section` | Start Menu Recommended Section | Privacy
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
