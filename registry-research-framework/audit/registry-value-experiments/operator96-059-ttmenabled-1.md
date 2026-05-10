# Registry Value Experiment - operator96-059-ttmenabled-1

- Status: **ok**
- Generated UTC: `2026-05-10T00:37:12Z`
- Target: `HKLM\SYSTEM\CurrentControlSet\Control\Power\TtmEnabled`
- Test value: `1`
- Outcome: `boot-failure-recovered`

## Result

- Error: `guest-did-not-return-after-apply-reboot`
- Controlled failure: `true`
- Snapshot recovery: `ok`
- `apply_smoke_hard_success`: `True`
- `post_reboot_smoke_hard_success`: `False`
- `post_rollback_smoke_hard_success`: `False`

## Stage: apply

- `status`: `ok`
- `error`: `None`
- `original`: status=`value-missing`, value_exists=`False`, value=`None`
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
- `benchmarks`: status=`ok`, cpu_single_seconds=`None`, cpu_multi_seconds=`None`, io_mib_s=`1616.9703`
- `baseline_smoke.failure_count`: `1`
- `baseline_smoke.hard_failure_count`: `0`
- `baseline_smoke.best_effort_failure_count`: `1`
- `baseline_benchmarks`: status=`ok`, cpu_single_seconds=`None`, cpu_multi_seconds=`None`, io_mib_s=`1084.9711`
