# Registry Value Experiment - operator96-019-timercheckflags-0

- Status: **ok**
- Generated UTC: `2026-05-09T13:06:13Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags`
- Test value: `0`

## Result

- `apply_smoke_hard_success`: `True`
- `post_reboot_smoke_hard_success`: `True`
- `post_rollback_smoke_hard_success`: `True`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-missing`, value_exists=`False`, value=`None`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`0`
- `smoke.failure_count`: `1`
- `smoke.hard_failure_count`: `0`
- `smoke.best_effort_failure_count`: `1`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=False;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `True` - started=True;alive_after_2s=True
- `notepad-x86-launch`: `True` - started=True;alive_after_2s=True
- `calc-launch`: `True` - started=True;alive_after_2s=False
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
- `interactive_user_smoke`: status=`ok`, failure_count=`0`
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.43`, cpu_multi_seconds=`0.2186`, io_mib_s=`449.7`
- `baseline_smoke.failure_count`: `1`
- `baseline_smoke.hard_failure_count`: `0`
- `baseline_smoke.best_effort_failure_count`: `1`
- `baseline_benchmarks`: status=`ok`, cpu_single_seconds=`0.4366`, cpu_multi_seconds=`0.2188`, io_mib_s=`485.53`

## Stage: post_reboot_rollback

- `status`: `ok`
- `error`: `None`
- `restore_action`: `removed-created-value`
- `after_reboot`: status=`value-present`, value_exists=`True`, value=`0`
- `after_restore`: status=`value-missing`, value_exists=`False`, value=`None`
- `smoke.failure_count`: `1`
- `smoke.hard_failure_count`: `0`
- `smoke.best_effort_failure_count`: `1`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=False;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `True` - started=True;alive_after_2s=True
- `notepad-x86-launch`: `True` - started=True;alive_after_2s=True
- `calc-launch`: `True` - started=True;alive_after_2s=False
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
- `interactive_user_smoke`: status=`ok`, failure_count=`0`
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.4597`, cpu_multi_seconds=`0.2238`, io_mib_s=`219.8`

## Stage: post_rollback

- `status`: `ok`
- `error`: `None`
- `final`: status=`value-missing`, value_exists=`False`, value=`None`
- `smoke.failure_count`: `1`
- `smoke.hard_failure_count`: `0`
- `smoke.best_effort_failure_count`: `1`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=False;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `True` - started=True;alive_after_2s=True
- `notepad-x86-launch`: `True` - started=True;alive_after_2s=True
- `calc-launch`: `True` - started=True;alive_after_2s=False
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
- `interactive_user_smoke`: status=`ok`, failure_count=`0`
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.4416`, cpu_multi_seconds=`0.2173`, io_mib_s=`254.86`
