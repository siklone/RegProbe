# WinSxS Folder
> Update (2025-12-30): LegacyTweakProvider restored missing tweaks; verify this doc against the current catalog.

> **Doc note (2025-12-27):** Reference material (mostly sourced from `win-config`). The app may not implement every item here yet; treat this as background when turning items into SAFE/reversible tweaks (Detect â†’ Apply â†’ Verify â†’ Rollback, Preview/DryRun by default).

Requires elevation: Yes (DISM/system files).

Get the current size of the WinSxS folder, by pasting the following command into `cmd`:
```cmd
Dism.exe /Online /Cleanup-Image /AnalyzeComponentStore
```
The output could look like:
```
C:\Users\Nohuxi>Dism.exe /Online /Cleanup-Image /AnalyzeComponentStore

Component Store (WinSxS) information:

Windows Explorer Reported Size of Component Store : 5.00 GB

Actual Size of Component Store : 4.94 GB

    Shared with Windows : 2.82 GB
    Backups and Disabled Features : 2.12 GB
    Cache and Temporary Data :  0 bytes

Date of Last Cleanup : 2025-03-30 11:05:43

Number of Reclaimable Packages : 0
Component Store Cleanup Recommended : No
```
`Number of Reclaimable Packages : 0` -> This is the number of superseded packages on the system that component cleanup can remove.

> https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/determine-the-actual-size-of-the-winsxs-folder?view=windows-11&source=recommendations#analyze-the-component-store

