# Promoted App QA Coverage

- Generated UTC: 2026-05-07T23:27:06Z
- History entries: 88
- Promoted app-QA candidates: 253
- Covered: 243
- Uncovered: 10
- Coverage: 96.05%

## Covered Categories

- Audio: 3
- Cleanup: 1
- Developer: 9
- Explorer: 17
- Network: 28
- Notifications: 5
- Performance: 3
- Peripheral: 4
- Power: 10
- Privacy: 67
- Security: 21
- System: 52
- Visibility: 23

## Uncovered Categories

- Misc: 5
- Peripheral: 4
- Power: 1

## Recommended Next Batches

- Misc: 5 uncovered cards | coverage 0.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id misc.disable-edge-features --id misc.disable-office-telemetry --id misc.disable-onedrive --id misc.disable-visual-studio-telemetry --id misc.optimize-7zip-settings`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id misc.disable-edge-features --id misc.disable-office-telemetry --id misc.disable-onedrive --id misc.disable-visual-studio-telemetry --id misc.optimize-7zip-settings --run-kvm --json`
- Peripheral: 4 uncovered cards | coverage 50.0%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id peripheral.keyboard-disable-language-hotkey --id peripheral.keyboard-optimize-repeat --id peripheral.mouse-disable-acceleration --id peripheral.mouse-disable-throttle`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id peripheral.keyboard-disable-language-hotkey --id peripheral.keyboard-optimize-repeat --id peripheral.mouse-disable-acceleration --id peripheral.mouse-disable-throttle --run-kvm --json`
- Power: 1 uncovered cards | coverage 90.91%
  command: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.disable-cpu-parking`
  live KVM: `dotnet run --project cli/cli.csproj -- research qa-batch --id power.disable-cpu-parking --run-kvm --json`

## Remaining Uncovered Sample

- `misc.disable-edge-features` | Disable Microsoft Edge Features | Misc
- `misc.disable-office-telemetry` | Disable Microsoft Office Telemetry | Misc
- `misc.disable-onedrive` | Disable OneDrive | Misc
- `misc.disable-visual-studio-telemetry` | Disable Visual Studio Telemetry | Misc
- `misc.optimize-7zip-settings` | Configure 7-Zip Context Menu Settings | Misc
- `peripheral.keyboard-disable-language-hotkey` | Disable Language Switch Hotkey | Peripheral
- `peripheral.keyboard-optimize-repeat` | Set Keyboard Repeat and Cursor Blink Values | Peripheral
- `peripheral.mouse-disable-acceleration` | Disable Enhanced Pointer Precision (Mouse Acceleration) | Peripheral
- `peripheral.mouse-disable-throttle` | Disable Mouse Throttling for Background Windows | Peripheral
- `power.disable-cpu-parking` | Disable CPU Core Parking | Power
