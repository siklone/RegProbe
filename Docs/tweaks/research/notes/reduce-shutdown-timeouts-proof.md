# Reduce Shutdown Timeouts Proof Bundle

This note collects the machine-checkable proof for `system.reduce-shutdown-timeouts`.

Observed app write bundle:
- `HKLM\SYSTEM\CurrentControlSet\Control\WaitToKillServiceTimeout = 2500`
- `HKCU\Control Panel\Desktop\WaitToKillAppTimeout = 2500`
- `HKCU\Control Panel\Desktop\HungAppTimeout = 1500`
- `HKCU\Control Panel\Desktop\AutoEndTasks = 1`

Microsoft sources used to anchor the bundle:
- `WaitToKillServiceTimeout` is the documented service shutdown timeout under `HKLM\SYSTEM\CurrentControlSet\Control`.
- `HungAppTimeout` and `WaitToKillAppTimeout` are shown as registry values that control application shutdown timing.
- `AutoEndTasks` is shown as the registry value behind the shutdown-termination policy that ends blocking applications.

Source links:
- https://learn.microsoft.com/en-us/windows/win32/services/service-control-handler-function
- https://learn.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_shutdown_type
- https://learn.microsoft.com/ja-jp/answers/questions/2188265/bat
- https://learn.microsoft.com/en-us/answers/questions/4334365/how-to-find-the-app-preventing-shutdown
