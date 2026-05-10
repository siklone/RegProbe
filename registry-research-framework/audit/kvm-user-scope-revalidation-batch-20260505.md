# KVM User-Scope Revalidation Batch

Date: 2026-05-06T00:27:35.2558263Z
Domain: `regprobe-win11-25h2-session`

This batch re-read selected current-user and command-backed targets on the live KVM guest without changing guest configuration.

## Machine

- Computer: `DESKTOP-AHPV0FV`
- CurrentBuildNumber: `26200`
- UBR: `8246`

## User Context

- UserName: `DESKTOP-AHPV0FV\rai`
- SID: `S-1-5-21-88678196-3999695453-342703815-1001`

## Observations

### `peripheral.autoplay-take-no-action`

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\StorageOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\StorageOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\StorageOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\StorageOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\CameraAlternate\ShowPicturesOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\CameraAlternate\ShowPicturesOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\CameraAlternate\ShowPicturesOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\CameraAlternate\ShowPicturesOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedDVDOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedDVDOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedDVDOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedDVDOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleDVDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleDVDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleDVDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleDVDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDAudioOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDAudioOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDAudioOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDAudioOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayBluRayOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayBluRayOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayBluRayOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayBluRayOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleBDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleBDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleBDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleBDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayCDAudioOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayCDAudioOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayCDAudioOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayCDAudioOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedCDOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedCDOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedCDOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedCDOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleCDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleCDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleCDBurningOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleCDBurningOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayVideoCDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayVideoCDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayVideoCDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayVideoCDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlaySuperVideoCDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlaySuperVideoCDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlaySuperVideoCDMovieOnArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlaySuperVideoCDMovieOnArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\AutorunINFLegacyArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\AutorunINFLegacyArrival`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\AutorunINFLegacyArrival` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\AutorunINFLegacyArrival`
  path_exists=`True` value_exists=`False` value=`None`

### `power.optimize-cpu-boost`

- `powercfg.exe /qh SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE` :: `PERFBOOSTMODE`
  path_exists=`True` value_exists=`True` value=`{'ac': 2, 'dc': 2}`

### `privacy.disable-cli-telemetry`

- `HKCU\Environment` :: `POWERSHELL_TELEMETRY_OPTOUT`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Environment`
  path_exists=`True` value_exists=`False` value=`None`
- `HKCU\Environment` :: `DOTNET_CLI_TELEMETRY_OPTOUT`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Environment`
  path_exists=`True` value_exists=`False` value=`None`

### `system.disable-jpeg-reduction`

- `HKCU\Control Panel\Desktop` :: `JPEGImportQuality`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Control Panel\Desktop`
  path_exists=`True` value_exists=`False` value=`None`

### `system.disable-startup-delay`

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize` :: `StartupDelayInMSec`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize`
  path_exists=`True` value_exists=`False` value=`None`

### `system.enable-game-mode`

- `HKCU\Software\Microsoft\GameBar` :: `AutoGameModeEnabled`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\GameBar`
  path_exists=`True` value_exists=`False` value=`None`

### `visibility.hide-language-bar`

- `HKCU\Software\Microsoft\CTF\LangBar` :: `ShowStatus`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Microsoft\CTF\LangBar`
  path_exists=`True` value_exists=`False` value=`None`

### `visibility.restore-classic-context-menu`

- `HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32` :: `(Default)`
  resolved_path=`Registry::HKEY_USERS\S-1-5-21-88678196-3999695453-342703815-1001\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32`
  path_exists=`False` value_exists=`False` value=`None`