Clean your folder with:
```cmd
Dism.exe /online /Cleanup-Image /StartComponentCleanup
```
or
```
Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase
```
, if you want to remove all superseded versions of every component in the component store. (no need, if there aren't any)

> https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/manage-the-component-store?view=windows-11
> https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/clean-up-the-winsxs-folder?view=windows-11

Permanently remove outdated update files from `C:\Windows\WinSxS` to free space. Once applied, previous updates cannot be uninstalled:
```json
"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\SideBySide\\Configuration": {
  "DisableResetbase": { "Type": "REG_DWORD", "Data": 0 }
}
```
The value doesn't exist on more recent versions.

# Windows.old

Requires elevation: Yes (system files).

Removes old/previous windows installation files from `Windows.old`.

```
Ten days after you upgrade to Windows, your previous version of Windows will be automatically deleted from your PC. However, if you need to free up drive space, and you're confident that your files and settings are where you want them to be in Windows, you can safely delete it yourself.

If it's been fewer than 10 days since you upgraded to Windows, your previous version of Windows will be listed as a system file you can delete. You can delete it, but keep in mind that you'll be deleting your Windows.old folder, which contains files that give you the option to go back to your previous version of Windows. If you delete your previous version of Windows, this can't be undone (you won't be able to go back to your previous version of Windows).
```
> https://support.microsoft.com/en-us/windows/delete-your-previous-version-of-windows-f8b26680-e083-c710-b757-7567d69dbb74


# SRUM Data

Requires elevation: Yes (system database).

Deletes the SRUM database file, which tracks app, service, and network usage.

Location:
```bat
%windir%\System32\sru
```
Read the SRUM data:
> https://github.com/MarkBaggett/srum-dump

# DirectX Shader Cache

Requires elevation: No (per-user caches; admin only for system-wide caches).

Clears the DirectX caches and any vendor caches (NVIDIA `DXCache`/`GLCache`/`NV_Cache`, AMD `DXCache`, Intel `DXCache`). Clearing the cache forces shaders to be recompiled the next time an application starts. Expect a short period of shader compilation stutter immediately after cleaning.

Remember to temporarily set `Shader Cache Size` to `Disabled`, use the option, then return it to `Unlimited` so the driver use the files.

![](https://github.com/nohuto/win-config/blob/main/nvidia/images/shadercache.png?raw=true)

# Recycle Bin

Requires elevation: No.

Empties the recycle bin for every mounted drive. Windows stores deleted files per volume in `$Recycle.Bin`, so if you have multiple volumes this can recover more space than the Explorer UI shows.

```powershell
C:\$Recycle.Bin\S-<user-id>
```

# Shadow Copies

Requires elevation: Yes (vssadmin).

Removes all copies (volume backups). See your current shadows with:
```cmd
vssadmin list shadows /for=<ForVolumeSpec> /shadow=<ShadowID>
```

`<ForVolumeSpec>` -> Volume
`<ShadowID>` -> Shadow copy specified by ShadowID

# Background History

Requires elevation: No.

The personalization window keeps the last five wallpaper paths in `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Wallpapers` (`BackgroundHistoryPath0-4`) and cached copies under `%AppData%\Microsoft\Windows\Themes\CachedFiles`.

# Font Cache

Requires elevation: Yes (system cache).

The font cache is a file or set of files to manage and display the installed fonts so they load faster. Sometimes the font cache may become corrupted and cause fonts to not rendering properly, or displaying invalid characters. If not having such issues there's no point in clearing it.

```powershell
"%WinDir%\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache\\*FontCache*", "%WinDir%\\System32\\FNTCACHE.DAT"
```

# Temporary Internet Files

Requires elevation: No.

Legacy WinINet consumers (Explorer, old Control Panel surfaces, webviews inside installers, etc.) still use `%LOCALAPPDATA%\Microsoft\Windows\INetCache`, `%LOCALAPPDATA%\Microsoft\Windows\INetCookies`, `%LOCALAPPDATA%\Microsoft\Windows\WebCache`, `%LOCALAPPDATA%\Microsoft\Windows\History`. Expect the first launch of an affected app to take longer while it rebuilds HTTP caches.

# Delivery Optimization Files

Requires elevation: Yes (system services/dirs).

Delivery Optimization (DoSvc) stores update files under `C:\Windows\SoftwareDistribution\DeliveryOptimization` and uses `C:\ProgramData\Microsoft\Network\Downloader` for the BITS session data. The option stops DoSvc to delete the files, but won't start it as it's not recommended to have it enabled anyway.

# Temporary Files

Requires elevation: Yes (system temp).

Per user temporary files are saved in `%TEMP%`, global files under `%WINDIR%\Temp`. Some installers never delete leftovers, so those can pollute the folder. Anything that is still used will be skipped.

# Clipboard History

Requires elevation: No.

Currently clears the in memory buffer via `echo. | clip`. [`clip`](https://github.com/nohuto/windowsserverdocs/blob/main/WindowsServerDocs/administration/windows-commands/clip.md) saves thatever it gets into the clipboard, and [`echo.`](https://github.com/nohuto/windowsserverdocs/blob/main/WindowsServerDocs/administration/windows-commands/echo.md#examples) = blank line.

See your current clipboard content via:
```powershell
Get-Clipboard
```

# DNS Cache

Requires elevation: Yes (flushdns).

`Get-DnsClientCache` shows the resolver cache that stores recent lookups. Flushing it via `ipconfig /flushdns` can fix stale entries after moving domains or switching VPN profiles (or if editing the hosts file).

# WER Files

Requires elevation: Yes (system WER).

Windows Error Reporting (WER) queues crash dumps and report metadata under `%PROGRAMDATA%\Microsoft\Windows\WER` (system) and `%LOCALAPPDATA%\Microsoft\Windows\WER` (per user). Clearing the queue removes pending uploads and archived `.wer` files.

# Event Logs

Requires elevation: Yes.

Only do this if you want to export the data elsewhere or purposely delete logs (security logs can't be recovered afterward).

Display all logs via:
```powershell
wevtutil el
```

> https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/wevtutil

# Windows Update Cache

Requires elevation: Yes.

Troubleshooting update loops often requires resetting `%WINDIR%\SoftwareDistribution` and `%WINDIR%\System32\catroot2`. This forces Windows Update to redownload the catalog metadata.

# Thumbnail Cache

Requires elevation: No.

Placeholder.

# Prefetch Files

Requires elevation: Yes.

Placeholder.

# BSoD Memory Dump Files

Requires elevation: Yes.

Placeholder.

# Product Key

Requires elevation: Yes.

"Some servicing operations require the product key to be available in the registry during Out of Box Experience (OOBE) operations. The /cpky option removes the product key from the registry to prevent this key from being stolen by malicious code. For retail installations that deploy keys, the best practice is to run this option. This option isn't required for MAK and KMS host keys, because this is the default behavior for those keys. This option is required only for other types of keys whose default behavior isn't to clear the key from the registry."

> https://learn.microsoft.com/en-us/windows-server/get-started/activation-slmgr-vbs-options#advanced-options

# Downloaded Program Files

Requires elevation: Yes.

Placeholder.

# System Logs

Requires elevation: Yes.

Placeholder.

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="cleanup.background-history"></a> `cleanup.background-history` | Clear Wallpaper History | Clears the personalization wallpaper history registry entries and cached background files. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearBackgroundHistoryTweak.cs#L14` |
| <a id="cleanup.component-store"></a> `cleanup.component-store` | Cleanup Component Store | Cleans up the Windows component store (WinSxS folder) to free up disk space. This is a safe operation that removes superseded components... | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\CleanupComponentStoreTweak.cs#L14` |
| <a id="cleanup.delivery-optimization"></a> `cleanup.delivery-optimization` | Clear Delivery Optimization Files | Deletes Delivery Optimization cache used for Windows Update P2P sharing. The DoSvc service will be stopped. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearDeliveryOptimizationFilesTweak.cs#L14` |
| <a id="cleanup.directx-shader-cache"></a> `cleanup.directx-shader-cache` | Clear DirectX Shader Cache | Clears DirectX and vendor shader caches (NVIDIA, AMD, Intel). Shaders will be recompiled on next app launch. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearDirectXShaderCacheTweak.cs#L11` |
| <a id="cleanup.disable-reserved-storage"></a> `cleanup.disable-reserved-storage` | Disable Reserved Storage | Disables Windows Reserved Storage, which reserves about 7GB of disk space for Windows updates and temporary files. Only recommended if yo... | Advanced | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\DisableReservedStorageTweak.cs#L14` |
| <a id="cleanup.eventlog-{logName.ToLowerInvariant()}"></a> `cleanup.eventlog-{logName.ToLowerInvariant()}` | Clear {logName} Event Log | Clears the Windows {logName} event log. WARNING: Logs cannot be recovered after clearing. | Advanced | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearEventLogsTweak.cs#L15` |
| <a id="cleanup.font-cache"></a> `cleanup.font-cache` | Clear Font Cache | Clears the Windows font cache. Use this if fonts are not rendering properly. The FontCache service will be stopped and restarted. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearFontCacheTweak.cs#L14` |
| <a id="cleanup.internet-temp-files"></a> `cleanup.internet-temp-files` | Clear Temporary Internet Files | Clears legacy WinINet cache (INetCache, INetCookies, WebCache, History). Used by Explorer, old Control Panel, and some installers. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearTemporaryInternetFilesTweak.cs#L11` |
| <a id="cleanup.memory-dumps"></a> `cleanup.memory-dumps` | Clear Memory Dump Files | Deletes BSoD memory dump files (MEMORY.DMP). These can be several GB in size. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearMemoryDumpFilesTweak.cs#L11` |
| <a id="cleanup.prefetch-files"></a> `cleanup.prefetch-files` | Clear Prefetch Files | Clears Windows prefetch files used for application launch optimization. Files will be regenerated over time. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearPrefetchFilesTweak.cs#L11` |
| <a id="cleanup.product-key"></a> `cleanup.product-key` | Remove Product Key from Registry | Removes the Windows product key from the registry to prevent theft by malicious code. The key can be reactivated if needed. | Advanced | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\RemoveProductKeyTweak.cs#L14` |
| <a id="cleanup.recycle-bin"></a> `cleanup.recycle-bin` | Empty Recycle Bin | Empties the Recycle Bin for all drives. Files cannot be recovered after deletion. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearRecycleBinTweak.cs#L14` |
| <a id="cleanup.shadow-copies"></a> `cleanup.shadow-copies` | Clear Shadow Copies | Removes all shadow copies (volume backups) to free up disk space. WARNING: This permanently removes System Restore points and volume snap... | Risky | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearShadowCopiesTweak.cs#L14` |
| <a id="cleanup.srum-data"></a> `cleanup.srum-data` | Clear SRUM Database | Deletes the System Resource Usage Monitor (SRUM) database which tracks app, service, and network usage. | Advanced | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearSRUMDataTweak.cs#L11` |
| <a id="cleanup.temp-files"></a> `cleanup.temp-files` | Clear Temporary Files | Deletes temporary files from user and system temp folders. Files in use will be skipped. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearTemporaryFilesTweak.cs#L11` |
| <a id="cleanup.thumbnail-cache"></a> `cleanup.thumbnail-cache` | Clear Thumbnail Cache | Clears Explorer thumbnail cache files. Thumbnails will be regenerated when needed. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearThumbnailCacheTweak.cs#L11` |
| <a id="cleanup.wer-files"></a> `cleanup.wer-files` | Clear Windows Error Reporting Files | Deletes Windows Error Reporting (WER) crash dumps and report metadata from system and user folders. | Safe | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearWERFilesTweak.cs#L11` |
| <a id="cleanup.windows-old"></a> `cleanup.windows-old` | Delete Windows.old Folder | Removes previous Windows installation files from Windows.old. WARNING: You will not be able to roll back to the previous Windows version... | Risky | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearWindowsOldTweak.cs#L11` |
| <a id="cleanup.windows-update-cache"></a> `cleanup.windows-update-cache` | Clear Windows Update Cache | Resets Windows Update cache (SoftwareDistribution and catroot2). Use this to fix update loops. Update catalog metadata will be redownloaded. | Advanced | `OpenTraceProject.Engine\Tweaks\Commands\Cleanup\ClearWindowsUpdateCacheTweak.cs#L11` |
<!-- TWEAK INDEX END -->
