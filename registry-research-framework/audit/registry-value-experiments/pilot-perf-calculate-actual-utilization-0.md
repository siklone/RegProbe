# Registry Value Experiment - pilot-perf-calculate-actual-utilization-0

- Status: **error**
- Generated UTC: `2026-05-08T20:03:42Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization`
- Test value: `0`

## Result

- Error: `guest-did-not-return-after-apply-reboot`
- Recovery status: `unrecovered-shut-off`
- Recovery note: see `pilot-perf-calculate-actual-utilization-0-recovery.md`
- Safety note: future boot-critical value experiments must use a libvirt snapshot or disposable overlay before reboot testing.

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-present`, value_exists=`True`, value=`1`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `4`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=True;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `False` - Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty. Provide an argument that is not null or empty, and then try the command again.
- `notepad-x86-launch`: `False` - Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty. Provide an argument that is not null or empty, and then try the command again.
- `calc-launch`: `False` - Cannot validate argument on parameter 'ArgumentList'. The argument is null or empty. Provide an argument that is not null or empty, and then try the command again.
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
