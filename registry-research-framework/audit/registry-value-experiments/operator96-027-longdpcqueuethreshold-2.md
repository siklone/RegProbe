# Registry Value Experiment - operator96-027-longdpcqueuethreshold-2

- Status: **ok**
- Generated UTC: `2026-05-09T17:09:27Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold`
- Test value: `2`

## Result

- `apply_smoke_hard_success`: `True`
- `post_reboot_smoke_hard_success`: `True`
- `post_rollback_smoke_hard_success`: `True`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-missing`, value_exists=`False`, value=`None`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`2`
- `smoke.failure_count`: `1`
- `smoke.hard_failure_count`: `0`
- `smoke.best_effort_failure_count`: `1`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=True;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `True` - started=True;alive_after_2s=True
- `notepad-x86-launch`: `True` - started=True;alive_after_2s=True
- `calc-launch`: `True` - started=True;alive_after_2s=False
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
- `interactive_user_smoke`: status=`ok`, failure_count=`0`
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.5053`, cpu_multi_seconds=`0.3373`, io_mib_s=`188.76`
- `baseline_smoke.failure_count`: `1`
- `baseline_smoke.hard_failure_count`: `0`
- `baseline_smoke.best_effort_failure_count`: `1`
- `baseline_benchmarks`: status=`ok`, cpu_single_seconds=`0.5051`, cpu_multi_seconds=`0.2523`, io_mib_s=`64.18`

## Stage: post_reboot_rollback

- `status`: `ok`
- `error`: `None`
- `restore_action`: `removed-created-value`
- `after_reboot`: status=`value-present`, value_exists=`True`, value=`2`
- `after_restore`: status=`value-missing`, value_exists=`False`, value=`None`
- `smoke.failure_count`: `1`
- `smoke.hard_failure_count`: `0`
- `smoke.best_effort_failure_count`: `1`
- `shell-process-presence`: `True` - explorer=True;SearchHost=True;ShellExperienceHost=True;sihost=True;StartMenuExperienceHost=True
- `cmd-ver`: `True` - exit=0
- `powershell-version`: `True` - exit=0
- `notepad-x64-launch`: `True` - started=True;alive_after_2s=True
- `notepad-x86-launch`: `True` - started=True;alive_after_2s=True
- `calc-launch`: `True` - started=True;alive_after_2s=False
- `settings-uri-launch`: `True` - launch-command-succeeded
- `store-uri-launch`: `False` - This command cannot be run due to the error: The operation attempted is not supported.
- `interactive_user_smoke`: status=`ok`, failure_count=`0`
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.7083`, cpu_multi_seconds=`0.3609`, io_mib_s=`77.08`

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
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.5194`, cpu_multi_seconds=`0.2837`, io_mib_s=`222.07`
