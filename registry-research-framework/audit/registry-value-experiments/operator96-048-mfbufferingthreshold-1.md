# Registry Value Experiment - operator96-048-mfbufferingthreshold-1

- Status: **ok**
- Generated UTC: `2026-05-09T21:49:12Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold`
- Test value: `1`

## Result

- `apply_smoke_hard_success`: `True`
- `post_reboot_smoke_hard_success`: `True`
- `post_rollback_smoke_hard_success`: `True`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-present`, value_exists=`True`, value=`0`
- `after_apply`: status=`value-present`, value_exists=`True`, value=`1`
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
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.4507`, cpu_multi_seconds=`0.2519`, io_mib_s=`332.17`
- `baseline_smoke.failure_count`: `1`
- `baseline_smoke.hard_failure_count`: `0`
- `baseline_smoke.best_effort_failure_count`: `1`
- `baseline_benchmarks`: status=`ok`, cpu_single_seconds=`0.4425`, cpu_multi_seconds=`0.2429`, io_mib_s=`314.7`

## Stage: post_reboot_rollback

- `status`: `ok`
- `error`: `None`
- `restore_action`: `restored-original-value`
- `after_reboot`: status=`value-present`, value_exists=`True`, value=`1`
- `after_restore`: status=`value-present`, value_exists=`True`, value=`0`
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
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.4504`, cpu_multi_seconds=`0.2613`, io_mib_s=`339.19`

## Stage: post_rollback

- `status`: `ok`
- `error`: `None`
- `final`: status=`value-present`, value_exists=`True`, value=`0`
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
- `benchmarks`: status=`ok`, cpu_single_seconds=`0.4441`, cpu_multi_seconds=`0.2366`, io_mib_s=`400.58`
