# Recovery Note - pilot-perf-calculate-actual-utilization-0

- Status: **unrecovered-shut-off**
- Generated UTC: `2026-05-08T22:01:20Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization`
- Tested value: `0`
- Original value observed by QGA before apply: `1`
- Applied value observed by QGA before reboot: `0`

## What Happened

- Immediate pre-reboot hard smoke passed for shell process presence, `cmd /c ver`, PowerShell version, and Settings URI launch.
- Store URI launch failed under QGA/SYSTEM with `The operation attempted is not supported`, which is treated as a best-effort app URI check.
- Notepad and Calculator smoke checks failed because the first harness version passed an empty `-ArgumentList` to `Start-Process`; the harness has since been fixed to omit `-ArgumentList` when empty.
- After the apply reboot, QGA did not return within the wait window and the VM entered Windows Automatic Repair / Startup Repair.

## Recovery Actions

- Entered WinRE Command Prompt from the recovery UI.
- Confirmed Windows volume as `C:` using a drive scan for `C:\Windows\System32\Config\SYSTEM`.
- Loaded the offline SYSTEM hive with `reg load HKLM\OFFLINE C:\Windows\System32\Config\SYSTEM`.
- Observed `HKLM\OFFLINE\Select` as `Current=1`, `Default=1`, `Failed=0`, `LastKnownGood=1`.
- Confirmed `HKLM\OFFLINE\ControlSet001\Control\Power\PerfCalculateActualUtilization` was `0x0`.
- Restored it to `0x1` using `reg add ... /d 1 /f`.
- Confirmed `HibernateEnabled=0` and disabled fast startup through `ControlSet001\Control\Session Manager\Power\HiberbootEnabled=0`.
- Unloaded the hive with `reg unload HKLM\OFFLINE`.
- Rebooted with `wpeutil reboot`.

## Follow-up Evidence

- Startup Repair reported `Number of root causes = 0` in `C:\Windows\System32\Logfiles\Srt\SrtTrail.txt`.
- Recovery loop bypass was attempted with `bcdedit /set {default} recoveryenabled No` and `bcdedit /set {default} bootstatuspolicy IgnoreAllFailures`.
- That BCD bypass did not recover the guest; the VM remained stuck after Windows Boot Manager and QGA did not return.
- Booted SystemRescue 13.00 from an official ISO, mounted the Windows disk, and verified `PerfCalculateActualUtilization` had already been restored to `1`.
- Ran `ntfsfix -d` from SystemRescue, then WinRE `chkdsk C: /f`; Windows corrected corrupt NTFS metadata and reported no further filesystem action required.
- Restored the BCD recovery flags from SystemRescue: `recoveryenabled=1` and `bootstatuspolicy=0`.
- Ran offline `sfc /scannow /offbootdir=C:\ /offwindir=C:\Windows`; it reported no integrity violations.
- Ran `dism /Image:C:\ /Cleanup-Image /RevertPendingActions`; the first run completed, but the guest still returned to Startup Repair.
- A later DISM retry with `/ScratchDir:C:\Temp` failed with `0x800f082f`.
- Removed temporary safe-mode boot flags after a safe-mode attempt also returned to Startup Repair.
- System Restore had no restore points.
- WinRE `Uninstall latest quality update` failed.
- WinRE `Uninstall latest feature update` failed.
- The VM was left shut off to preserve the failed state without further mutation.

## Verdict

- `PerfCalculateActualUtilization=0` produced a reboot regression on this VM profile during the pilot lane.
- The evidence does not prove the value is the only possible cause because the image also had recoverable NTFS metadata damage and pending-action symptoms after the crash.
- The app-facing decision should treat non-default values for this record as advanced, opt-in, reboot-risky, and VM-profile-sensitive until a disposable-overlay replay proves otherwise.
- Future boot-critical value experiments must require a libvirt snapshot or disposable overlay before apply/reboot testing.
