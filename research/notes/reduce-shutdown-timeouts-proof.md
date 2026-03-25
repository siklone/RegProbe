# Historical Reduce Shutdown Timeouts Proof Bundle

This note now stays with the retired mixed bundle `system.reduce-shutdown-timeouts`.

Historical app write bundle:
- `HKCU/Control Panel/Desktop/WaitToKillAppTimeout = 2500`
- `HKCU/Control Panel/Desktop/HungAppTimeout = 1500`
- `HKCU/Control Panel/Desktop/AutoEndTasks = 1`

The documented service-side timeout now lives in `system.wait-to-kill-service-timeout`.

Microsoft sources used to anchor the remaining current-user bundle:
- `HungAppTimeout` and `WaitToKillAppTimeout` are shown as registry values that control application shutdown timing.
- `AutoEndTasks` is shown as the registry value behind the shutdown-termination policy that ends blocking applications.

Source links:
- https://learn.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_shutdown_type
- https://learn.microsoft.com/ja-jp/answers/questions/2188265/bat
- https://learn.microsoft.com/en-us/answers/questions/4334365/how-to-find-the-app-preventing-shutdown
